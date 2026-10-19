-- Cow farm (ฟาร์มวัว): the cows in your village pen. The อบต. gives every player one cow, once
-- (โครงการวัวให้ชาวบ้าน); more are bought from the cow seller at the farm gate.
-- Cows eat one unit of feed (อาหารวัว) per game day. A fed cow gives a bottle of milk that day and
-- grows; after GROWN_DAYS fed days it can be sold back for more than it cost.
--
--   ranch_get    {}
--   ranch_claim  {}               -> the free cow from the อบต. (once per player)
--   ranch_buy    {}               -> one cow from the cow seller
--   ranch_feed   {"packs": n}     -> buy feed (FEED_PACK units per pack) into the trough store
--   ranch_milk   {}               -> milk the cows: sells the milk to the dairy co-op (สหกรณ์โคนม)
--   ranch_sell   {}               -> sell your most grown cow (must be fully grown)

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")

local HERD = { collection = "ranch", key = "herd" }
local CURRENCY = data.CURRENCY
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local COW_PRICE = 28000
local MAX_COWS = 8          -- the pen holds eight
local FEED_PACK = 10        -- units per pack
local FEED_PACK_PRICE = 250
local MAX_FEED = 200
local MILK_PRICE = 70       -- per bottle, one bottle per fed cow per game day
local MAX_MILK_PER_COW = 5  -- the milk waits this long before it spoils
local GROWN_DAYS = 12       -- fed days until a cow is fully grown
local GROWN_PRICE = 42000
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
  h.claimed = h.claimed or false
  h.feed = h.feed or 0
  h.milk = h.milk or 0
  h.settled = h.settled or math.floor(game_days(nk.time()))
  return h
end

local function write(user_id, h)
  nk.storage_write({ { collection = HERD.collection, key = HERD.key, user_id = user_id, value = h,
                       permission_read = 1, permission_write = 0 } })
end

-- Run the days that passed: each cow eats one feed a day; a fed cow grows and gives milk
local function settle(h)
  local today = math.floor(game_days(nk.time()))
  local days = math.min(today - h.settled, MAX_SETTLE_DAYS)
  for _ = 1, days do
    for _, cow in ipairs(h.cows) do
      if h.feed <= 0 then break end
      h.feed = h.feed - 1
      cow.g = (cow.g or 0) + 1
      h.milk = h.milk + 1
    end
  end
  h.milk = math.min(h.milk, #h.cows * MAX_MILK_PER_COW)
  if today > h.settled then h.settled = today end
  return h
end

local function view(h, done)
  local growth, grown = {}, 0
  for _, cow in ipairs(h.cows) do
    table.insert(growth, math.min(cow.g or 0, GROWN_DAYS))
    if (cow.g or 0) >= GROWN_DAYS then grown = grown + 1 end
  end
  local next_day_ms = (h.settled + 1 - game_days(nk.time())) * life.DAY_MS
  local result = {
    cows = #h.cows, max_cows = MAX_COWS, price = COW_PRICE, claimed = h.claimed,
    feed = h.feed, max_feed = MAX_FEED, feed_pack = FEED_PACK, feed_pack_price = FEED_PACK_PRICE,
    milk = h.milk, milk_price = MILK_PRICE, grown = grown, grown_days = GROWN_DAYS, grown_price = GROWN_PRICE,
    next_day_s = math.max(0, math.floor(next_day_ms / 1000)), done = done,
  }
  if #growth > 0 then result.growth = growth end -- an empty Lua table would encode as {}
  return nk.json_encode(result)
end

local function rpc(name, handler)
  nk.register_rpc(function(context, payload)
    local user_id = context.user_id
    local h = settle(read(user_id))
    local done = handler(user_id, h, decode(payload))
    write(user_id, h)
    return view(h, done)
  end, name)
end

rpc("ranch_get", function() return nil end)

rpc("ranch_claim", function(user_id, h)
  if h.claimed then err("already claimed") end
  if #h.cows >= MAX_COWS then err("pen full") end
  h.claimed = true
  table.insert(h.cows, { g = 0 })
  return "รับวัวจาก อบต. 1 ตัว — ไปดูที่ฟาร์มวัวในหมู่บ้าน"
end)

rpc("ranch_buy", function(user_id, h)
  if #h.cows >= MAX_COWS then err("pen full") end
  if wallet(user_id) < COW_PRICE then err("not enough baht") end
  pay(user_id, -COW_PRICE, "buy_cow")
  table.insert(h.cows, { g = 0 })
  return "ซื้อวัว 1 ตัว " .. COW_PRICE .. " บาท"
end)

rpc("ranch_feed", function(user_id, h, req)
  local packs = math.floor(tonumber(req.packs) or 1)
  if packs < 1 or packs > 20 then err("bad amount", GRPC_INVALID_ARGUMENT) end
  if h.feed + packs * FEED_PACK > MAX_FEED then err("feed store full") end
  local cost = packs * FEED_PACK_PRICE
  if wallet(user_id) < cost then err("not enough baht") end
  pay(user_id, -cost, "cow_feed")
  h.feed = h.feed + packs * FEED_PACK
  return "ซื้ออาหารวัว " .. (packs * FEED_PACK) .. " ถุง " .. cost .. " บาท"
end)

rpc("ranch_milk", function(user_id, h)
  if h.milk <= 0 then err("no milk") end
  life.spend(user_id, 5)
  local earned = h.milk * MILK_PRICE
  local bottles = h.milk
  pay(user_id, earned, "sell_milk")
  h.milk = 0
  return "รีดนม " .. bottles .. " ขวด ขายให้สหกรณ์ได้ " .. earned .. " บาท"
end)

rpc("ranch_sell", function(user_id, h)
  local best, best_i = -1, nil
  for i, cow in ipairs(h.cows) do
    if (cow.g or 0) > best then best, best_i = cow.g or 0, i end
  end
  if not best_i or best < GROWN_DAYS then err("not grown") end
  table.remove(h.cows, best_i)
  pay(user_id, GROWN_PRICE, "sell_cow")
  return "ขายวัวโตเต็มที่ 1 ตัว ได้ " .. GROWN_PRICE .. " บาท"
end)
