-- Economy (เงินบาท): money lives in the Nakama wallet, which clients cannot modify directly.
--   economy_get        {}            -> {baht, fish:[...], total_value, rod, rod_name, camp_member, catalog:[...]}
--   shop_sell_fish     {}            -> same, plus {sold, earned}; sells every fish in the basket
--   shop_buy           {"item": id}  -> same, plus {bought}; checks price and balance on the server
-- Fish come from fishing.lua (storage "inventory"/"fish").

local nk = require("nakama")
local data = require("shop_data")

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

-- Everything sellable (fish + rice from the barn) as a list the client can parse, plus its value
local function basket(inv, crops)
  local counts = inv.counts or {}
  local list, value = {}, 0
  for _, f in ipairs(FISH_PRICES) do
    local n = counts[f.id] or 0
    table.insert(list, { id = f.id, name = f.name, count = n, price = f.price })
    value = value + n * f.price
  end
  for _, v in ipairs(data.RICE_ORDER) do
    local r = data.RICE[v]
    local kg = crops.rice[v] or 0
    table.insert(list, { id = "rice_" .. v, name = r.name .. " (กก.)", count = kg, price = r.price_per_kg })
    value = value + kg * r.price_per_kg
  end
  return list, value
end

local function catalog(items)
  local list = {}
  for _, entry in ipairs(data.CATALOG) do
    local have = ""
    if entry.seed then have = "มีอยู่ " .. items.seeds[entry.seed] .. " ถุง" end
    if entry.barn then have = "ยุ้งจุ " .. items.barn_capacity .. " กก." end
    table.insert(list, { id = entry.id, shop = entry.shop, name = entry.name, desc = entry.desc,
                         price = entry.price, owned = data.owns(items, entry), have = have })
  end
  return list
end

local function view(user_id, inv, extra)
  local crops = data.read_barn(user_id)
  local list, value = basket(inv, crops)
  local items = data.read_items(user_id)
  local result = {
    baht = balance(user_id), fish = list, total_value = value,
    rod = items.rod, rod_name = data.RODS[items.rod].name, camp_member = items.camp_member,
    catalog = catalog(items),
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function rpc_get(context, payload)
  return view(context.user_id, read_inventory(context.user_id))
end

local function rpc_sell_fish(context, payload)
  local user_id = context.user_id
  local inv = read_inventory(user_id)
  local crops, rice_kg = data.read_barn(user_id)
  local _, value = basket(inv, crops)
  local sold = (inv.total or 0) + rice_kg

  if value > 0 then
    -- Pay first, then empty the fish basket and the rice barn (keep the biggest-fish record)
    nk.wallet_update(user_id, { [CURRENCY] = value }, { reason = "sell_produce", fish = inv.total or 0, rice_kg = rice_kg }, true)
    inv.counts = {}
    inv.total = 0
    for _, v in ipairs(data.RICE_ORDER) do crops.rice[v] = 0 end
    nk.storage_write({
      { collection = INVENTORY.collection, key = INVENTORY.key, user_id = user_id, value = inv, permission_read = 1, permission_write = 0 },
      { collection = data.CROPS.collection, key = data.CROPS.key, user_id = user_id, value = crops, permission_read = 1, permission_write = 0 },
    })
  end

  return view(user_id, inv, { sold = sold, earned = value })
end

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local function rpc_buy(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end

  local entry
  for _, e in ipairs(data.CATALOG) do
    if e.id == req.item then entry = e end
  end
  if not entry then error({ "unknown item", GRPC_INVALID_ARGUMENT }) end

  local items = data.read_items(user_id)
  if data.owns(items, entry) then error({ "already owned", GRPC_FAILED_PRECONDITION }) end
  if balance(user_id) < entry.price then error({ "not enough baht", GRPC_FAILED_PRECONDITION }) end

  -- Charge first (the wallet refuses to go below zero), then grant the item
  nk.wallet_update(user_id, { [CURRENCY] = -entry.price }, { reason = "buy", item = entry.id }, true)
  if entry.rod then
    items.rods[entry.rod] = true
    items.rod = entry.rod -- equip the new rod
  elseif entry.seed then
    items.seeds[entry.seed] = (items.seeds[entry.seed] or 0) + 1
  elseif entry.barn then
    items.barn_capacity = math.min(data.BARN_MAX, items.barn_capacity + data.BARN_STEP)
  elseif entry.id == "camp_membership" then
    items.camp_member = true
  end
  data.write_items(user_id, items)

  return view(user_id, read_inventory(user_id), { bought = entry.id })
end

nk.register_rpc(rpc_get, "economy_get")
nk.register_rpc(rpc_buy, "shop_buy")
nk.register_rpc(rpc_sell_fish, "shop_sell_fish")
