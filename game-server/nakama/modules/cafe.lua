-- ร้านกาแฟของฉัน: when you run a coffee business (bank.lua bank_open "coffee") you get the real shop
-- in the City (the café building east of the land office). Pick a style for the walls and floor,
-- give it a name for the sign, and buy furniture to arrange inside — counters, the espresso machine,
-- tables, sofas, plants, lamps, art — so you can show it off (photo share in the client).
-- The takings themselves go into your bank savings by themselves (bank.lua settle).
-- The same goes for the lobby of your hotel (bank_open "hotel"): the hotel in the City, furnished
-- from its own list (reception desk, key rack, luggage trolley, chandelier, sofas...) through the
-- hotelshop_* calls, which take the same arguments. A new hotel starts with a furnished lobby.
-- And the shophouse you built on your city land (land.lua "shophouse"): its ground floor through the
-- shophouse_* calls, named and furnished by you, with a few pieces to start.
--
--   cafe_get    {}
--   cafe_place  {"item": "table_round", "x": 1.2, "z": -0.5, "r": 90}
--   cafe_move   {"id": 3, "x": 1.2, "z": -0.5, "r": 180}
--   cafe_remove {"id": 3}                        -> half the price back
--   cafe_style  {"style": "loft", "name": "Rnai Coffee"}

local nk = require("nakama")
local data = require("shop_data")
local land = require("land_core")

local CURRENCY = data.CURRENCY
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

-- the floor you can furnish (metres from the room's centre: right up to the walls, the client keeps
-- each piece inside by its own size)
local MAX_PIECES = 40
local NAME_MAX = 24
local STYLE_PRICE = 5000

local ITEMS = {
  counter      = { name = "เคาน์เตอร์บาร์",        price = 15000 },
  espresso     = { name = "เครื่องชงเอสเปรสโซ",     price = 25000 },
  cake_display = { name = "ตู้เค้กกระจก",           price = 8000 },
  shelf_cups   = { name = "ชั้นวางแก้วและเมล็ดกาแฟ", price = 4000 },
  menu_board   = { name = "กระดานเมนู",            price = 2500 },
  table_round  = { name = "โต๊ะกลม",                price = 3000 },
  table_square = { name = "โต๊ะสี่เหลี่ยม",           price = 3500 },
  table_long   = { name = "โต๊ะยาวไม้จริง",          price = 6000 },
  chair_wood   = { name = "เก้าอี้ไม้",              price = 1200 },
  stool_bar    = { name = "สตูลบาร์",               price = 1500 },
  armchair     = { name = "อาร์มแชร์",              price = 5000 },
  sofa         = { name = "โซฟา",                  price = 9000 },
  rug          = { name = "พรม",                   price = 2000 },
  plant_big    = { name = "ต้นไม้ใหญ่ในกระถาง",      price = 1500 },
  plant_small  = { name = "กระถางต้นไม้เล็ก",        price = 600 },
  lamp_pendant = { name = "โคมไฟห้อย",              price = 2000 },
  lamp_floor   = { name = "โคมไฟตั้งพื้น",           price = 2500 },
  bookshelf    = { name = "ชั้นหนังสือ",             price = 4500 },
  art_frame    = { name = "กรอบรูปติดผนัง",          price = 1800 },
  neon_sign    = { name = "ป้ายไฟนีออน",            price = 3500 },
  coffee_sacks = { name = "กระสอบเมล็ดกาแฟ",        price = 800 },
  bicycle      = { name = "จักรยานวินเทจ",           price = 6000 },
}
local ITEM_ORDER = { "counter", "espresso", "cake_display", "shelf_cups", "menu_board", "table_round", "table_square",
  "table_long", "chair_wood", "stool_bar", "armchair", "sofa", "rug", "plant_big", "plant_small", "lamp_pendant",
  "lamp_floor", "bookshelf", "art_frame", "neon_sign", "coffee_sacks", "bicycle" }

local HOTEL_ITEMS = {
  counter      = { name = "เคาน์เตอร์ต้อนรับ",       price = 20000 },
  key_rack     = { name = "ตู้กุญแจห้องพัก",          price = 6000 },
  luggage_cart = { name = "รถเข็นกระเป๋า",           price = 7000 },
  chandelier   = { name = "โคมระย้าคริสตัล",          price = 15000 },
  sofa         = { name = "โซฟาล็อบบี้",             price = 9000 },
  armchair     = { name = "อาร์มแชร์",              price = 5000 },
  table_round  = { name = "โต๊ะกลม",                price = 3000 },
  table_square = { name = "โต๊ะกลางรับแขก",          price = 3500 },
  chair_wood   = { name = "เก้าอี้ไม้",              price = 1200 },
  rug          = { name = "พรมผืนใหญ่",             price = 2000 },
  plant_big    = { name = "ต้นไม้ใหญ่ในกระถาง",      price = 1500 },
  plant_small  = { name = "กระถางต้นไม้เล็ก",        price = 600 },
  lamp_floor   = { name = "โคมไฟตั้งพื้น",           price = 2500 },
  lamp_pendant = { name = "โคมไฟห้อย",              price = 2000 },
  bookshelf    = { name = "ชั้นหนังสือ",             price = 4500 },
  art_frame    = { name = "กรอบรูปติดผนัง",          price = 1800 },
  espresso     = { name = "มุมกาแฟล็อบบี้",           price = 25000 },
  cake_display = { name = "ตู้เบเกอรี่",             price = 8000 },
  bicycle      = { name = "จักรยานวินเทจ",           price = 6000 },
}
local HOTEL_ORDER = { "counter", "key_rack", "luggage_cart", "chandelier", "sofa", "armchair", "table_round", "table_square",
  "chair_wood", "rug", "plant_big", "plant_small", "lamp_floor", "lamp_pendant", "bookshelf", "art_frame", "espresso",
  "cake_display", "bicycle" }

