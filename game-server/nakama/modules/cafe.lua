-- ร้านกาแฟของฉัน: when you run a coffee business (bank.lua bank_open "coffee") you get the real shop
-- in the City (the café building east of the land office). Pick a style for the walls and floor,
-- give it a name for the sign, and buy furniture to arrange inside — counters, the espresso machine,
-- tables, sofas, plants, lamps, art — so you can show it off (photo share in the client).
-- The takings themselves go into your bank savings by themselves (bank.lua settle).
--
--   cafe_get    {}
--   cafe_place  {"item": "table_round", "x": 1.2, "z": -0.5, "r": 90}
--   cafe_move   {"id": 3, "x": 1.2, "z": -0.5, "r": 180}
--   cafe_remove {"id": 3}                        -> half the price back
--   cafe_style  {"style": "loft", "name": "Rnai Coffee"}

local nk = require("nakama")
local data = require("shop_data")

local CURRENCY = data.CURRENCY
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local SHOP = { collection = "cafe", key = "shop" }
local HALF_W, HALF_D = 4.5, 3.5   -- the floor you can furnish (metres from the room's centre)
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

-- Your coffee business at the bank (the first one if you somehow have two)
local function coffee(user_id)
  local objects = nk.storage_read({ { collection = "bank", key = "account", user_id = user_id } })
  local a = (#objects > 0) and objects[1].value or {}
  for _, b in ipairs(a.businesses or {}) do
    if b.type == "coffee" then return b end
  end
  return nil
end

local function read(user_id)
  local objects = nk.storage_read({ { collection = SHOP.collection, key = SHOP.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.style = STYLES[s.style or ""] and s.style or "minimal"
  s.name = s.name or ""
  s.pieces = s.pieces or {}
  s.next = s.next or 1
  return s
end

local function write(user_id, s)
  nk.storage_write({ { collection = SHOP.collection, key = SHOP.key, user_id = user_id, value = s,
                       permission_read = 2, permission_write = 0 } })
end

local function clamp(v, lo, hi) return math.max(lo, math.min(hi, v)) end

local function spot(req)
  local x, z = tonumber(req.x), tonumber(req.z)
  if not x or not z then err("bad position", GRPC_INVALID_ARGUMENT) end
  local r = math.floor(((tonumber(req.r) or 0) % 360) / 15 + 0.5) * 15 % 360
  return math.floor(clamp(x, -HALF_W, HALF_W) * 100 + 0.5) / 100, math.floor(clamp(z, -HALF_D, HALF_D) * 100 + 0.5) / 100, r
end

local function find(s, id)
  id = math.floor(tonumber(id) or 0)
  for i, p in ipairs(s.pieces) do if p.id == id then return i, p end end
  err("no such piece", GRPC_INVALID_ARGUMENT)
end

local function view(user_id, s, b, extra)
  local items, styles = {}, {}
  for _, id in ipairs(ITEM_ORDER) do
    table.insert(items, { id = id, name = ITEMS[id].name, price = ITEMS[id].price })
  end
  for _, id in ipairs(STYLE_ORDER) do table.insert(styles, { id = id, name = STYLES[id] }) end
  local result = {
    open = b ~= nil, staff = b and b.staff or 0, earned = b and b.earned or 0,
    name = s.name, style = s.style, cash = wallet(user_id),
    pieces = (#s.pieces > 0) and s.pieces or nil, -- an empty table would go out as {} not []
    items = items, styles = styles, style_price = STYLE_PRICE, max_pieces = MAX_PIECES,
    half_w = HALF_W, half_d = HALF_D, name_max = NAME_MAX,
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

-- Every call reads the shop and the bank; changes need a coffee business
local function rpc(name, handler)
  nk.register_rpc(function(context, payload)
    local user_id = context.user_id
    local req = decode(payload)
    local s = read(user_id)
    local b = coffee(user_id)
    local extra
    if handler then
      if not b then err("no coffee business") end
      extra = handler(user_id, s, req)
      write(user_id, s)
    end
    return view(user_id, s, b, extra)
  end, name)
end

rpc("cafe_get", nil)

rpc("cafe_place", function(user_id, s, req)
  local item = ITEMS[req.item or ""]
  if not item then err("unknown item", GRPC_INVALID_ARGUMENT) end
  if #s.pieces >= MAX_PIECES then err("shop is full") end
  if wallet(user_id) < item.price then err("not enough baht") end
  local x, z, r = spot(req)
  pay(user_id, -item.price, "cafe_decor")
  local p = { id = s.next, item = req.item, x = x, z = z, r = r }
  s.next = s.next + 1
  table.insert(s.pieces, p)
  return { done = "วาง" .. item.name .. "แล้ว", placed = p.id }
end)

rpc("cafe_move", function(user_id, s, req)
  local _, p = find(s, req.id)
  p.x, p.z, p.r = spot(req)
  return { done = "ย้ายแล้ว" }
end)

rpc("cafe_remove", function(user_id, s, req)
  local i, p = find(s, req.id)
  local item = ITEMS[p.item]
  local back = item and math.floor(item.price / 2) or 0
  table.remove(s.pieces, i)
  pay(user_id, back, "cafe_decor_sell")
  return { done = "เก็บแล้ว ได้คืน " .. back .. " บาท" }
end)

rpc("cafe_style", function(user_id, s, req)
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
