-- The town's church (โบสถ์คริสต์) and mosque (มัสยิด): pray there, join the congregation at its times,
-- and give to it. Buddhist temple life is temple.lua; this is the same idea for the other two faiths.
--   faith_get   {}                                              -> view
--   faith_pray  {"place":"church"|"mosque","action":"start","candle":bool} -> {token, seconds}
--               {"place":..., "action":"finish","token":"..."}  -> view + gained (energy every time; main
--                                                                 XP for the first prayer of the game day
--                                                                 at each place, and again once in each
--                                                                 service / prayer time, which counts double)
--   faith_give  {"place":..., "amount": n}                       -> ถวายทรัพย์ (church) / บริจาค ศอดะเกาะฮ์
--                                                                 (mosque): baht in, main XP back for up to
--                                                                 GIVE.cap baht a game day at each place
-- Times (game clock, life_core): the church holds its Sunday service (นมัสการวันอาทิตย์, the game week
-- starts on Sunday with game day 1) 09:00-11:00 and evening prayer every day 18:00-19:00; the mosque's
-- five daily prayers (ละหมาด) are ซุบฮิ 05:00, ซุฮรี 12:30, อัศริ 15:30, มัฆริบ 18:30 and อีชา 19:45, each
-- for an hour, with วันศุกร์ (ญุมอะฮ์) at ซุฮรี.

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")
local trade = require("trade_core")