-- A new hotel's lobby: the desk at the back with the key rack behind it, two sofas facing over a
-- table on a rug, a chandelier, palms in the corners and a trolley by the door
local HOTEL_STARTER = {
  { item = "counter", x = 0, z = 2.9, r = 180 }, { item = "key_rack", x = 0, z = 4.2, r = 180 },
  { item = "rug", x = 0, z = -0.6, r = 0 }, { item = "sofa", x = -1.7, z = -0.6, r = 90 },
  { item = "sofa", x = 1.7, z = -0.6, r = 270 }, { item = "table_square", x = 0, z = -0.6, r = 0 },
  { item = "chandelier", x = 0, z = -0.6, r = 0 }, { item = "plant_big", x = -5.3, z = 3.8, r = 0 },
  { item = "plant_big", x = 5.3, z = 3.8, r = 0 }, { item = "luggage_cart", x = 4.6, z = -3.4, r = 300 },
}

-- The shophouse: everything from the café's list and the chandelier, a shop counter, a table to
-- talk business at, a sofa and plants to start with
local SHOPHOUSE_ITEMS, SHOPHOUSE_ORDER = {}, {}
for _, id in ipairs(ITEM_ORDER) do SHOPHOUSE_ITEMS[id] = ITEMS[id]; table.insert(SHOPHOUSE_ORDER, id) end
SHOPHOUSE_ITEMS.chandelier = HOTEL_ITEMS.chandelier
table.insert(SHOPHOUSE_ORDER, "chandelier")
local SHOPHOUSE_STARTER = {
  { item = "counter", x = -3.2, z = 3.4, r = 180 }, { item = "table_long", x = 1.6, z = 0.6, r = 0 },
  { item = "chair_wood", x = 0.8, z = -0.2, r = 0 }, { item = "chair_wood", x = 2.4, z = -0.2, r = 0 },
  { item = "sofa", x = 4.9, z = -2.8, r = 270 }, { item = "plant_big", x = -5.3, z = -4.1, r = 0 },
  { item = "plant_big", x = 5.3, z = 4.1, r = 0 }, { item = "lamp_pendant", x = 1.6, z = 0.6, r = 0 },
}

-- Your shophouse (land.lua): the city land is yours and the shophouse is up on it
local function shophouse(user_id)
  if not land.owned(user_id).city or land.read(user_id).city.build ~= "shophouse" then return nil end
  return { staff = 0, earned = 0 }
end

local KINDS = {
  cafe  = { key = "shop",  business = "coffee", none = "no coffee business", half_w = 4.7,  half_d = 3.76, items = ITEMS, order = ITEM_ORDER },
  hotel = { key = "hotel", business = "hotel",  none = "no hotel business",  half_w = 5.82, half_d = 4.32, items = HOTEL_ITEMS, order = HOTEL_ORDER,
            starter = HOTEL_STARTER },
  shophouse = { key = "shophouse", owner = shophouse, none = "no shophouse", half_w = 5.82, half_d = 4.7,
                items = SHOPHOUSE_ITEMS, order = SHOPHOUSE_ORDER, starter = SHOPHOUSE_STARTER },
}

local STYLES = {
  minimal = "มินิมอล",
  vintage = "วินเทจ",
  loft    = "ลอฟท์",
  thai    = "ไทยร่วมสมัย",
}
local STYLE_ORDER = { "minimal", "vintage", "loft", "thai" }

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

