-- School and skilled jobs (การศึกษาและอาชีพ). Classes at the tambon school raise a subject level
-- (คอมพิวเตอร์ กีฬา ช่าง ตรรกะ สุขภาพ) and give XP to the main level, another way to level up besides boxing
-- training. Skilled jobs need a main level and subject levels before you can take them.
-- The server times classes and shifts, so they can't be skipped.
--
--   edu_get       {}                -> subjects, jobs (with requirements), current class / shift
--   class_start   {"subject": id, "library": bool} -> pays the fee and energy, starts the class
--                 (library = read on your own in the school library: free, slower, less XP)
--   class_finish  {}                -> subject XP + main XP
--   job_start     {"job": id}       -> checks requirements and today's shifts, spends energy
--   job_finish    {}                -> pays the wage
--
-- Other modules call require("education").check(user_id, job_id) to gate their own jobs.

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")
local quest = require("quest_core")
local trade = require("trade_core")

local M = {}

local STORE = { collection = "education", key = "state" }
local CAREER = { collection = "career", key = "stats" }
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9
local MAX_LEVEL = 100
local SUBJECT_MAX = 30

local CLASS_SECONDS = 30
-- Self-study in the library (ห้องสมุด): no fee, a little energy, takes longer, smaller XP
M.LIBRARY = { secs = 45, energy = 4, xp = 6, main_xp = 10 }
local function class_secs(c) return c.library and M.LIBRARY.secs or CLASS_SECONDS end
local SUBJECT_ORDER = { "computer", "sports", "craft", "logic", "health" }
M.SUBJECTS = {
  computer = { name = "คอมพิวเตอร์", desc = "พิมพ์งาน ใช้โปรแกรม เขียนโค้ดเบื้องต้น", need_level = 1, fee = 20, energy = 6, xp = 10, main_xp = 20 },
  sports   = { name = "กีฬา",       desc = "ฟุตบอล วิ่ง มวยไทยในคาบพละ",          need_level = 1, fee = 20, energy = 8, xp = 10, main_xp = 20 },
  craft    = { name = "ช่าง",        desc = "ซ่อมเครื่องยนต์ ไฟฟ้า งานไม้",         need_level = 1, fee = 20, energy = 7, xp = 10, main_xp = 20 },
  logic    = { name = "ตรรกะ",       desc = "คณิตศาสตร์ ปริศนา วางแผน",            need_level = 3, fee = 30, energy = 6, xp = 10, main_xp = 25 },
  health   = { name = "สุขภาพ",      desc = "ปฐมพยาบาล ร่างกายคน ดูแลคนไข้",        need_level = 5, fee = 30, energy = 7, xp = 10, main_xp = 25 },
}

-- Profession XP (trade_core) for a class in each subject (half in the library) and for each shift
local CLASS_TRADES = {
  computer = { computer = 16, programmer = 6 },
  sports   = { boxer = 10, teacher = 4 },
  craft    = { repair = 16, guide = 4 },
  logic    = { programmer = 10, teacher = 10 },
  health   = { doctor = 16 },
}
local JOB_TRADES = {
  repair = { repair = 45 }, coach = { teacher = 40, boxer = 10 }, office = { computer = 45 },
  tutor = { teacher = 55 }, programmer = { programmer = 70 }, doctor = { doctor = 75 },
}

-- Skilled jobs: main level + subject levels; a few shifts per game day each
local JOB_ORDER = { "boat_tour", "repair", "coach", "office", "tutor", "programmer", "doctor" }
M.JOBS = {
  boat_tour  = { name = "ขับเรือพานักท่องเที่ยว", place = "ทะเล",        level = 5,  need = { craft = 2 } },
  repair     = { name = "ช่างซ่อมมอเตอร์ไซค์",   place = "ตำบล",        level = 6,  need = { craft = 3 },               energy = 12, secs = 45, pay = 260 },
  coach      = { name = "โค้ชกีฬาโรงเรียน",      place = "โรงเรียน",     level = 8,  need = { sports = 4 },              energy = 14, secs = 45, pay = 300 },
  office     = { name = "พนักงานคอมพิวเตอร์",    place = "ตัวเมือง",      level = 8,  need = { computer = 4 },            energy = 10, secs = 45, pay = 320 },
  tutor      = { name = "ติวเตอร์ตรรกะ",         place = "โรงเรียน",     level = 10, need = { logic = 5 },               energy = 10, secs = 45, pay = 380 },
  programmer = { name = "โปรแกรมเมอร์",          place = "ตัวเมือง",      level = 15, need = { computer = 8, logic = 6 }, energy = 12, secs = 60, pay = 650 },
  doctor     = { name = "หมออนามัย",             place = "โรงพยาบาล",    level = 20, need = { health = 8, logic = 4 },   energy = 14, secs = 60, pay = 800 },
}
local SHIFTS_PER_DAY = 3

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function subject_xp_to_next(level) return 30 + 15 * (level - 1) end
local function main_xp_to_next(level) return 40 + 20 * (level - 1) end

