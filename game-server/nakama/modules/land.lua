-- Your own land (ที่ดิน), bought at the city bank (bank_buy "land_*"), put to use where it lies:
--   land_rice  ที่นาติดคลอง 5 ไร่   six more rice paddies beside Kok Nong Na (farming.lua plots 7..12)
--   land_plain ที่ดินเปล่า 1 ไร่     build one of: a crop field (ไร่, 4 plots: corn, cassava, sugarcane),
--                                 a second cow pen (ranch.lua: room for PEN_COWS more cows) or a house to let
--   land_city  ที่ดินติดถนนในเมือง    build a shophouse (ตึกแถว) to let
-- Rent from the buildings piles up per game day (up to MAX_RENT_DAYS) until you collect it there.
--
--   land_get     {}
--   land_build   {"parcel": "plain"|"city", "build": "field"|"pen"|"rental"|"shophouse"}
--   land_clear   {"parcel": "plain"|"city"}                 -> knock the building down (no refund)
--   land_collect {"parcel": "plain"|"city"}                 -> take the rent
--   land_field   {"plot": 1..4, "action": "plow"|"plant"|"water"|"harvest", "crop": "corn"|...}
--   land_sell    {"crop": "corn"|...}                       -> sell that crop from the field store

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")
local quest = require("quest_core")
local core = require("land_core")

local CURRENCY = data.CURRENCY
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local FIELD_PLOTS = 4
local MAX_RENT_DAYS = 10

-- What each parcel can carry: cost to build, rent per game day
local PARCELS = {
  plain = { asset = "land_plain", builds = { "field", "pen", "rental" } },
  city  = { asset = "land_city",  builds = { "shophouse" } },
}
local BUILDS = {
  field     = { name = "ไร่ 4 แปลง",          cost = 50000,   rent = 0 },
  pen       = { name = "คอกวัวแปลงใหม่",       cost = 150000,  rent = 0 },
  rental    = { name = "บ้านเช่า",            cost = 400000,  rent = 800 },
  shophouse = { name = "ตึกแถว 3 ชั้นให้เช่า",  cost = 2000000, rent = 5000 },
}

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

local function wallet(user_id)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[CURRENCY] or 0
end

local function pay(user_id, delta, reason)
  if delta ~= 0 then nk.wallet_update(user_id, { [CURRENCY] = delta }, { reason = reason }, true) end
end

local function game_days(now) return (now - life.EPOCH_MS) / life.DAY_MS end

local read, write = core.read, core.write

local function rent_due(p)
  local b = BUILDS[p.build or ""]
  if not b or b.rent == 0 or not p.since then return 0 end
  local days = math.min(MAX_RENT_DAYS, math.max(0, game_days(nk.time()) - p.since))
  return math.floor(b.rent * days)
end

local function crop_of(p) return data.FIELD_CROPS[p.crop or ""] end

local function plot_stage(p, now)
  if not p or not p.state or p.state == "empty" then return "empty", 0 end
  if p.state == "plowed" then return "plowed", 0 end
  local c = crop_of(p)
  if not c then return "empty", 0 end
  local progress = math.min(1, (now - p.planted_ms) / c.grow_ms)
  return (progress >= 1) and "ripe" or "growing", progress
end

