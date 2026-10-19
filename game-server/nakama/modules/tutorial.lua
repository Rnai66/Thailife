-- Starter quest (ภารกิจเริ่มต้น) for new players: a short chain through the basics, one step at a
-- time, a small reward per step and a bonus at the end. The client notices when a step is done;
-- the server only pays the step that is current, once, so rewards can't be farmed.
--
--   tutorial_get   {}
--   tutorial_done  {"step": n}   -> pays step n (must be the current one) and moves on
--   tutorial_skip  {}            -> stop showing it (no rewards)

local nk = require("nakama")
local data = require("shop_data")

local STATE = { collection = "tutorial", key = "progress" }
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local STEPS = 7
local STEP_REWARD = 50
local FINAL_BONUS = 300

local function read(user_id)
  local objects = nk.storage_read({ { collection = STATE.collection, key = STATE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.step = s.step or 1
  return s
end

local function write(user_id, s)
  nk.storage_write({ { collection = STATE.collection, key = STATE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

local function view(s, extra)
  local r = { step = s.step, steps = STEPS, done = s.step > STEPS or s.skipped == true,
              step_reward = STEP_REWARD, final_bonus = FINAL_BONUS }
  for k, v in pairs(extra or {}) do r[k] = v end
  return nk.json_encode(r)
end

nk.register_rpc(function(context)
  return view(read(context.user_id))
end, "tutorial_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local s = read(user_id)
  if s.skipped or s.step > STEPS then error({ "tutorial finished", GRPC_FAILED_PRECONDITION }) end
  if math.floor(tonumber(req.step) or 0) ~= s.step then error({ "not the current step", GRPC_FAILED_PRECONDITION }) end
  local reward = STEP_REWARD + (s.step == STEPS and FINAL_BONUS or 0)
  nk.wallet_update(user_id, { [data.CURRENCY] = reward }, { reason = "tutorial", step = s.step }, true)
  s.step = s.step + 1
  write(user_id, s)
  return view(s, { reward = reward })
end, "tutorial_done")

nk.register_rpc(function(context)
  local s = read(context.user_id)
  s.skipped = true
  write(context.user_id, s)
  return view(s)
end, "tutorial_skip")
