-- Chicken raising rules (no RPCs here): coop storage, egg laying, hatching, breeding.
-- require("chicken_core") from chickens.lua and economy.lua (buying chickens).
--
-- Real-time based like the rice: hens lay while the coop is fed, eggs hatch and chicks grow
-- while the game is closed. The server rolls every trait.

local nk = require("nakama")

local M = {}

M.CAPACITY = 8
M.MAX_EGGS = 12
M.LAY_MS = 8 * 60 * 1000          -- one egg per adult hen every 8 real minutes while fed
M.FEED_MS = 24 * 60 * 1000        -- one bag of feed lasts a game day
M.HATCH_MS = 10 * 60 * 1000
M.GROW_MS = 15 * 60 * 1000        -- chick -> adult

M.COLORS = { "ประดู่หางดำ", "เหลืองหางขาว", "เขียวหางดำ", "ทองแดงหางดำ", "ขาวล้วน" }

local COOP = { collection = "chickens", key = "coop" }

function M.read(user_id)
  local objects = nk.storage_read({ { collection = COOP.collection, key = COOP.key, user_id = user_id } })
  local coop = (#objects > 0) and objects[1].value or {}
  coop.chickens = coop.chickens or {}
  coop.eggs = coop.eggs or 0
  coop.fertile_eggs = coop.fertile_eggs or 0
  coop.incubating = coop.incubating or {}
  coop.lay_progress = coop.lay_progress or 0
  coop.fed_until_ms = coop.fed_until_ms or 0
  coop.next_id = coop.next_id or 1
  return coop
end

function M.write(user_id, coop)
  nk.storage_write({ {
    collection = COOP.collection, key = COOP.key, user_id = user_id, value = coop,
    permission_read = 1, permission_write = 0,
  } })
end

local function clamp(v, lo, hi) return math.max(lo, math.min(hi, v)) end

function M.is_adult(c, now) return now - c.born_ms >= M.GROW_MS end

function M.count(coop) return #coop.chickens + #coop.incubating end

-- New chicken; traits come from the parents when there are any
function M.new_chicken(coop, sex, beauty, color, now)
  local id = coop.next_id
  coop.next_id = id + 1
  local c = {
    id = id, sex = sex, beauty = clamp(math.floor(beauty), 1, 100), color = color,
    born_ms = now, name = (sex == "rooster" and "พ่อไก่" or "แม่ไก่") .. " #" .. id,
  }
  table.insert(coop.chickens, c)
  return c
end

-- Bought from the shop: a grown chicken (born a day ago), average looks
function M.buy(coop, sex, now)
  return M.new_chicken(coop, sex, math.random(25, 50), M.COLORS[math.random(#M.COLORS)], now - 24 * 60 * 60 * 1000)
end

local function best(coop, sex, now)
  local pick
  for _, c in ipairs(coop.chickens) do
    if c.sex == sex and M.is_adult(c, now) and (not pick or c.beauty > pick.beauty) then pick = c end
  end
  return pick
end

-- Bring the coop up to date: lay eggs while fed, hatch finished eggs
function M.settle(coop, now)
  local from = coop.settled_ms or now
  local to = math.min(now, coop.fed_until_ms)
  if to > from then
    local hens = 0
    for _, c in ipairs(coop.chickens) do
      if c.sex == "hen" and M.is_adult(c, from) then hens = hens + 1 end
    end
    coop.lay_progress = coop.lay_progress + (to - from) * hens
    local laid = math.floor(coop.lay_progress / M.LAY_MS)
    coop.lay_progress = coop.lay_progress - laid * M.LAY_MS
    if laid > 0 then
      local room = math.max(0, M.MAX_EGGS - coop.eggs - coop.fertile_eggs)
      laid = math.min(laid, room)
      if best(coop, "rooster", now) then
        coop.fertile_eggs = coop.fertile_eggs + laid   -- a rooster in the coop = fertile eggs
      else
        coop.eggs = coop.eggs + laid
      end
    end
  end
  coop.settled_ms = now

  -- Hatching: chick inherits from the parents recorded when incubation started
  local still = {}
  for _, egg in ipairs(coop.incubating) do
    if now - egg.start_ms >= M.HATCH_MS then
      local beauty = (egg.father_beauty + egg.mother_beauty) / 2 + math.random(-10, 15)
      local color = (math.random() < 0.5) and egg.father_color or egg.mother_color
      local chick = M.new_chicken(coop, (math.random() < 0.5) and "rooster" or "hen", beauty, color, egg.start_ms + M.HATCH_MS)
      chick.name = "ลูกไก่ #" .. chick.id
    else
      table.insert(still, egg)
    end
  end
  coop.incubating = still
  return coop
end

-- Start incubating one fertile egg; parents are the best adult rooster and hen right now
function M.start_hatch(coop, now)
  local father, mother = best(coop, "rooster", now), best(coop, "hen", now)
  table.insert(coop.incubating, {
    start_ms = now,
    father_beauty = father and father.beauty or 40, father_color = father and father.color or M.COLORS[1],
    mother_beauty = mother and mother.beauty or 40, mother_color = mother and mother.color or M.COLORS[2],
  })
end

-- Breeding stock is the main income: price grows with the square of beauty, so ordinary
-- chickens sell below shop price and only well-bred ones are profitable (no buy-and-flip).
function M.sell_price(c, now)
  if not M.is_adult(c, now) then return 25 end
  local price = 10 + (c.beauty * c.beauty) / 25
  if c.sex == "rooster" then price = price * 1.2 end
  return math.floor(price)
end

-- Fertile eggs (ไข่แม่พันธุ์) are worth more when the parents are beautiful
function M.fertile_egg_price(coop, now)
  local father, mother = best(coop, "rooster", now), best(coop, "hen", now)
  local avg = ((father and father.beauty or 40) + (mother and mother.beauty or 40)) / 2
  return 8 + math.floor(avg / 4)
end

return M
