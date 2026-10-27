-- Professions (อาชีพ), shared rules (RPCs in trades.lua). Every player has a level 1..100 in each
-- profession, raised by doing that work: training raises นักมวย, fishing ชาวประมง, rice ชาวนา, the
-- school subjects and skilled jobs ช่างซ่อม ช่างคอม ครู โปรแกรมเมอร์ หมอ, and so on. Each profession
-- has a rank title per milestone level (1, 10, 25, 50, 75, 100); the player's shown title is the
-- one they pick, or their highest profession. Work that doesn't already raise the main level
-- (boxing training and classes do) also gives the same XP to the main level.
--
-- Other modules: require("trade_core").gain(user_id, trade, xp) or .from_kind(user_id, kind, amount)
-- (quest_core.progress calls from_kind for every action it counts).

local nk = require("nakama")

local M = {}

M.STORE = { collection = "career", key = "trades" }
M.MAX_LEVEL = 100
M.TIERS = { 1, 10, 25, 50, 75, 100 }

M.ORDER = { "boxer", "fisher", "farmer", "fishpond", "chicken", "cattle", "musician", "merchant",
            "repair", "computer", "teacher", "programmer", "doctor", "guide", "politician" }

-- main = also gives main-level XP (false where the source already gives it: training, classes)
M.TRADES = {
  boxer      = { name = "นักมวย", how = "ซ้อมมวย ชกต่อย ขึ้นเวที",
                 ranks = { "นักมวยหน้าใหม่", "นักมวยฝีมือดี", "นักมวยดาวรุ่ง", "แชมป์เวทีตำบล", "ยอดมวยไทย", "ตำนานมวยไทย" } },
  fisher     = { name = "ชาวประมง", how = "ตกปลาในคลองและทะเล",
                 ranks = { "มือใหม่หัดตกปลา", "นักตกปลา", "พรานปลาประจำคลอง", "ชาวประมงชำนาญ", "เซียนตกปลา", "ราชาแห่งสายน้ำ" } },
  farmer     = { name = "ชาวนา", how = "ปลูกข้าว เกี่ยวข้าว ที่โคกหนองนา",
                 ranks = { "ชาวนามือใหม่", "ชาวนาขยัน", "ชาวนาชำนาญ", "ชาวนาตัวอย่าง", "ปราชญ์ชาวนา", "ราชาข้าวหอม" } },
  fishpond   = { name = "เลี้ยงปลา", how = "เลี้ยงปลาในกระชังที่สระโคกหนองนา",
                 ranks = { "มือใหม่เลี้ยงปลา", "คนเลี้ยงปลา", "เจ้าของบ่อปลา", "ผู้เชี่ยวชาญบ่อปลา", "เศรษฐีบ่อปลา", "ตำนานโคกหนองนา" } },
  chicken    = { name = "เลี้ยงไก่", how = "ให้อาหารไก่ ขายไข่",
                 ranks = { "มือใหม่เลี้ยงไก่", "คนเลี้ยงไก่", "เจ้าของเล้าไก่", "เซียนไก่", "เจ้าของฟาร์มไก่ไข่", "ราชาไก่ชน" } },
  cattle     = { name = "เลี้ยงวัว", how = "ดูแลวัว รีดนม ขายวัว",
                 ranks = { "มือใหม่เลี้ยงวัว", "คนเลี้ยงวัว", "นายฮ้อยฝึกหัด", "เจ้าของฟาร์มวัว", "เศรษฐีโคนม", "ตำนานนายฮ้อย" } },
  musician   = { name = "นักดนตรี", how = "เล่นดนตรีที่ศาลาและงานวัด",
                 ranks = { "หัดเล่นดนตรี", "นักดนตรีหน้าใหม่", "นักดนตรีงานวัด", "ศิลปินพื้นบ้าน", "ครูดนตรี", "ศิลปินแห่งชาติ" } },
  merchant   = { name = "พ่อค้าแม่ค้า", how = "ขายปลา ข้าว ไข่ และซื้อของที่ร้าน",
                 ranks = { "แม่ค้ามือใหม่", "พ่อค้าตลาดนัด", "พ่อค้าขายส่ง", "เจ้าของร้านค้า", "เถ้าแก่", "เจ้าสัว" } },
  repair     = { name = "ช่างซ่อม", how = "เรียนวิชาช่าง ทำงานช่างซ่อม",
                 ranks = { "เด็กฝึกงานช่าง", "ช่างซ่อมฝึกหัด", "ช่างซ่อมมอเตอร์ไซค์", "ช่างฝีมือ", "หัวหน้าช่าง", "ช่างเทพ" } },
  computer   = { name = "ช่างคอม", how = "เรียนคอมพิวเตอร์ ทำงานพนักงานคอม",
                 ranks = { "หัดใช้คอม", "ช่างคอมฝึกหัด", "ช่างคอมประจำตำบล", "ไอทีซัพพอร์ต", "ผู้เชี่ยวชาญไอที", "กูรูคอมพิวเตอร์" } },
  teacher    = { name = "ครู", how = "เรียนตรรกะ เป็นโค้ชและติวเตอร์",
                 ranks = { "ผู้ช่วยสอน", "ครูอัตราจ้าง", "ครูประจำชั้น", "ครูชำนาญการ", "ผู้อำนวยการโรงเรียน", "ครูแห่งชาติ" } },
  programmer = { name = "โปรแกรมเมอร์", how = "เรียนคอมและตรรกะ ทำงานโปรแกรมเมอร์",
                 ranks = { "หัดเขียนโค้ด", "จูเนียร์โปรแกรมเมอร์", "โปรแกรมเมอร์", "ซีเนียร์โปรแกรมเมอร์", "หัวหน้าทีมเทค", "ซีทีโอ" } },
  doctor     = { name = "หมอ", how = "เรียนสุขภาพ ทำงานที่โรงพยาบาล",
                 ranks = { "นักเรียนแพทย์", "ผู้ช่วยพยาบาล", "หมออนามัย", "แพทย์ประจำโรงพยาบาล", "แพทย์ผู้เชี่ยวชาญ", "หมอเทวดา" } },
  guide      = { name = "ไกด์นำเที่ยว", how = "พานักท่องเที่ยวนั่งเรือ",
                 ranks = { "ไกด์ฝึกหัด", "คนขับเรือ", "ไกด์นำเที่ยว", "ไกด์มืออาชีพ", "เจ้าของบริษัททัวร์", "ไกด์ระดับโลก" } },
  politician = { name = "นักการเมือง", how = "สมัครเลือกตั้งที่ อบต. หาเสียงที่ศาลา",
                 ranks = { "ผู้สมัครหน้าใหม่", "ผู้นำชุมชน", "สมาชิกสภา อบต.", "นายก อบต.", "นายกเทศมนตรี", "รัฐบุรุษ" } },
}

