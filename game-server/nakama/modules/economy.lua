-- Economy (เงินบาท): money lives in the Nakama wallet, which clients cannot modify directly.
--   economy_get        {} -> {baht, fish:[{id,name,count,price}], total_value}
--   shop_sell_fish     {} -> same, plus {sold, earned}; sells every fish in the basket at the shop
-- Fish come from fishing.lua (storage "inventory"/"fish").

local nk = require("nakama")

local INVENTORY = { collection = "inventory", key = "fish" }
local CURRENCY = "baht"

-- Buy prices at the community shop (ร้านค้าชุมชน), baht per fish
local FISH_PRICES = {
  { id = "barb",      name = "ปลาตะเพียน", price = 30 },
  { id = "tilapia",   name = "ปลานิล",     price = 40 },
  { id = "catfish",   name = "ปลาดุก",     price = 60 },
  { id = "snakehead", name = "ปลาช่อน",    price = 80 },
}

local function read_inventory(user_id)
  local objects = nk.storage_read({ { collection = INVENTORY.collection, key = INVENTORY.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function balance(user_id)
  local account = nk.account_get_id(user_id)
  return (account.wallet and account.wallet[CURRENCY]) or 0
end

-- Basket contents as a list the client can parse, plus what it would sell for
local function basket(inv)
  local counts = inv.counts or {}
  local list, value = {}, 0
  for _, f in ipairs(FISH_PRICES) do
    local n = counts[f.id] or 0
    table.insert(list, { id = f.id, name = f.name, count = n, price = f.price })
    value = value + n * f.price
  end
  return list, value
end

local function view(user_id, inv, extra)
  local list, value = basket(inv)
  local result = { baht = balance(user_id), fish = list, total_value = value }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function rpc_get(context, payload)
  return view(context.user_id, read_inventory(context.user_id))
end

local function rpc_sell_fish(context, payload)
  local user_id = context.user_id
  local inv = read_inventory(user_id)
  local _, value = basket(inv)
  local sold = inv.total or 0

  if value > 0 then
    -- Pay first, then empty the basket (keep the biggest-fish record)
    nk.wallet_update(user_id, { [CURRENCY] = value }, { reason = "sell_fish", fish = sold }, true)
    inv.counts = {}
    inv.total = 0
    nk.storage_write({ {
      collection = INVENTORY.collection, key = INVENTORY.key, user_id = user_id, value = inv,
      permission_read = 1, permission_write = 0,
    } })
  end

  return view(user_id, inv, { sold = sold, earned = value })
end

nk.register_rpc(rpc_get, "economy_get")
nk.register_rpc(rpc_sell_fish, "shop_sell_fish")
