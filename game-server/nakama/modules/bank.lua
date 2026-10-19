-- City bank (ธนาคาร): savings with interest, loans, buying land and cars, renting buildings to
-- run a business with staff. Everything is settled on the server per game day (24 real minutes).
-- Player level for the big-money services = the best career level (boxing or music).
--
--   bank_get       {}
--   bank_deposit   {"amount": n}        bank_withdraw {"amount": n}
--   bank_borrow    {"amount": n}        bank_repay    {"amount": n}
--   bank_buy       {"asset": "car_pickup" | "land_rice" ...}
--   bank_open      {"business": "coffee", "building": "shop_left_3"}
--   bank_staff     {"slot": 1, "delta": 1 | -1}
--   bank_collect   {}                    -> takes every business's profit (or pays its loss)
--   bank_close     {"slot": 1}

local nk = require("nakama")
local life = require("life_core")
local data = require("shop_data")

local ACCOUNT = { collection = "bank", key = "account" }
local CURRENCY = data.CURRENCY
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local SAVINGS_RATE = 0.001    -- per game day, compounded
local LOAN_RATE = 0.003       -- per game day
local LOAN_LEVEL = 20
local MAX_SETTLE_DAYS = 10    -- businesses stop piling up money if nobody collects
local MAX_BUSINESSES = 3
local MAX_STAFF = 5

local ASSETS = {
  car_motorbike = { kind = "car",  name = "มอเตอร์ไซค์",                 price = 45000,   level = 5,  speed = 1.2 },
  car_pickup    = { kind = "car",  name = "รถกระบะ",                    price = 650000,  level = 8,  speed = 1.35 },
  car_sedan     = { kind = "car",  name = "รถเก๋ง",                     price = 1200000, level = 10, speed = 1.4 },
  car_luxury    = { kind = "car",  name = "รถหรู",                      price = 4500000, level = 15, speed = 1.5 },
  land_plain    = { kind = "land", name = "ที่ดินเปล่า 1 ไร่",              price = 300000,  level = 50, rent = 0.0015 },
  land_rice     = { kind = "land", name = "ที่นาติดคลอง 5 ไร่",            price = 1200000, level = 50, rent = 0.0015 },
  land_city     = { kind = "land", name = "ที่ดินติดถนนในเมือง",          price = 3500000, level = 50, rent = 0.0015 },
}
local ASSET_ORDER = { "car_motorbike", "car_pickup", "car_sedan", "car_luxury", "land_plain", "land_rice", "land_city" }

-- Businesses: setup cost, rent and wage per game day, revenue per day at full staff
local BUSINESSES = {
  coffee   = { name = "ร้านกาแฟ",   setup = 300000,  rent = 2000,  wage = 500, revenue = 9000 },
  food     = { name = "ร้านอาหาร",  setup = 600000,  rent = 3000,  wage = 500, revenue = 15000 },
  minimart = { name = "มินิมาร์ท",   setup = 1200000, rent = 5000,  wage = 600, revenue = 26000 },
  hotel    = { name = "โรงแรมเล็ก",  setup = 3000000, rent = 10000, wage = 800, revenue = 60000 },
}
local BUSINESS_ORDER = { "coffee", "food", "minimart", "hotel" }
local BUSINESS_LEVEL = 100 -- end game: the top of a career
local BUSINESS_CASH = 1000000 -- you need at least this much (wallet + savings) to open one

local BUILDINGS = {
  shop_left_3   = "ตึกแถวห้องว่าง ฝั่งซ้าย",
  shop_right_2  = "ตึกแถวห้องว่าง ฝั่งขวา",
  market_corner = "อาคารหัวมุมข้างตลาดสด",
  plaza         = "อาคารพาณิชย์ 3 ชั้น กลางเมือง",
}
local BUILDING_ORDER = { "shop_left_3", "shop_right_2", "market_corner", "plaza" }

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

local function amount_of(req)
  local n = math.floor(tonumber(req.amount) or 0)
  if n <= 0 then err("bad amount", GRPC_INVALID_ARGUMENT) end
  return n
end

local function wallet(user_id)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[CURRENCY] or 0
end

local function pay(user_id, delta, reason)
  if delta ~= 0 then nk.wallet_update(user_id, { [CURRENCY] = delta }, { reason = reason }, true) end
end

local function player_level(user_id)
  local objects = nk.storage_read({
    { collection = "career", key = "stats", user_id = user_id },
    { collection = "music", key = "stats", user_id = user_id },
  })
  local lv = 1
  for _, o in ipairs(objects) do lv = math.max(lv, o.value.level or 1) end
  return lv
end

