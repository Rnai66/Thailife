-- Fair ticket system (v1.3): a gate pass (บัตรผ่านประตู) for each festival, ride coupons (คูปอง) for
-- the rides and skill games, points (แต้ม) won by skill, and the prize counter (ซุ้มแลกรางวัล) where
-- points buy prizes up to the Red Cross fair style big ones. Nothing here is random: the score is
-- what the player did, and a prize costs a fixed number of points.
--   fair_get     {}                              -> pass, coupons, points, games, prize list
--   fair_pass    {}                              -> buys today's gate pass (comes with coupons)
--   fair_coupons {"pack": "s"|"m"|"l"}           -> buys more coupons (needs today's pass)
--   fair_ride    {"ride": "ferris"}              -> spends coupons on a ride, restores energy
--   fair_start   {"game": "shooting"}            -> spends a coupon, opens a round
--   fair_finish  {"round": "...", "hits": n}     -> closes the round, pays points for the hits
--   fair_redeem  {"prize": "teddy"}              -> spends points on a prize

local nk = require("nakama")
local life = require("life_core")
local data = require("shop_data")

local STATE = { collection = "fair", key = "tickets" }
local CURRENCY = data.CURRENCY
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

-- Gate pass price by venue size, and the coupons that come with it
local PASS = {
  big     = { price = 40, coupons = 3 },
  small   = { price = 20, coupons = 2 },
  village = { price = 10, coupons = 1 },
}
local COUPON_PACKS = {
  s = { coupons = 1,  price = 10 },
  m = { coupons = 5,  price = 45 },
  l = { coupons = 12, price = 100 },
}

-- Rides: coupons per go, energy restored, big = city festivals only
local RIDES = {
  ferris         = { name = "ชิงช้าสวรรค์", coupons = 2, energy = 10, big = true },
  carousel       = { name = "ม้าหมุน",     coupons = 1, energy = 6,  big = true },
  ferris_village = { name = "ชิงช้าสวรรค์", coupons = 2, energy = 10 },
}

-- Skill games: one coupon a round. shots = throws/shots in a round, min_s = the fastest a real
-- round can be played, points[hits + 1] = what the round pays.
local GAMES = {
  shooting = { name = "ยิงปืนจุก", shots = 10, min_s = 4,
               points = { 0, 0, 1, 2, 3, 4, 5, 6, 8, 10, 14 } },
  rings    = { name = "โยนห่วง",   shots = 5, min_s = 4, points = { 0, 1, 3, 5, 8, 14 } },
  darts    = { name = "ปาโป่ง",    shots = 5, min_s = 3, points = { 0, 1, 3, 5, 8, 14 } },
}
local GAME_ORDER = { "shooting", "rings", "darts" }
local ROUND_TTL_S = 300

-- Prize counter: points for each prize. Goods cost more points than the shop pays for them, so the
-- counter is never a way to turn coupons into baht. big = shown on the podium.
local PRIZES = {
  { id = "look_chin",   points = 12,   food = "look_chin",       icon = "food_look_chin" },
  { id = "bubble_tea",  points = 25,   food = "bubble_tea",      icon = "food_bubble_tea" },
  { id = "mama",        points = 15,   food = "mama",            icon = "food_mama" },
  { id = "soap",        points = 15,   goods = "soap",           icon = "sell_goods_soap" },
  { id = "toothpaste",  points = 25,   goods = "toothpaste",     icon = "sell_goods_toothpaste" },
  { id = "basin",       points = 60,   goods = "basin",          icon = "sell_goods_basin" },
  { id = "teddy",       points = 80,   goods = "teddy",          icon = "sell_goods_teddy" },
  { id = "flask",       points = 120,  goods = "flask",          icon = "sell_goods_flask" },
  { id = "mat",         points = 100,  furniture = "mat",        icon = "furn_mat" },
  { id = "backpack",    points = 180,  goods = "backpack",       icon = "sell_goods_backpack" },
  { id = "radio",       points = 300,  furniture = "radio",      icon = "furn_radio" },
  { id = "futon",       points = 320,  goods = "futon",          icon = "sell_goods_futon",       big = true },
  { id = "fan",         points = 350,  furniture = "fan",        icon = "furn_fan",               big = true },
  { id = "rice_cooker", points = 450,  goods = "rice_cooker",    icon = "sell_goods_rice_cooker", big = true },
  { id = "bicycle",     points = 1200, bicycle = true,           icon = "goods_bicycle",          big = true, name = "รถจักรยาน" },
  { id = "tv",          points = 1500, furniture = "tv",         icon = "furn_tv",                big = true },
  { id = "fridge",      points = 1800, furniture = "fridge",     icon = "furn_fridge",            big = true },
}

local function prize_name(p)
  if p.name then return p.name end
  if p.food then return data.FOODS[p.food].name end
  if p.goods then return data.GOODS[p.goods].name end
  return data.FURNITURE[p.furniture].name
end

local function fair_always_open(env) return env ~= nil and env.fair_always_open == "true" end

-- Venue size of today's festival as the pass sees it (the village temple fair is the cheap one)
local function venue(f)
  if f.scale == "big" or f.scale == "both" then return "big" end
  if f.scale == "small" or f.scale == "beach" then return "small" end
  return "village"
end

local function read(user_id)
  local objects = nk.storage_read({ { collection = STATE.collection, key = STATE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.coupons = s.coupons or 0
  s.points = s.points or 0
  s.best = s.best or {}
  s.redeemed = s.redeemed or {}
  return s
end

local function write(user_id, s)
  nk.storage_write({ { collection = STATE.collection, key = STATE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

local function pay(user_id, baht, meta)
  local account = nk.account_get_id(user_id)
  local wallet = account.wallet or {}
  if type(wallet) == "string" then wallet = nk.json_decode(wallet) end
  if (wallet[CURRENCY] or 0) < baht then error({ "not enough baht", GRPC_FAILED_PRECONDITION }) end
  nk.wallet_update(user_id, { [CURRENCY] = -baht }, meta, true)
end

local function owns_bicycle(user_id)
  local objects = nk.storage_read({ { collection = "bank", key = "account", user_id = user_id } })
  local a = (#objects > 0) and objects[1].value or {}
  return a.assets ~= nil and a.assets.car_bicycle == true, a
end

-- Today's festival or an error; returns fair id and the festival
local function festival_now(context)
  local open, fair_id, _, _, f = life.fair(nk.time(), fair_always_open(context.env))
  if not open then error({ "no festival today", GRPC_FAILED_PRECONDITION }) end
  return fair_id, f
end

local function need_pass(s, fair_id)
  if s.pass ~= fair_id then error({ "no pass", GRPC_FAILED_PRECONDITION }) end
end

local function view(context, s, extra)
  local open, fair_id, _, _, f = life.fair(nk.time(), fair_always_open(context.env))
  local where = open and venue(f) or ""
  local games = {}
  for _, id in ipairs(GAME_ORDER) do
    local g = GAMES[id]
    table.insert(games, { id = id, name = g.name, shots = g.shots, points = g.points, best = s.best[id] or 0 })
  end
  local rides = {}
  for id, r in pairs(RIDES) do table.insert(rides, { id = id, name = r.name, coupons = r.coupons, energy = r.energy }) end
  table.sort(rides, function(a, b) return a.id < b.id end)
  local prizes = {}
  for _, p in ipairs(PRIZES) do
    table.insert(prizes, { id = p.id, name = prize_name(p), points = p.points, icon = p.icon, big = p.big == true,
                           redeemed = s.redeemed[p.id] or 0 })
  end
  local packs = {}
  for _, id in ipairs({ "s", "m", "l" }) do
    table.insert(packs, { id = id, coupons = COUPON_PACKS[id].coupons, price = COUPON_PACKS[id].price })
  end
  local result = {
    open = open, fair = open and f.name or "", venue = where,
    has_pass = open and s.pass == fair_id,
    pass_price = open and PASS[where].price or 0, pass_coupons = open and PASS[where].coupons or 0,
    coupons = s.coupons, points = s.points,
    games = games, rides = rides, prizes = prizes, packs = packs,
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  return req
end

nk.register_rpc(function(context, payload)
  return view(context, read(context.user_id))
end, "fair_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local fair_id, f = festival_now(context)
  local s = read(user_id)
  if s.pass == fair_id then error({ "already have pass", GRPC_FAILED_PRECONDITION }) end
  local p = PASS[venue(f)]
  pay(user_id, p.price, { reason = "fair_pass", fair = fair_id })
  s.pass = fair_id
  s.coupons = s.coupons + p.coupons
  write(user_id, s)
  return view(context, s, { bought = "pass", got = p.coupons, paid = p.price })
end, "fair_pass")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local pack = COUPON_PACKS[req.pack or ""]
  if not pack then error({ "unknown pack", GRPC_INVALID_ARGUMENT }) end
  local fair_id = festival_now(context)
  local s = read(user_id)
  need_pass(s, fair_id)
  pay(user_id, pack.price, { reason = "fair_coupons", pack = req.pack })
  s.coupons = s.coupons + pack.coupons
  write(user_id, s)
  return view(context, s, { bought = "coupons", got = pack.coupons, paid = pack.price })
end, "fair_coupons")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local r = RIDES[req.ride or ""]
  if not r then error({ "unknown ride", GRPC_INVALID_ARGUMENT }) end
  local fair_id, f = festival_now(context)
  if r.big and f.scale ~= "big" and f.scale ~= "both" then error({ "big festivals only", GRPC_FAILED_PRECONDITION }) end
  local s = read(user_id)
  need_pass(s, fair_id)
  if s.coupons < r.coupons then error({ "not enough coupons", GRPC_FAILED_PRECONDITION }) end
  s.coupons = s.coupons - r.coupons
  write(user_id, s)
  local e = life.add_energy(user_id, r.energy)
  return view(context, s, { ride = req.ride, ride_name = r.name, used = r.coupons, gained = r.energy, energy = math.floor(e.energy) })
end, "fair_ride")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local g = GAMES[req.game or ""]
  if not g then error({ "unknown game", GRPC_INVALID_ARGUMENT }) end
  local fair_id = festival_now(context)
  local s = read(user_id)
  need_pass(s, fair_id)
  if s.coupons < 1 then error({ "not enough coupons", GRPC_FAILED_PRECONDITION }) end
  s.coupons = s.coupons - 1
  -- One open round at a time; starting another forfeits the old one (its coupon is spent)
  local id = nk.uuid_v4()
  s.round = { id = id, game = req.game, started = nk.time() }
  write(user_id, s)
  return view(context, s, { round = id, game = req.game, shots = g.shots, seed = math.random(1, 2147483646) })
end, "fair_start")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local s = read(user_id)
  local r = s.round
  if r == nil or r.id ~= req.round then error({ "no round", GRPC_FAILED_PRECONDITION }) end
  local g = GAMES[r.game]
  local elapsed = (nk.time() - r.started) / 1000
  s.round = nil
  if elapsed > ROUND_TTL_S then
    write(user_id, s)
    error({ "round expired", GRPC_FAILED_PRECONDITION })
  end
  local hits = math.floor(tonumber(req.hits) or 0)
  if hits < 0 or hits > g.shots then error({ "invalid hits", GRPC_INVALID_ARGUMENT }) end
  -- Too quick to have really played: the round still counts but pays nothing
  if elapsed < g.min_s then hits = 0 end
  local earned = g.points[hits + 1]
  s.points = s.points + earned
  local record = hits > (s.best[r.game] or 0)
  if record then s.best[r.game] = hits end
  write(user_id, s)
  return view(context, s, { game = r.game, game_name = g.name, hits = hits, shots = g.shots, earned = earned, record = record })
end, "fair_finish")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local p
  for _, x in ipairs(PRIZES) do if x.id == req.prize then p = x end end
  if not p then error({ "unknown prize", GRPC_INVALID_ARGUMENT }) end
  -- The counter is open whenever a festival is on; points keep from one festival to the next
  festival_now(context)
  local s = read(user_id)
  if s.points < p.points then error({ "not enough points", GRPC_FAILED_PRECONDITION }) end
  if p.bicycle then
    local owned, a = owns_bicycle(user_id)
    if owned then error({ "already own", GRPC_FAILED_PRECONDITION }) end
    a.assets = a.assets or {}
    a.assets.car_bicycle = true
    nk.storage_write({ { collection = "bank", key = "account", user_id = user_id, value = a, permission_read = 1, permission_write = 0 } })
  else
    local items = data.read_items(user_id)
    if p.food then items.food[p.food] = (items.food[p.food] or 0) + 1 end
    if p.goods then items.goods[p.goods] = (items.goods[p.goods] or 0) + 1 end
    if p.furniture then items.furniture[p.furniture] = (items.furniture[p.furniture] or 0) + 1 end
    data.write_items(user_id, items)
  end
  s.points = s.points - p.points
  s.redeemed[p.id] = (s.redeemed[p.id] or 0) + 1
  write(user_id, s)
  return view(context, s, { prize = prize_name(p), icon = p.icon, big = p.big == true, spent = p.points })
end, "fair_redeem")
