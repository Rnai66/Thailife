-- Shared "daily life" rules (no RPCs here): game clock and energy (พลังงาน).
-- require("life_core") from life.lua, farming.lua, fishing.lua.
--
-- Game clock: 1 real second = 1 game minute, so one game day = 24 real minutes.
-- Every client computes the same clock from UTC time; the server uses it for nothing secret.

local nk = require("nakama")

local M = {}

M.MAX_ENERGY = 100
M.PASSIVE_REGEN_PER_MIN = 1   -- real minutes
M.SLEEP_REGEN_PER_SEC = 3     -- real seconds while sleeping

M.COST = { plow = 10, plant = 10, water = 5, harvest = 10, cast = 3 }

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

-- Returns open, fair_id (this year's edition), ends_in_ms, next_in_ms, festival (current or next)
function M.fair(now, always_open)
  local t_days = (now - M.EPOCH_MS) / M.DAY_MS           -- game days since the epoch (fractional)
  local day0 = math.floor(t_days)
  local year = math.floor(day0 / M.YEAR_DAYS)
  local doy = day0 % M.YEAR_DAYS
  local next_f, next_in
  for i, f in ipairs(M.FESTIVALS) do
    local start = (f.month - 1) * 30 + (f.day - 1)
    if doy >= start and doy < start + f.days then
      local ends = (year * M.YEAR_DAYS + start + f.days - t_days) * M.DAY_MS
      local id = year * 100 + i
      return true, id, ends, 0, f
    end
    local ahead = start - doy
    if ahead <= 0 then ahead = ahead + M.YEAR_DAYS end
    if next_in == nil or ahead < next_in then next_in, next_f = ahead, f end
  end
  local next_ms = (next_in - (t_days - day0)) * M.DAY_MS
  if always_open then return true, year * 100 + 99, M.DAY_MS, next_ms, { id = "test", name = "งานอีเวนต์ (โหมดทดสอบ)", scale = "both" } end
  return false, year * 100 + 98, 0, next_ms, next_f
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
