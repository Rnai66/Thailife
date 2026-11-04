-- Temple life (ชีวิตในวัด), the ghost doctor (หมอผี) and keeping your home safe at night.
--
--   temple_get       {}                                  -> view (also settles the nights at home)
--   temple_ordain    {}                                  -> บวช: main Lv 5 and 2,000 baht for the robes and
--                                                           alms bowl; you wear the saffron robe, head shaved
--   temple_disrobe   {"exorcist": true|false}             -> สึก; with monk Lv 30 you may come out a หมอผี
--   temple_meditate  {"action":"start","incense":bool}   -> {token, seconds}  (ธูปเทียน 20 baht)
--                    {"action":"finish","token":"..."}   -> view + gained   (บารมี for everyone, monk XP
--                                                           while ordained, double at ทำวัตรเช้า/เย็น)
--   temple_buy       {"item": id}                        -> บูชา an amulet or a yantra cloth for the home
--                                                           shrine, adopt a temple dog, raise a กุมารทอง
--   temple_wear      {"item": id, "wear": bool}          -> ห้อยคอ / ถอด an amulet (at most 3 worn); worn
--                                                           amulets give their พุทธคุณ (amulet_core.lua)
--   ghost_start      {"spot": "...", "ghost": id}        -> {token, ghost, sigils, seconds, need}
--   ghost_finish     {"token": "...", "hits": n}         -> view + result (exorcists only, at night)
--
-- Every game night something may come for your home: a ghost (eats your food) or a thief (takes a little
-- money). Amulets and yantra cloths keep ghosts away, they and the dog keep thieves off, and the dog and
-- the กุมารทอง warn you so nothing is lost. What happened is kept in a short log the client shows.
-- Dev switch: GHOSTS_ALWAYS_OUT=true (game-server/.env) lets ghosts be banished in daytime for testing.

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")
local trade = require("trade_core")
local amulet = require("amulet_core")

