-- Musician career (นักดนตรี): play a rhythm game at a venue to earn tips and skill.
--   music_get     {}                         -> stats, instruments, venues (open/locked)
--   music_equip   {"instrument": id}         -> switch to an owned instrument
--   music_start   {"venue": id}              -> {session, song, notes:[{t, lane}], duration_ms}
--   music_finish  {"session": n, "perfect": a, "good": b} -> tips, xp, level
-- The server writes the note chart, checks the venue (level, fair, night), charges energy,
-- refuses to pay before the song could have ended, and caps hits at the note count.

local nk = require("nakama")
local life = require("life_core")
local data = require("shop_data")

local STATS = { collection = "music", key = "stats" }
local CURRENCY = "baht"
local MAX_LEVEL = 20
local ENERGY_COST = 10
local SONG_MS = 30000
local LEAD_IN_MS = 2000

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

-- Venues: base tips, required level, which instruments get +20%, when it's open
local VENUES = {
  village_hall = { name = "ศาลากลางหมู่บ้าน", level = 1,  base = 40,  bonus = "folk" },
  temple_fair  = { name = "งานวัดคอโค",    level = 1,  base = 120, bonus = "folk", fair = true },
  tambon_event = { name = "เวทีอีเวนต์ตำบล",   level = 5,  base = 150, bonus = "any" },
  city_event   = { name = "เวทีอีเวนต์เมือง",   level = 8,  base = 220, bonus = "modern" },
  -- Pubs (ผับ) in the city, named by style: open 19:00-03:00, each one once per game day
  pub_puea_chiwit = { name = "ผับเพื่อชีวิต", level = 10, base = 350, bonus = "any",    night = true, pub = true },
  pub_blues       = { name = "ผับบลูส์",     level = 10, base = 350, bonus = "modern", night = true, pub = true },
  pub_indie       = { name = "ผับอินดี้",     level = 10, base = 350, bonus = "modern", night = true, pub = true },
}
local VENUE_ORDER = { "village_hall", "temple_fair", "tambon_event", "city_event", "pub_puea_chiwit", "pub_blues", "pub_indie" }

local SONGS = { "ลำเพลิน", "เต้ยโขง", "ลาวแพน", "มนต์รักลูกทุ่ง", "สาวบ้านแต้", "ลำเต้ย", "รำวงชาวบ้าน", "เพลงงานวัด" }

local function xp_to_next(level) return 30 + 15 * (level - 1) end

