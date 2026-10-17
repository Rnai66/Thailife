-- Rice farming (ทำนา) on the Kok Nong Na paddies. Every player has their own 6 plots.
--   farm_get  {}  -> {plots:[...], seeds:{...}, barn:{...}, varieties:[...], energy...}
--   farm_act  {"plot": 1..6, "action": "plow"|"plant"|"water"|"harvest", "variety": "khao_niao"|"hommali"}
-- Stages: empty -> plowed -> growing -> ripe -> (harvest into the barn) -> empty
-- Planting uses one seed bag bought at the shop (shop_data.RICE). Harvest needs room in the
-- rice barn (ยุ้งข้าว). Growth runs on real time, so rice keeps growing while the game is closed.

local nk = require("nakama")
local life = require("life_core")
local data = require("shop_data")
local quest = require("quest_core")

local FARM = { collection = "farm", key = "plots" }
local PLOTS = 6

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local function read_farm(user_id)
  local objects = nk.storage_read({ { collection = FARM.collection, key = FARM.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function write_farm(user_id, farm)
  nk.storage_write({ {
    collection = FARM.collection, key = FARM.key, user_id = user_id, value = farm,
    permission_read = 1, permission_write = 0,
  } })
end

local function variety_of(p)
  return data.RICE[p.variety or "khao_niao"] or data.RICE.khao_niao
end

local function plot_stage(p, now)
  if not p or not p.state or p.state == "empty" then return "empty", 0 end
  if p.state == "plowed" then return "plowed", 0 end
  local progress = math.min(1, (now - p.planted_ms) / variety_of(p).grow_ms)
  return (progress >= 1) and "ripe" or "growing", progress
end

local function view(user_id, farm, extra)
  local now = nk.time()
  local plots = {}
  for i = 1, PLOTS do
    local p = farm[tostring(i)] or {}
    local stage, progress = plot_stage(p, now)
    local grow_ms = variety_of(p).grow_ms
    local ripe_in = (stage == "growing") and math.ceil((p.planted_ms + grow_ms - now) / 1000) or 0
    table.insert(plots, {
      id = i, stage = stage, progress = progress, watered = p.watered == true, ripe_in_s = ripe_in,
      variety = (stage ~= "empty" and stage ~= "plowed") and (p.variety or "khao_niao") or "",
      variety_name = (stage ~= "empty" and stage ~= "plowed") and variety_of(p).name or "",
      grow_s = grow_ms / 1000,
    })
  end

  local items = data.read_items(user_id)
  local crops, stored = data.read_barn(user_id)
  local varieties = {}
  for _, v in ipairs(data.RICE_ORDER) do
    local r = data.RICE[v]
    table.insert(varieties, { id = v, name = r.name, seeds = items.seeds[v], stored_kg = crops.rice[v],
                              seed_price = r.seed_price, grow_s = r.grow_ms / 1000, price_per_kg = r.price_per_kg })
  end

  local result = life.view(life.state(user_id))
  result.plots = plots
  result.varieties = varieties
  result.barn_capacity = items.barn_capacity
  result.barn_total = stored
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function rpc_get(context, payload)
  return view(context.user_id, read_farm(context.user_id))
end

local function rpc_act(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end

  local id = math.floor(tonumber(req.plot) or 0)
  if id < 1 or id > PLOTS then error({ "unknown plot", GRPC_INVALID_ARGUMENT }) end

  local farm = read_farm(user_id)
  local key = tostring(id)
  local p = farm[key] or { state = "empty" }
  local stage = plot_stage(p, nk.time())
  local action = req.action
  local extra = { action = action, plot = id }

  if action == "plow" then
    if stage ~= "empty" then error({ "plot is not empty", GRPC_FAILED_PRECONDITION }) end
    life.spend(user_id, life.COST.plow)
    p = { state = "plowed" }

  elseif action == "plant" then
    if stage ~= "plowed" then error({ "plow first", GRPC_FAILED_PRECONDITION }) end
    local variety = req.variety or ""
    if not data.RICE[variety] then error({ "unknown rice variety", GRPC_INVALID_ARGUMENT }) end
    local items = data.read_items(user_id)
    if (items.seeds[variety] or 0) < 1 then error({ "no seeds", GRPC_FAILED_PRECONDITION }) end
    life.spend(user_id, life.COST.plant)
    items.seeds[variety] = items.seeds[variety] - 1
    data.write_items(user_id, items)
    p = { state = "planted", variety = variety, planted_ms = nk.time(), watered = false }
    quest.progress(user_id, "plant", 1)

  elseif action == "water" then
    if stage ~= "growing" then error({ "nothing growing", GRPC_FAILED_PRECONDITION }) end
    if p.watered then error({ "already watered", GRPC_FAILED_PRECONDITION }) end
    life.spend(user_id, life.COST.water)
    p.watered = true

  elseif action == "harvest" then
    if stage ~= "ripe" then error({ "not ripe yet", GRPC_FAILED_PRECONDITION }) end
    local rice = variety_of(p)
    local range = p.watered and rice.yield_watered or rice.yield_dry
    local kg = math.random(range[1], range[2])

    -- The barn must have room for the whole harvest
    local items = data.read_items(user_id)
    local crops, stored = data.read_barn(user_id)
    if stored + kg > items.barn_capacity then
      error({ "barn full", GRPC_FAILED_PRECONDITION })
    end

    life.spend(user_id, life.COST.harvest)
    local variety = p.variety or "khao_niao"
    crops.rice[variety] = crops.rice[variety] + kg
    data.write_barn(user_id, crops)
    extra.harvested_kg = kg
    extra.harvested_name = rice.name
    quest.progress(user_id, "harvest", 1)
    p = { state = "empty" }

  else
    error({ "unknown action", GRPC_INVALID_ARGUMENT })
  end

  farm[key] = p
  write_farm(user_id, farm)
  return view(user_id, farm, extra)
end

nk.register_rpc(rpc_get, "farm_get")
nk.register_rpc(rpc_act, "farm_act")
