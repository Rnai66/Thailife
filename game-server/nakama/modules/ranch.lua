-- Cow farm (ฟาร์มวัว): the cows in your village pen. The อบต. gives every player one cow, once
-- (โครงการวัวให้ชาวบ้าน); more are bought from the cow seller at the farm gate.
--
--   ranch_get    {}
--   ranch_claim  {}   -> the free cow from the อบต. (once per player)
--   ranch_buy    {}   -> one cow from the cow seller

local nk = require("nakama")
local data = require("shop_data")

local HERD = { collection = "ranch", key = "herd" }
local CURRENCY = data.CURRENCY
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local COW_PRICE = 28000
local MAX_COWS = 8 -- the pen holds eight

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

local function read(user_id)
  local objects = nk.storage_read({ { collection = HERD.collection, key = HERD.key, user_id = user_id } })
  local h = (#objects > 0) and objects[1].value or {}
  h.cows = h.cows or 0
  h.claimed = h.claimed or false
  return h
end

local function write(user_id, h)
  nk.storage_write({ { collection = HERD.collection, key = HERD.key, user_id = user_id, value = h,
                       permission_read = 1, permission_write = 0 } })
end

local function view(h, done)
  return nk.json_encode({ cows = h.cows, max_cows = MAX_COWS, price = COW_PRICE, claimed = h.claimed, done = done })
end

local function rpc(name, handler)
  nk.register_rpc(function(context, payload)
    local user_id = context.user_id
    local h = read(user_id)
    local done = handler(user_id, h, decode(payload))
    if done then write(user_id, h) end
    return view(h, done)
  end, name)
end

rpc("ranch_get", function() return nil end)

rpc("ranch_claim", function(user_id, h)
  if h.claimed then err("already claimed") end
  if h.cows >= MAX_COWS then err("pen full") end
  h.claimed = true
  h.cows = h.cows + 1
  return "รับวัวจาก อบต. 1 ตัว — ไปดูที่ฟาร์มวัวในหมู่บ้าน"
end)

rpc("ranch_buy", function(user_id, h)
  if h.cows >= MAX_COWS then err("pen full") end
  if wallet(user_id) < COW_PRICE then err("not enough baht") end
  nk.wallet_update(user_id, { [CURRENCY] = -COW_PRICE }, { reason = "buy_cow" }, true)
  h.cows = h.cows + 1
  return "ซื้อวัว 1 ตัว " .. COW_PRICE .. " บาท"
end)
