-- Shared game data for economy.lua, fishing.lua and career.lua (no RPCs here).
-- require("shop_data") returns this table.

local M = {}

M.CURRENCY = "baht"
M.ITEMS = { collection = "inventory", key = "items" }

-- Fishing rods: better rods bite sooner, favour rarer fish and land bigger ones.
-- weights are per fish id (see fishing.lua FISH)
M.RODS = {
  bamboo = { name = "เบ็ดไม้ไผ่",  bite_min = 2000, bite_max = 6000, size = 1.00,
             weights = { tilapia = 40, barb = 30, snakehead = 20, catfish = 10 } },
  fiber  = { name = "เบ็ดไฟเบอร์", bite_min = 1500, bite_max = 5000, size = 1.15,
             weights = { tilapia = 35, barb = 25, snakehead = 25, catfish = 15 } },
  carbon = { name = "เบ็ดคาร์บอน", bite_min = 1000, bite_max = 4000, size = 1.30,
             weights = { tilapia = 25, barb = 20, snakehead = 30, catfish = 25 } },
}

-- Rice varieties (พันธุ์ข้าว): seeds are bought at the shop, one bag plants one plot.
M.RICE = {
  khao_niao = { name = "ข้าวเหนียว กข6",    seed_price = 20, grow_ms = 12 * 60 * 1000, price_per_kg = 12,
                yield_watered = { 10, 14 }, yield_dry = { 5, 7 } },
  hommali   = { name = "ข้าวหอมมะลิ 105", seed_price = 30, grow_ms = 16 * 60 * 1000, price_per_kg = 18,
                yield_watered = { 9, 12 },  yield_dry = { 4, 6 } },
}
M.RICE_ORDER = { "khao_niao", "hommali" }

-- Food & drinks (ของกิน): bought into the bag, eaten any time to restore energy
M.FOODS = {
  soda        = { name = "น้ำอัดลม",         energy = 8 },
  iced_coffee = { name = "กาแฟเย็น",         energy = 15 },
  moo_ping    = { name = "ข้าวเหนียวหมูปิ้ง", energy = 20 },
  som_tam     = { name = "ส้มตำ",            energy = 25 },
  thai_tea    = { name = "ชาไทย",            energy = 18 },
  coffee      = { name = "กาแฟสด",           energy = 25 },
  khao_man_gai = { name = "ข้าวมันไก่",       energy = 35 },
  -- City fresh market (ตลาดสด)
  jok          = { name = "โจ๊กหมู",           energy = 22 },
  fruit        = { name = "ผลไม้รวม",          energy = 12 },
  pad_thai     = { name = "ผัดไทย",            energy = 32 },
  -- Tambon evening market (ตลาดนัด)
  look_chin    = { name = "ลูกชิ้นปิ้ง",        energy = 10 },
  fried_chicken = { name = "ไก่ทอด",           energy = 24 },
  bubble_tea   = { name = "ชานมไข่มุก",        energy = 18 },
  mango_sticky = { name = "ข้าวเหนียวมะม่วง",    energy = 28 },
}
M.FOOD_ORDER = { "soda", "iced_coffee", "moo_ping", "som_tam", "thai_tea", "coffee", "khao_man_gai",
                 "jok", "fruit", "pad_thai", "look_chin", "fried_chicken", "bubble_tea", "mango_sticky" }

-- Markets keep game-clock hours: the fresh market in the morning, the ตลาดนัด in the evening
M.MARKETS = {
  market = { name = "ตลาดสดเทศบาล", open = 5,  close = 13 },
  flea   = { name = "ตลาดนัดคอโค",  open = 16, close = 22 },
}

-- Musical instruments (เครื่องดนตรี): folk ones shine at village/temple venues, modern ones in the city
M.INSTRUMENTS = {
  pin      = { name = "พิณ",      folk = true,  mult = 1.00 },
  khaen    = { name = "แคน",      folk = true,  mult = 1.05 },
  so       = { name = "ซอ",       folk = true,  mult = 1.05 },
  ranat    = { name = "ระนาด",    folk = true,  mult = 1.10 },
  guitar   = { name = "กีต้าร์",   folk = false, mult = 1.15 },
  drums    = { name = "กลอง",     folk = false, mult = 1.15 },
  bass     = { name = "เบส",      folk = false, mult = 1.15 },
  keyboard = { name = "คีย์บอร์ด", folk = false, mult = 1.25 },
}
M.INSTRUMENT_ORDER = { "pin", "khaen", "so", "ranat", "guitar", "drums", "bass", "keyboard" }

