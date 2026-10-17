-- Shared "daily life" rules (no RPCs here): game clock and energy (พลังงาน).
-- require("life_core") from life.lua, farming.lua, fishing.lua.
--
-- Game clock: 1 real second = 1 game minute, so one game day = 24 real minutes.
-- Every client computes the same clock from UTC time; the server uses it for nothing secret.

local nk = require("nakama")

local M = {}

M.MAX_ENERGY = 100
M.PASSIVE_REGEN_PER_MIN = 1   -- real minutes
M.SLEEP_REGEN_PER_SEC = 3     -- real seconds while sleeping

M.COST = { plow = 10, plant = 10, water = 5, harvest = 10, cast = 3 }

-- Calendar (same epoch as the client's GameClock): 1 game day = 24 real minutes,
-- 1 game month = 30 game days = 12 real hours.
M.EPOCH_MS = 1767225600000   -- 2026-01-01T00:00:00Z
M.DAY_MS = 24 * 60 * 1000
M.MONTH_DAYS = 30

function M.game_day(now)   -- 1-based
  return math.floor((now - M.EPOCH_MS) / M.DAY_MS) + 1
end

-- Temple fair (งานวัด): days 1-2 of every game month.
-- Returns open, fair_id (month number), ends_in_ms, next_in_ms
M.FAIR_DAYS = 2
function M.fair(now, always_open)
  local day0 = math.floor((now - M.EPOCH_MS) / M.DAY_MS)        -- 0-based game day
  local month = math.floor(day0 / M.MONTH_DAYS)
  local month_start = M.EPOCH_MS + month * M.MONTH_DAYS * M.DAY_MS
  local fair_end = month_start + M.FAIR_DAYS * M.DAY_MS
  local next_start = month_start + M.MONTH_DAYS * M.DAY_MS
  if always_open then return true, month, M.DAY_MS, next_start - now end
  if now < fair_end then return true, month, fair_end - now, next_start - now end
  return false, month, 0, next_start - now
end

local LIFE = { collection = "life", key = "state" }
local GRPC_FAILED_PRECONDITION = 9

local function read(user_id)
  local objects = nk.storage_read({ { collection = LIFE.collection, key = LIFE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.energy = s.energy or M.MAX_ENERGY
  s.updated_ms = s.updated_ms or nk.time()
  return s
end

local function write(user_id, s)
  nk.storage_write({ {
    collection = LIFE.collection, key = LIFE.key, user_id = user_id, value = s,
    permission_read = 1, permission_write = 0,
  } })
end

-- Bring energy up to date (passive regen, or sleep regen while sleeping)
local function settle(s, now)
  local elapsed_s = math.max(0, (now - s.updated_ms) / 1000)
  local gain = s.sleep_ms and (elapsed_s * M.SLEEP_REGEN_PER_SEC) or (elapsed_s / 60 * M.PASSIVE_REGEN_PER_MIN)
  s.energy = math.min(M.MAX_ENERGY, s.energy + gain)
  s.updated_ms = now
  return s
end

function M.state(user_id)
  local s = settle(read(user_id), nk.time())
  write(user_id, s)
  return s
end

-- Spend energy or raise a FAILED_PRECONDITION error ("not enough energy")
function M.spend(user_id, amount)
  local s = settle(read(user_id), nk.time())
  if s.sleep_ms then error({ "sleeping", GRPC_FAILED_PRECONDITION }) end
  if s.energy < amount then error({ "not enough energy", GRPC_FAILED_PRECONDITION }) end
  s.energy = s.energy - amount
  write(user_id, s)
  return s
end

function M.add_energy(user_id, amount)
  local s = settle(read(user_id), nk.time())
  s.energy = math.min(M.MAX_ENERGY, s.energy + amount)
  write(user_id, s)
  return s
end

function M.set_sleeping(user_id, sleeping)
  local s = settle(read(user_id), nk.time())
  s.sleep_ms = sleeping and nk.time() or nil
  write(user_id, s)
  return s
end

function M.view(s)
  return { energy = math.floor(s.energy), max_energy = M.MAX_ENERGY, sleeping = s.sleep_ms ~= nil }
end

return M