local function view(user_id, s, extra)
  local now = nk.time()
  local own = core.owned(user_id)
  local plots = {}
  for i = 1, FIELD_PLOTS do
    local p = s.plain.field[tostring(i)] or {}
    local stage, progress = plot_stage(p, now)
    local c = crop_of(p)
    table.insert(plots, {
      id = i, stage = stage, progress = progress, watered = p.watered == true,
      crop = (stage == "growing" or stage == "ripe") and p.crop or "",
      crop_name = (stage == "growing" or stage == "ripe") and c.name or "",
      ripe_in_s = (stage == "growing") and math.ceil((p.planted_ms + c.grow_ms - now) / 1000) or 0,
    })
  end
  local crops = data.read_barn(user_id)
  local list = {}
  for _, id in ipairs(data.FIELD_ORDER) do
    local c = data.FIELD_CROPS[id]
    table.insert(list, { id = id, name = c.name, seed_price = c.seed_price, grow_s = c.grow_ms / 1000,
                         price_per_kg = c.price_per_kg, stored = crops.field[id] or 0 })
  end
  local builds = {}
  for id, b in pairs(BUILDS) do table.insert(builds, { id = id, name = b.name, cost = b.cost, rent = b.rent }) end
  table.sort(builds, function(a, b) return a.cost < b.cost end)
  local result = {
    own_plain = own.plain, own_rice = own.rice, own_city = own.city,
    plain_build = own.plain and (s.plain.build or "") or "", city_build = own.city and (s.city.build or "") or "",
    plain_rent = own.plain and rent_due(s.plain) or 0, city_rent = own.city and rent_due(s.city) or 0,
    max_rent_days = MAX_RENT_DAYS, pen_cows = core.PEN_COWS, store = data.FIELD_STORE,
    plots = plots, crops = list, builds = builds,
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function parcel_of(req, s, own)
  local id = req.parcel
  if not PARCELS[id] then err("unknown parcel", GRPC_INVALID_ARGUMENT) end
  if not own[id] then err("not your land") end
  return s[id], PARCELS[id]
end

local function rpc(name, handler)
  nk.register_rpc(function(context, payload)
    local user_id = context.user_id
    local s = read(user_id)
    local extra = handler(user_id, s, decode(payload))
    write(user_id, s)
    return view(user_id, s, extra)
  end, name)
end

rpc("land_get", function() return nil end)

rpc("land_build", function(user_id, s, req)
  local p, parcel = parcel_of(req, s, core.owned(user_id))
  if p.build then err("already built") end
  local ok = false
  for _, b in ipairs(parcel.builds) do if b == req.build then ok = true end end
  if not ok then err("cannot build that here", GRPC_INVALID_ARGUMENT) end
  local b = BUILDS[req.build]
  if wallet(user_id) < b.cost then err("not enough baht") end
  life.spend(user_id, 15)
  pay(user_id, -b.cost, "land_build")
  p.build = req.build
  p.since = game_days(nk.time())
  if req.build == "field" then p.field = {} end
  return { done = "สร้าง" .. b.name .. " แล้ว " .. b.cost .. " บาท" }
end)

rpc("land_clear", function(user_id, s, req)
  local p = parcel_of(req, s, core.owned(user_id))
  if not p.build then err("nothing built") end
  if p.build == "pen" then
    -- the cows living there must fit back in the village pen first
    local objects = nk.storage_read({ { collection = "ranch", key = "herd", user_id = user_id } })
    local h = (#objects > 0) and objects[1].value or {}
    local cows = type(h.cows) == "table" and #h.cows or 0
    if cows > 8 + 4 * (h.pen or 0) then err("pen in use") end
  end
  local name = BUILDS[p.build].name
  local rent = rent_due(p)
  if rent > 0 then pay(user_id, rent, "land_rent") end
  p.build, p.since = nil, nil
  p.field = {}
  return { done = "รื้อ" .. name .. "แล้ว" .. (rent > 0 and (" · ได้ค่าเช่าค้าง " .. rent .. " บาท") or "") }
end)

rpc("land_collect", function(user_id, s, req)
  local p = parcel_of(req, s, core.owned(user_id))
  local rent = rent_due(p)
  if rent <= 0 then err("no rent") end
  pay(user_id, rent, "land_rent")
  p.since = game_days(nk.time())
  return { done = "เก็บค่าเช่า " .. rent .. " บาท" }
end)

rpc("land_field", function(user_id, s, req)
  if not core.owned(user_id).plain then err("not your land") end
  if s.plain.build ~= "field" then err("no field") end
  local id = math.floor(tonumber(req.plot) or 0)
  if id < 1 or id > FIELD_PLOTS then err("unknown plot", GRPC_INVALID_ARGUMENT) end
  local key = tostring(id)
  local p = s.plain.field[key] or { state = "empty" }
  local stage = plot_stage(p, nk.time())
  local action = req.action
  local extra = { action = action, plot = id }

  if action == "plow" then
    if stage ~= "empty" then err("plot is not empty") end
    life.spend(user_id, life.COST.plow)
    p = { state = "plowed" }
  elseif action == "plant" then
    if stage ~= "plowed" then err("plow first") end
    local c = data.FIELD_CROPS[req.crop or ""]
    if not c then err("unknown crop", GRPC_INVALID_ARGUMENT) end
    if wallet(user_id) < c.seed_price then err("not enough baht") end
    life.spend(user_id, life.COST.plant)
    pay(user_id, -c.seed_price, "field_seed")
    p = { state = "planted", crop = req.crop, planted_ms = nk.time(), watered = false }
    quest.progress(user_id, "plant", 1)
  elseif action == "water" then
    if stage ~= "growing" then err("nothing growing") end
    if p.watered then err("already watered") end
    life.spend(user_id, life.COST.water)
    p.watered = true
  elseif action == "harvest" then
    if stage ~= "ripe" then err("not ripe yet") end
    local c = crop_of(p)
    local range = p.watered and c.yield_watered or c.yield_dry
    local kg = math.random(range[1], range[2])
    local crops = data.read_barn(user_id)
    local stored = 0
    for _, v in pairs(crops.field) do stored = stored + v end
    if stored + kg > data.FIELD_STORE then err("store full") end
    life.spend(user_id, life.COST.harvest)
    crops.field[p.crop] = (crops.field[p.crop] or 0) + kg
    data.write_barn(user_id, crops)
    extra.harvested_kg = kg
    extra.harvested_name = c.name
    quest.progress(user_id, "harvest", 1)
    p = { state = "empty" }
  else
    err("unknown action", GRPC_INVALID_ARGUMENT)
  end
  s.plain.field[key] = p
  return extra
end)

rpc("land_sell", function(user_id, s, req)
  local c = data.FIELD_CROPS[req.crop or ""]
  if not c then err("unknown crop", GRPC_INVALID_ARGUMENT) end
  local crops = data.read_barn(user_id)
  local kg = crops.field[req.crop] or 0
  if kg <= 0 then err("nothing to sell") end
  local earned = kg * c.price_per_kg
  crops.field[req.crop] = 0
  data.write_barn(user_id, crops)
  pay(user_id, earned, "sell_field_crop")
  quest.progress(user_id, "sell_produce", math.max(1, math.floor(kg / 50)))
  return { done = "ขาย" .. c.name .. " " .. kg .. " กก. ได้ " .. earned .. " บาท" }
end)

