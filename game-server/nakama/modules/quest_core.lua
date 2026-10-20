-- Daily quests (ภารกิจประจำวัน), shared rules (no RPCs here; quests.lua has them).
-- Every game day (24 real minutes) each player gets 3 quests picked from the jobs they can
-- actually do. Other modules report what happened with quest.progress(user_id, kind, amount);
-- progress only counts for today's quests, and rewards are claimed through quests_claim.

local nk = require("nakama")
local life = require("life_core")
local chickens = require("chicken_core")

local M = {}

M.STATE = { collection = "quests", key = "today" }
M.PER_DAY = 3
M.BONUS = 80   -- extra baht for finishing all of today's quests

-- kind -> what counts, how many, reward (baht). needs = only offered when the player can do it.
M.POOL = {
  fish         = { title = "ตกปลาให้ได้ 3 ตัว",              need = 3,  reward = 40, hint = "คลองในตำบล" },
  sell_produce = { title = "ขายปลาหรือข้าวที่ร้านค้า 1 ครั้ง", need = 1,  reward = 25, hint = "ร้านค้าชุมชน" },
  plant        = { title = "ปลูกข้าว 1 แปลง",                need = 1,  reward = 35, hint = "โคกหนองนา" },
  harvest      = { title = "เกี่ยวข้าว 1 แปลง",              need = 1,  reward = 50, hint = "โคกหนองนา", needs = "planted" },
  feed         = { title = "ให้อาหารไก่ 1 ครั้ง",            need = 1,  reward = 25, hint = "เล้าไก่ข้างบ้าน", needs = "chickens" },
  sell_eggs    = { title = "ขายไข่ไก่ 5 ฟอง",                need = 5,  reward = 40, hint = "เล้าไก่ข้างบ้าน", needs = "chickens" },
  music        = { title = "เล่นดนตรี 1 เพลง",               need = 1,  reward = 40, hint = "ศาลากลางหมู่บ้าน" },
  train        = { title = "ซ้อมมวย ชก 30 ครั้ง",            need = 30, reward = 30, hint = "ต้นกล้วยข้างบ้าน" },
  eat          = { title = "กินอาหาร 1 มื้อ",                need = 1,  reward = 15, hint = "ปุ่ม กิน ข้างแถบพลังงาน" },
  shop         = { title = "ซื้อของที่ร้าน 1 ครั้ง",          need = 1,  reward = 15, hint = "ร้านค้าชุมชน" },
  study        = { title = "เข้าเรียนที่โรงเรียน 1 คาบ",       need = 1,  reward = 30, hint = "โรงเรียนในตำบล" },
}
M.ORDER = { "fish", "sell_produce", "plant", "harvest", "feed", "sell_eggs", "music", "train", "eat", "shop", "study" }

local function has_chickens(user_id)
  return #chickens.read(user_id).chickens > 0
end

local function has_planted(user_id)
  local objects = nk.storage_read({ { collection = "farm", key = "plots", user_id = user_id } })
  for _, p in pairs((#objects > 0) and objects[1].value or {}) do
    if p.state == "planted" then return true end
  end
  return false
end

local function generate(user_id, day)
  local can = { chickens = has_chickens(user_id), planted = has_planted(user_id) }
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
  end)
  if not ok then nk.logger_warn("quest progress failed: " .. tostring(err)) end
end

return M
