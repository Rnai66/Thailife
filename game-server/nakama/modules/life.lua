-- Daily life RPCs (energy, sleep, food). Rules live in life_core.lua.
--   life_get    {}               -> {energy, max_energy, sleeping, foods:[{id,name,count,energy}]}
--   life_sleep  {}               -> start sleeping (energy regenerates fast; actions are blocked)
--   life_wake   {}               -> stop sleeping
--   life_eat    {"food": id}     -> eat one food from the bag, energy goes up (capped at max)

local nk = require("nakama")
local life = require("life_core")
local data = require("shop_data")

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local function view(user_id, state, extra)
  local result = life.view(state)
  local items = data.read_items(user_id)
  local foods = {}
  for _, id in ipairs(data.FOOD_ORDER) do
    local f = data.FOODS[id]
    table.insert(foods, { id = id, name = f.name, count = items.food[id] or 0, energy = f.energy })
  end
  result.foods = foods
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

nk.register_rpc(function(context, payload)
  return view(context.user_id, life.state(context.user_id))
end, "life_get")

nk.register_rpc(function(context, payload)
  return view(context.user_id, life.set_sleeping(context.user_id, true))
end, "life_sleep")

nk.register_rpc(function(context, payload)
  return view(context.user_id, life.set_sleeping(context.user_id, false))
end, "life_wake")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local food = data.FOODS[req.food or ""]
  if not food then error({ "unknown food", GRPC_INVALID_ARGUMENT }) end

  local items = data.read_items(user_id)
  if (items.food[req.food] or 0) < 1 then error({ "no such food in bag", GRPC_FAILED_PRECONDITION }) end
  local before = life.state(user_id).energy
  if before >= life.MAX_ENERGY - 1 then error({ "already full", GRPC_FAILED_PRECONDITION }) end

  items.food[req.food] = items.food[req.food] - 1
  data.write_items(user_id, items)
  local state = life.add_energy(user_id, food.energy)
  return view(user_id, state, { ate = food.name, gained = math.floor(state.energy - before) })
end, "life_eat")
