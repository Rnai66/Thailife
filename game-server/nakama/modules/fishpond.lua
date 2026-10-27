-- Fish farming (เลี้ยงปลาในกระชัง) in the Kok Nong Na pond (สระโคกหนองนา). Every player has 3 net
-- cages. Stock a cage with fry bought on the spot, feed it once per game day, and harvest when the
-- fish are grown; the catch is sold right away to the fish trader. Fed fish grow bigger. Better
-- species open as the เลี้ยงปลา profession levels up. Growth runs on real time, like the rice.
--
--   pond_get  {}
--   pond_act  {"cage": 1..3, "action": "stock" | "feed" | "harvest", "species": id}

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")
local trade = require("trade_core")

local STORE = { collection = "fishpond", key = "cages" }
local CAGES = 3
local MIN = 60 * 1000
local FEED_PRICE = 40
local ENERGY = { stock = 4, feed = 3, harvest = 8 }

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

-- yield in kg: dry = never fed, fed = fed every game day it grew
local ORDER = { "duk", "tilapia", "tapien", "prawn" }
local SPECIES = {
  duk     = { name = "ปลาดุก",       price = 150, grow_ms = 20 * MIN, dry = { 6, 9 },   fed = { 10, 15 }, per_kg = 40,  level = 1 },
  tilapia = { name = "ปลานิล",       price = 200, grow_ms = 30 * MIN, dry = { 8, 11 },  fed = { 14, 20 }, per_kg = 45,  level = 1 },
  tapien  = { name = "ปลาตะเพียน",   price = 300, grow_ms = 45 * MIN, dry = { 10, 14 }, fed = { 18, 26 }, per_kg = 55,  level = 10 },
  prawn   = { name = "กุ้งก้ามกราม", price = 900, grow_ms = 60 * MIN, dry = { 5, 7 },   fed = { 9, 13 },  per_kg = 180, level = 25 },
}

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function read(user_id)
  local objects = nk.storage_read({ { collection = STORE.collection, key = STORE.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function write(user_id, v)
  nk.storage_write({ { collection = STORE.collection, key = STORE.key, user_id = user_id, value = v,
                       permission_read = 1, permission_write = 0 } })
end

local function wallet(user_id)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[data.CURRENCY] or 0
end

-- How many game days a batch spans (and so how many feeds make it fully fed)
local function feeds_needed(sp) return math.max(1, math.ceil(sp.grow_ms / life.DAY_MS)) end

local function stage(c, now)
  if not c or not c.species then return "empty", 0 end
  local progress = math.min(1, (now - c.stocked_ms) / SPECIES[c.species].grow_ms)
  return (progress >= 1) and "grown" or "growing", progress
end

local function view(user_id, cages, extra)
  local now = nk.time()
  local today = life.game_day(now)
  local level = trade.read(user_id).list.fishpond.level
  local list = {}
  for i = 1, CAGES do
    local c = cages[tostring(i)] or {}
    local st, progress = stage(c, now)
    local sp = c.species and SPECIES[c.species]
    table.insert(list, {
      id = i, stage = st, progress = progress, species = c.species or "", name = sp and sp.name or "",
      ready_in_s = (st == "growing") and math.ceil((c.stocked_ms + sp.grow_ms - now) / 1000) or 0,
      feeds = c.feeds or 0, feeds_needed = sp and feeds_needed(sp) or 0,
      fed_today = c.fed_day == today,
    })
  end
  local species = {}
  for _, id in ipairs(ORDER) do
    local sp = SPECIES[id]
    table.insert(species, { id = id, name = sp.name, price = sp.price, grow_s = sp.grow_ms / 1000,
                            per_kg = sp.per_kg, fed_kg = sp.fed[2], level = sp.level, unlocked = level >= sp.level })
  end
  local result = { cages = list, species = species, feed_price = FEED_PRICE, level = level }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

nk.register_rpc(function(context, payload)
  return view(context.user_id, read(context.user_id))
end, "pond_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  local id = math.floor(tonumber(req.cage) or 0)
  if id < 1 or id > CAGES then err("unknown cage", GRPC_INVALID_ARGUMENT) end

  local cages = read(user_id)
  local key = tostring(id)
  local c = cages[key] or {}
  local now = nk.time()
  local st = stage(c, now)
  local extra = { action = req.action, cage = id }

  if req.action == "stock" then
    if st ~= "empty" then err("cage not empty") end
    local sp = SPECIES[req.species or ""]
    if not sp then err("unknown species", GRPC_INVALID_ARGUMENT) end
    if trade.read(user_id).list.fishpond.level < sp.level then err("level too low") end
    if wallet(user_id) < sp.price then err("not enough baht") end
    life.spend(user_id, ENERGY.stock)
    nk.wallet_update(user_id, { [data.CURRENCY] = -sp.price }, { reason = "fish_fry", species = req.species }, true)
    c = { species = req.species, stocked_ms = now, feeds = 0 }
    trade.gain(user_id, "fishpond", 10)
    extra.done = "ปล่อยลูก" .. sp.name .. "ลงกระชัง " .. id

  elseif req.action == "feed" then
    if st ~= "growing" then err("nothing growing") end
    local today = life.game_day(now)
    if c.fed_day == today then err("already fed") end
    if wallet(user_id) < FEED_PRICE then err("not enough baht") end
    life.spend(user_id, ENERGY.feed)
    nk.wallet_update(user_id, { [data.CURRENCY] = -FEED_PRICE }, { reason = "fish_feed" }, true)
    c.feeds = (c.feeds or 0) + 1
    c.fed_day = today
    trade.gain(user_id, "fishpond", 8)
    extra.done = "ให้อาหารปลากระชัง " .. id .. " แล้ว"

  elseif req.action == "harvest" then
    if st ~= "grown" then err("not grown yet") end
    local sp = SPECIES[c.species]
    local fed = math.min(1, (c.feeds or 0) / feeds_needed(sp))
    local lo = math.floor(sp.dry[1] + (sp.fed[1] - sp.dry[1]) * fed + 0.5)
    local hi = math.floor(sp.dry[2] + (sp.fed[2] - sp.dry[2]) * fed + 0.5)
    local kg = math.random(lo, hi)
    local earned = kg * sp.per_kg
    life.spend(user_id, ENERGY.harvest)
    nk.wallet_update(user_id, { [data.CURRENCY] = earned }, { reason = "fish_pond", species = c.species, kg = kg }, true)
    trade.gain_many(user_id, { fishpond = 25 + kg * 2, merchant = 5 }, true)
    extra.harvested_kg = kg
    extra.earned = earned
    extra.done = "จับ" .. sp.name .. " " .. kg .. " กก. ขายให้แม่ค้าปลาได้ " .. earned .. " บาท"
    c = {}

  else
    err("unknown action", GRPC_INVALID_ARGUMENT)
  end

  cages[key] = c
  write(user_id, cages)
  return view(user_id, cages, extra)
end, "pond_act")