local STORE = { collection = "faith", key = "state" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9

local PRAY = { seconds = 25, candle = 20, energy = 4, candle_energy = 2, xp_day = 8, xp_time = 20 }
local GIVE = { min = 20, max = 100000, cap = 2000, xp_per = 20, amounts = { 20, 100, 500, 1000 } }
local PLACES = { church = "โบสถ์คริสต์", mosque = "มัสยิด" }
local DAYS = { "อาทิตย์", "จันทร์", "อังคาร", "พุธ", "พฤหัสบดี", "ศุกร์", "เสาร์" }

-- id, name, from, to (game minutes); `days`: only on these weekdays (0 = Sunday)
local TIMES = {
  church = {
    { id = "sunday", name = "นมัสการวันอาทิตย์", from = 540, to = 660, days = { [0] = true } },
    { id = "evening", name = "อธิษฐานภาวนาภาคค่ำ", from = 1080, to = 1140 },
  },
  mosque = {
    { id = "subh", name = "ละหมาดซุบฮิ", from = 300, to = 360 },
    { id = "zuhr", name = "ละหมาดซุฮรี", from = 750, to = 810 },
    { id = "asr", name = "ละหมาดอัศริ", from = 930, to = 990 },
    { id = "maghrib", name = "ละหมาดมัฆริบ", from = 1110, to = 1170 },
    { id = "isha", name = "ละหมาดอีชา", from = 1185, to = 1245 },
  },
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
  s.places = s.places or {}
  return s
end

local function write(user_id, s)
  nk.storage_write({ { collection = STORE.collection, key = STORE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
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

local function token()
  return string.format("%08x%08x", math.random(0, 0x7fffffff), math.random(0, 0x7fffffff))
end

local function weekday(now) return (life.game_day(now) - 1) % 7 end

local function clock(minute) return string.format("%02d:%02d", math.floor(minute / 60) % 24, minute % 60) end

-- the service / prayer time on now (nil if none), and the next one today or tomorrow
local function times_now(place, now)
  local m, wd = life.minute_of_day(now), weekday(now)
  local current, nxt, best = nil, nil, nil
  for day = 0, 7 do
    local d = (wd + day) % 7
    for _, t in ipairs(TIMES[place]) do
      if not t.days or t.days[d] then
        if day == 0 and m >= t.from and m < t.to then current = t end
        local wait = day * 1440 + t.from - m
        if wait > 0 and (not best or wait < best) then best, nxt = wait, { t = t, day = d } end
      end
    end
    if nxt then break end
  end
  local friday = place == "mosque" and wd == 5
  return current, nxt, friday
end

local function place_state(s, place, day)
  local p = s.places[place] or {}
  if p.day ~= day then p = { day = day, prayed = 0, times = {}, given = 0, counted = 0 } end
  p.times = p.times or {}
  s.places[place] = p
  return p
end

local function view(user_id, s, extra)
  local now = nk.time()
  local day = life.game_day(now)
  local out = { day_name = DAYS[weekday(now) + 1], candle_cost = PRAY.candle, seconds = PRAY.seconds,
                give_amounts = GIVE.amounts, give_cap = GIVE.cap, places = {} }
  for place, name in pairs(PLACES) do
    local p = place_state(s, place, day)
    local current, nxt, friday = times_now(place, now)
    local v = { id = place, name = name, prayed = p.prayed, given = p.given, total = s.places_total and s.places_total[place] or 0 }
    if current then
      v.now = current.name .. ((friday and current.id == "zuhr") and " (ญุมอะฮ์)" or "")
      v.now_done = p.times[current.id] == true
    end
    if nxt then v.next = nxt.t.name .. " วัน" .. DAYS[nxt.day + 1] .. " " .. clock(nxt.t.from) end
    table.insert(out.places, v)
  end
  for k, v in pairs(extra or {}) do out[k] = v end
  return nk.json_encode(out)
end

local function rpc(fn)
  return function(context, payload)
    local user_id = context.user_id
    local s = read(user_id)
    local extra = fn(user_id, s, decode(payload)) or {}
    write(user_id, s)
    return view(user_id, s, extra)
  end
end

local function check_place(req)
  local place = tostring(req.place or "")
  if not PLACES[place] then err("unknown place", GRPC_INVALID_ARGUMENT) end
  return place
end

nk.register_rpc(rpc(function() end), "faith_get")

nk.register_rpc(rpc(function(user_id, s, req)
  local place = check_place(req)
  local now = nk.time()
  if req.action == "start" then
    local candle = place == "church" and req.candle == true
    if candle then pay(user_id, PRAY.candle, "candle") end
    s.prayer = { token = token(), place = place, start_ms = now, candle = candle }
    return { token = s.prayer.token, seconds = PRAY.seconds }
  end
  local m = s.prayer
  if not m or m.token ~= req.token or m.place ~= place then err("no prayer") end
  s.prayer = nil
  local elapsed = (now - m.start_ms) / 1000
  if elapsed < PRAY.seconds - 1 then err("too soon") end
  if elapsed > 600 then err("expired") end

  local p = place_state(s, place, life.game_day(now))
  local xp, with = 0, nil
  if p.prayed == 0 then xp = xp + PRAY.xp_day end
  local current = times_now(place, now)
  if current and not p.times[current.id] then
    p.times[current.id] = true
    xp = xp + PRAY.xp_time
    with = current.name
  end
  p.prayed = p.prayed + 1
  local energy = PRAY.energy + (m.candle and PRAY.candle_energy or 0)
  life.add_energy(user_id, energy)
  if xp > 0 then trade.add_main_xp(user_id, xp) end
  local done = (place == "mosque" and "ละหมาดเสร็จแล้ว" or "อธิษฐานเสร็จแล้ว อาเมน")
  if with then done = done .. " · ร่วม" .. with .. "กับผู้ศรัทธา" end
  return { gained_xp = xp, gained_energy = energy, done = done }
end), "faith_pray")

nk.register_rpc(rpc(function(user_id, s, req)
  local place = check_place(req)
  local amount = math.floor(tonumber(req.amount) or 0)
  if amount < GIVE.min or amount > GIVE.max then err("bad amount", GRPC_INVALID_ARGUMENT) end
  pay(user_id, amount, "give_" .. place)
  local p = place_state(s, place, life.game_day(nk.time()))
  local counts = math.max(0, math.min(amount, GIVE.cap - p.counted))
  p.given = p.given + amount
  p.counted = p.counted + counts
  s.places_total = s.places_total or {}
  s.places_total[place] = (s.places_total[place] or 0) + amount
  local xp = math.floor(counts / GIVE.xp_per)
  if xp > 0 then trade.add_main_xp(user_id, xp) end
  local what = place == "mosque" and "บริจาคให้มัสยิด" or "ถวายทรัพย์บำรุงโบสถ์"
  local done = what .. " " .. amount .. " บาท ขอบคุณครับ"
  if xp > 0 then done = done .. " · XP +" .. xp end
  return { gained_xp = xp, done = done }
end), "faith_give")
