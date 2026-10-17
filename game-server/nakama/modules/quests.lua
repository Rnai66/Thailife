-- Daily quest RPCs (rules in quest_core.lua).
--   quests_get   {}                          -> today's quests
--   quests_claim {"quest": 1..3} | {"bonus": true} -> pays the reward into the wallet

local nk = require("nakama")
local quest = require("quest_core")
local life = require("life_core")
local data = require("shop_data")

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local function all_done(s)
  for _, q in ipairs(s.list) do
    if q.have < quest.POOL[q.kind].need then return false end
  end
  return true
end

local function view(s, extra)
  local list = {}
  for i, q in ipairs(s.list) do
    local def = quest.POOL[q.kind]
    table.insert(list, {
      id = i, kind = q.kind, title = def.title, hint = def.hint, need = def.need,
      have = q.have, reward = def.reward, done = q.have >= def.need, claimed = q.claimed,
    })
  end
  local now = nk.time()
  local next_day_ms = life.EPOCH_MS + life.game_day(now) * life.DAY_MS
  local result = {
    day = s.day, quests = list, bonus = quest.BONUS, all_done = all_done(s),
    bonus_claimed = s.bonus_claimed, resets_in_s = math.max(0, math.floor((next_day_ms - now) / 1000)),
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

nk.register_rpc(function(context, payload)
  return view(quest.read(context.user_id))
end, "quests_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end

  local s = quest.read(user_id)
  local amount
  if req.bonus then
    if not all_done(s) then error({ "quests not finished", GRPC_FAILED_PRECONDITION }) end
    if s.bonus_claimed then error({ "already claimed", GRPC_FAILED_PRECONDITION }) end
    s.bonus_claimed = true
    amount = quest.BONUS
  else
    local q = s.list[math.floor(tonumber(req.quest) or 0)]
    if q == nil then error({ "unknown quest", GRPC_INVALID_ARGUMENT }) end
    local def = quest.POOL[q.kind]
    if q.have < def.need then error({ "quest not finished", GRPC_FAILED_PRECONDITION }) end
    if q.claimed then error({ "already claimed", GRPC_FAILED_PRECONDITION }) end
    q.claimed = true
    amount = def.reward
  end

  quest.write(user_id, s)
  nk.wallet_update(user_id, { [data.CURRENCY] = amount }, { reason = "daily_quest", day = s.day }, true)
  return view(s, { earned = amount })
end, "quests_claim")
