-- Trips between provinces (ต่างจังหวัด): รถทัวร์ บขส. from the bus terminal, รถไฟ from the station,
-- เครื่องบิน between provinces with an airport. The fare goes by the distance between the two places
-- (as the crow flies over the real map) and the way you travel; the trip itself is the scene change
-- on the client. Trips inside the home province (สองแถว, เรือ) stay free and don't come here, except
-- the เรือข้ามฟาก across the Chao Phraya in Bangkok (a flat fare).
--
--   travel_quote {from, mode}        -> { mode, from, trips = [{to, fare, hours}] }
--   travel_buy   {from, to, mode}    -> { to, mode, fare, hours, done }
--   ferry_ride   {}                  -> { fare }

local nk = require("nakama")
local data = require("shop_data")

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

-- Where each place is (lat, lon) and how you can get there. "city" is the player's home province
-- town (a central-plains province, no airport).
local PLACES = {
  city      = { lat = 14.47, lon = 100.12, bus = true, train = true },
  bangkok   = { lat = 13.75, lon = 100.50, bus = true, train = true, air = true },
  chiangmai = { lat = 18.79, lon = 98.98,  bus = true, train = true, air = true },
  pattaya   = { lat = 12.93, lon = 100.88, bus = true, train = true, air = true },
  khonkaen  = { lat = 16.43, lon = 102.83, bus = true, train = true, air = true },
  ubon      = { lat = 15.24, lon = 104.85, bus = true, train = true, air = true },
  buriram   = { lat = 14.99, lon = 103.10, bus = true, train = true, air = true },
  songkhla  = { lat = 7.01,  lon = 100.47, bus = true, train = true, air = true },
  nakhonsi  = { lat = 8.43,  lon = 99.96,  bus = true, train = true, air = true },
}
local ORDER = { "city", "bangkok", "chiangmai", "pattaya", "khonkaen", "ubon", "buriram", "songkhla", "nakhonsi" }

-- Fare = base + per km; hours = fixed part + km / speed (by road a little longer than straight)
local MODES = {
  bus   = { base = 60,  per_km = 0.75, wait = 0.5, speed = 65,  road = 1.25 },
  train = { base = 40,  per_km = 0.6,  wait = 0.5, speed = 55,  road = 1.3 },
  air   = { base = 900, per_km = 1.4,  wait = 1.5, speed = 600, road = 1.0 },
}

local function km(a, b)
  local dlat = a.lat - b.lat
  local dlon = (a.lon - b.lon) * math.cos(math.rad((a.lat + b.lat) / 2))
  return 111.2 * math.sqrt(dlat * dlat + dlon * dlon)
end

local function trip(from, to, mode)
  local a, b, m = PLACES[from], PLACES[to], MODES[mode]
  if not a or not b or not m or from == to or not a[mode] or not b[mode] then return nil end
  local d = km(a, b) * m.road
  local fare = math.floor((m.base + d * m.per_km) / 10 + 0.5) * 10 -- whole tens of baht
  local hours = math.floor((m.wait + d / m.speed) * 2 + 0.5) / 2    -- to the half hour
  return { to = to, fare = fare, hours = hours }
end

local FERRY_FARE = 10

local function charge(user_id, fare, meta)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  if (w[data.CURRENCY] or 0) < fare then error({ "not enough baht", GRPC_FAILED_PRECONDITION }) end
  nk.wallet_update(user_id, { [data.CURRENCY] = -fare }, meta, true)
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  if not PLACES[req.from or ""] then error({ "unknown place", GRPC_INVALID_ARGUMENT }) end
  if not MODES[req.mode or ""] then error({ "unknown mode", GRPC_INVALID_ARGUMENT }) end
  return req
end

nk.register_rpc(function(context, payload)
  local req = decode(payload)
  local trips = {}
  for _, to in ipairs(ORDER) do
    local t = trip(req.from, to, req.mode)
    if t then table.insert(trips, t) end
  end
  if #trips == 0 then trips = nil end -- an empty table would encode as {} (not a list)
  return nk.json_encode({ mode = req.mode, from = req.from, trips = trips })
end, "travel_quote")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local t = trip(req.from, req.to or "", req.mode)
  if not t then error({ "no such trip", GRPC_FAILED_PRECONDITION }) end

  charge(user_id, t.fare, { reason = "travel", mode = req.mode, from = req.from, to = req.to })
  return nk.json_encode({ to = t.to, mode = req.mode, fare = t.fare, hours = t.hours, done = "ซื้อตั๋วแล้ว — ออกเดินทาง!" })
end, "travel_buy")

nk.register_rpc(function(context, payload)
  charge(context.user_id, FERRY_FARE, { reason = "ferry" })
  return nk.json_encode({ fare = FERRY_FARE })
end, "ferry_ride")
