-- Daily quests (ภารกิจประจำวัน) and level quests (ภารกิจตามเลเวล), shared rules (no RPCs here;
-- quests.lua has them).
-- Every game day (36 real minutes) each player gets 3 quests picked from the jobs they can
-- actually do. Other modules report what happened with quest.progress(user_id, kind, amount);
-- progress only counts for today's quests, and rewards are claimed through quests_claim.

local nk = require("nakama")
local life = require("life_core")
local chickens = require("chicken_core")
local trade = require("trade_core")
local shop = require("shop_data")

local M = {}

M.STATE = { collection = "quests", key = "today" }
M.PER_DAY = 3
M.BONUS = 80   -- extra baht for finishing all of today's quests

-- kind -> what counts, how many, reward (baht). needs = only offered when the player can do it.
M.POOL = {
  fish         = { title = "ตกปลาให้ได้ 3 ตัว",              need = 3,  reward = 40, hint = "คลองในตำบล" },
  sell_produce = { title = "ขายปลาหรือข้าวที่ร้านค้า 1 ครั้ง", need = 1,  reward = 25, hint = "ร้านค้าชุมชน" },
  plant        = { title = "ปลูกข้าว 1 แปลง",                need = 1,  reward = 35, hint = "โคกหนองนา", needs = "free_plot" },
  harvest      = { title = "เกี่ยวข้าว 1 แปลง",              need = 1,  reward = 50, hint = "โคกหนองนา", needs = "ripe" },
  feed         = { title = "ให้อาหารไก่ 1 ครั้ง",            need = 1,  reward = 25, hint = "เล้าไก่ข้างบ้าน", needs = "chickens" },
  sell_eggs    = { title = "ขายไข่ไก่ 5 ฟอง",                need = 5,  reward = 40, hint = "เล้าไก่ข้างบ้าน", needs = "chickens" },
  music        = { title = "เล่นดนตรี 1 เพลง",               need = 1,  reward = 40, hint = "ศาลากลางหมู่บ้าน" },
  train        = { title = "ซ้อมมวย ชก 30 ครั้ง",            need = 30, reward = 30, hint = "ต้นกล้วยข้างบ้าน" },
  eat          = { title = "กินอาหาร 1 มื้อ",                need = 1,  reward = 15, hint = "ปุ่ม กิน ข้างแถบพลังงาน" },
  shop         = { title = "ซื้อของที่ร้าน 1 ครั้ง",          need = 1,  reward = 15, hint = "ร้านค้าชุมชน" },
  study        = { title = "เข้าเรียนที่โรงเรียน 1 คาบ",       need = 1,  reward = 30, hint = "โรงเรียนในตำบล" },
}
M.ORDER = { "fish", "sell_produce", "plant", "harvest", "feed", "sell_eggs", "music", "train", "eat", "shop", "study" }

-- ── Level quests (ภารกิจตามเลเวล) ───────────────────────────────────────────────────────────
-- One chapter per milestone level (career level, 1..100). A chapter opens when the player reaches its
-- level; from then on the same actions that count for daily quests count toward its tasks. Finish
-- every task to claim the chapter's big reward, once per player.
M.LEVEL_STATE = { collection = "quests", key = "levels" }

-- What each kind of task says, with %d for how many
M.TASK_TEXT = {
  fish = "ตกปลา %d ตัว", sell_produce = "ขายปลาหรือข้าว %d ครั้ง", plant = "ปลูกข้าว %d แปลง",
  harvest = "เกี่ยวข้าว %d แปลง", feed = "ให้อาหารไก่ %d ครั้ง", sell_eggs = "ขายไข่ไก่ %d ฟอง",
  music = "เล่นดนตรี %d เพลง", train = "ซ้อมมวย ชก %d ครั้ง", eat = "กินอาหาร %d มื้อ",
  shop = "ซื้อของที่ร้าน %d ครั้ง", study = "เข้าเรียน %d คาบ",
}

M.CHAPTERS = {
  { level = 1,   title = "ก้าวแรกในหมู่บ้าน", story = "เริ่มชีวิตใหม่ ทำความรู้จักหมู่บ้าน", reward = 150,
    tasks = { { "eat", 1 }, { "shop", 1 }, { "fish", 3 }, { "train", 30 } } },
  { level = 10,  title = "ลูกชาวนาขยัน", story = "ลงนา ปลูกข้าว เข้าโรงเรียน", reward = 600,
    tasks = { { "plant", 3 }, { "harvest", 3 }, { "sell_produce", 3 }, { "study", 3 } } },
  { level = 25,  title = "หัวหน้าครอบครัว", story = "เลี้ยงไก่ หาปลา หาเงินเข้าบ้าน", reward = 1500,
    tasks = { { "fish", 30 }, { "feed", 10 }, { "sell_eggs", 30 }, { "music", 5 } } },
  { level = 50,  title = "ปราชญ์ชาวบ้าน", story = "เก่งทั้งวิชา งานนา และดนตรี", reward = 4000,
    tasks = { { "study", 20 }, { "harvest", 6 }, { "music", 15 }, { "train", 1000 } } },
  { level = 75,  title = "เศรษฐีประจำตำบล", story = "ค้าขายจนเป็นที่รู้จักทั้งตำบล", reward = 8000,
    tasks = { { "fish", 100 }, { "sell_produce", 30 }, { "shop", 30 }, { "sell_eggs", 100 } } },
  { level = 100, title = "ตำนานแห่งตำบล", story = "สุดยอดคนเก่งที่ทุกคนเล่าขาน", reward = 20000,
    tasks = { { "train", 5000 }, { "study", 50 }, { "harvest", 18 }, { "music", 50 } } },
}