-- Furniture (เฟอร์นิเจอร์) for decorating your stilt house: bought at the community shop,
-- placed in the room with house.lua. The client builds each piece's look from its id.
M.FURNITURE = {
  mat      = { name = "เสื่อ",          price = 80,   desc = "เสื่อกก ปูนั่งเล่นกลางบ้าน" },
  pillow   = { name = "หมอนข้าง",       price = 120,  desc = "หมอนข้างนุ่มๆ ไว้กอดนอน" },
  plant    = { name = "กระถางต้นไม้",    price = 150,  desc = "เพิ่มความเขียวในบ้าน" },
  chair    = { name = "เก้าอี้ไม้",       price = 180,  desc = "เก้าอี้ไม้สัก" },
  lamp     = { name = "โคมไฟ",          price = 220,  desc = "โคมไฟตั้งพื้น" },
  low_table = { name = "โต๊ะเตี้ย",      price = 250,  desc = "โต๊ะนั่งพื้น กินข้าวกับครอบครัว" },
  rug      = { name = "พรมผืนใหญ่",      price = 300,  desc = "พรมลายไทยสีแดง" },
  radio    = { name = "วิทยุ",           price = 300,  desc = "ฟังเพลงลูกทุ่ง" },
  fan      = { name = "พัดลม",          price = 350,  desc = "พัดลมตั้งพื้น คลายร้อน" },
  table    = { name = "โต๊ะไม้",         price = 400,  desc = "โต๊ะไม้สูง" },
  shelf    = { name = "ชั้นวางของ",      price = 450,  desc = "ชั้นไม้ 3 ชั้น" },
  wardrobe = { name = "ตู้เสื้อผ้า",       price = 700,  desc = "ตู้ไม้บานคู่" },
  bed      = { name = "เตียงนอน",       price = 900,  desc = "เตียงไม้พร้อมฟูก" },
  tv       = { name = "โทรทัศน์",        price = 1500, desc = "ทีวีจอแบนพร้อมตู้วาง" },
  fridge   = { name = "ตู้เย็น",          price = 1800, desc = "ตู้เย็น 2 ประตู" },
}
M.FURNITURE_ORDER = { "mat", "pillow", "plant", "chair", "lamp", "low_table", "rug", "radio", "fan",
                      "table", "shelf", "wardrobe", "bed", "tv", "fridge" }

-- Rice barn (ยุ้งข้าว): harvested rice is stored here; full barn = can't harvest
M.BARN_START = 100
M.BARN_STEP = 100
M.BARN_MAX = 500

