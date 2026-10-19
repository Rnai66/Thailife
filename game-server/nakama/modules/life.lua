-- Daily life RPCs (energy, sleep, food). Rules live in life_core.lua.
--   life_get    {}               -> {energy, max_energy, sleeping, foods:[{id,name,count,energy}]}
--   life_sleep  {"spot": id?}    -> start sleeping (energy regenerates fast; actions are blocked);
--                                   paid spots (beach chairs) charge their rent first
--   life_wake   {}               -> stop sleeping
--   life_eat    {"food": id}     -> eat one food from the bag, energy goes up (capped at max)

local nk = require("nakama")
local life = require("life_core")
local data = require("shop_data")
local quest = require("quest_core")

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

-- Rest spots you pay for (rent per rest)
local PAID_SPOTS = {
  beach_chair = { price = 40, name = "เช่าเก้าอี้ชายหาด" },
}

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then req = {} end
  local spot = PAID_SPOTS[req.spot or ""]
  local extra
  if spot then
    local state = life.state(user_id)
    if state.sleep_ms then error({ "already sleeping", GRPC_FAILED_PRECONDITION }) end
    local account = nk.account_get_id(user_id)
    local w = account.wallet or {}
    if type(w) == "string" then w = nk.json_decode(w) end
    if (w[data.CURRENCY] or 0) < spot.price then error({ "not enough baht", GRPC_FAILED_PRECONDITION }) end
    nk.wallet_update(user_id, { [data.CURRENCY] = -spot.price }, { reason = "rest_rent", spot = req.spot }, true)
    extra = { paid = spot.price }
  end
  return view(user_id, life.set_sleeping(user_id, true), extra)
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
  quest.progress(user_id, "eat", 1)
  return view(user_id, state, { ate = food.name, gained = math.floor(state.energy - before) })
end, "life_eat")

-- Treatment (รักษาตัว): the health station is cheap, the hospital also restores full energy
local CLINICS = {
  station  = { name = "รพ.สต.",         price = 50 },
  hospital = { name = "โรงพยาบาล",      price = 200, full_energy = true },
}

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local clinic = CLINICS[req.place or ""]
  if not clinic then error({ "unknown clinic", GRPC_INVALID_ARGUMENT }) end
  if not life.injured(user_id) then error({ "not injured", GRPC_FAILED_PRECONDITION }) end

  local account = nk.account_get_id(user_id)
  local wallet = account.wallet or {}
  if type(wallet) == "string" then wallet = nk.json_decode(wallet) end
  if (wallet[data.CURRENCY] or 0) < clinic.price then error({ "not enough baht", GRPC_FAILED_PRECONDITION }) end

  nk.wallet_update(user_id, { [data.CURRENCY] = -clinic.price }, { reason = "treatment", place = req.place }, true)
  local state = life.set_injured(user_id, false)
  if clinic.full_energy then state = life.add_energy(user_id, life.MAX_ENERGY) end
  return view(user_id, state, { treated_at = clinic.name, paid = clinic.price })
end, "life_heal")