function M.player_level(user_id)
  local objects = nk.storage_read({ { collection = "career", key = "stats", user_id = user_id } })
  return ((#objects > 0) and objects[1].value.level) or 1
end

-- { ["10"] = { have = { plant = 2, ... }, claimed = false }, ... } keyed by chapter level
function M.read_levels(user_id)
  local objects = nk.storage_read({ { collection = M.LEVEL_STATE.collection, key = M.LEVEL_STATE.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

function M.write_levels(user_id, s)
  nk.storage_write({ {
    collection = M.LEVEL_STATE.collection, key = M.LEVEL_STATE.key, user_id = user_id, value = s,
    permission_read = 1, permission_write = 0,
  } })
end

function M.chapter_done(c, entry)
  for _, t in ipairs(c.tasks) do
    if ((entry and entry.have and entry.have[t[1]]) or 0) < t[2] then return false end
  end
  return true
end

-- Count an action toward every open, unclaimed chapter that has a task of that kind
local function level_progress(user_id, kind, amount)
  local level = M.player_level(user_id)
  local s = M.read_levels(user_id)
  local changed = false
  for _, c in ipairs(M.CHAPTERS) do
    if level >= c.level then
      local key = tostring(c.level)
      local entry = s[key] or { have = {}, claimed = false }
      if not entry.claimed then
        for _, t in ipairs(c.tasks) do
          local have = entry.have[t[1]] or 0
          if t[1] == kind and have < t[2] then
            entry.have[t[1]] = math.min(t[2], have + amount)
            s[key] = entry
            changed = true
          end
        end
      end
    end
  end
  if changed then M.write_levels(user_id, s) end
end

local function has_chickens(user_id)
  return #chickens.read(user_id).chickens > 0
end

-- A rice plot that is ripe or ripens within a game day (rice takes 120 game days to grow)
local function has_ripe(user_id)
  local objects = nk.storage_read({ { collection = "farm", key = "plots", user_id = user_id } })
  local now = nk.time()
  for _, p in pairs((#objects > 0) and objects[1].value or {}) do
    local rice = shop.RICE[p.variety or "khao_niao"] or shop.RICE.khao_niao
    if p.state == "planted" and now - (p.planted_ms or now) >= rice.grow_ms - life.DAY_MS then return true end
  end
  return false
end

-- A rice plot not already growing (6 plots at โคกหนองนา)
local function has_free_plot(user_id)
  local objects = nk.storage_read({ { collection = "farm", key = "plots", user_id = user_id } })
  local planted = 0
  for _, p in pairs((#objects > 0) and objects[1].value or {}) do
    if p.state == "planted" then planted = planted + 1 end
  end
  return planted < 6
end

local function generate(user_id, day)
  local can = { chickens = has_chickens(user_id), ripe = has_ripe(user_id), free_plot = has_free_plot(user_id) }
  local eligible = {}
  for _, kind in ipairs(M.ORDER) do
    local q = M.POOL[kind]
    if q.needs == nil or can[q.needs] then table.insert(eligible, kind) end
  end
  local list = {}
  for i = 1, M.PER_DAY do
    local kind = table.remove(eligible, math.random(#eligible))
    table.insert(list, { kind = kind, have = 0, claimed = false })
  end
  return { day = day, list = list, bonus_claimed = false }
end

-- Today's quests (a new set once the game day changes)
function M.read(user_id)
  local day = life.game_day(nk.time())
  local objects = nk.storage_read({ { collection = M.STATE.collection, key = M.STATE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or nil
  if s == nil or s.day ~= day then
    s = generate(user_id, day)
    M.write(user_id, s)
  end
  return s
end

function M.write(user_id, s)
  nk.storage_write({ {
    collection = M.STATE.collection, key = M.STATE.key, user_id = user_id, value = s,
    permission_read = 1, permission_write = 0,
  } })
end

-- Called by the other modules after an action succeeded. Never breaks the caller.
function M.progress(user_id, kind, amount)
  local ok, err = pcall(function()
    if (amount or 0) <= 0 then return end
    local s = M.read(user_id)
    local changed = false
    for _, q in ipairs(s.list) do
      local def = M.POOL[q.kind]
      if q.kind == kind and q.have < def.need then
        q.have = math.min(def.need, q.have + amount)
        changed = true
      end
    end
    if changed then M.write(user_id, s) end
    level_progress(user_id, kind, amount)
  end)
  if not ok then nk.logger_warn("quest progress failed: " .. tostring(err)) end
  trade.from_kind(user_id, kind, amount) -- profession XP for the same action
end

return M
