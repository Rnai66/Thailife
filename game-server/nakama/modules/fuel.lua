-- น้ำมัน and driving yourself: every motorbike and car bought at the bank has a tank. Fill it at a
-- petrol station (ปั๊มน้ำมัน) at the pump's price per litre; driving round town burns a little, and
-- driving off to another place (from the petrol station on the edge of the map) burns what the
-- distance takes and takes the time it takes. The bicycle needs no fuel, only legs.
--
--   fuel_get   {}                         -> { fuels = {id: {name, price}}, cars = [{id, name, litres, tank, kmpl, fuel}] }
--   fuel_buy   {car, baht? | full = true} -> { car, litres, added, paid, tank }
--   fuel_use   {car, metres}              -> { car, litres }            (driving round the map)
--   drive_quote {from, car}               -> { car, litres, tank, trips = [{to, km, litres, hours}] }
--   drive_go   {from, to, car}            -> { to, km, litres, left, hours, seconds }

local nk = require("nakama")
local data = require("shop_data")
local provinces = require("provinces")

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

-- ราคาหน้าปั๊ม (baht per litre)
local FUELS = {
  g95    = { name = "แก๊สโซฮอล์ 95", price = 33.05 },
  diesel = { name = "ดีเซล",         price = 31.94 },
}

-- tank (litres), km per litre, fuel, cruising speed on the open road (km/h)
local VEHICLES = {
  car_bicycle   = { tank = 0,  kmpl = 0,  fuel = nil,      speed = 15 },
  car_motorbike = { tank = 4,  kmpl = 45, fuel = "g95",    speed = 60 },
  car_pickup    = { tank = 80, kmpl = 12, fuel = "diesel", speed = 80 },
  car_sedan     = { tank = 50, kmpl = 15, fuel = "g95",    speed = 85 },
  car_luxury    = { tank = 70, kmpl = 9,  fuel = "g95",    speed = 90 },
}