local function read_stats(user_id)
  local objects = nk.storage_read({ { collection = STATS.collection, key = STATS.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.level = s.level or 1
  s.xp = s.xp or 0
  s.performances = s.performances or 0
  s.next_session = s.next_session or 1
  return s
end

local function write_stats(user_id, s)
  nk.storage_write({ {
    collection = STATS.collection, key = STATS.key, user_id = user_id, value = s,
    permission_read = 1, permission_write = 0,
  } })
end

local function fair_always_open(env) return env ~= nil and env.fair_always_open == "true" end

-- Pub hours: 19:00-03:00 game time
local function is_night(now)
  local minute = math.floor((now - life.EPOCH_MS) / 1000) % 1440
  return minute >= 19 * 60 or minute < 3 * 60
end

local function venue_open(v, now, env)
  if v.fair then return (life.fair(now, fair_always_open(env))) end
  if v.night then return is_night(now) or (env ~= nil and env.pubs_always_open == "true") end
  return true
end

local function bonus_for(v, instrument)
  if v.bonus == "any" then return 1.2 end
  if v.bonus == "folk" and instrument.folk then return 1.2 end
  if v.bonus == "modern" and not instrument.folk then return 1.2 end
  return 1.0
end

local function view(user_id, s, env, extra)
  local now = nk.time()
  local items = data.read_items(user_id)
  local instruments = {}
  for _, id in ipairs(data.INSTRUMENT_ORDER) do
    local inst = data.INSTRUMENTS[id]
    table.insert(instruments, { id = id, name = inst.name, owned = items.instruments[id] == true,
                                equipped = items.instrument == id, folk = inst.folk, mult = inst.mult })
  end
  local venues = {}
  for _, id in ipairs(VENUE_ORDER) do
    local v = VENUES[id]
    table.insert(venues, { id = id, name = v.name, level = v.level, base = v.base, bonus = v.bonus,
                           unlocked = s.level >= v.level, open = venue_open(v, now, env),
                           played_today = v.pub == true and (s.pub_days or {})[id] == life.game_day(now),
                           when = v.fair and "งานวัด (วันที่ 1-2 ของเดือน)" or v.night and "กลางคืน 19:00-03:00 (ร้านละครั้งต่อวัน)" or "ทุกเวลา" })
  end
  local result = {
    level = s.level, xp = s.xp, xp_to_next = (s.level >= MAX_LEVEL) and 0 or xp_to_next(s.level),
    performances = s.performances, instruments = instruments, venues = venues,
    instrument = items.instrument, instrument_name = data.INSTRUMENTS[items.instrument].name,
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  return req
end

local function rpc_get(context, payload)
  return view(context.user_id, read_stats(context.user_id), context.env)
end

local function rpc_equip(context, payload)
  local req = decode(payload)
  local items = data.read_items(context.user_id)
  if not data.INSTRUMENTS[req.instrument or ""] then error({ "unknown instrument", GRPC_INVALID_ARGUMENT }) end
  if not items.instruments[req.instrument] then error({ "not owned", GRPC_FAILED_PRECONDITION }) end
  items.instrument = req.instrument
  data.write_items(context.user_id, items)
  return view(context.user_id, read_stats(context.user_id), context.env)
end

-- Note chart: beats and half-beats at a random tempo, in 3 lanes
local function make_chart()
  local bpm = math.random(96, 128)
  local beat = 60000 / bpm
  local notes = {}
  local t = LEAD_IN_MS
  local last_lane = 2
  while t < LEAD_IN_MS + SONG_MS - 1000 do
    if math.random() < 0.75 then
      local lane = math.random(1, 3)
      if lane == last_lane and math.random() < 0.5 then lane = (lane % 3) + 1 end
      last_lane = lane
      table.insert(notes, { t = math.floor(t), lane = lane })
    end
    t = t + ((math.random() < 0.3) and beat / 2 or beat)
  end
  return notes, bpm
end

local function rpc_start(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local v = VENUES[req.venue or ""]
  if not v then error({ "unknown venue", GRPC_INVALID_ARGUMENT }) end

  local s = read_stats(user_id)
  local now = nk.time()
  if s.level < v.level then error({ "venue needs level " .. v.level, GRPC_FAILED_PRECONDITION }) end
  if not venue_open(v, now, context.env) then error({ "venue closed", GRPC_FAILED_PRECONDITION }) end
  s.pub_days = s.pub_days or {}
  if v.pub and s.pub_days[req.venue] == life.game_day(now) then
    error({ "already played this pub today", GRPC_FAILED_PRECONDITION })
  end
  life.spend(user_id, ENERGY_COST)
  if v.pub then s.pub_days[req.venue] = life.game_day(now) end

  local notes, bpm = make_chart()
  local session = s.next_session
  s.next_session = session + 1
  s.session = { id = session, venue = req.venue, start_ms = now, notes = #notes }
  write_stats(user_id, s)

  return nk.json_encode({
    session = session, venue = req.venue, venue_name = v.name, song = SONGS[math.random(#SONGS)],
    bpm = bpm, notes = notes, duration_ms = LEAD_IN_MS + SONG_MS, energy = math.floor(life.state(user_id).energy),
  })
end

local function rpc_finish(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local s = read_stats(user_id)
  local session = s.session
  if not session or session.id ~= tonumber(req.session) then error({ "no such performance", GRPC_FAILED_PRECONDITION }) end
  local now = nk.time()
  if now - session.start_ms < LEAD_IN_MS + SONG_MS - 3000 then
    error({ "song not finished", GRPC_FAILED_PRECONDITION })
  end
  s.session = nil

  -- Hits can't exceed the chart; perfect counts fully, good counts 60%
  local perfect = math.max(0, math.min(math.floor(tonumber(req.perfect) or 0), session.notes))
  local good = math.max(0, math.min(math.floor(tonumber(req.good) or 0), session.notes - perfect))
  local accuracy = (session.notes > 0) and ((perfect + good * 0.6) / session.notes) or 0

  local v = VENUES[session.venue]
  local items = data.read_items(user_id)
  local inst = data.INSTRUMENTS[items.instrument] or data.INSTRUMENTS.pin
  local level_bonus = 1 + 0.03 * (s.level - 1)
  local tips = math.floor(v.base * accuracy * inst.mult * bonus_for(v, inst) * level_bonus + 0.5)
  if tips > 0 then nk.wallet_update(user_id, { [CURRENCY] = tips }, { reason = "music_tips", venue = session.venue }, true) end

  local gained = perfect + good
  s.xp = s.xp + gained
  s.performances = s.performances + 1
  local leveled = false
  while s.level < MAX_LEVEL and s.xp >= xp_to_next(s.level) do
    s.xp = s.xp - xp_to_next(s.level)
    s.level = s.level + 1
    leveled = true
  end
  if s.level >= MAX_LEVEL then s.xp = 0 end
  write_stats(user_id, s)

  return view(user_id, s, context.env, {
    tips = tips, accuracy = math.floor(accuracy * 100 + 0.5), gained_xp = gained, leveled_up = leveled,
    perfect = perfect, good = good, total_notes = session.notes, venue_name = v.name,
  })
end

nk.register_rpc(rpc_get, "music_get")
nk.register_rpc(rpc_equip, "music_equip")
nk.register_rpc(rpc_start, "music_start")
nk.register_rpc(rpc_finish, "music_finish")
