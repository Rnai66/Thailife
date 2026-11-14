-- Daily quest RPCs (rules in quest_core.lua).
--   quests_get   {}                          -> today's quests
--   quests_claim {"quest": 1..5} | {"bonus": true} | {"chapter": level} -> pays the reward into the wallet
--   quests_auto  {"quest": 1..5} | {"all": true} -> the character does it by itself (energy, then a timer)
-- Both return today's quests plus the level chapters (milestones) and the player's level.

local nk = require("nakama")
local quest = require("quest_core")
local life = require("life_core")
local data = require("shop_data")
local amulet = require("amulet_core")

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

-- Auto fishing brings up real fish: rolled with the equipped rod like fishing.lua, put in the fish
-- bag (inventory/fish), and listed on the quest so the client can show a card for each.
local FISH_BAG = { collection = "inventory", key = "fish" }

local function auto_fish(user_id, count)
  local items = data.read_items(user_id)
  local rod = data.RODS[items.rod] or data.RODS.bamboo
  local objects = nk.storage_read({ { collection = FISH_BAG.collection, key = FISH_BAG.key, user_id = user_id } })
  local inv = (#objects > 0) and objects[1].value or {}
  inv.counts = inv.counts or {}
  local caught = {}
  for _ = 1, count do
    local total = 0
    for _, f in ipairs(data.FISH) do total = total + (rod.weights[f.id] or f.weight) end
    local r, fish = math.random() * total, data.FISH[#data.FISH]
    for _, f in ipairs(data.FISH) do
      r = r - (rod.weights[f.id] or f.weight)
      if r <= 0 then fish = f; break end
    end
    local kg = (fish.min_kg + math.random() * (fish.max_kg - fish.min_kg)) * (rod.size or 1)
    kg = math.floor(kg * 100 + 0.5) / 100
    inv.counts[fish.id] = (inv.counts[fish.id] or 0) + 1
    inv.total = (inv.total or 0) + 1
    if kg > (inv.biggest_kg or 0) then inv.biggest_kg = kg; inv.biggest_name = fish.name end
    table.insert(caught, { id = fish.id, name = fish.name, kg = kg })
  end
  nk.storage_write({ { collection = FISH_BAG.collection, key = FISH_BAG.key, user_id = user_id, value = inv,
                       permission_read = 1, permission_write = 0 } })
  return caught
end

-- Auto quests whose time is up get finished and their rewards paid (and the all-done bonus).
-- Returns the baht paid.
local function settle_auto(user_id, s)
  local a = s.auto
  if a == nil or a.queue == nil or #a.queue == 0 then s.auto = nil; return 0 end
  local now = nk.time()
  local paid, changed = 0, false
  while #a.queue > 0 and now >= a.next_ms do
    local q = s.list[table.remove(a.queue, 1)]
    if q ~= nil then
      local def = quest.POOL[q.kind]
      if q.kind == "fish" and q.have < def.need then
        local ok, caught = pcall(auto_fish, user_id, def.need - q.have)
        if ok then q.catch = caught end
      end
      q.have = def.need
      if not q.claimed then q.claimed = true; paid = paid + def.reward end
    end
    a.next_ms = a.next_ms + quest.AUTO_SECONDS * 1000
    changed = true
  end
  if #a.queue == 0 then s.auto = nil end
  if changed then
    if all_done(s) and not s.bonus_claimed then
      s.bonus_claimed = true
      paid = paid + quest.BONUS
    end
    quest.write(user_id, s)
    paid = amulet.boost(user_id, "niyom", paid)
    if paid > 0 then nk.wallet_update(user_id, { [data.CURRENCY] = paid }, { reason = "daily_quest_auto", day = s.day }, true) end
  end
  return paid
end

-- Today's quests with any finished auto ones settled
local function read(user_id)
  local s = quest.read(user_id)
  local paid = settle_auto(user_id, s)
  return s, paid
end

local function view(s, extra, user_id)
  local list = {}
  local now = nk.time()
  -- when each queued quest finishes
  local finishes = {}
  if s.auto and s.auto.queue then
    for n, i in ipairs(s.auto.queue) do
      finishes[i] = math.max(0, math.ceil((s.auto.next_ms + (n - 1) * quest.AUTO_SECONDS * 1000 - now) / 1000))
    end
  end
  for i, q in ipairs(s.list) do
    local def = quest.POOL[q.kind]
    table.insert(list, {
      id = i, kind = q.kind, title = def.title, hint = def.hint, need = def.need,
      have = q.have, reward = def.reward, done = q.have >= def.need, claimed = q.claimed,
      auto = finishes[i] ~= nil, auto_left_s = finishes[i] or 0,
      catch = (q.catch ~= nil and #q.catch > 0) and q.catch or nil,
    })
  end
  local next_day_ms = life.EPOCH_MS + life.game_day(now) * life.DAY_MS
  local result = {
    day = s.day, quests = list, bonus = quest.BONUS, all_done = all_done(s),
    bonus_claimed = s.bonus_claimed, resets_in_s = math.max(0, math.floor((next_day_ms - now) / 1000)),
    auto_seconds = quest.AUTO_SECONDS, auto_energy = quest.AUTO_ENERGY,
  }
  result.chapters, result.level = chapters(user_id)
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

nk.register_rpc(function(context, payload)
  local s, paid = read(context.user_id)
  return view(s, { earned = paid, auto_earned = paid }, context.user_id)
end, "quests_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local s, paid = read(user_id)
  s.auto = s.auto or { queue = {}, next_ms = 0 }
  local queued = {}
  for _, i in ipairs(s.auto.queue) do queued[i] = true end
  local wanted = {}
  local one = math.floor(tonumber(req.quest) or 0)
  for i, q in ipairs(s.list) do
    if (req.all == true or i == one) and q.have < quest.POOL[q.kind].need and not queued[i] then table.insert(wanted, i) end
  end
  if #wanted == 0 then error({ "nothing to do", GRPC_FAILED_PRECONDITION }) end
  -- as many as the energy allows (at least one, or "not enough energy")
  local energy = life.state(user_id).energy or 0
  local n = math.max(1, math.min(#wanted, math.floor(energy / quest.AUTO_ENERGY)))
  life.spend(user_id, n * quest.AUTO_ENERGY)
  if #s.auto.queue == 0 then s.auto.next_ms = nk.time() + quest.AUTO_SECONDS * 1000 end
  for k = 1, n do table.insert(s.auto.queue, wanted[k]) end
  quest.write(user_id, s)
  return view(s, { earned = paid, auto_earned = paid, auto_started = n }, user_id)
end, "quests_auto")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end

  local s = read(user_id)
  local amount
  -- The client sends every field ({"quest": 2, "bonus": false, "chapter": 0}); 0 is truthy in Lua,
  -- so only a real chapter level or bonus = true picks those branches
  local chapter = tonumber(req.chapter) or 0
  if chapter > 0 then
    local c
    for _, ch in ipairs(quest.CHAPTERS) do if ch.level == chapter then c = ch end end
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
  elseif req.bonus == true then
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
  amount = amulet.boost(user_id, "niyom", amount) -- มหานิยม: a little extra for each quest
  nk.wallet_update(user_id, { [data.CURRENCY] = amount }, { reason = "daily_quest", day = s.day }, true)
  return view(s, { earned = amount }, user_id)
end, "quests_claim")