local WORLD_SCALE = 1.6      -- a metre of the game world is this much road (the speedometer's scale)
local MAX_USE_METRES = 6000  -- one fuel_use report at most
local ROAD = 1.25            -- road distance / straight line
local GAME_HOUR_SECONDS = 90 -- life_core: 1 game minute = 1.5 real seconds

-- Places you can drive to. The home province's own places sit along the road out of town
-- (km from the village); the other provinces are where they are on the map (travel.lua's list).
local LOCAL_KM = { village_khoko = 0, tambon_khoko = 7, city = 22, beach = 57 }
local PROVINCE_AT = {
  bangkok   = { lat = 13.75, lon = 100.50 },
  chiangmai = { lat = 18.79, lon = 98.98 },
  pattaya   = { lat = 12.93, lon = 100.88 },
  khonkaen  = { lat = 16.43, lon = 102.83 },
  ubon      = { lat = 15.24, lon = 104.85 },
  buriram   = { lat = 14.99, lon = 103.10 },
  korat     = { lat = 14.97, lon = 102.10 },
  songkhla  = { lat = 7.01,  lon = 100.47 },
  nakhonsi  = { lat = 8.43,  lon = 99.96 },
}
local ORDER = { "village_khoko", "tambon_khoko", "city", "beach", "bangkok", "chiangmai", "pattaya", "khonkaen", "ubon", "buriram", "korat", "songkhla", "nakhonsi" }

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload ~= "" and payload or "{}")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

local function wallet(user_id)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[data.CURRENCY] or 0
end

local function owned(user_id)
  local objects = nk.storage_read({ { collection = "bank", key = "account", user_id = user_id } })
  local a = (#objects > 0) and objects[1].value or {}
  return a.assets or {}
end

-- litres in each car's tank; a car never filled yet comes with a full tank from the dealer
local function read_tanks(user_id)
  local objects = nk.storage_read({ { collection = "fuel", key = "tanks", user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function write_tanks(user_id, tanks)
  nk.storage_write({ { collection = "fuel", key = "tanks", user_id = user_id, value = tanks, permission_read = 1, permission_write = 0 } })
end

local function litres_of(tanks, id)
  local v = VEHICLES[id]
  if tanks[id] == nil then return v.tank end
  return math.max(0, math.min(v.tank, tanks[id]))
end

local function round2(x) return math.floor(x * 100 + 0.5) / 100 end

local function my_car(user_id, req)
  local id = req.car or ""
  local v = VEHICLES[id]
  if not v then err("unknown car", GRPC_INVALID_ARGUMENT) end
  if not owned(user_id)[id] then err("not your car") end
  return id, v
end

-- km by road between two places, from the player's home province
local function distance(user_id, from, to)
  if from == to then return nil end
  local home = provinces.home(nk, user_id)
  local function at(id)
    if LOCAL_KM[id] then return home, LOCAL_KM[id] end
    return PROVINCE_AT[id], nil
  end
  local pa, la = at(from)
  local pb, lb = at(to)
  if not pa or not pb then return nil end
  if la and lb then return math.max(2, math.abs(la - lb)) end
  -- out of the home province: to its town first (the local places' distance to the city), then across
  local out = (la and math.abs(la - LOCAL_KM.city) or 0) + (lb and math.abs(lb - LOCAL_KM.city) or 0)
  return math.floor(math.max(30, provinces.km(pa, pb) * ROAD) + out + 0.5)
end

local function trip(user_id, from, to, v)
  local km = distance(user_id, from, to)
  if not km then return nil end
  local litres = v.kmpl > 0 and round2(km / v.kmpl) or 0
  local hours = math.floor((0.1 + km / v.speed) * 4 + 0.5) / 4 -- to the quarter hour
  return { to = to, km = km, litres = litres, hours = math.max(0.25, hours) }
end

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local tanks, have = read_tanks(user_id), owned(user_id)
  local cars = {}
  for _, id in ipairs(data.BANK_ASSET_ORDER) do
    local v = VEHICLES[id]
    if v and have[id] then
      table.insert(cars, { id = id, name = data.BANK_ASSETS[id].name, litres = round2(litres_of(tanks, id)), tank = v.tank,
                           kmpl = v.kmpl, fuel = v.fuel or "" })
    end
  end
  if #cars == 0 then cars = nil end
  return nk.json_encode({ fuels = FUELS, cars = cars })
end, "fuel_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local id, v = my_car(user_id, req)
  if not v.fuel then err("no tank") end
  local price = FUELS[v.fuel].price
  local tanks = read_tanks(user_id)
  local now = litres_of(tanks, id)
  local room = v.tank - now
  if room < 0.05 then err("tank full") end
  local litres
  if req.full then
    litres = room
  else
    local baht = math.floor(tonumber(req.baht) or 0)
    if baht <= 0 then err("bad amount", GRPC_INVALID_ARGUMENT) end
    litres = math.min(room, baht / price)
  end
  local paid = math.ceil(litres * price)
  if wallet(user_id) < paid then err("not enough baht") end
  nk.wallet_update(user_id, { [data.CURRENCY] = -paid }, { reason = "fuel", car = id, litres = round2(litres) }, true)
  tanks[id] = round2(now + litres)
  write_tanks(user_id, tanks)
  return nk.json_encode({ car = id, litres = tanks[id], added = round2(litres), paid = paid, tank = v.tank })
end, "fuel_buy")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local id, v = my_car(user_id, req)
  local tanks = read_tanks(user_id)
  local now = litres_of(tanks, id)
  if v.fuel then
    local metres = math.max(0, math.min(MAX_USE_METRES, tonumber(req.metres) or 0))
    now = math.max(0, now - metres * WORLD_SCALE / 1000 / v.kmpl)
    tanks[id] = round2(now)
    write_tanks(user_id, tanks)
  end
  return nk.json_encode({ car = id, litres = round2(now) })
end, "fuel_use")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local id, v = my_car(user_id, req)
  if not LOCAL_KM[req.from or ""] and not PROVINCE_AT[req.from or ""] then err("unknown place", GRPC_INVALID_ARGUMENT) end
  local trips = {}
  for _, to in ipairs(ORDER) do
    local t = trip(user_id, req.from, to, v)
    if t then table.insert(trips, t) end
  end
  if #trips == 0 then trips = nil end
  return nk.json_encode({ car = id, litres = round2(litres_of(read_tanks(user_id), id)), tank = v.tank, trips = trips })
end, "drive_quote")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local id, v = my_car(user_id, req)
  local t = trip(user_id, req.from or "", req.to or "", v)
  if not t then err("no such trip") end
  local tanks = read_tanks(user_id)
  local now = litres_of(tanks, id)
  if v.fuel then
    if now + 0.001 < t.litres then err("not enough fuel") end
    now = round2(math.max(0, now - t.litres))
    tanks[id] = now
    write_tanks(user_id, tanks)
  end
  return nk.json_encode({ to = t.to, km = t.km, litres = t.litres, left = now, hours = t.hours,
                          seconds = math.floor(t.hours * GAME_HOUR_SECONDS + 0.5) })
end, "drive_go")
