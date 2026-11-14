-- Sharing the level-up card (การ์ดเลเวลอัป) to Facebook / LINE / any app pays a reward once per level:
--   share_levelup {"level": N, "app": "<activity type>"} -> {"baht", "xp", "level", "xp_now", "xp_to_next", "leveled_up"}
-- Only the player's current level (or the one just before it, in case the reward itself levelled them
-- up) can be claimed, each level once, and at most DAILY claims a real day.

local nk = require("nakama")
local data = require("shop_data")

local STORE = { collection = "share", key = "levelup" }
local CAREER = { collection = "career", key = "stats" }
local DAILY = 3
local MAX_LEVEL = 100 -- career.lua MAX_LEVEL

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

-- same curve as career.lua
local function xp_to_next(level) return 40 + 20 * (level - 1) end

local function reward_for(level) return 100 + 10 * level, 20 + level end

local function rpc_share(context, payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local level = math.floor(tonumber(req.level) or 0)
  local user_id = context.user_id

  local objects = nk.storage_read({ { collection = CAREER.collection, key = CAREER.key, user_id = user_id },
                                    { collection = STORE.collection, key = STORE.key, user_id = user_id } })
  local stats, s
  for _, o in ipairs(objects) do
    if o.collection == CAREER.collection then stats = o.value else s = o.value end
  end
  stats = stats or {}
  stats.level = stats.level or 1
  stats.xp = stats.xp or 0
  s = s or {}
  s.levels = s.levels or {}

  if level < 2 or level > stats.level or level < stats.level - 1 then error({ "not your level", GRPC_FAILED_PRECONDITION }) end
  if s.levels[tostring(level)] then error({ "already rewarded", GRPC_FAILED_PRECONDITION }) end
  local day = math.floor(nk.time() / 86400000)
  if s.day ~= day then s.day = day; s.count = 0 end
  if (s.count or 0) >= DAILY then error({ "daily limit", GRPC_FAILED_PRECONDITION }) end

  local baht, xp = reward_for(level)
  local before = stats.level
  stats.xp = stats.xp + xp
  while stats.level < MAX_LEVEL and stats.xp >= xp_to_next(stats.level) do
    stats.xp = stats.xp - xp_to_next(stats.level)
    stats.level = stats.level + 1
  end
  if stats.level >= MAX_LEVEL then stats.xp = 0 end

  s.levels[tostring(level)] = true
  s.count = (s.count or 0) + 1
  nk.storage_write({
    { collection = CAREER.collection, key = CAREER.key, user_id = user_id, value = stats, permission_read = 1, permission_write = 0 },
    { collection = STORE.collection, key = STORE.key, user_id = user_id, value = s, permission_read = 1, permission_write = 0 },
  })
  nk.wallet_update(user_id, { [data.CURRENCY] = baht }, { reason = "share_levelup", level = level, app = tostring(req.app or "") }, true)

  return nk.json_encode({
    baht = baht, xp = xp, level = stats.level, xp_now = stats.xp,
    xp_to_next = (stats.level >= MAX_LEVEL) and 0 or xp_to_next(stats.level),
    leveled_up = stats.level > before, left_today = DAILY - s.count,
  })
end

nk.register_rpc(rpc_share, "share_levelup")
