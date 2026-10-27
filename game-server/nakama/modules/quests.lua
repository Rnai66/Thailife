-- Daily quest RPCs (rules in quest_core.lua).
--   quests_get   {}                          -> today's quests
--   quests_claim {"quest": 1..3} | {"bonus": true} | {"chapter": level} -> pays the reward into the wallet
-- Both return today's quests plus the level chapters (milestones) and the player's level.

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

-- Level chapters: open once the player's level reaches them; tasks with progress; claimable when all done
local function chapters(user_id)
  local level = quest.player_level(user_id)
  local s = quest.read_levels(user_id)
  local out = {}
  for _, c in ipairs(quest.CHAPTERS) do
    local entry = s[tostring(c.level)]
    local tasks = {}
    for _, t in ipairs(c.tasks) do
      table.insert(tasks, { kind = t[1], title = string.format(quest.TASK_TEXT[t[1]], t[2]), need = t[2],
                            have = (entry and entry.have and entry.have[t[1]]) or 0 })
    end
    table.insert(out, {
      level = c.level, title = c.title, story = c.story, reward = c.reward, tasks = tasks,
      unlocked = level >= c.level, done = level >= c.level and quest.chapter_done(c, entry),
      claimed = entry ~= nil and entry.claimed == true,
    })
  end
  return out, level
end

local function view(s, extra, user_id)
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
  result.chapters, result.level = chapters(user_id)
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

nk.register_rpc(function(context, payload)
  return view(quest.read(context.user_id), nil, context.user_id)
end, "quests_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end

  local s = quest.read(user_id)
  local amount
  if req.chapter then
    local c
    for _, ch in ipairs(quest.CHAPTERS) do if ch.level == tonumber(req.chapter) then c = ch end end
    if c == nil then error({ "unknown chapter", GRPC_INVALID_ARGUMENT }) end
    if quest.player_level(user_id) < c.level then error({ "chapter locked", GRPC_FAILED_PRECONDITION }) end
    local levels = quest.read_levels(user_id)
    local entry = levels[tostring(c.level)]
    if not quest.chapter_done(c, entry) then error({ "quest not finished", GRPC_FAILED_PRECONDITION }) end
    if entry.claimed then error({ "already claimed", GRPC_FAILED_PRECONDITION }) end
    entry.claimed = true
    quest.write_levels(user_id, levels)
    nk.wallet_update(user_id, { [data.CURRENCY] = c.reward }, { reason = "level_quest", chapter = c.level }, true)
    return view(s, { earned = c.reward }, user_id)
  elseif req.bonus then
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
  return view(s, { earned = amount }, user_id)
end, "quests_claim")
