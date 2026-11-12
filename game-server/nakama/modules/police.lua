-- ตำรวจจราจร: fines for driving your own vehicle too fast past a speed trap (ตั้งด่านจับความเร็ว) or
-- running a checkpoint (ฝ่าด่านตรวจ). The client sees it happen and asks for the ticket; the server
-- checks the numbers make sense, keeps to one ticket every POLICE_GAP seconds and takes the money.
-- No money for the fine: a warning (ตักเตือน) instead.
--
--   police_fine {kind = "speed", speed, limit} | {kind = "checkpoint", speed}
--       -> { kind, speed, limit, fine, paid, warning }

local nk = require("nakama")
local data = require("shop_data")

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local POLICE_GAP = 90 * 1000             -- ms between two tickets
local LIMITS = { [30] = true, [40] = true, [50] = true, [60] = true, [80] = true, [90] = true, [100] = true, [120] = true }
local CHECKPOINT_SPEED = 20              -- km/h: faster than this through a checkpoint is running it
local CHECKPOINT_FINE = 1000
local OVER = 10                          -- km/h over the limit before a ticket

-- ตามอัตราค่าปรับ: by how far over the limit
local function speed_fine(over)
  if over <= 20 then return 500 end
  if over <= 40 then return 1000 end
  return 2000
end

local function wallet(user_id)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[data.CURRENCY] or 0
end

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local kind = req.kind
  local speed = math.floor(tonumber(req.speed) or -1)
  if speed < 0 or speed > 250 then error({ "bad speed", GRPC_INVALID_ARGUMENT }) end

  local fine, limit
  if kind == "speed" then
    limit = math.floor(tonumber(req.limit) or 0)
    if not LIMITS[limit] then error({ "bad limit", GRPC_INVALID_ARGUMENT }) end
    if speed <= limit + OVER then error({ "not speeding", GRPC_FAILED_PRECONDITION }) end
    fine = speed_fine(speed - limit)
  elseif kind == "checkpoint" then
    if speed <= CHECKPOINT_SPEED then error({ "not speeding", GRPC_FAILED_PRECONDITION }) end
    limit = CHECKPOINT_SPEED
    fine = CHECKPOINT_FINE
  else
    error({ "unknown kind", GRPC_INVALID_ARGUMENT })
  end

  local now = nk.time()
  local objects = nk.storage_read({ { collection = "police", key = "record", user_id = user_id } })
  local rec = (#objects > 0) and objects[1].value or {}
  if rec.last and now - rec.last < POLICE_GAP then error({ "too soon", GRPC_FAILED_PRECONDITION }) end

  local paid, warning = 0, false
  if wallet(user_id) >= fine then
    nk.wallet_update(user_id, { [data.CURRENCY] = -fine }, { reason = "police_" .. kind, speed = speed, limit = limit }, true)
    paid = fine
  else
    warning = true
  end
  rec.last = now
  rec.tickets = (rec.tickets or 0) + (warning and 0 or 1)
  rec.warnings = (rec.warnings or 0) + (warning and 1 or 0)
  nk.storage_write({ { collection = "police", key = "record", user_id = user_id, value = rec, permission_read = 1, permission_write = 0 } })
  return nk.json_encode({ kind = kind, speed = speed, limit = limit, fine = fine, paid = paid, warning = warning })
end, "police_fine")
