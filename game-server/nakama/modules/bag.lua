-- The bag (กระเป๋า): one read-only look at everything the player owns, grouped for the HUD bag
-- window. Nothing here changes state; each item says which action the client can offer
-- (eat = life_eat, otherwise just shown).
--
--   bag_get  {}      -> {sections:[{title, items:[{id, name, count, unit, note, action}]}], equipped}
--   bag_use  {id}    -> the same, after equipping a rod (rod_<id>) or an instrument (inst_<id>)
-- Actions: eat (life_eat), equip (bag_use), wear (profile_set_look place "wardrobe", outfit_<n>).
-- equipped = {rod, rod_name, instrument, instrument_name, vehicle, vehicle_name, outfit, outfit_name}:
-- the character panel's gear slots.

local nk = require("nakama")
local data = require("shop_data")
local chickens = require("chicken_core")

local FISH_NAMES = {
  barb = "ปลาตะเพียน", tilapia = "ปลานิล", catfish = "ปลาดุก", snakehead = "ปลาช่อน",
  mackerel = "ปลาทู", squid = "ปลาหมึก", seabass = "ปลากะพงขาว", grouper = "ปลาเก๋า", kingfish = "ปลาอินทรี",
}
local FISH_ORDER = { "barb", "tilapia", "catfish", "snakehead", "mackerel", "squid", "seabass", "grouper", "kingfish" }
-- profile.lua OUTFITS, by index (0 = your own clothes)
local OUTFIT_NAMES = { [0] = "เสื้อผ้าลำลอง", "ชุดปกติขาว ข้าราชการ", "ชุดสูทสากล", "ชุดนักมวยไทย", "ชุดกากี อบต.",
  "ชุดม่อฮ่อม ชาวนา", "เสื้อกาวน์หมอ", "ชุดไทยราชปะแตน", "ชุดกีฬา", "ชุดตำรวจ", "จีวรพระ" }