-- Daily-quest action kinds (quest_core.progress) -> XP per unit for each profession
M.FROM_KIND = {
  train        = { boxer = 2 },
  fish         = { fisher = 12 },
  plant        = { farmer = 10 },
  harvest      = { farmer = 20 },
  sell_produce = { merchant = 8 },
  shop         = { merchant = 3 },
  music        = { musician = 25 },
  feed         = { chicken = 10 },
  sell_eggs    = { chicken = 2, merchant = 1 },
}
-- These already raise the main level themselves
local NO_MAIN = { train = true }

local MAIN = { collection = "career", key = "stats" }

function M.xp_to_next(level) return 30 + 10 * (level - 1) end
local function main_xp_to_next(level) return 40 + 20 * (level - 1) end

function M.rank(trade, level)
  local def = M.TRADES[trade]
  local tier = 1
  for i, at in ipairs(M.TIERS) do if level >= at then tier = i end end
  return def.ranks[tier]
end

local function add_xp(t, xp)
  t.xp = t.xp + xp
  while t.level < M.MAX_LEVEL and t.xp >= M.xp_to_next(t.level) do
    t.xp = t.xp - M.xp_to_next(t.level)
    t.level = t.level + 1
  end
  if t.level >= M.MAX_LEVEL then t.xp = 0 end
end