local function game_days(now) return (now - life.EPOCH_MS) / life.DAY_MS end

local function read(user_id)
  local objects = nk.storage_read({ { collection = ACCOUNT.collection, key = ACCOUNT.key, user_id = user_id } })
  local a = (#objects > 0) and objects[1].value or {}
  a.savings = a.savings or 0
  a.loan = a.loan or 0
  a.assets = a.assets or {}          -- asset id -> true
  a.businesses = a.businesses or {}  -- list of { type, building, staff, till, settled }
  a.settled = a.settled or game_days(nk.time())
  return a
end

local function write(user_id, a)
  nk.storage_write({ { collection = ACCOUNT.collection, key = ACCOUNT.key, user_id = user_id, value = a,
                       permission_read = 1, permission_write = 0 } })
end

-- Bring interest, land rent and business takings up to now
local function settle(user_id, a)
  local now = game_days(nk.time())
  local days = math.max(0, now - a.settled)
  if days > 0 then
    a.savings = math.floor(a.savings * (1 + SAVINGS_RATE) ^ days)
    a.loan = math.floor(a.loan * (1 + LOAN_RATE) ^ days + 0.5)
    local rent = 0
    for id, _ in pairs(a.assets) do
      local x = ASSETS[id]
      if x and x.rent then rent = rent + x.price * x.rent * days end
    end
    a.savings = a.savings + math.floor(rent) -- land rent is paid into savings
    a.settled = now
  end
  for _, b in ipairs(a.businesses) do
    local t = BUSINESSES[b.type]
    local d = math.min(MAX_SETTLE_DAYS, math.max(0, now - (b.settled or now)))
    if t and d > 0 then
      local mood = 0.85 + math.random() * 0.3
      local revenue = t.revenue * (0.4 + 0.12 * b.staff) * mood
      b.till = math.floor((b.till or 0) + (revenue - t.rent - t.wage * b.staff) * d)
    end
    b.settled = now
  end
  return a
end

local function assets_value(a)
  local v = 0
  for id, _ in pairs(a.assets) do if ASSETS[id] then v = v + ASSETS[id].price end end
  for _, b in ipairs(a.businesses) do if BUSINESSES[b.type] then v = v + BUSINESSES[b.type].setup end end
  return v
end

local function loan_limit(level, a)
  if level < LOAN_LEVEL then return 0 end
  return 20000 * level + math.floor(assets_value(a) * 0.5)
end

local function view(user_id, a, extra)
  local level = player_level(user_id)
  local assets = {}
  local speed = 1
  for _, id in ipairs(ASSET_ORDER) do
    local x = ASSETS[id]
    local owned = a.assets[id] == true
    if owned and x.speed then speed = math.max(speed, x.speed) end
    table.insert(assets, { id = id, kind = x.kind, name = x.name, price = x.price, level = x.level, owned = owned,
                           perk = x.speed and ("เดินทางเร็วขึ้น " .. math.floor((x.speed - 1) * 100 + 0.5) .. "%")
                                  or ("ค่าเช่าที่ดิน ~" .. math.floor(x.price * x.rent) .. " บาท/วัน") })
  end
  local types = {}
  for _, id in ipairs(BUSINESS_ORDER) do
    local t = BUSINESSES[id]
    table.insert(types, { id = id, name = t.name, setup = t.setup, rent = t.rent, wage = t.wage, revenue = t.revenue })
  end
  local mine, till = {}, 0
  for i, b in ipairs(a.businesses) do
    local t = BUSINESSES[b.type]
    till = till + (b.till or 0)
    table.insert(mine, { slot = i, type = b.type, name = t.name, building = BUILDINGS[b.building] or b.building,
                         staff = b.staff, till = b.till or 0,
                         per_day = math.floor(t.revenue * (0.4 + 0.12 * b.staff) - t.rent - t.wage * b.staff) })
  end
  local buildings = {}
  for _, id in ipairs(BUILDING_ORDER) do
    local taken = false
    for _, b in ipairs(a.businesses) do if b.building == id then taken = true end end
    table.insert(buildings, { id = id, name = BUILDINGS[id], taken = taken })
  end
  local result = {
    level = level, cash = wallet(user_id), savings = a.savings, loan = a.loan, loan_limit = loan_limit(level, a),
    savings_rate = SAVINGS_RATE, loan_rate = LOAN_RATE, loan_level = LOAN_LEVEL,
    business_level = BUSINESS_LEVEL, business_cash = BUSINESS_CASH, max_staff = MAX_STAFF,
    assets = assets, business_types = types, buildings = buildings, till = till, speed = speed,
  }
  if #mine > 0 then result.businesses = mine end
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function rpc(name, handler)
  nk.register_rpc(function(context, payload)
    local user_id = context.user_id
    local a = settle(user_id, read(user_id))
    local extra = handler(user_id, a, decode(payload))
    write(user_id, a)
    return view(user_id, a, extra)
  end, name)
end

rpc("bank_get", function() return nil end)

rpc("bank_deposit", function(user_id, a, req)
  local n = amount_of(req)
  if wallet(user_id) < n then err("not enough baht") end
  pay(user_id, -n, "bank_deposit")
  a.savings = a.savings + n
  return { done = "ฝากเงิน " .. n .. " บาท" }
end)

rpc("bank_withdraw", function(user_id, a, req)
  local n = amount_of(req)
  if a.savings < n then err("not enough savings") end
  a.savings = a.savings - n
  pay(user_id, n, "bank_withdraw")
  return { done = "ถอนเงิน " .. n .. " บาท" }
end)

rpc("bank_borrow", function(user_id, a, req)
  local n = amount_of(req)
  local level = player_level(user_id)
  if level < LOAN_LEVEL then err("level too low") end
  if a.loan + n > loan_limit(level, a) then err("over loan limit") end
  a.loan = a.loan + n
  pay(user_id, n, "bank_loan")
  return { done = "กู้เงิน " .. n .. " บาท" }
end)

rpc("bank_repay", function(user_id, a, req)
  local n = math.min(amount_of(req), a.loan)
  if n <= 0 then err("no loan") end
  if wallet(user_id) < n then err("not enough baht") end
  pay(user_id, -n, "bank_repay")
  a.loan = a.loan - n
  return { done = "ชำระหนี้ " .. n .. " บาท" }
end)

rpc("bank_buy", function(user_id, a, req)
  local x = ASSETS[req.asset or ""]
  if not x then err("unknown asset", GRPC_INVALID_ARGUMENT) end
  if a.assets[req.asset] then err("already owned") end
  if player_level(user_id) < x.level then err("level too low") end
  if wallet(user_id) < x.price then err("not enough baht") end
  pay(user_id, -x.price, "bank_buy")
  a.assets[req.asset] = true
  return { done = "ซื้อ" .. x.name .. "แล้ว" }
end)

rpc("bank_open", function(user_id, a, req)
  local t = BUSINESSES[req.business or ""]
  if not t then err("unknown business", GRPC_INVALID_ARGUMENT) end
  if not BUILDINGS[req.building or ""] then err("unknown building", GRPC_INVALID_ARGUMENT) end
  if player_level(user_id) < BUSINESS_LEVEL then err("level too low") end
  if #a.businesses >= MAX_BUSINESSES then err("too many businesses") end
  for _, b in ipairs(a.businesses) do if b.building == req.building then err("building taken") end end
  local cash = wallet(user_id)
  if cash + a.savings < BUSINESS_CASH then err("need a million") end
  if cash < t.setup then err("not enough baht") end
  pay(user_id, -t.setup, "business_setup")
  table.insert(a.businesses, { type = req.business, building = req.building, staff = 1, till = 0, settled = game_days(nk.time()) })
  return { done = "เปิด" .. t.name .. "แล้ว" }
end)

rpc("bank_staff", function(user_id, a, req)
  local b = a.businesses[math.floor(tonumber(req.slot) or 0)]
  if not b then err("unknown business", GRPC_INVALID_ARGUMENT) end
  local delta = (tonumber(req.delta) or 0) > 0 and 1 or -1
  local n = b.staff + delta
  if n < 0 or n > MAX_STAFF then err("staff limit") end
  b.staff = n
  return { done = delta > 0 and "จ้างพนักงานเพิ่ม 1 คน" or "ลดพนักงาน 1 คน" }
end)

rpc("bank_collect", function(user_id, a, req)
  local total = 0
  for _, b in ipairs(a.businesses) do total = total + (b.till or 0) end
  if total == 0 then err("nothing to collect") end
  if total < 0 and wallet(user_id) < -total then err("not enough baht") end
  pay(user_id, total, "business_profit")
  for _, b in ipairs(a.businesses) do b.till = 0 end
  return { done = total >= 0 and ("รับกำไร " .. total .. " บาท") or ("จ่ายขาดทุน " .. -total .. " บาท"), collected = total }
end)

rpc("bank_close", function(user_id, a, req)
  local slot = math.floor(tonumber(req.slot) or 0)
  local b = a.businesses[slot]
  if not b then err("unknown business", GRPC_INVALID_ARGUMENT) end
  if (b.till or 0) ~= 0 then err("collect first") end
  table.remove(a.businesses, slot)
  return { done = "ปิดกิจการแล้ว" }
end)
