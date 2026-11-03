-- Cow farm (ฟาร์มวัว): the cows in your village pen. The อบต. gives every player one cow, once
-- (โครงการวัวให้ชาวบ้าน); more are bought from the cow seller at the farm gate.
-- Cows eat one unit of feed (อาหารวัว) per game day. A fed cow grows, and a fed female (แม่วัว) gives
-- a bottle of milk that day; after GROWN_DAYS fed days a cow can be sold back for more than it cost.
-- Every cow is a female or a male (random when bought). A grown female can be bred by artificial
-- insemination (ผสมเทียม) and has a calf (ลูกวัว) GESTATION_DAYS game days later.
--
--   ranch_get    {}
--   ranch_claim  {}               -> the free cow from the อบต. (once per player)
--   ranch_buy    {}               -> one cow from the cow seller
--   ranch_feed   {"packs": n}     -> buy feed (FEED_PACK units per pack) into the trough store
--   ranch_milk   {}               -> milk the cows: sells the milk to the dairy co-op (สหกรณ์โคนม)
--   ranch_sell   {"cow": i}       -> sell cow i (1-based; not in calf). A grown cow fetches the full
--                                    price, a young one less. Without "cow": the most grown cow.
--   ranch_breed  {"cow": i}       -> artificial insemination for grown female i (or the first one ready)
--   ranch_expand {}               -> build the pen bigger (PEN_STEP more cows, up to #PEN_PRICES times)

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")
local trade = require("trade_core")
local land = require("land_core")

local HERD = { collection = "ranch", key = "herd" }
local CURRENCY = data.CURRENCY
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local COW_PRICE = 28000
local BASE_COWS = 8         -- the pen starts with room for eight
local PEN_STEP = 4          -- each extension makes room for four more
local PEN_PRICES = { 20000, 30000, 45000 } -- up to 20 cows
local YOUNG_PRICE = 16000   -- a cow sold before it is grown: from this up to the grown price
local FEED_PACK = 10        -- units per pack
local FEED_PACK_PRICE = 250
local MAX_FEED = 200
local MILK_PRICE = 70       -- per bottle, one bottle per fed cow per game day
local MAX_MILK_PER_COW = 5  -- the milk waits this long before it spoils
local GROWN_DAYS = 15       -- fed days until a cow is fully grown
local GROWN_PRICE = 36000   -- a grown female; a grown male (วัวเนื้อ) fetches a bit more
local GROWN_PRICE_MALE = 38000
local BREED_PRICE = 1500    -- artificial insemination (ผสมเทียม) by the livestock officer
local GESTATION_DAYS = 60   -- game days in calf (36 real hours)
local MAX_SETTLE_DAYS = 10

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
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

local function game_days(now) return (now - life.EPOCH_MS) / life.DAY_MS end

local function read(user_id)
  local objects = nk.storage_read({ { collection = HERD.collection, key = HERD.key, user_id = user_id } })
  local h = (#objects > 0) and objects[1].value or {}
  if type(h.cows) == "number" then -- first version stored only a count
    local list = {}
    for _ = 1, h.cows do table.insert(list, { g = 0 }) end
    h.cows = list
  end
  h.cows = h.cows or {}
  for _, cow in ipairs(h.cows) do cow.sex = cow.sex or "f" end -- cows from before sexes were all milk cows
  h.claimed = h.claimed or false
  h.feed = h.feed or 0
  h.milk = h.milk or 0
  h.settled = h.settled or math.floor(game_days(nk.time()))
  h.pen = h.pen or 0
  return h
end

-- Room in the village pen plus a second pen built on your own land (land_core)
local function capacity(h) return BASE_COWS + PEN_STEP * h.pen + (h.extra or 0) end

-- What a cow fetches now: grown cows the full price (males a bit more), young ones less
local function cow_price(cow)
  local full = (cow.sex == "m") and GROWN_PRICE_MALE or GROWN_PRICE
  local g = math.min(cow.g or 0, GROWN_DAYS)
  if g >= GROWN_DAYS then return full end
  return math.floor((YOUNG_PRICE + (full - YOUNG_PRICE) * g / GROWN_DAYS) / 100) * 100
end

local function cow_index(h, req)
  if req.cow == nil then return nil end
  local i = math.floor(tonumber(req.cow) or 0)
  if i < 1 or i > #h.cows then err("unknown cow", GRPC_INVALID_ARGUMENT) end
  return i
end

local function write(user_id, h)
  nk.storage_write({ { collection = HERD.collection, key = HERD.key, user_id = user_id, value = h,
                       permission_read = 1, permission_write = 0 } })
end

local function random_sex() return (math.random() < 0.5) and "f" or "m" end

local function females(h)
  local n = 0
  for _, cow in ipairs(h.cows) do if cow.sex == "f" then n = n + 1 end end
  return n
end

-- Run the days that passed: each cow eats one feed a day; a fed cow grows, a fed female gives milk.
-- Calves due by today are born (when the pen has room).
local function settle(h)
  local today = math.floor(game_days(nk.time()))
  local days = math.min(today - h.settled, MAX_SETTLE_DAYS)
  for _ = 1, days do
    for _, cow in ipairs(h.cows) do
      if h.feed <= 0 then break end
      h.feed = h.feed - 1
      cow.g = (cow.g or 0) + 1
      if cow.sex == "f" then h.milk = h.milk + 1 end
    end
  end
  h.milk = math.min(h.milk, females(h) * MAX_MILK_PER_COW)
  local born = {}
  for _, cow in ipairs(h.cows) do
    if cow.bred and today >= cow.bred + GESTATION_DAYS and #h.cows + #born < capacity(h) then
      cow.bred = nil
      table.insert(born, { g = 0, sex = random_sex() })
    end
  end
  for _, calf in ipairs(born) do table.insert(h.cows, calf) end
  if #born > 0 then h.born = (h.born or 0) + #born end
  if today > h.settled then h.settled = today end
  return h
end

local function view(h, done)
  local growth, herd, grown, breedable, pregnant = {}, {}, 0, 0, 0
  local today = math.floor(game_days(nk.time()))
  for _, cow in ipairs(h.cows) do
    local g = cow.g or 0
    table.insert(growth, math.min(g, GROWN_DAYS))
    table.insert(herd, { g = math.min(g, GROWN_DAYS), sex = cow.sex, pregnant = cow.bred ~= nil,
                         due_days = cow.bred and math.max(0, cow.bred + GESTATION_DAYS - today) or 0,
                         price = cow_price(cow) })
    if g >= GROWN_DAYS and not cow.bred then grown = grown + 1 end
    if cow.bred then pregnant = pregnant + 1 end
    if cow.sex == "f" and g >= GROWN_DAYS and not cow.bred then breedable = breedable + 1 end
  end
  local next_day_ms = (h.settled + 1 - game_days(nk.time())) * life.DAY_MS
  local result = {
    cows = #h.cows, max_cows = capacity(h), price = COW_PRICE, claimed = h.claimed,
    pen = h.pen, pen_max = #PEN_PRICES, land_cows = h.extra or 0, expand_price = PEN_PRICES[h.pen + 1] or 0, pen_step = PEN_STEP,
    young_price = YOUNG_PRICE,
    feed = h.feed, max_feed = MAX_FEED, feed_pack = FEED_PACK, feed_pack_price = FEED_PACK_PRICE,
    milk = h.milk, milk_price = MILK_PRICE, grown = grown, grown_days = GROWN_DAYS, grown_price = GROWN_PRICE,
    next_day_s = math.max(0, math.floor(next_day_ms / 1000)), done = done,
    grown_price_male = GROWN_PRICE_MALE, breed_price = BREED_PRICE, gestation_days = GESTATION_DAYS,
    females = females(h), pregnant = pregnant, breedable = breedable, born = h.born or 0,
  }
  if #growth > 0 then result.growth = growth; result.herd = herd end -- an empty Lua table would encode as {}
  return nk.json_encode(result)
end

local function rpc(name, handler)
  nk.register_rpc(function(context, payload)
    local user_id = context.user_id
    local h = read(user_id)
    h.extra = land.extra_cows(user_id)
    settle(h)
    local done = handler(user_id, h, decode(payload))
    write(user_id, h)
    return view(h, done)
  end, name)
end

rpc("ranch_get", function() return nil end)

rpc("ranch_claim", function(user_id, h)
  if h.claimed then err("already claimed") end
  if #h.cows >= capacity(h) then err("pen full") end
  h.claimed = true
  local sex = random_sex()
  table.insert(h.cows, { g = 0, sex = sex })
  return "รับ" .. (sex == "f" and "วัวตัวเมีย" or "วัวตัวผู้") .. "จาก อบต. 1 ตัว — ไปดูที่ฟาร์มวัวในหมู่บ้าน"
end)

rpc("ranch_buy", function(user_id, h)
  if #h.cows >= capacity(h) then err("pen full") end
  if wallet(user_id) < COW_PRICE then err("not enough baht") end
  pay(user_id, -COW_PRICE, "buy_cow")
  local sex = random_sex()
  table.insert(h.cows, { g = 0, sex = sex })
  trade.gain(user_id, "cattle", 30)
  return "ซื้อวัว 1 ตัว " .. COW_PRICE .. " บาท — ได้" ..
    (sex == "f" and "ตัวเมีย! โตแล้วผสมเทียมให้มีลูกได้" or "ตัวผู้ โตแล้วขายได้ราคาดี")
end)

rpc("ranch_feed", function(user_id, h, req)
  local packs = math.floor(tonumber(req.packs) or 1)
  if packs < 1 or packs > 20 then err("bad amount", GRPC_INVALID_ARGUMENT) end
  if h.feed + packs * FEED_PACK > MAX_FEED then err("feed store full") end
  local cost = packs * FEED_PACK_PRICE
  if wallet(user_id) < cost then err("not enough baht") end
  pay(user_id, -cost, "cow_feed")
  h.feed = h.feed + packs * FEED_PACK
  trade.gain(user_id, "cattle", 4 * packs)
  return "ซื้ออาหารวัว " .. (packs * FEED_PACK) .. " ถุง " .. cost .. " บาท"
end)

rpc("ranch_milk", function(user_id, h)
  if h.milk <= 0 then err("no milk") end
  life.spend(user_id, 5)
  local earned = h.milk * MILK_PRICE
  local bottles = h.milk
  pay(user_id, earned, "sell_milk")
  h.milk = 0
  trade.gain_many(user_id, { cattle = 8 * bottles, merchant = bottles }, true)
  return "รีดนม " .. bottles .. " ขวด ขายให้สหกรณ์ได้ " .. earned .. " บาท"
end)

rpc("ranch_sell", function(user_id, h, req)
  local i = cow_index(h, req)
  if i then
    if h.cows[i].bred then err("in calf") end
  else
    -- the most grown cow not in calf; males go first (the females are kept for milk and calves)
    local best = -1
    for j, cow in ipairs(h.cows) do
      local score = (cow.g or 0) + ((cow.sex == "m") and 0.5 or 0)
      if not cow.bred and score > best then best, i = score, j end
    end
    if not i or best < GROWN_DAYS then err("not grown") end
  end
  local cow = table.remove(h.cows, i)
  local price = cow_price(cow)
  local grown = (cow.g or 0) >= GROWN_DAYS
  pay(user_id, price, "sell_cow")
  trade.gain_many(user_id, { cattle = grown and 80 or 30, merchant = grown and 20 or 8 }, true)
  return "ขาย" .. (cow.sex == "m" and "วัวตัวผู้" or "วัวตัวเมีย") .. (grown and "โตเต็มที่" or "ที่ยังไม่โต") ..
    " 1 ตัว ได้ " .. price .. " บาท"
end)

rpc("ranch_breed", function(user_id, h, req)
  local pick
  local i = cow_index(h, req)
  local function ready(cow) return cow.sex == "f" and (cow.g or 0) >= GROWN_DAYS and not cow.bred end
  if i then
    if ready(h.cows[i]) then pick = h.cows[i] end
  else
    for _, cow in ipairs(h.cows) do if ready(cow) then pick = cow; break end end
  end
  if not pick then err("no cow to breed") end
  local due = 0
  for _, cow in ipairs(h.cows) do if cow.bred then due = due + 1 end end
  if #h.cows + due >= capacity(h) then err("pen full") end
  if wallet(user_id) < BREED_PRICE then err("not enough baht") end
  life.spend(user_id, 5)
  pay(user_id, -BREED_PRICE, "cow_breed")
  pick.bred = math.floor(game_days(nk.time()))
  trade.gain(user_id, "cattle", 25, true)
  return "ผสมเทียมแม่วัวแล้ว " .. BREED_PRICE .. " บาท · ลูกวัวจะเกิดในอีก " .. GESTATION_DAYS .. " วันในเกม"
end)

rpc("ranch_expand", function(user_id, h)
  local cost = PEN_PRICES[h.pen + 1]
  if not cost then err("pen max") end
  if wallet(user_id) < cost then err("not enough baht") end
  life.spend(user_id, 10)
  pay(user_id, -cost, "cow_pen")
  h.pen = h.pen + 1
  trade.gain(user_id, "cattle", 40, true)
  return "ขยายคอกวัวแล้ว " .. cost .. " บาท · เลี้ยงได้ " .. capacity(h) .. " ตัว"
end)
