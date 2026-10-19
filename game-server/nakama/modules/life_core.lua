-- Shared "daily life" rules (no RPCs here): game clock and energy (พลังงาน).
-- require("life_core") from life.lua, farming.lua, fishing.lua.
--
-- Game clock: 1 real second = 1 game minute, so one game day = 24 real minutes.
-- Every client computes the same clock from UTC time; the server uses it for nothing secret.

local nk = require("nakama")

local M = {}

M.MAX_ENERGY = 100
M.PASSIVE_REGEN_PER_MIN = 1   -- real minutes
M.SLEEP_REGEN_PER_SEC = 1     -- real seconds while sleeping (a full rest ~100 s, so energy paces the work)

M.COST = { plow = 10, plant = 10, water = 5, harvest = 10, cast = 4 }

-- Calendar (same epoch as the client's GameClock): 1 game day = 24 real minutes,
-- 1 game month = 30 game days = 12 real hours.
M.EPOCH_MS = 1767225600000   -- 2026-01-01T00:00:00Z
M.DAY_MS = 24 * 60 * 1000
M.MONTH_DAYS = 30

function M.game_day(now)   -- 1-based
  return math.floor((now - M.EPOCH_MS) / M.DAY_MS) + 1
end

-- Temple fair (งานวัด) = the traditional festivals (งานประเพณี) on the game calendar.
-- The game year has 12 months of 30 days (1 game day = 24 real minutes, 1 year = 6 real days),
-- month 1 = มกราคม, the same calendar the client shows. Every festival comes once a year.
-- scale: "big" festivals are held in the city (with the ferris wheel, carousel, bingo),
-- "small" ones in the tambon.
M.YEAR_DAYS = 360
M.FESTIVALS = {
  { id = "temple_annual", name = "งานประจำปีวัด",            month = 1,  day = 10, days = 3, scale = "big" },
  { id = "makha",         name = "งานวันมาฆบูชา",             month = 2,  day = 14, days = 2, scale = "small" },
  { id = "phapa_1",       name = "งานทอดผ้าป่า",              month = 3,  day = 8,  days = 2, scale = "small" },
  { id = "songkran",      name = "งานสงกรานต์",              month = 4,  day = 13, days = 3, scale = "big" },
  { id = "visakha",       name = "งานวันวิสาขบูชา",            month = 5,  day = 14, days = 2, scale = "small" },
  { id = "khao_phansa",   name = "งานวันเข้าพรรษา",            month = 7,  day = 14, days = 2, scale = "small" },
  { id = "ok_phansa",     name = "งานวันออกพรรษา",            month = 10, day = 14, days = 2, scale = "big" },
  { id = "kathin",        name = "งานทอดกฐิน",               month = 10, day = 21, days = 2, scale = "big" },
  { id = "loy_krathong",  name = "งานลอยกระทง",              month = 11, day = 14, days = 2, scale = "big" },
  { id = "phapa_2",       name = "งานทอดผ้าป่าสามัคคี",         month = 12, day = 8,  days = 2, scale = "small" },
}

-- Monthly events (งานประจำเดือน) so there's always something coming: every month one in the
-- village (at the temple ground), one at the tambon and one in the city, names rotating.
-- National events (ระดับประเทศ): beach concerts (คอนเสิร์ตริมทะเล) three times a year.
local MONTHLY = {
  { scale = "village", day = 2,  days = 2, names = { "งานรื่นเริงหมู่บ้าน", "ตลาดนัดคนเดินหมู่บ้าน", "งานบุญข้าวใหม่",
                                                   "งานกีฬาสีหมู่บ้าน", "งานลานวัฒนธรรมหมู่บ้าน", "งานบุญกลางบ้าน" } },
  { scale = "small",   day = 17, days = 2, names = { "งานกาชาดตำบล", "งานของดีตำบล", "เทศกาลผลไม้ตำบล",
                                                   "งานแข่งเรือตำบล", "งานมหกรรมสินค้าชุมชน", "งานวันเด็กตำบล" } },
  { scale = "big",     day = 25, days = 2, names = { "เทศกาลอาหารในเมือง", "งานแสดงสินค้า OTOP", "เทศกาลดอกไม้ในเมือง",
                                                   "งานมหกรรมของดีจังหวัด", "งานกาชาดจังหวัด", "เทศกาลแสงสีในเมือง" } },
}
M.NATIONAL = {
  { id = "beach_fest",   name = "เทศกาลดนตรีริมหาด",          month = 3,  day = 20, days = 3, scale = "beach", kind = "national" },
  { id = "summer_conc",  name = "คอนเสิร์ตฤดูร้อนริมทะเล",       month = 8,  day = 21, days = 3, scale = "beach", kind = "national" },
  { id = "countdown",    name = "คอนเสิร์ตเคาท์ดาวน์ริมหาด",       month = 12, day = 27, days = 3, scale = "beach", kind = "national" },
}

-- Every event in a game year, ordered by start day: { id, name, start (day of year, 0-based), days, scale, kind, temple }
local function build_events()
  local list, taken = {}, {}
  local function add(e)
    for d = e.start, e.start + e.days - 1 do taken[d] = true end
    table.insert(list, e)
  end
  local function free(start, days)
    for d = start, start + days - 1 do if taken[d] then return false end end
    return true
  end
  for _, f in ipairs(M.FESTIVALS) do
    add({ id = f.id, name = f.name, start = (f.month - 1) * 30 + (f.day - 1), days = f.days, scale = f.scale,
          kind = "tradition", temple = true })
  end
  for _, f in ipairs(M.NATIONAL) do
    add({ id = f.id, name = f.name, start = (f.month - 1) * 30 + (f.day - 1), days = f.days, scale = f.scale, kind = "national" })
  end
  for month = 1, 12 do
    for _, m in ipairs(MONTHLY) do
      local start = (month - 1) * 30 + (m.day - 1)
      if free(start, m.days) then -- a tradition or a concert that month takes the slot
        add({ id = m.scale .. "_" .. month, name = m.names[(month - 1) % #m.names + 1], start = start, days = m.days,
              scale = m.scale, kind = "monthly" })
      end
    end
  end
  table.sort(list, function(a, b) return a.start < b.start end)
  return list
end
M.EVENTS = build_events()

-- Returns open, fair_id (this year's edition), ends_in_ms, next_in_ms, event (current or next)
function M.fair(now, always_open)
  local t_days = (now - M.EPOCH_MS) / M.DAY_MS           -- game days since the epoch (fractional)
  local day0 = math.floor(t_days)
  local year = math.floor(day0 / M.YEAR_DAYS)
  local doy = day0 % M.YEAR_DAYS
  local next_f, next_in
  for i, f in ipairs(M.EVENTS) do
    if doy >= f.start and doy < f.start + f.days then
      local ends = (year * M.YEAR_DAYS + f.start + f.days - t_days) * M.DAY_MS
      local id = year * 100 + i
      return true, id, ends, 0, f
    end
    local ahead = f.start - doy
    if ahead <= 0 then ahead = ahead + M.YEAR_DAYS end
    if next_in == nil or ahead < next_in then next_in, next_f = ahead, f end
  end
  local next_ms = (next_in - (t_days - day0)) * M.DAY_MS
  if always_open then return true, year * 100 + 99, M.DAY_MS, next_ms, { id = "test", name = "งานอีเวนต์ (โหมดทดสอบ)", scale = "both", kind = "test", temple = true } end
  return false, year * 100 + 98, 0, next_ms, next_f
end

-- The next `count` events from now (the current one first if it's on), for the calendar
function M.upcoming(now, count)
  local t_days = (now - M.EPOCH_MS) / M.DAY_MS
  local day0 = math.floor(t_days)
  local year = math.floor(day0 / M.YEAR_DAYS)
  local out = {}
  for y = year, year + 1 do
    for _, f in ipairs(M.EVENTS) do
      local s = y * M.YEAR_DAYS + f.start
      if s + f.days > t_days then
        table.insert(out, { event = f, start_day = s, open = s <= t_days,
                            in_ms = math.max(0, (s - t_days) * M.DAY_MS), ends_ms = (s + f.days - t_days) * M.DAY_MS })
        if #out >= count then return out end
      end
    end
  end
  return out
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

-- Injury (บาดเจ็บ) after being knocked out in a fight: no training or fighting until treated
-- at a health station (รพ.สต.) or the city hospital (โรงพยาบาล)
function M.set_injured(user_id, injured)
  local s = settle(read(user_id), nk.time())
  s.injured = injured or nil
  write(user_id, s)
  return s
end

function M.injured(user_id)
  return read(user_id).injured == true
end

function M.view(s)
  return { energy = math.floor(s.energy), max_energy = M.MAX_ENERGY, sleeping = s.sleep_ms ~= nil, injured = s.injured == true }
end

return M
