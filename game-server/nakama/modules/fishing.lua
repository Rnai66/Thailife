-- Fishing (ตกปลา) at the tambon canal. The server owns the timing and the catch:
--   fishing_cast  {}  -> {"bite_in_ms": N}           server picks when the fish bites
--   fishing_reel  {}  -> {"caught": bool, ...}       server checks the timing and rolls the fish
--   fishing_get   {}  -> inventory summary
-- The client only shows the bobber and asks; it can't choose the fish or skip the wait.

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")

local STATE = { collection = "fishing", key = "state" }
local INVENTORY = { collection = "inventory", key = "fish" }

local MIN_BITE_MS, MAX_BITE_MS = 2000, 6000
local EARLY_SLACK_MS = 300   -- network jitter before the bite
local LATE_WINDOW_MS = 2200  -- reaction time + round trip after the bite
local MIN_CAST_INTERVAL_MS = 1000

local GRPC_FAILED_PRECONDITION = 9
local GRPC_RESOURCE_EXHAUSTED = 8

-- Thai freshwater fish: weight = how common, kg = size range
local FISH = {
  { id = "tilapia",   name = "ปลานิล",     weight = 40, min_kg = 0.3, max_kg = 1.2 },
  { id = "barb",      name = "ปลาตะเพียน", weight = 30, min_kg = 0.2, max_kg = 0.8 },
  { id = "snakehead", name = "ปลาช่อน",    weight = 20, min_kg = 0.5, max_kg = 2.5 },
  { id = "catfish",   name = "ปลาดุก",     weight = 10, min_kg = 0.4, max_kg = 1.8 },
}

local function read(user_id, spec)
  local objects = nk.storage_read({ { collection = spec.collection, key = spec.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or nil
end

local function write(user_id, spec, value)
  nk.storage_write({ {
    collection = spec.collection, key = spec.key, user_id = user_id, value = value,
    permission_read = 1, permission_write = 0,
  } })
end

local function inventory_view(inv)
  return { total = inv.total or 0, counts = inv.counts or {}, biggest_kg = inv.biggest_kg or 0, biggest_name = inv.biggest_name or "" }
end

-- Pick a fish using the equipped rod's weights (falls back to the base weights)
local function roll_fish(rod)
  local weights = (rod and rod.weights) or {}
  local total = 0
  for _, f in ipairs(FISH) do total = total + (weights[f.id] or f.weight) end
  local r = math.random() * total
  for _, f in ipairs(FISH) do
    r = r - (weights[f.id] or f.weight)
    if r <= 0 then return f end
  end
  return FISH[#FISH]
end

local function equipped_rod(user_id)
  return data.RODS[data.read_items(user_id).rod] or data.RODS.bamboo
end

local function rpc_cast(context, payload)
  local now = nk.time()
  local state = read(context.user_id, STATE)
  if state and state.cast_ms and now - state.cast_ms < MIN_CAST_INTERVAL_MS then
    error({ "casting too fast", GRPC_RESOURCE_EXHAUSTED })
  end

  life.spend(context.user_id, life.COST.cast)
  local rod = equipped_rod(context.user_id)
  local bite_in = math.random(rod.bite_min or MIN_BITE_MS, rod.bite_max or MAX_BITE_MS)
  write(context.user_id, STATE, { cast_ms = now, bite_ms = now + bite_in })
  return nk.json_encode({ bite_in_ms = bite_in })
end

local function rpc_reel(context, payload)
  local now = nk.time()
  local state = read(context.user_id, STATE)
  if not state or not state.bite_ms then
    error({ "no line in the water", GRPC_FAILED_PRECONDITION })
  end
  nk.storage_delete({ { collection = STATE.collection, key = STATE.key, user_id = context.user_id } })

  local inv = read(context.user_id, INVENTORY) or {}
  if now < state.bite_ms - EARLY_SLACK_MS then
    return nk.json_encode({ caught = false, reason = "early", inventory = inventory_view(inv) })
  end
  if now > state.bite_ms + LATE_WINDOW_MS then
    return nk.json_encode({ caught = false, reason = "late", inventory = inventory_view(inv) })
  end

  local rod = equipped_rod(context.user_id)
  local fish = roll_fish(rod)
  local kg = (fish.min_kg + math.random() * (fish.max_kg - fish.min_kg)) * (rod.size or 1)
  kg = math.floor(kg * 100 + 0.5) / 100

  inv.counts = inv.counts or {}
  inv.counts[fish.id] = (inv.counts[fish.id] or 0) + 1
  inv.total = (inv.total or 0) + 1
  if kg > (inv.biggest_kg or 0) then
    inv.biggest_kg = kg
    inv.biggest_name = fish.name
  end
  write(context.user_id, INVENTORY, inv)

  return nk.json_encode({ caught = true, fish_id = fish.id, fish_name = fish.name, kg = kg, inventory = inventory_view(inv) })
end

local function rpc_get(context, payload)
  return nk.json_encode({ inventory = inventory_view(read(context.user_id, INVENTORY) or {}) })
end

nk.register_rpc(rpc_cast, "fishing_cast")
nk.register_rpc(rpc_reel, "fishing_reel")
nk.register_rpc(rpc_get, "fishing_get")
