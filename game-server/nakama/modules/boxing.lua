-- Boxing beyond the bags (มวย): pad work with the camp trainer, sparring, and real fights at
-- the temple fair (งานวัด), the tambon event (งานตำบล) and the championship (ชิงแชมป์).
-- The fight itself plays on the client; the server picks the opponent, charges energy, checks
-- the reported result is possible (time, hits, damage) and pays purse + XP. Like music.lua.
--
--   boxing_get    {}                               -> record + every event's status
--   boxing_start  {"event": "fair"}                -> session with the opponent
--   boxing_finish {"session": id, "result": "ko_win"|"ko_loss"|"points"|"pads",
--                  "landed": n, "taken": n, "damage": n, "good": n} -> outcome, purse, XP

local nk = require("nakama")
local life = require("life_core")
local data = require("shop_data")
local quest = require("quest_core")

local RECORD = { collection = "boxing", key = "record" }
local CAREER = { collection = "career", key = "stats" }
local CURRENCY = data.CURRENCY
local MAX_LEVEL = 100
local FIGHT_LEVEL_CAP = 20 -- opponents and fight strength top out here; levels above are prestige
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

-- when: "any" | "fair" (temple fair days) | "tambon" (every 5th game day) | "champ" (day 15 of the month)
local EVENTS = {
  pads   = { name = "ซ้อมเป้าล่อกับครูฝึก", level = 6,  member = true, energy = 8,  when = "any", pads = true,
             seconds = 45, xp_per_good = 3 },
  spar   = { name = "ซ้อมชกกับคู่ซ้อม",     level = 8,  member = true, energy = 12, when = "any",
             rounds = 2, round_s = 30, opp = { 0, 0 }, power = 0.6, purse = { 0, 0 },       xp = { 60, 30 } },
  fair   = { name = "ชกมวยงานวัด",         level = 10, energy = 15, when = "fair",
             rounds = 3, round_s = 30, opp = { 0, 2 }, power = 0.9, purse = { 800, 200 },   xp = { 120, 50 } },
  tambon = { name = "ชกมวยงานตำบล",        level = 13, energy = 15, when = "tambon",
             rounds = 3, round_s = 30, opp = { 1, 3 }, power = 1.0, purse = { 1500, 400 },  xp = { 200, 80 } },
  champ  = { name = "ชิงแชมป์",     level = 16, energy = 20, when = "champ",
             rounds = 3, round_s = 40, opp = { 2, 4 }, power = 1.15, purse = { 5000, 1000 }, xp = { 400, 150 },
             title = "แชมป์มวย" },
}
local ORDER = { "pads", "spar", "fair", "tambon", "champ" }

local FIRST = { "ยอด", "เพชร", "สิงห์", "เสือ", "ดาว", "ฟ้า", "พยัคฆ์", "สายฟ้า", "ทอง", "ศักดิ์" }
local LAST  = { "เล็ก ศ.สมบัติ", "น้อย ลูกบ้านนา", "ดำ ส.ตำบล", "เดช ศิษย์ครูแดง", "ใหญ่ ท.เมืองใหม่", "แก้ว ช.ไพบูลย์" }

local function always_open(env) return env ~= nil and env.boxing_always_open == "true" end

local function day0(now) return math.floor((now - life.EPOCH_MS) / life.DAY_MS) end

-- Is the event on right now, and which "edition" is it (so each one is fought once)?
local function edition(e, now, env)
  local d = day0(now)
  if e.when == "any" then return true, nil end
  if e.when == "fair" then
    -- The temple fair is monthly (days 1-2); only FAIR_ALWAYS_OPEN (not the boxing switch) overrides it
    local open, fair_id = life.fair(now, env ~= nil and env.fair_always_open == "true")
    return open, "fair" .. fair_id
  end
  if e.when == "tambon" then return always_open(env) or d % 5 == 4, "tambon" .. d end
  if e.when == "champ" then
    return always_open(env) or d % life.MONTH_DAYS == 14, "champ" .. math.floor(d / life.MONTH_DAYS)
  end
  return false, nil
end

