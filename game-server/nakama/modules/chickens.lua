-- Chicken raising (เลี้ยงไก่) and the chicken beauty contest (ประกวดไก่สวย).
--   coop_get      {}                                  -> coop view
--   coop_act      {"action": "feed"|"sell_eggs"|"hatch"|"sell_chicken", "chicken": id} -> coop view + result
--   contest_enter {}                                  -> coop view + contest result (best adult enters)
-- The beauty contest is held at the monthly temple fair (งานวัด, game days 1-2 of each month).
-- Main income is selling eggs and breeding stock; contest prizes are a monthly bonus.
-- Rules live in chicken_core.lua; feed bags and chickens are bought at the shop (economy.lua).

local nk = require("nakama")
local core = require("chicken_core")
local life = require("life_core")
local data = require("shop_data")

local CURRENCY = "baht"
local EGG_PRICE = 6
local PRIZES = { 1000, 500, 250 }
local VILLAGERS = { "ไก่ลุงมี", "ไก่ป้าศรี", "ไก่ผู้ใหญ่บ้าน", "ไก่ตาเฒ่า" }
local ENERGY = { feed = 3, sell_eggs = 1, hatch = 2, sell_chicken = 1, contest = 5 }

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

-- Dev switch: FAIR_ALWAYS_OPEN=true in game-server/.env keeps the temple fair open for testing
local context_env
local function fair_always_open(env)
  return env ~= nil and env.fair_always_open == "true"
end

local function view(user_id, coop, extra)
  local now = nk.time()
  local list = {}
  for _, c in ipairs(coop.chickens) do
    local adult = core.is_adult(c, now)
    table.insert(list, {
      id = c.id, name = c.name, sex = c.sex, beauty = c.beauty, color = c.color,
      adult = adult, grown_in_s = adult and 0 or math.ceil((c.born_ms + core.GROW_MS - now) / 1000),
      price = core.sell_price(c, now),
    })
  end
  local hatching = {}
  for _, egg in ipairs(coop.incubating) do
    table.insert(hatching, math.max(0, math.ceil((egg.start_ms + core.HATCH_MS - now) / 1000)))
  end

  local items = data.read_items(user_id)
  local result = life.view(life.state(user_id))
  result.chickens = list
  result.capacity = core.CAPACITY
  result.eggs = coop.eggs
  result.fertile_eggs = coop.fertile_eggs
  result.hatching_s = hatching
  result.fed_for_s = math.max(0, math.ceil((coop.fed_until_ms - now) / 1000))
  result.feed_bags = items.feed or 0
  result.egg_price = EGG_PRICE
  result.fertile_egg_price = core.fertile_egg_price(coop, now)
  local open, fair_id, ends_in, next_in = life.fair(now, fair_always_open(context_env))
  result.fair_open = open
  result.fair_ends_in_s = math.ceil(ends_in / 1000)
  result.next_fair_in_s = math.ceil(next_in / 1000)
  result.entered_this_fair = coop.last_contest_fair == fair_id
  result.game_day = life.game_day(now)
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function pay(user_id, amount, reason)
  if amount > 0 then nk.wallet_update(user_id, { [CURRENCY] = amount }, { reason = reason }, true) end
end

local function rpc_get(context, payload)
  context_env = context.env
  local coop = core.settle(core.read(context.user_id), nk.time())
  core.write(context.user_id, coop)
  return view(context.user_id, coop)
end

local function rpc_act(context, payload)
  context_env = context.env
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end

  local now = nk.time()
  local coop = core.settle(core.read(user_id), now)
  local action = req.action
  local extra = { action = action }

  if action == "feed" then
    local items = data.read_items(user_id)
    if (items.feed or 0) < 1 then error({ "no feed", GRPC_FAILED_PRECONDITION }) end
    life.spend(user_id, ENERGY.feed)
    items.feed = items.feed - 1
    data.write_items(user_id, items)
    coop.fed_until_ms = math.max(coop.fed_until_ms, now) + core.FEED_MS

  elseif action == "sell_eggs" then
    local total = coop.eggs * EGG_PRICE + coop.fertile_eggs * core.fertile_egg_price(coop, now)
    if total == 0 then error({ "no eggs", GRPC_FAILED_PRECONDITION }) end
    life.spend(user_id, ENERGY.sell_eggs)
    pay(user_id, total, "sell_eggs")
    extra.earned = total
    extra.sold = coop.eggs + coop.fertile_eggs
    coop.eggs, coop.fertile_eggs = 0, 0

  elseif action == "hatch" then
    if coop.fertile_eggs < 1 then error({ "no fertile eggs", GRPC_FAILED_PRECONDITION }) end
    if core.count(coop) >= core.CAPACITY then error({ "coop full", GRPC_FAILED_PRECONDITION }) end
    life.spend(user_id, ENERGY.hatch)
    coop.fertile_eggs = coop.fertile_eggs - 1
    core.start_hatch(coop, now)

  elseif action == "sell_chicken" then
    local index
    for i, c in ipairs(coop.chickens) do
      if c.id == tonumber(req.chicken) then index = i end
    end
    if not index then error({ "unknown chicken", GRPC_INVALID_ARGUMENT }) end
    life.spend(user_id, ENERGY.sell_chicken)
    local c = table.remove(coop.chickens, index)
    local price = core.sell_price(c, now)
    pay(user_id, price, "sell_chicken")
    extra.earned = price
    extra.sold_name = c.name

  else
    error({ "unknown action", GRPC_INVALID_ARGUMENT })
  end

  core.write(user_id, coop)
  return view(user_id, coop, extra)
end

-- Beauty contest at the temple fair: your most beautiful adult vs four villagers' chickens
local function rpc_contest(context, payload)
  context_env = context.env
  local user_id = context.user_id
  local now = nk.time()
  local coop = core.settle(core.read(user_id), now)

  local open, fair_id = life.fair(now, fair_always_open(context.env))
  if not open then error({ "temple fair is closed", GRPC_FAILED_PRECONDITION }) end
  if coop.last_contest_fair == fair_id and not fair_always_open(context.env) then
    error({ "already entered this fair", GRPC_FAILED_PRECONDITION })
  end
  local entry
  for _, c in ipairs(coop.chickens) do
    if core.is_adult(c, now) and (not entry or c.beauty > entry.beauty) then entry = c end
  end
  if not entry then error({ "no adult chicken", GRPC_FAILED_PRECONDITION }) end
  life.spend(user_id, ENERGY.contest)

  local field = { { name = entry.name .. " (ของคุณ)", score = entry.beauty + math.random(0, 15), mine = true } }
  for _, v in ipairs(VILLAGERS) do
    table.insert(field, { name = v, score = math.random(35, 85) + math.random(0, 15), mine = false })
  end
  table.sort(field, function(a, b) return a.score > b.score end)

  local place = 0
  for i, f in ipairs(field) do if f.mine then place = i end end
  local prize = PRIZES[place] or 0
  pay(user_id, prize, "chicken_contest")

  coop.last_contest_fair = fair_id
  core.write(user_id, coop)
  return view(user_id, coop, { contest = field, place = place, prize = prize, entry_name = entry.name })
end

nk.register_rpc(rpc_get, "coop_get")
nk.register_rpc(rpc_act, "coop_act")
nk.register_rpc(rpc_contest, "contest_enter")