-- Players from before professions existed start where their history puts them:
-- boxing hits so far, and the school subject levels they reached
local SUBJECT_SEED = {
  computer = { computer = 40 }, sports = { boxer = 30 }, craft = { repair = 40 },
  logic = { teacher = 25, programmer = 25 }, health = { doctor = 40 },
}

local function seed(user_id, s)
  local objects = nk.storage_read({
    { collection = MAIN.collection, key = MAIN.key, user_id = user_id },
    { collection = "education", key = "state", user_id = user_id },
  })
  for _, o in ipairs(objects) do
    if o.collection == MAIN.collection then
      add_xp(s.list.boxer, (o.value.total_hits or 0) * 2)
    elseif o.value.subjects then
      for subject, st in pairs(o.value.subjects) do
        for trade, per in pairs(SUBJECT_SEED[subject] or {}) do
          add_xp(s.list[trade], per * math.max(0, (st.level or 1) - 1))
        end
      end
    end
  end
end

-- { list = { boxer = { level, xp }, ... }, shown = "farmer" | nil }
function M.read(user_id)
  local objects = nk.storage_read({ { collection = M.STORE.collection, key = M.STORE.key, user_id = user_id } })
  local fresh = #objects == 0
  local s = fresh and {} or objects[1].value
  s.list = s.list or {}
  for _, id in ipairs(M.ORDER) do s.list[id] = s.list[id] or { level = 1, xp = 0 } end
  if fresh then
    local ok, e = pcall(seed, user_id, s)
    if not ok then nk.logger_warn("trade seed failed: " .. tostring(e)) end
  end
  return s
end

function M.write(user_id, s)
  nk.storage_write({ { collection = M.STORE.collection, key = M.STORE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

-- The profession whose title the player shows: their pick, else the highest level (first on ties)
function M.shown(s)
  if s.shown and M.TRADES[s.shown] then return s.shown end
  local best, best_level, best_xp = M.ORDER[1], 0, -1
  for _, id in ipairs(M.ORDER) do
    local t = s.list[id]
    if t.level > best_level or (t.level == best_level and t.xp > best_xp) then
      best, best_level, best_xp = id, t.level, t.xp
    end
  end
  return best
end

function M.title(user_id)
  local s = M.read(user_id)
  local id = M.shown(s)
  return M.rank(id, s.list[id].level), id, s.list[id].level
end

local function add_main_xp(user_id, amount)
  local objects = nk.storage_read({ { collection = MAIN.collection, key = MAIN.key, user_id = user_id } })
  local c = (#objects > 0) and objects[1].value or {}
  c.level = c.level or 1
  c.xp = (c.xp or 0) + amount
  while c.level < M.MAX_LEVEL and c.xp >= main_xp_to_next(c.level) do
    c.xp = c.xp - main_xp_to_next(c.level)
    c.level = c.level + 1
  end
  if c.level >= M.MAX_LEVEL then c.xp = 0 end
  nk.storage_write({ { collection = MAIN.collection, key = MAIN.key, user_id = user_id, value = c,
                       permission_read = 1, permission_write = 0 } })
end

-- Several professions at once: gains = { trade = xp, ... }; main = also raise the main level
function M.gain_many(user_id, gains, main)
  local s = M.read(user_id)
  local total = 0
  for trade, xp in pairs(gains) do
    local t = s.list[trade]
    if t and xp > 0 then
      total = total + xp
      add_xp(t, xp)
    end
  end
  if total <= 0 then return end
  M.write(user_id, s)
  if main then add_main_xp(user_id, total) end
end

function M.gain(user_id, trade, xp, main)
  M.gain_many(user_id, { [trade] = xp }, main ~= false)
end

-- Never breaks the caller
function M.from_kind(user_id, kind, amount)
  local per = M.FROM_KIND[kind]
  if not per or (amount or 0) <= 0 then return end
  local ok, e = pcall(function()
    local gains = {}
    for trade, xp in pairs(per) do gains[trade] = xp * amount end
    M.gain_many(user_id, gains, not NO_MAIN[kind])
  end)
  if not ok then nk.logger_warn("trade gain failed: " .. tostring(e)) end
end

return M