-- Things for sale. shop = where the client shows it (the server sells it anywhere).
-- consumable items can be bought again and again.
M.CATALOG = {
  -- Village community shop snacks
  { id = "food_soda",         shop = "community", price = 15, name = "น้ำอัดลม",          desc = "พลังงาน +8",  food = "soda",        consumable = true },
  { id = "food_iced_coffee",  shop = "community", price = 25, name = "กาแฟเย็น",          desc = "พลังงาน +15", food = "iced_coffee", consumable = true },
  { id = "food_moo_ping",     shop = "community", price = 30, name = "ข้าวเหนียวหมูปิ้ง",  desc = "พลังงาน +20", food = "moo_ping",    consumable = true },
  { id = "food_som_tam",      shop = "community", price = 40, name = "ส้มตำ",             desc = "พลังงาน +25", food = "som_tam",     consumable = true },
  -- City music shop (ร้านเครื่องดนตรี)
  { id = "inst_khaen",    shop = "music", price = 300,  name = "แคน",      desc = "พื้นบ้าน · ทิป x1.05 · โบนัสที่หมู่บ้าน/งานวัด", instrument = "khaen" },
  { id = "inst_so",       shop = "music", price = 350,  name = "ซอ",       desc = "พื้นบ้าน · ทิป x1.05 · โบนัสที่หมู่บ้าน/งานวัด", instrument = "so" },
  { id = "inst_ranat",    shop = "music", price = 600,  name = "ระนาด",    desc = "พื้นบ้าน · ทิป x1.10 · โบนัสที่หมู่บ้าน/งานวัด", instrument = "ranat" },
  { id = "inst_guitar",   shop = "music", price = 800,  name = "กีต้าร์",   desc = "สากล · ทิป x1.15 · โบนัสที่ผับ/อีเวนต์เมือง", instrument = "guitar" },
  { id = "inst_drums",    shop = "music", price = 900,  name = "กลอง",     desc = "สากล · ทิป x1.15 · โบนัสที่ผับ/อีเวนต์เมือง", instrument = "drums" },
  { id = "inst_bass",     shop = "music", price = 900,  name = "เบส",      desc = "สากล · ทิป x1.15 · โบนัสที่ผับ/อีเวนต์เมือง", instrument = "bass" },
  { id = "inst_keyboard", shop = "music", price = 1200, name = "คีย์บอร์ด", desc = "สากล · ทิป x1.25 · โบนัสที่ผับ/อีเวนต์เมือง", instrument = "keyboard" },
  -- City restaurants (ร้านข้าวมันไก่ / ร้านกาแฟ)
  { id = "food_thai_tea",     shop = "city_food", price = 35, name = "ชาไทย",             desc = "พลังงาน +18", food = "thai_tea",    consumable = true },
  { id = "food_coffee",       shop = "city_food", price = 45, name = "กาแฟสด",            desc = "พลังงาน +25", food = "coffee",      consumable = true },
  { id = "food_khao_man_gai", shop = "city_food", price = 50, name = "ข้าวมันไก่",         desc = "พลังงาน +35", food = "khao_man_gai", consumable = true },
  { id = "seed_khao_niao",  shop = "community", price = 20,  name = "พันธุ์ข้าวเหนียว กข6 (1 ถุง)", desc = "ปลูกได้ 1 แปลง · โต 12 นาที · ขาย 12 บาท/กก.", seed = "khao_niao", consumable = true },
  { id = "seed_hommali",    shop = "community", price = 30,  name = "พันธุ์ข้าวหอมมะลิ 105 (1 ถุง)", desc = "ปลูกได้ 1 แปลง · โต 16 นาที · ขาย 18 บาท/กก.", seed = "hommali", consumable = true },
  { id = "chicken_feed",    shop = "community", price = 15,  name = "อาหารไก่ (1 ถุง)", desc = "ให้อาหารเล้า 1 ครั้ง อิ่ม 1 วันในเกม (แม่ไก่ถึงจะออกไข่)", feed = true, consumable = true },
  { id = "chicken_hen",     shop = "community", price = 120, name = "แม่ไก่พื้นเมือง", desc = "ออกไข่เมื่อได้กินอาหาร · ค่าความสวยสุ่ม", chicken = "hen", consumable = true },
  { id = "chicken_rooster", shop = "community", price = 180, name = "พ่อไก่พื้นเมือง", desc = "มีพ่อไก่ในเล้า ไข่จะเป็นไข่มีเชื้อ ฟักได้", chicken = "rooster", consumable = true },
  { id = "barn_upgrade",    shop = "community", price = 300, name = "ขยายยุ้งข้าว +100 กก.", desc = "เก็บข้าวได้มากขึ้น (สูงสุด 500 กก.)", barn = true, consumable = true },
  { id = "rod_fiber",       shop = "community", price = 150, name = "เบ็ดไฟเบอร์", desc = "ปลากินเร็วขึ้น ปลาหายากบ่อยขึ้น ตัวใหญ่ขึ้น 15%", rod = "fiber" },
  { id = "rod_carbon",      shop = "community", price = 400, name = "เบ็ดคาร์บอน", desc = "ปลากินเร็วสุด ปลาช่อน/ปลาดุกบ่อย ตัวใหญ่ขึ้น 30%", rod = "carbon" },
  { id = "camp_membership", shop = "camp",      price = 300, name = "สมาชิกค่ายมวย", desc = "ปลดล็อกการซ้อมกระสอบหนักในค่าย (ต้อง Lv 6 ด้วย)" },
}