local function when_text(e)
  if e.when == "fair" then return "เฉพาะงานวัดตามประเพณี (สงกรานต์ ออกพรรษา กฐิน ฯลฯ)" end
  if e.when == "tambon" then return "ทุก 5 วันในเกม (วันที่ 5, 10, 15, ...)" end
  if e.when == "champ" then return "เดือนละครั้ง วันที่ 15" end
  return "ซ้อมได้ทุกเวลา"
end

local function read(collection_spec, user_id)
  local objects = nk.storage_read({ { collection = collection_spec.collection, key = collection_spec.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function write(collection_spec, user_id, value)
  nk.storage_write({ {
    collection = collection_spec.collection, key = collection_spec.key, user_id = user_id, value = value,
    permission_read = 1, permission_write = 0,
  } })
end

local function read_record(user_id)
  local r = read(RECORD, user_id)
  r.wins = r.wins or 0
  r.losses = r.losses or 0
  r.kos = r.kos or 0
  r.titles = r.titles or {}
  r.fought = r.fought or {}   -- edition id -> true
  return r
end

local function career(user_id)
  local c = read(CAREER, user_id)
  c.level = c.level or 1
  c.xp = c.xp or 0
  c.total_hits = c.total_hits or 0
  c.tokens = c.tokens or 15
  c.last_train_ms = c.last_train_ms or 0
  return c
end

local function xp_to_next(level) return 40 + 20 * (level - 1) end

local function add_xp(c, xp)
  c.xp = c.xp + xp
  local leveled = false
  while c.level < MAX_LEVEL and c.xp >= xp_to_next(c.level) do
    c.xp = c.xp - xp_to_next(c.level)
    c.level = c.level + 1
    leveled = true
  end
  if c.level >= MAX_LEVEL then c.xp = 0 end
  return leveled
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  return req
end

local function view(user_id, env, extra)
  local r = read_record(user_id)
  local c = career(user_id)
  local member = data.read_items(user_id).camp_member
  local now = nk.time()
  local events = {}
  for _, id in ipairs(ORDER) do
    local e = EVENTS[id]
    local open, ed = edition(e, now, env)
    table.insert(events, {
      id = id, name = e.name, level = e.level, energy = e.energy, member = e.member == true, when = when_text(e),
      open = open, fought = ed ~= nil and r.fought[ed] == true,
      unlocked = always_open(env) or (c.level >= e.level and (not e.member or member)),
      purse_win = e.purse and e.purse[1] or 0, purse_lose = e.purse and e.purse[2] or 0,
      xp_win = e.xp and e.xp[1] or 0, pads = e.pads == true,
    })
  end
  local titles = {}
  for t, _ in pairs(r.titles) do table.insert(titles, t) end
  local result = { level = c.level, wins = r.wins, losses = r.losses, kos = r.kos, events = events,
                   titles = (#titles > 0) and table.concat(titles, ", ") or "" }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

nk.register_rpc(function(context, payload)
  return view(context.user_id, context.env)
end, "boxing_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local e = EVENTS[req.event or ""]
  if not e then error({ "unknown event", GRPC_INVALID_ARGUMENT }) end

  local c = career(user_id)
  local testing = always_open(context.env) -- BOXING_ALWAYS_OPEN: also skips level/membership for tests
  if not testing and c.level < e.level then error({ "level too low", GRPC_FAILED_PRECONDITION }) end
  if not testing and e.member and not data.read_items(user_id).camp_member then error({ "camp membership required", GRPC_FAILED_PRECONDITION }) end
  if life.injured(user_id) then error({ "injured", GRPC_FAILED_PRECONDITION }) end
  local r = read_record(user_id)
  local open, ed = edition(e, nk.time(), context.env)
  if not open then error({ "event closed", GRPC_FAILED_PRECONDITION }) end
  if ed and r.fought[ed] then error({ "already fought", GRPC_FAILED_PRECONDITION }) end

  life.spend(user_id, e.energy)

  local session = { id = math.random(1, 2000000000), event = req.event, start_ms = nk.time(), edition = ed }
  local extra = { session = session.id, event = req.event, event_name = e.name }
  if e.pads then
    extra.seconds = e.seconds
  else
    local lv = math.min(FIGHT_LEVEL_CAP, math.min(c.level, FIGHT_LEVEL_CAP) + math.random(e.opp[1], e.opp[2]))
    -- Sometimes a much stronger boxer (ตัวเก่ง) is on the card
    local strong = e.purse[1] > 0 and math.random() < 0.18
    if strong then lv = lv + math.random(4, 6) end
    session.opp_hp = math.floor((90 + lv * 8) * (strong and 1.3 or 1))
    extra.rounds = e.rounds
    extra.round_s = e.round_s
    extra.opponent = {
      name = FIRST[math.random(#FIRST)] .. LAST[math.random(#LAST)], level = lv, hp = session.opp_hp,
      power = e.power * (1 + 0.03 * (lv - 1)) * (strong and 1.35 or 1), speed = math.min(1, 0.45 + lv * 0.03 + (strong and 0.15 or 0)),
      skin = math.random(0, 7), strong = strong,
    }
    if strong then extra.opponent.name = "ยอดมวย" .. extra.opponent.name end
  end
  r.session = session
  write(RECORD, user_id, r)
  return view(user_id, context.env, extra)
end, "boxing_start")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local r = read_record(user_id)
  local s = r.session
  if not s or s.id ~= tonumber(req.session) then error({ "no such fight", GRPC_FAILED_PRECONDITION }) end
  local e = EVENTS[s.event]
  local elapsed = (nk.time() - s.start_ms) / 1000
  r.session = nil

  local c = career(user_id)
  local outcome = { event_name = e.name }

  if e.pads then
    if elapsed < e.seconds - 3 then error({ "too early", GRPC_FAILED_PRECONDITION }) end
    local good = math.max(0, math.min(math.floor(tonumber(req.good) or 0), math.floor(e.seconds / 1.1)))
    local xp = good * e.xp_per_good
    outcome.leveled_up = add_xp(c, xp)
    outcome.xp = xp
    outcome.good = good
    outcome.win = true
  else
    local landed = math.max(0, math.floor(tonumber(req.landed) or 0))
    local taken = math.max(0, math.floor(tonumber(req.taken) or 0))
    local damage = math.max(0, math.floor(tonumber(req.damage) or 0))
    -- What a human could do in the time: ~3 attacks a second, at most 36 damage each
    landed = math.min(landed, math.floor(elapsed * 3))
    damage = math.min(damage, landed * 36)
    local full = e.rounds * e.round_s
    local win, ko = false, false
    if req.result == "ko_win" then
      if elapsed < 10 or damage < s.opp_hp then error({ "impossible result", GRPC_FAILED_PRECONDITION }) end
      win, ko = true, true
    elseif req.result == "ko_loss" then
      win = false
    else
      if elapsed < full - 5 then error({ "fight not finished", GRPC_FAILED_PRECONDITION }) end
      win = landed > taken
    end

    local purse = win and e.purse[1] or e.purse[2]
    local xp = win and e.xp[1] or e.xp[2]
    if win then r.wins = r.wins + 1 else r.losses = r.losses + 1 end
    if ko then r.kos = r.kos + 1 end
    -- Knocked out: injured until treated at the health station or hospital (sparring is safe)
    if req.result == "ko_loss" and e.purse[1] > 0 then
      life.set_injured(user_id, true)
      outcome.injured = true
    end
    if s.edition then r.fought[s.edition] = true end
    if win and e.title then r.titles[e.title] = true; outcome.title = e.title end
    if purse > 0 then nk.wallet_update(user_id, { [CURRENCY] = purse }, { reason = "boxing_purse", event = s.event }, true) end
    outcome.leveled_up = add_xp(c, xp)
    outcome.win = win
    outcome.ko = ko
    outcome.purse = purse
    outcome.xp = xp
    quest.progress(user_id, "train", landed)
  end

  write(CAREER, user_id, c)
  write(RECORD, user_id, r)
  outcome.new_level = c.level
  return view(user_id, context.env, outcome)
end, "boxing_finish")
