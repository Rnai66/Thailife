-- Economy (เงินบาท): money lives in the Nakama wallet, which clients cannot modify directly.
--   economy_get        {}            -> {baht, fish:[...], total_value, rod, rod_name, camp_member, catalog:[...]}
--   shop_sell_fish     {}            -> same, plus {sold, earned}; sells every fish in the basket
--   shop_buy           {"item": id}  -> same, plus {bought}; checks price and balance on the server
-- Fish come from fishing.lua (storage "inventory"/"fish").

local nk = require("nakama")
local data = require("shop_data")
local chickens = require("chicken_core")
local quest = require("quest_core")

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

local function catalog(items, coop_count)
  local list = {}
  for _, entry in ipairs(data.CATALOG) do
    local have = ""
    if entry.seed then have = "มีอยู่ " .. items.seeds[entry.seed] .. " ถุง" end
    if entry.barn then have = "ยุ้งจุ " .. items.barn_capacity .. " กก." end
    if entry.feed then have = "มีอยู่ " .. items.feed .. " ถุง" end
    if entry.food then have = "ในกระเป๋า " .. items.food[entry.food] end
    if entry.chicken then have = "ในเล้า " .. coop_count .. "/" .. chickens.CAPACITY .. " ตัว" end
    table.insert(list, { id = entry.id, shop = entry.shop, name = entry.name, desc = entry.desc,
                         price = entry.price, owned = data.owns(items, entry), have = have,
                         consumable = entry.consumable == true })
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
    catalog = catalog(items, chickens.count(chickens.read(user_id))),
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
    quest.progress(user_id, "sell_produce", 1)
  end

  return view(user_id, inv, { sold = sold, earned = value })
end

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local MAX_QTY = 99

local function find_entry(id)
  for _, e in ipairs(data.CATALOG) do
    if e.id == id then return e end
  end
end

-- Validate a whole cart, then charge once and grant everything. If any line is invalid,
-- nothing is bought and nothing is charged.
--   lines: { { item = id, qty = n }, ... }
local function checkout(user_id, lines)
  if type(lines) ~= "table" or #lines == 0 then error({ "empty cart", GRPC_INVALID_ARGUMENT }) end

  local items = data.read_items(user_id)
  local now = nk.time()
  local coop = chickens.settle(chickens.read(user_id), now)
  local total, new_chickens, barn_steps = 0, 0, 0
  local cart = {}

  for _, line in ipairs(lines) do
    local entry = find_entry(line.item)
    if not entry then error({ "unknown item", GRPC_INVALID_ARGUMENT }) end
    local qty = math.floor(tonumber(line.qty) or 1)
    if qty < 1 or qty > MAX_QTY then error({ "bad quantity", GRPC_INVALID_ARGUMENT }) end
    if not entry.consumable then
      if qty > 1 then error({ "only one allowed: " .. entry.id, GRPC_INVALID_ARGUMENT }) end
      if data.owns(items, entry) then error({ "already owned", GRPC_FAILED_PRECONDITION }) end
    end
    if entry.chicken then new_chickens = new_chickens + qty end
    if entry.barn then barn_steps = barn_steps + qty end
    total = total + entry.price * qty
    table.insert(cart, { entry = entry, qty = qty })
  end

  if chickens.count(coop) + new_chickens > chickens.CAPACITY then error({ "coop full", GRPC_FAILED_PRECONDITION }) end
  if items.barn_capacity + barn_steps * data.BARN_STEP > data.BARN_MAX then error({ "barn max", GRPC_FAILED_PRECONDITION }) end
  if balance(user_id) < total then error({ "not enough baht", GRPC_FAILED_PRECONDITION }) end

  -- Charge once (the wallet refuses to go below zero), then grant every line
  nk.wallet_update(user_id, { [CURRENCY] = -total }, { reason = "checkout", lines = #cart }, true)
  for _, line in ipairs(cart) do
    local entry, qty = line.entry, line.qty
    if entry.rod then
      items.rods[entry.rod] = true
      items.rod = entry.rod -- equip the new rod
    elseif entry.instrument then
      items.instruments[entry.instrument] = true
      items.instrument = entry.instrument -- play the new instrument
    elseif entry.seed then
      items.seeds[entry.seed] = (items.seeds[entry.seed] or 0) + qty
    elseif entry.feed then
      items.feed = items.feed + qty
    elseif entry.food then
      items.food[entry.food] = items.food[entry.food] + qty
    elseif entry.chicken then
      for _ = 1, qty do chickens.buy(coop, entry.chicken, now) end
    elseif entry.barn then
      items.barn_capacity = items.barn_capacity + qty * data.BARN_STEP
    elseif entry.id == "camp_membership" then
      items.camp_member = true
    end
  end
  data.write_items(user_id, items)
  if new_chickens > 0 then chickens.write(user_id, coop) end
  quest.progress(user_id, "shop", 1)
  return total, #cart
end

-- Single item (kept for older clients)
local function rpc_buy(context, payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  checkout(context.user_id, { { item = req.item, qty = 1 } })
  return view(context.user_id, read_inventory(context.user_id), { bought = req.item })
end

-- Cart: {"items": [{"item": "seed_hommali", "qty": 3}, ...]}
local function rpc_checkout(context, payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local spent, lines = checkout(context.user_id, req.items)
  return view(context.user_id, read_inventory(context.user_id), { spent = spent, bought = tostring(lines) })
end

nk.register_rpc(rpc_get, "economy_get")
nk.register_rpc(rpc_buy, "shop_buy")
nk.register_rpc(rpc_checkout, "shop_checkout")
nk.register_rpc(rpc_sell_fish, "shop_sell_fish")