-- temple.lua ITEMS shown in the bag (what's on your home shrine and who guards the house)
local TEMPLE_NAMES = {
  phra_pidta = "พระปิดตา", takrut = "ตะกรุดโทน", nangphaya = "พระนางพญา", somdej = "พระสมเด็จ", khunphaen = "พระขุนแผน",
  thuat = "หลวงพ่อทวด", phra_rod = "พระรอด", rian_raek = "เหรียญหลวงพ่อรุ่นแรก", phra_kring = "พระกริ่ง",
  yant_5 = "ผ้ายันต์ห้าแถว", yant_8 = "ผ้ายันต์แปดทิศ", yant_kraphet = "ผ้ายันต์เกราะเพชร", yant_wessuwan = "ผ้ายันต์ท้าวเวสสุวรรณ",
  dog = "หมาวัดเฝ้าบ้าน", kuman = "กุมารทอง",
}
local TEMPLE_ORDER = { "phra_pidta", "takrut", "nangphaya", "somdej", "khunphaen", "thuat", "phra_rod", "rian_raek", "phra_kring",
                       "yant_5", "yant_8", "yant_kraphet", "yant_wessuwan", "dog", "kuman" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9

local function read(user_id, collection, key)
  local objects = nk.storage_read({ { collection = collection, key = key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function item(list, id, name, count, unit, note, action)
  if count and count > 0 then
    table.insert(list, { id = id, name = name, count = count, unit = unit or "", note = note or "", action = action or "" })
  end
end

-- The outfit being worn: the 7th part of the avatar url "look:g.f.h.c.t.b.o"
local function outfit_worn(user_id)
  local url = nk.account_get_id(user_id).user.avatar_url or ""
  local code = url:match("^look:(.+)$") or ""
  local n = 0
  for v in code:gmatch("[^%.]+") do n = n + 1; if n == 7 then return tonumber(v) or 0 end end
  return 0
end

local function build(user_id)
  local items = data.read_items(user_id)
  local crops = data.read_barn(user_id)
  local sections = {}
  local function section(title, list) if #list > 0 then table.insert(sections, { title = title, items = list }) end end

  local food = {}
  for _, id in ipairs(data.FOOD_ORDER) do
    local f = data.FOODS[id]
    item(food, "food_" .. id, f.name, items.food[id], "ชิ้น", "+" .. f.energy .. " พลังงาน", "eat")
  end
  section("ของกิน", food)

  local farm = {}
  for _, v in ipairs(data.RICE_ORDER) do
    local r = data.RICE[v]
    item(farm, "seed_" .. v, "เมล็ด" .. r.name, items.seeds[v], "ถุง", "หว่านได้ 1 แปลงต่อถุง")
    item(farm, "rice_" .. v, r.name, crops.rice[v], "กก.", "ในยุ้งข้าว · ขายที่ร้านค้าชุมชน")
  end
  for _, id in ipairs(data.FIELD_ORDER or {}) do
    local c = data.FIELD_CROPS[id]
    item(farm, "seed_" .. id, "พันธุ์" .. c.name, (items.field_seeds or {})[id], "ชุด", "ปลูกในไร่ได้ 1 แปลงต่อชุด")
    item(farm, "crop_" .. id, c.name, (crops.field or {})[id], c.unit, "ขายที่ลานรับซื้อหน้าไร่")
  end
  item(farm, "chicken_feed", "อาหารไก่", items.feed, "ถุง")
  local herd = read(user_id, "ranch", "herd")
  local cows = type(herd.cows) == "table" and #herd.cows or (herd.cows or 0)
  item(farm, "cows", "วัวในคอก", cows, "ตัว", "ฟาร์มวัวข้างโคกหนองนา")
  item(farm, "cow_feed", "อาหารวัว", herd.feed, "ถุง")
  item(farm, "milk", "นมวัวรอรีด", herd.milk, "ขวด")
  local coop = chickens.read(user_id)
  item(farm, "chickens", "ไก่ในเล้า", #coop.chickens, "ตัว")
  item(farm, "eggs", "ไข่ไก่", coop.eggs, "ฟอง")
  section("ไร่นาและสัตว์", farm)

  local fish = {}
  local inv = read(user_id, "inventory", "fish")
  for _, id in ipairs(FISH_ORDER) do item(fish, "fish_" .. id, FISH_NAMES[id], (inv.counts or {})[id], "ตัว", "ในข้อง · ขายที่ร้านค้าชุมชน") end
  section("ปลา", fish)

  local goods = {}
  for _, id in ipairs(data.GOODS_ORDER) do
    local g = data.GOODS[id]
    item(goods, "goods_" .. id, g.name, items.goods[id], "ชิ้น", "ขายร้านค้าชุมชน " .. g.price .. " บาท")
  end
  local fair = read(user_id, "fair", "tickets")
  item(goods, "coupons", "คูปองเครื่องเล่น", fair.coupons, "ใบ", "ใช้ในงานวัด/งานกาชาด")
  item(goods, "fair_points", "แต้มเกมฝีมือ", fair.points, "แต้ม", "แลกของที่ซุ้มแลกรางวัล")
  section("ของรางวัลและของใช้", goods)

  local gear = {}
  for _, id in ipairs({ "bamboo", "fiber", "carbon" }) do
    if items.rods[id] and data.RODS[id] then
      local using = id == items.rod
      item(gear, "rod_" .. id, data.RODS[id].name, 1, "คัน", using and "ใช้อยู่" or "คันเบ็ด · กดใช้แทนคันเดิม", using and "" or "equip")
    end
  end
  for _, id in ipairs(data.INSTRUMENT_ORDER) do
    if items.instruments[id] then
      local using = id == items.instrument
      item(gear, "inst_" .. id, data.INSTRUMENTS[id].name, 1, "ชิ้น", using and "ใช้อยู่" or "เครื่องดนตรี · กดใช้เล่นแทน", using and "" or "equip")
    end
  end
  for _, id in ipairs(data.FURNITURE_ORDER) do
    item(gear, "furn_" .. id, data.FURNITURE[id].name, items.furniture[id], "ชิ้น", "แต่งบ้าน")
  end
  section("อุปกรณ์และเฟอร์นิเจอร์", gear)

  -- Outfits bought at the tailor: put one on (or back into your own clothes) from the bag
  local clothes = {}
  local look = read(user_id, "profile", "look")
  local worn = outfit_worn(user_id)
  local owned = {}
  for k, v in pairs(look.outfits or {}) do if v and OUTFIT_NAMES[tonumber(k) or -1] then table.insert(owned, tonumber(k)) end end
  table.sort(owned)
  if #owned > 0 then
    item(clothes, "outfit_0", OUTFIT_NAMES[0], 1, "ชุด", worn == 0 and "ใส่อยู่" or "ถอดชุด กลับไปใส่เสื้อผ้าของคุณเอง", worn == 0 and "" or "wear")
  end
  for _, o in ipairs(owned) do
    item(clothes, "outfit_" .. o, OUTFIT_NAMES[o], 1, "ชุด", worn == o and "ใส่อยู่" or "กดสวมใส่ได้ฟรี", worn == o and "" or "wear")
  end
  section("เสื้อผ้า", clothes)

  local holy = {}
  local temple = read(user_id, "temple", "state")
  for _, id in ipairs(TEMPLE_ORDER) do
    if (temple.items or {})[id] then
      local note = (id == "dog" or id == "kuman") and "เฝ้าบ้าน เตือนภัยตอนกลางคืน" or "อยู่บนหิ้งพระที่บ้าน กันผีกันขโมย"
      item(holy, "holy_" .. id, TEMPLE_NAMES[id], 1, (id == "dog" or id == "kuman") and "ตัว" or "องค์", note)
    end
  end
  section("พระเครื่องและเครื่องราง", holy)

  local assets = {}
  local bank = read(user_id, "bank", "account")
  for _, id in ipairs(data.BANK_ASSET_ORDER) do
    if (bank.assets or {})[id] then
      local a = data.BANK_ASSETS[id]
      item(assets, "asset_" .. id, a.name, 1, a.kind == "land" and "แปลง" or "คัน", a.kind == "land" and (id == "land_city" and "ที่ดินในเมือง" or "ที่ดินข้างโคกหนองนา") or "")
    end
  end
  section("ทรัพย์สิน", assets)

  -- The character panel's gear: rod and instrument in use, the fastest vehicle owned, the outfit
  local vehicle, vehicle_name = "", ""
  for _, id in ipairs(data.BANK_ASSET_ORDER) do
    local a = data.BANK_ASSETS[id]
    if (bank.assets or {})[id] and a.kind == "car" then vehicle, vehicle_name = id, a.name end
  end
  local equipped = {
    rod = items.rod, rod_name = data.RODS[items.rod] and data.RODS[items.rod].name or "",
    instrument = items.instrument, instrument_name = data.INSTRUMENTS[items.instrument] and data.INSTRUMENTS[items.instrument].name or "",
    vehicle = vehicle, vehicle_name = vehicle_name, outfit = worn, outfit_name = OUTFIT_NAMES[worn] or "",
  }

  local result = { equipped = equipped }
  if #sections > 0 then result.sections = sections end -- an empty Lua table would encode as {}
  return nk.json_encode(result)
end

nk.register_rpc(function(context, payload)
  return build(context.user_id)
end, "bag_get")

nk.register_rpc(function(context, payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" or type(req.id) ~= "string" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local items = data.read_items(context.user_id)
  local rod = req.id:match("^rod_(.+)$")
  local inst = req.id:match("^inst_(.+)$")
  if rod then
    if not items.rods[rod] then error({ "not owned", GRPC_FAILED_PRECONDITION }) end
    items.rod = rod
  elseif inst then
    if not items.instruments[inst] then error({ "not owned", GRPC_FAILED_PRECONDITION }) end
    items.instrument = inst
  else
    error({ "cannot use", GRPC_INVALID_ARGUMENT })
  end
  data.write_items(context.user_id, items)
  return build(context.user_id)
end, "bag_use")