local function read(user_id)
  local objects = nk.storage_read({ { collection = STORE.collection, key = STORE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.subjects = s.subjects or {}
  for id, _ in pairs(M.SUBJECTS) do
    s.subjects[id] = s.subjects[id] or { level = 1, xp = 0 }
  end
  s.shifts = s.shifts or {}
  return s
end

local function write(user_id, s)
  nk.storage_write({ { collection = STORE.collection, key = STORE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

local function read_career(user_id)
  local objects = nk.storage_read({ { collection = CAREER.collection, key = CAREER.key, user_id = user_id } })
  local c = (#objects > 0) and objects[1].value or {}
  c.level = c.level or 1
  c.xp = c.xp or 0
  return c
end

-- Main level XP (the same level boxing training raises)
local function add_main_xp(user_id, amount)
  local c = read_career(user_id)
  local before = c.level
  c.xp = c.xp + amount
  while c.level < MAX_LEVEL and c.xp >= main_xp_to_next(c.level) do
    c.xp = c.xp - main_xp_to_next(c.level)
    c.level = c.level + 1
  end
  if c.level >= MAX_LEVEL then c.xp = 0 end
  nk.storage_write({ { collection = CAREER.collection, key = CAREER.key, user_id = user_id, value = c,
                       permission_read = 1, permission_write = 0 } })
  return c, c.level > before
end

local function wallet(user_id)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[data.CURRENCY] or 0
end

-- What's missing for a job: nil when you qualify, otherwise a Thai sentence
local function missing(s, main_level, job)
  local parts = {}
  if main_level < job.level then table.insert(parts, "เลเวล " .. job.level) end
  for _, id in ipairs(SUBJECT_ORDER) do
    local need = job.need[id]
    if need and s.subjects[id].level < need then
      table.insert(parts, M.SUBJECTS[id].name .. " Lv " .. need)
    end
  end
  if #parts == 0 then return nil end
  return "ต้องมี " .. table.concat(parts, " · ")
end

-- For other modules: raise an error if the player can't do this job yet
function M.check(user_id, job_id)
  local job = M.JOBS[job_id]
  if not job then return end
  local why = missing(read(user_id), read_career(user_id).level, job)
  if why then err("skill: " .. why) end
end

local function shifts_today(s, job_id, today)
  local rec = s.shifts[job_id]
  if not rec or rec.day ~= today then return 0 end
  return rec.count
end

local function view(user_id, s, extra)
  local now = nk.time()
  local c = read_career(user_id)
  local today = life.game_day(now)
  local subjects = {}
  for _, id in ipairs(SUBJECT_ORDER) do
    local def, st = M.SUBJECTS[id], s.subjects[id]
    table.insert(subjects, {
      id = id, name = def.name, desc = def.desc, level = st.level, xp = st.xp,
      xp_to_next = (st.level >= SUBJECT_MAX) and 0 or subject_xp_to_next(st.level),
      fee = def.fee, energy = def.energy, main_xp = def.main_xp, need_level = def.need_level,
      unlocked = c.level >= def.need_level,
      lib_energy = M.LIBRARY.energy, lib_xp = M.LIBRARY.xp, lib_main_xp = M.LIBRARY.main_xp,
    })
  end
  local jobs = {}
  for _, id in ipairs(JOB_ORDER) do
    local job = M.JOBS[id]
    local why = missing(s, c.level, job)
    table.insert(jobs, {
      id = id, name = job.name, place = job.place, pay = job.pay or 0, energy = job.energy or 0, secs = job.secs or 0,
      unlocked = why == nil, missing = why or "", shifts = shifts_today(s, id, today), cap = SHIFTS_PER_DAY,
    })
  end
  local result = { main_level = c.level, subjects = subjects, jobs = jobs }
  if s.class then
    local secs = class_secs(s.class)
    result.class = { id = s.class.id, remain = math.max(0, secs - (now - s.class.start_ms) / 1000), secs = secs,
                     library = s.class.library == true }
  end
  if s.job then
    local job = M.JOBS[s.job.id]
    result.job = { id = s.job.id, remain = math.max(0, job.secs - (now - s.job.start_ms) / 1000) }
  end
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then return {} end
  return req
end

nk.register_rpc(function(context, payload)
  return view(context.user_id, read(context.user_id))
end, "edu_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local def = M.SUBJECTS[req.subject or ""]
  if not def then err("unknown subject", GRPC_INVALID_ARGUMENT) end
  local s = read(user_id)
  if s.class then err("already in class") end
  if s.job then err("working") end
  if read_career(user_id).level < def.need_level then err("level too low") end
  if s.subjects[req.subject].level >= SUBJECT_MAX then err("subject maxed") end
  local library = req.library == true
  if library then
    life.spend(user_id, M.LIBRARY.energy)
  else
    if wallet(user_id) < def.fee then err("not enough baht") end
    life.spend(user_id, def.energy)
    nk.wallet_update(user_id, { [data.CURRENCY] = -def.fee }, { reason = "school_fee", subject = req.subject }, true)
  end
  s.class = { id = req.subject, start_ms = nk.time(), library = library or nil }
  write(user_id, s)
  return view(user_id, s)
end, "class_start")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local s = read(user_id)
  if not s.class then err("no class") end
  local elapsed = (nk.time() - s.class.start_ms) / 1000
  if elapsed < class_secs(s.class) - 2 then err("too early") end
  local id = s.class.id
  local def, st = M.SUBJECTS[id], s.subjects[s.class.id]
  local library = s.class.library == true
  local xp = library and M.LIBRARY.xp or def.xp
  local main_xp = library and M.LIBRARY.main_xp or def.main_xp
  s.class = nil
  st.xp = st.xp + xp
  local subject_up = false
  while st.level < SUBJECT_MAX and st.xp >= subject_xp_to_next(st.level) do
    st.xp = st.xp - subject_xp_to_next(st.level)
    st.level = st.level + 1
    subject_up = true
  end
  if st.level >= SUBJECT_MAX then st.xp = 0 end
  write(user_id, s)
  local c, main_up = add_main_xp(user_id, main_xp)
  local gains = {}
  for t, txp in pairs(CLASS_TRADES[id] or {}) do gains[t] = library and math.ceil(txp / 2) or txp end
  trade.gain_many(user_id, gains, false) -- the class already gave main XP
  quest.progress(user_id, "study", 1)
  return view(user_id, s, { finished = { subject = id, name = def.name, xp = xp, main_xp = main_xp, library = library,
                                         level = st.level, subject_up = subject_up, main_level = c.level, main_up = main_up } })
end, "class_finish")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local job = M.JOBS[req.job or ""]
  if not job or not job.secs then err("unknown job", GRPC_INVALID_ARGUMENT) end
  local s = read(user_id)
  if s.job then err("already working") end
  if s.class then err("in class") end
  local why = missing(s, read_career(user_id).level, job)
  if why then err("skill: " .. why) end
  local today = life.game_day(nk.time())
  if shifts_today(s, req.job, today) >= SHIFTS_PER_DAY then err("no more shifts today") end
  life.spend(user_id, job.energy)
  s.job = { id = req.job, start_ms = nk.time() }
  write(user_id, s)
  return view(user_id, s)
end, "job_start")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local s = read(user_id)
  if not s.job then err("not working") end
  local id = s.job.id
  local job = M.JOBS[id]
  local elapsed = (nk.time() - s.job.start_ms) / 1000
  if elapsed < job.secs - 2 then err("too early") end
  s.job = nil
  local today = life.game_day(nk.time())
  local rec = s.shifts[id]
  if not rec or rec.day ~= today then rec = { day = today, count = 0 } end
  rec.count = rec.count + 1
  s.shifts[id] = rec
  write(user_id, s)
  nk.wallet_update(user_id, { [data.CURRENCY] = job.pay }, { reason = "job", job = id }, true)
  trade.gain_many(user_id, JOB_TRADES[id] or {}, true)
  return view(user_id, s, { paid = { job = id, name = job.name, pay = job.pay } })
end, "job_finish")

return M
