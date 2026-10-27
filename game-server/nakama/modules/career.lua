-- Boxer career (อาชีพนักมวย): training XP and levels, stored and validated on the server.
-- The client only reports "N hits on target X"; the server decides how much that is worth,
-- rate-limits hits, and enforces which training targets are unlocked.
--
-- RPCs:
--   career_get   payload {}                          -> stats
--   career_train payload {"target":"banana","hits":N} -> stats (+ "accepted" hits)

local nk = require("nakama")
local data = require("shop_data")
local quest = require("quest_core")
local life = require("life_core")
local trade = require("trade_core")

local COLLECTION = "career"
local KEY = "stats"
local MAX_LEVEL = 100

-- Training tiers: banana tree (ต้นกล้วย) -> punching bag (กระสอบทราย) -> boxing camp (ค่ายมวย)
-- energy = พลังงาน spent per hit (harder training is more tiring and worth more XP)
local TARGETS = {
  banana = { xp = 2, level = 1, energy = 0.3 },
  bag    = { xp = 4, level = 3, energy = 0.4 },
  camp   = { xp = 7, level = 6, energy = 0.5, membership = true },
}

-- Token bucket: attacks have a ~0.35 s cooldown, so ~3 hits/s is the honest maximum.
local HITS_PER_SECOND = 3
local BUCKET_SIZE = 15

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local function xp_to_next(level)
  return 40 + 20 * (level - 1)
end

local function read_stats(user_id)
  local objects = nk.storage_read({ { collection = COLLECTION, key = KEY, user_id = user_id } })
  local stats = (#objects > 0) and objects[1].value or {}
  stats.level = stats.level or 1
  stats.xp = stats.xp or 0
  stats.total_hits = stats.total_hits or 0
  stats.tokens = stats.tokens or BUCKET_SIZE
  stats.last_train_ms = stats.last_train_ms or 0
  return stats
end

local function write_stats(user_id, stats)
  -- permission_write = 0: only the server can change it, clients can read their own
  nk.storage_write({ {
    collection = COLLECTION, key = KEY, user_id = user_id, value = stats,
    permission_read = 1, permission_write = 0,
  } })
end

local function view(stats, accepted, energy, user_id)
  local result = {
    level = stats.level,
    xp = stats.xp,
    xp_to_next = (stats.level >= MAX_LEVEL) and 0 or xp_to_next(stats.level),
    total_hits = stats.total_hits,
    accepted = accepted or 0,
  }
  if energy then
    result.energy = math.floor(energy.energy)
    result.max_energy = life.MAX_ENERGY
    result.tired = energy.tired
  end
  -- The title by the player's name: their picked or best profession's rank (trade_core)
  if user_id then
    result.title, result.trade, result.trade_level = trade.title(user_id)
    result.trade_name = trade.TRADES[result.trade].name
  end
  return nk.json_encode(result)
end

local function rpc_get(context, payload)
  return view(read_stats(context.user_id), nil, nil, context.user_id)
end

local function rpc_train(context, payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then
    error({ "invalid payload", GRPC_INVALID_ARGUMENT })
  end

  local target = TARGETS[req.target]
  if target == nil then
    error({ "unknown training target", GRPC_INVALID_ARGUMENT })
  end

  local stats = read_stats(context.user_id)
  if stats.level < target.level then
    error({ "training target locked until level " .. target.level, GRPC_FAILED_PRECONDITION })
  end
  if target.membership and not data.read_items(context.user_id).camp_member then
    error({ "boxing camp membership required", GRPC_FAILED_PRECONDITION })
  end

  -- Refill the bucket for the time since the last report, then spend it on the reported hits
  local now = nk.time()
  if stats.last_train_ms > 0 then
    local elapsed = math.max(0, (now - stats.last_train_ms) / 1000)
    stats.tokens = math.min(BUCKET_SIZE, stats.tokens + elapsed * HITS_PER_SECOND)
  end
  stats.last_train_ms = now

  local requested = math.max(0, math.floor(tonumber(req.hits) or 0))
  local accepted = math.min(requested, math.floor(stats.tokens))

  -- Training is tiring: only the hits you still have energy for count
  local body = life.state(context.user_id)
  if body.sleep_ms then error({ "sleeping", GRPC_FAILED_PRECONDITION }) end
  if body.injured then error({ "injured", GRPC_FAILED_PRECONDITION }) end
  local affordable = math.floor(body.energy / target.energy)
  local tired = accepted > affordable
  accepted = math.min(accepted, affordable)
  if accepted == 0 and requested > 0 then error({ "not enough energy", GRPC_FAILED_PRECONDITION }) end
  if accepted > 0 then body = life.spend(context.user_id, accepted * target.energy) end
  stats.tokens = stats.tokens - accepted

  stats.total_hits = stats.total_hits + accepted
  stats.xp = stats.xp + accepted * target.xp
  while stats.level < MAX_LEVEL and stats.xp >= xp_to_next(stats.level) do
    stats.xp = stats.xp - xp_to_next(stats.level)
    stats.level = stats.level + 1
  end
  if stats.level >= MAX_LEVEL then
    stats.xp = 0
  end

  write_stats(context.user_id, stats)
  quest.progress(context.user_id, "train", accepted)
  return view(stats, accepted, { energy = body.energy, tired = tired }, context.user_id)
end

nk.register_rpc(rpc_get, "career_get")
nk.register_rpc(rpc_train, "career_train")