-- Your business of that kind at the bank (the first one if you somehow have two)
local function business(user_id, kind)
  if kind.owner then return kind.owner(user_id) end
  local objects = nk.storage_read({ { collection = "bank", key = "account", user_id = user_id } })
  local a = (#objects > 0) and objects[1].value or {}
  for _, b in ipairs(a.businesses or {}) do
    if b.type == kind.business then return b end
  end
  return nil
end

local function read(user_id, kind)
  local objects = nk.storage_read({ { collection = "cafe", key = kind.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.style = STYLES[s.style or ""] and s.style or "minimal"
  s.name = s.name or ""
  s.pieces = s.pieces or {}
  s.next = s.next or 1
  return s
end

local function write(user_id, kind, s)
  nk.storage_write({ { collection = "cafe", key = kind.key, user_id = user_id, value = s,
                       permission_read = 2, permission_write = 0 } })
end

local function clamp(v, lo, hi) return math.max(lo, math.min(hi, v)) end

local function spot(kind, req)
  local x, z = tonumber(req.x), tonumber(req.z)
  if not x or not z then err("bad position", GRPC_INVALID_ARGUMENT) end
  local r = math.floor(((tonumber(req.r) or 0) % 360) / 15 + 0.5) * 15 % 360
  return math.floor(clamp(x, -kind.half_w, kind.half_w) * 100 + 0.5) / 100, math.floor(clamp(z, -kind.half_d, kind.half_d) * 100 + 0.5) / 100, r
end

local function find(s, id)
  id = math.floor(tonumber(id) or 0)
  for i, p in ipairs(s.pieces) do if p.id == id then return i, p end end
  err("no such piece", GRPC_INVALID_ARGUMENT)
end

local function view(user_id, kind, s, b, extra)
  local items, styles = {}, {}
  for _, id in ipairs(kind.order) do
    table.insert(items, { id = id, name = kind.items[id].name, price = kind.items[id].price })
  end
  for _, id in ipairs(STYLE_ORDER) do table.insert(styles, { id = id, name = STYLES[id] }) end
  local result = {
    open = b ~= nil, staff = b and b.staff or 0, earned = b and b.earned or 0,
    name = s.name, style = s.style, cash = wallet(user_id),
    pieces = (#s.pieces > 0) and s.pieces or nil, -- an empty table would go out as {} not []
    items = items, styles = styles, style_price = STYLE_PRICE, max_pieces = MAX_PIECES,
    half_w = kind.half_w, half_d = kind.half_d, name_max = NAME_MAX,
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

-- Every call reads the shop and the bank; changes need a business of that kind. Registered once for
-- the café (cafe_*) and once for the hotel lobby (hotelshop_*).
local HANDLERS = {}
local function rpc(name, handler) HANDLERS[name] = handler or false end

local function register(prefix, kind)
  for name, handler in pairs(HANDLERS) do
    nk.register_rpc(function(context, payload)
      local user_id = context.user_id
      local req = decode(payload)
      local s = read(user_id, kind)
      local b = business(user_id, kind)
      local extra
      local changed = false
      -- a new hotel comes with its lobby furnished (once)
      if b and kind.starter and not s.seeded and #s.pieces == 0 then
        for _, p in ipairs(kind.starter) do
          table.insert(s.pieces, { id = s.next, item = p.item, x = p.x, z = p.z, r = p.r })
          s.next = s.next + 1
        end
        s.seeded = true
        changed = true
      end
      if handler then
        if not b then err(kind.none) end
        extra = handler(user_id, kind, s, req)
        changed = true
      end
      if changed then write(user_id, kind, s) end
      return view(user_id, kind, s, b, extra)
    end, prefix .. "_" .. name)
  end
end

rpc("get", nil)

rpc("place", function(user_id, kind, s, req)
  local item = kind.items[req.item or ""]
  if not item then err("unknown item", GRPC_INVALID_ARGUMENT) end
  if #s.pieces >= MAX_PIECES then err("shop is full") end
  if wallet(user_id) < item.price then err("not enough baht") end
  local x, z, r = spot(kind, req)
  pay(user_id, -item.price, "cafe_decor")
  local p = { id = s.next, item = req.item, x = x, z = z, r = r }
  s.next = s.next + 1
  table.insert(s.pieces, p)
  return { done = "วาง" .. item.name .. "แล้ว", placed = p.id }
end)

rpc("move", function(user_id, kind, s, req)
  local _, p = find(s, req.id)
  p.x, p.z, p.r = spot(kind, req)
  return { done = "ย้ายแล้ว" }
end)

rpc("remove", function(user_id, kind, s, req)
  local i, p = find(s, req.id)
  local item = kind.items[p.item]
  local back = item and math.floor(item.price / 2) or 0
  table.remove(s.pieces, i)
  pay(user_id, back, "cafe_decor_sell")
  return { done = "เก็บแล้ว ได้คืน " .. back .. " บาท" }
end)

rpc("style", function(user_id, kind, s, req)
  local msg
  if req.style and req.style ~= s.style then
    if not STYLES[req.style] then err("unknown style", GRPC_INVALID_ARGUMENT) end
    if wallet(user_id) < STYLE_PRICE then err("not enough baht") end
    pay(user_id, -STYLE_PRICE, "cafe_style")
    s.style = req.style
    msg = "ตกแต่งสไตล์" .. STYLES[req.style] .. "แล้ว"
  end
  if type(req.name) == "string" then
    local n = req.name:gsub("^%s+", ""):gsub("%s+$", ""):gsub("[%c]", "")
    if nk.utf8_len and nk.utf8_len(n) > NAME_MAX or #n > NAME_MAX * 3 then err("name too long") end
    if n ~= s.name then
      s.name = n
      msg = msg and (msg .. " · ตั้งชื่อร้านแล้ว") or "ตั้งชื่อร้านแล้ว"
    end
  end
  return { done = msg or "ไม่มีอะไรเปลี่ยน" }
end)

register("cafe", KINDS.cafe)
register("hotelshop", KINDS.hotel)
register("shophouse", KINDS.shophouse)