local STORE = { collection = "temple", key = "state" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9

local ORDAIN = { level = 5, cost = 2000 }
local EXORCIST_MONK_LEVEL = 30
local MEDITATE = { seconds = 25, incense = 20, barami = 8, barami_incense = 8, monk = 40, monk_incense = 20, energy = 4 }
local LOG_SIZE = 6

-- บูชา at the temple. guard = keeps ghosts away, luck = keeps thieves away (percent points),
-- barami = the บารมี level needed to keep it
local ITEMS = {
  phra_pidta   = { kind = "amulet", name = "พระปิดตา", price = 900, guard = 5, luck = 3, barami = 1 },
  takrut       = { kind = "amulet", name = "ตะกรุดโทน", price = 800, guard = 7, luck = 2, barami = 1 },
  nangphaya    = { kind = "amulet", name = "พระนางพญา", price = 1200, guard = 4, luck = 6, barami = 1 },
  somdej       = { kind = "amulet", name = "พระสมเด็จ", price = 1500, guard = 6, luck = 4, barami = 1 },
  khunphaen    = { kind = "amulet", name = "พระขุนแผน", price = 2000, guard = 3, luck = 10, barami = 3 },
  thuat        = { kind = "amulet", name = "หลวงพ่อทวด", price = 2500, guard = 8, luck = 6, barami = 3 },
  phra_rod     = { kind = "amulet", name = "พระรอด", price = 3500, guard = 8, luck = 8, barami = 5 },
  rian_raek    = { kind = "amulet", name = "เหรียญหลวงพ่อรุ่นแรก", price = 5000, guard = 10, luck = 10, barami = 10 },
  phra_kring   = { kind = "amulet", name = "พระกริ่ง", price = 9999, guard = 14, luck = 12, barami = 20 },
  yant_5       = { kind = "yantra", name = "ผ้ายันต์ห้าแถว", price = 500, guard = 8, luck = 4, barami = 1 },
  yant_8       = { kind = "yantra", name = "ผ้ายันต์แปดทิศ", price = 900, guard = 10, luck = 8, barami = 2 },
  yant_kraphet = { kind = "yantra", name = "ผ้ายันต์เกราะเพชร", price = 2000, guard = 15, luck = 6, barami = 5 },
  yant_wessuwan = { kind = "yantra", name = "ผ้ายันต์ท้าวเวสสุวรรณ", price = 4500, guard = 22, luck = 6, barami = 12 },
  dog          = { kind = "guardian", name = "หมาวัด", price = 300, guard = 0, luck = 40, barami = 1 },
  kuman        = { kind = "guardian", name = "กุมารทอง", price = 9999, guard = 0, luck = 0, barami = 10 },
}
local ITEM_ORDER = { "phra_pidta", "takrut", "nangphaya", "somdej", "khunphaen", "thuat", "phra_rod", "rian_raek", "phra_kring",
                     "yant_5", "yant_8", "yant_kraphet", "yant_wessuwan", "dog", "kuman" }
local GUARD_CAP, LUCK_CAP = 70, 85

-- Ghosts the หมอผี can banish at night. level = exorcist level needed; sigils = how many ยันต์
-- appear in the chant; need = how many must be hit to win; seconds = how long the chant lasts
local GHOSTS = {
  krasue  = { name = "กระสือ", level = 1, sigils = 12, need = 8, seconds = 30, energy = 8, baht = 300, xp = 40 },
  krahang = { name = "ผีกระหัง", level = 5, sigils = 14, need = 10, seconds = 30, energy = 9, baht = 450, xp = 55 },
  pop     = { name = "ผีปอบ", level = 10, sigils = 16, need = 11, seconds = 32, energy = 10, baht = 650, xp = 70 },
  tani    = { name = "นางตานี", level = 15, sigils = 18, need = 13, seconds = 32, energy = 10, baht = 900, xp = 90 },
  takhian = { name = "นางตะเคียน", level = 20, sigils = 20, need = 14, seconds = 34, energy = 12, baht = 1200, xp = 110 },
  phrai   = { name = "ผีพราย", level = 25, sigils = 22, need = 16, seconds = 34, energy = 12, baht = 1500, xp = 130 },
  pret    = { name = "ผีเปรต", level = 35, sigils = 26, need = 19, seconds = 36, energy = 15, baht = 2200, xp = 170 },
  nak     = { name = "แม่นาก", level = 50, sigils = 32, need = 24, seconds = 40, energy = 20, baht = 5000, xp = 260 },
}

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

local function read(user_id)
  local objects = nk.storage_read({ { collection = STORE.collection, key = STORE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.items = s.items or {}
  s.barami = s.barami or { level = 1, xp = 0 }
  s.log = s.log or {}
  s.seq = s.seq or 0
  s.banished = s.banished or {} -- spot -> game day it was last banished
  s.worn = s.worn or {}         -- amulets hanging from the neck (ids, owned)
  return s
end

local function write(user_id, s)
  nk.storage_write({ { collection = STORE.collection, key = STORE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

local function main_level(user_id)
  local objects = nk.storage_read({ { collection = "career", key = "stats", user_id = user_id } })
  return (#objects > 0 and objects[1].value.level) or 1
end

local function baht(user_id)
  local w = nk.account_get_id(user_id).wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[data.CURRENCY] or 0
end

local function pay(user_id, cost, reason)
  if cost <= 0 then return end
  if baht(user_id) < cost then err("not enough baht") end
  nk.wallet_update(user_id, { [data.CURRENCY] = -cost }, { reason = reason }, true)
end

local function barami_to_next(level) return 20 + 10 * (level - 1) end

local function add_barami(s, xp)
  local b = s.barami
  b.xp = b.xp + xp
  while b.level < 100 and b.xp >= barami_to_next(b.level) do
    b.xp = b.xp - barami_to_next(b.level)
    b.level = b.level + 1
  end
  if b.level >= 100 then b.xp = 0 end
end

local function protection(s)
  local guard, luck = 0, 0
  for id, owned in pairs(s.items) do
    local it = ITEMS[id]
    if owned and it then guard, luck = guard + it.guard, luck + it.luck end
  end
  if s.exorcist then guard = guard + 25 end
  local p = amulet.totals(s.worn)
  guard, luck = guard + p.pongkan, luck + p.bangta
  return math.min(GUARD_CAP, guard), math.min(LUCK_CAP, luck)
end

local function is_night(now)
  local m = life.minute_of_day(now)
  return m >= 19 * 60 or m < 5 * 60
end

local function token()
  return string.format("%08x%08x", math.random(0, 0x7fffffff), math.random(0, 0x7fffffff))
end

-- ── The avatar url "look:g.f.h.c.t.b[.o]" (profile.lua) ────────────────────────────────────────
local BALD, ROBE = 10, 10

local function look_parts(user_id)
  local url = nk.account_get_id(user_id).user.avatar_url or ""
  local code = url:match("^look:(.+)$")
  local p = {}
  for v in (code or ""):gmatch("[^%.]+") do table.insert(p, tonumber(v) or 0) end
  if #p < 6 then
    local skin = tonumber(url:match("^skin:(%d+)$") or "0") or 0
    p = { skin >= 2 and 1 or 0, skin % 2, 0, 0, 0, 0 }
  end
  return p, code
end

local function set_look(user_id, code)
  nk.account_update_id(user_id, nil, nil, nil, nil, nil, nil, "look:" .. code)
end

-- ── Nights at home ────────────────────────────────────────────────────────────────────────────
local function note(s, day, kind, saved, text)
  s.seq = s.seq + 1
  table.insert(s.log, 1, { seq = s.seq, day = day, kind = kind, saved = saved, text = text })
  while #s.log > LOG_SIZE do table.remove(s.log) end
end

local GHOST_VISITORS = { "ผีปอบ", "กระสือ", "ผีกระหัง", "ผีเปรต" }

local function ghost_night(user_id, s, day)
  local who = GHOST_VISITORS[math.random(#GHOST_VISITORS)]
  if s.items.kuman then
    note(s, day, "ghost", true, "กุมารทองเตือน! " .. who .. "มาด้อมๆ หน้าบ้านเมื่อคืน แต่เข้าบ้านไม่ได้")
    return
  end
  -- A ghost eats what it finds: up to three of your food (ผีปอบชอบของสด)
  local items = data.read_items(user_id)
  local eaten = 0
  for _ = 1, 3 do
    local have = {}
    for id, n in pairs(items.food) do if n > 0 then table.insert(have, id) end end
    if #have == 0 then break end
    local id = have[math.random(#have)]
    items.food[id] = items.food[id] - 1
    eaten = eaten + 1
  end
  if eaten > 0 then
    data.write_items(user_id, items)
    note(s, day, "ghost", false, who .. "เข้าบ้านเมื่อคืน กินของกินไป " .. eaten .. " อย่าง · บูชาพระเครื่องหรือผ้ายันต์ไว้กันผี")
  else
    life.add_energy(user_id, -math.min(10, life.state(user_id).energy))
    note(s, day, "ghost", false, who .. "มาหลอกทั้งคืน นอนไม่หลับ พลังงาน -10 · บูชาพระเครื่องหรือผ้ายันต์ไว้กันผี")
  end
end

local function thief_night(user_id, s, day)
  if s.items.dog and math.random() < 0.8 then
    note(s, day, "thief", true, "หมาเห่าไล่ขโมยที่มาด้อมๆ บ้านเมื่อคืน ไม่มีอะไรหาย")
    return
  end
  if s.items.kuman and math.random() < 0.5 then
    note(s, day, "thief", true, "กุมารทองปลุกให้ตื่น ขโมยตกใจหนีไป ไม่มีอะไรหาย")
    return
  end
  local take = math.min(2000, math.floor(baht(user_id) * 0.03))
  if take > 0 then
    nk.wallet_update(user_id, { [data.CURRENCY] = -take }, { reason = "thief" }, true)
    note(s, day, "thief", false, "ขโมยขึ้นบ้านเมื่อคืน เงินหายไป " .. take .. " บาท · รับหมาวัดไปเลี้ยงเฝ้าบ้านได้")
  else
    note(s, day, "thief", false, "ขโมยขึ้นบ้านเมื่อคืน แต่ไม่เจออะไรมีค่า")
  end
end

-- Roll each night that has passed since the last look (at most the last three)
local function settle_nights(user_id, s, now)
  local today = life.game_day(now)
  if not s.night_day then s.night_day = today return end
  local from = math.max(s.night_day + 1, today - 2)
  local guard, luck = protection(s)
  for day = from, today do
    if math.random() < 0.25 * (1 - guard / 100) then ghost_night(user_id, s, day) end
    if math.random() < 0.2 * (1 - luck / 100) then thief_night(user_id, s, day) end
  end
  s.night_day = today
end

local function is_worn(s, id)
  for _, w in ipairs(s.worn) do if w == id then return true end end
  return false
end

local function powers_or_nil(list) return (#list > 0) and list or nil end

-- ── View ──────────────────────────────────────────────────────────────────────────────────────
local function view(user_id, s, extra)
  local trades = trade.read(user_id)
  local monk, exorcist = trades.list.monk, trades.list.exorcist
  local guard, luck = protection(s)
  local catalog = {}
  for _, id in ipairs(ITEM_ORDER) do
    local it = ITEMS[id]
    table.insert(catalog, { id = id, kind = it.kind, name = it.name, price = it.price, guard = it.guard, luck = it.luck,
                            barami = it.barami, owned = s.items[id] == true, worn = is_worn(s, id),
                            powers = amulet.list(id) })
  end
  local ghosts = {}
  for id, g in pairs(GHOSTS) do
    table.insert(ghosts, { id = id, name = g.name, level = g.level, baht = g.baht })
  end
  table.sort(ghosts, function(a, b) return a.level < b.level end)
  local banished = {}
  local today = life.game_day(nk.time())
  for spot, day in pairs(s.banished) do if day == today then table.insert(banished, spot) end end
  local result = {
    ordained = s.ordained == true, exorcist = s.exorcist == true,
    monk_level = monk.level, monk_xp = monk.xp, monk_to_next = trade.xp_to_next(monk.level),
    exorcist_level = exorcist.level, exorcist_xp = exorcist.xp, exorcist_to_next = trade.xp_to_next(exorcist.level),
    barami = s.barami.level, barami_xp = s.barami.xp, barami_to_next = barami_to_next(s.barami.level),
    guard = guard, luck = luck, dog = s.items.dog == true, kuman = s.items.kuman == true,
    ordain_level = ORDAIN.level, ordain_cost = ORDAIN.cost, exorcist_monk_level = EXORCIST_MONK_LEVEL,
    incense_cost = MEDITATE.incense, meditate_seconds = MEDITATE.seconds,
    worn_max = amulet.WORN_MAX, my_powers = powers_or_nil(amulet.totals_list(amulet.totals(s.worn))),
    night = is_night(nk.time()), items = catalog, ghosts = ghosts,
    log = (#s.log > 0) and s.log or nil, banished = (#banished > 0) and banished or nil,
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function rpc(fn)
  return function(context, payload)
    local user_id = context.user_id
    local s = read(user_id)
    settle_nights(user_id, s, nk.time())
    local extra = fn(context, user_id, s, decode(payload)) or {}
    write(user_id, s)
    return view(user_id, s, extra)
  end
end

nk.register_rpc(rpc(function() end), "temple_get")

nk.register_rpc(rpc(function(context, user_id, s, req)
  if s.ordained then err("already ordained") end
  if main_level(user_id) < ORDAIN.level then err("level") end
  pay(user_id, ORDAIN.cost, "ordain")
  local p, code = look_parts(user_id)
  s.lay_look = code or table.concat(p, ".", 1, 6)
  p[3], p[7] = BALD, ROBE
  set_look(user_id, table.concat(p, ".", 1, 7))
  s.ordained = true
  s.ordain_ms = nk.time()
  add_barami(s, 20)
  return { done = "บวชแล้ว สาธุ! ห่มจีวรเรียบร้อย · นั่งสมาธิในโบสถ์เพื่อเลื่อนเลเวลพระ" }
end), "temple_ordain")

nk.register_rpc(rpc(function(context, user_id, s, req)
  if not s.ordained then err("not ordained") end
  local becoming = req.exorcist == true
  if becoming and trade.read(user_id).list.monk.level < EXORCIST_MONK_LEVEL then err("monk level") end
  local p = look_parts(user_id)
  local lay = s.lay_look
  if not lay then p[7] = nil; lay = table.concat(p, ".", 1, 6) end
  set_look(user_id, lay)
  s.ordained = false
  s.lay_look = nil
  if becoming then s.exorcist = true end
  return { done = becoming and "สึกออกมาเป็นหมอผีแล้ว! ออกไปปราบผีในป่าช้าและป่าชุมชนตอนกลางคืนได้" or "สึกแล้ว กลับไปใช้ชีวิตฆราวาส" }
end), "temple_disrobe")

nk.register_rpc(rpc(function(context, user_id, s, req)
  local now = nk.time()
  if req.action == "start" then
    local incense = req.incense == true
    if incense then pay(user_id, MEDITATE.incense, "incense") end
    s.meditation = { token = token(), start_ms = now, incense = incense }
    return { token = s.meditation.token, seconds = MEDITATE.seconds }
  end
  local m = s.meditation
  if not m or m.token ~= req.token then err("no meditation") end
  s.meditation = nil
  local elapsed = (now - m.start_ms) / 1000
  if elapsed < MEDITATE.seconds - 1 then err("too soon") end
  if elapsed > 600 then err("expired") end
  local barami = MEDITATE.barami + (m.incense and MEDITATE.barami_incense or 0)
  add_barami(s, barami)
  local monk = 0
  if s.ordained then
    local minute = life.minute_of_day(now)
    local chanting = (minute >= 300 and minute < 420) or (minute >= 1080 and minute < 1200) -- ทำวัตรเช้า / ทำวัตรเย็น
    monk = (MEDITATE.monk + (m.incense and MEDITATE.monk_incense or 0)) * (chanting and 2 or 1)
    trade.gain(user_id, "monk", monk)
  end
  life.add_energy(user_id, MEDITATE.energy)
  return { gained_barami = barami, gained_monk = monk, gained_energy = MEDITATE.energy }
end), "temple_meditate")

nk.register_rpc(rpc(function(context, user_id, s, req)
  local it = ITEMS[req.item or ""]
  if not it then err("unknown item", GRPC_INVALID_ARGUMENT) end
  if s.items[req.item] then err("already owned") end
  if s.barami.level < it.barami then err("barami") end
  pay(user_id, it.price, "temple_" .. req.item)
  s.items[req.item] = true
  if amulet.ITEMS[req.item] and #s.worn < amulet.WORN_MAX then
    table.insert(s.worn, req.item)
    return { done = "บูชา" .. it.name .. "แล้ว ห้อยคอไว้เลย · ได้พุทธคุณติดตัว" }
  end
  return { done = it.kind == "guardian" and ("รับ" .. it.name .. "ไปเลี้ยงเฝ้าบ้านแล้ว") or ("บูชา" .. it.name .. "ขึ้นหิ้งพระที่บ้านแล้ว") }
end), "temple_buy")

nk.register_rpc(rpc(function(context, user_id, s, req)
  local id = req.item or ""
  local it = ITEMS[id]
  if not it or not amulet.ITEMS[id] then err("unknown item", GRPC_INVALID_ARGUMENT) end
  if not s.items[id] then err("not owned") end
  local kept = {}
  for _, w in ipairs(s.worn) do if w ~= id then table.insert(kept, w) end end
  if req.wear == true then
    if #kept >= amulet.WORN_MAX then err("neck full") end
    table.insert(kept, id)
  end
  s.worn = kept
  return { done = req.wear == true and ("ห้อย" .. it.name .. "ไว้ที่คอแล้ว") or ("ถอด" .. it.name .. "ขึ้นหิ้งพระแล้ว") }
end), "temple_wear")

nk.register_rpc(rpc(function(context, user_id, s, req)
  local g = GHOSTS[req.ghost or ""]
  if not g then err("unknown ghost", GRPC_INVALID_ARGUMENT) end
  local spot = tostring(req.spot or "")
  if spot == "" or #spot > 40 then err("unknown spot", GRPC_INVALID_ARGUMENT) end
  if not s.exorcist then err("not exorcist") end
  local now = nk.time()
  local always = context.env and context.env.ghosts_always_out == "true"
  if not always and not is_night(now) then err("daytime") end
  if trade.read(user_id).list.exorcist.level < g.level then err("level") end
  if s.banished[spot] == life.game_day(now) then err("banished") end
  life.spend(user_id, g.energy)
  s.chant = { token = token(), start_ms = now, ghost = req.ghost, spot = spot }
  return { token = s.chant.token, ghost = req.ghost, ghost_name = g.name, sigils = g.sigils, need = g.need, seconds = g.seconds }
end), "ghost_start")

nk.register_rpc(rpc(function(context, user_id, s, req)
  local c = s.chant
  if not c or c.token ~= req.token then err("no chant") end
  s.chant = nil
  local g = GHOSTS[c.ghost]
  local now = nk.time()
  local elapsed = (now - c.start_ms) / 1000
  local hits = math.floor(tonumber(req.hits) or 0)
  if hits < 0 or hits > g.sigils then err("invalid hits", GRPC_INVALID_ARGUMENT) end
  if hits >= g.need and elapsed < g.seconds * 0.4 then err("too soon") end
  if elapsed > g.seconds + 120 then err("expired") end
  if hits < g.need then
    return { won = false, ghost_name = g.name, done = g.name .. "หนีไปได้! ลองใหม่คืนนี้" }
  end
  s.banished[c.spot] = life.game_day(now)
  nk.wallet_update(user_id, { [data.CURRENCY] = g.baht }, { reason = "exorcise_" .. c.ghost }, true)
  trade.gain(user_id, "exorcist", g.xp)
  add_barami(s, 3)
  return { won = true, ghost_name = g.name, reward = g.baht, xp = g.xp,
           done = "ปราบ" .. g.name .. "สำเร็จ! ชาวบ้านให้ค่าครู " .. g.baht .. " บาท" }
end), "ghost_finish")