-- Market stalls
for _, e in ipairs({
  { id = "food_jok",           shop = "market", price = 30, name = "โจ๊กหมู",         desc = "พลังงาน +22", food = "jok" },
  { id = "food_fruit",         shop = "market", price = 20, name = "ผลไม้รวม",        desc = "พลังงาน +12 · มะม่วง สับปะรด แตงโม", food = "fruit" },
  { id = "food_pad_thai",      shop = "market", price = 45, name = "ผัดไทย",          desc = "พลังงาน +32", food = "pad_thai" },
  { id = "market_seed_hommali", shop = "market", price = 25, name = "พันธุ์ข้าวหอมมะลิ (ราคาตลาด)", desc = "ถูกกว่าร้านค้าชุมชน", seed = "hommali" },
  { id = "market_feed",        shop = "market", price = 12, name = "อาหารไก่ (ราคาตลาด)", desc = "ถูกกว่าร้านค้าชุมชน", feed = true },
  { id = "food_look_chin",     shop = "flea", price = 15, name = "ลูกชิ้นปิ้ง",        desc = "พลังงาน +10", food = "look_chin" },
  { id = "food_fried_chicken", shop = "flea", price = 35, name = "ไก่ทอด",           desc = "พลังงาน +24", food = "fried_chicken" },
  { id = "food_bubble_tea",    shop = "flea", price = 30, name = "ชานมไข่มุก",        desc = "พลังงาน +18", food = "bubble_tea" },
  { id = "food_mango_sticky",  shop = "flea", price = 40, name = "ข้าวเหนียวมะม่วง",   desc = "พลังงาน +28", food = "mango_sticky" },
  { id = "flea_furn_mat",      shop = "flea", price = 60,  name = "เสื่อ (ราคาตลาดนัด)",       desc = "ถูกกว่าร้านค้าชุมชน", furniture = "mat" },
  { id = "flea_furn_pillow",   shop = "flea", price = 90,  name = "หมอนข้าง (ราคาตลาดนัด)",    desc = "ถูกกว่าร้านค้าชุมชน", furniture = "pillow" },
  { id = "flea_furn_plant",    shop = "flea", price = 110, name = "กระถางต้นไม้ (ราคาตลาดนัด)", desc = "ถูกกว่าร้านค้าชุมชน", furniture = "plant" },
  { id = "flea_furn_radio",    shop = "flea", price = 230, name = "วิทยุมือสอง",               desc = "ยังใช้ได้ดี", furniture = "radio" },
  { id = "flea_furn_fan",      shop = "flea", price = 260, name = "พัดลมมือสอง",               desc = "ยังใช้ได้ดี", furniture = "fan" },
}) do
  e.consumable = true
  table.insert(M.CATALOG, e)
end

for _, id in ipairs(M.FURNITURE_ORDER) do
  local f = M.FURNITURE[id]
  table.insert(M.CATALOG, { id = "furn_" .. id, shop = "community", price = f.price, name = f.name,
                            desc = f.desc, furniture = id, consumable = true })
end

local nk = require("nakama")

-- Owned items: { rods = { bamboo = true, ... }, rod = "bamboo", camp_member = false,
--                 seeds = { khao_niao = n, hommali = n }, barn_capacity = kg }
function M.read_items(user_id)
  local objects = nk.storage_read({ { collection = M.ITEMS.collection, key = M.ITEMS.key, user_id = user_id } })
  local items = (#objects > 0) and objects[1].value or {}
  items.rods = items.rods or { bamboo = true }
  items.rod = items.rod or "bamboo"
  items.camp_member = items.camp_member or false
  items.seeds = items.seeds or {}
  for _, v in ipairs(M.RICE_ORDER) do items.seeds[v] = items.seeds[v] or 0 end
  items.barn_capacity = items.barn_capacity or M.BARN_START
  items.feed = items.feed or 0
  items.instruments = items.instruments or { pin = true }   -- everyone starts with a พิณ
  items.instrument = items.instrument or "pin"
  items.furniture = items.furniture or {}   -- id -> how many owned (placed or stored)
  items.food = items.food or {}
  for _, f in ipairs(M.FOOD_ORDER) do items.food[f] = items.food[f] or 0 end
  return items
end

-- Rice in the barn: { khao_niao = kg, hommali = kg } (older saves had a single rice_kg)
M.CROPS = { collection = "inventory", key = "crops" }

function M.read_barn(user_id)
  local objects = nk.storage_read({ { collection = M.CROPS.collection, key = M.CROPS.key, user_id = user_id } })
  local crops = (#objects > 0) and objects[1].value or {}
  crops.rice = crops.rice or {}
  if crops.rice_kg and crops.rice_kg > 0 then
    crops.rice.khao_niao = (crops.rice.khao_niao or 0) + crops.rice_kg
  end
  crops.rice_kg = nil
  local total = 0
  for _, v in ipairs(M.RICE_ORDER) do
    crops.rice[v] = crops.rice[v] or 0
    total = total + crops.rice[v]
  end
  return crops, total
end

function M.write_barn(user_id, crops)
  nk.storage_write({ {
    collection = M.CROPS.collection, key = M.CROPS.key, user_id = user_id, value = crops,
    permission_read = 1, permission_write = 0,
  } })
end

function M.write_items(user_id, items)
  nk.storage_write({ {
    collection = M.ITEMS.collection, key = M.ITEMS.key, user_id = user_id, value = items,
    permission_read = 1, permission_write = 0,
  } })
end

function M.owns(items, entry)
  if entry.barn then return items.barn_capacity >= M.BARN_MAX end -- "owned" = maxed out
  if entry.consumable then return false end
  if entry.rod then return items.rods[entry.rod] == true end
  if entry.instrument then return items.instruments[entry.instrument] == true end
  if entry.id == "camp_membership" then return items.camp_member == true end
  return false
end

return M
