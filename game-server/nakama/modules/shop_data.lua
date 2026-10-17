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

-- Things for sale. shop = where the client shows it (the server sells it anywhere).
M.CATALOG = {
  { id = "rod_fiber",       shop = "community", price = 150, name = "เบ็ดไฟเบอร์", desc = "ปลากินเร็วขึ้น ปลาหายากบ่อยขึ้น ตัวใหญ่ขึ้น 15%", rod = "fiber" },
  { id = "rod_carbon",      shop = "community", price = 400, name = "เบ็ดคาร์บอน", desc = "ปลากินเร็วสุด ปลาช่อน/ปลาดุกบ่อย ตัวใหญ่ขึ้น 30%", rod = "carbon" },
  { id = "camp_membership", shop = "camp",      price = 300, name = "สมาชิกค่ายมวย", desc = "ปลดล็อกการซ้อมกระสอบหนักในค่าย (ต้อง Lv 6 ด้วย)" },
}

local nk = require("nakama")

-- Owned items: { rods = { bamboo = true, ... }, rod = "bamboo", camp_member = false }
function M.read_items(user_id)
  local objects = nk.storage_read({ { collection = M.ITEMS.collection, key = M.ITEMS.key, user_id = user_id } })
  local items = (#objects > 0) and objects[1].value or {}
  items.rods = items.rods or { bamboo = true }
  items.rod = items.rod or "bamboo"
  items.camp_member = items.camp_member or false
  return items
end

function M.write_items(user_id, items)
  nk.storage_write({ {
    collection = M.ITEMS.collection, key = M.ITEMS.key, user_id = user_id, value = items,
    permission_read = 1, permission_write = 0,
  } })
end

function M.owns(items, entry)
  if entry.rod then return items.rods[entry.rod] == true end
  if entry.id == "camp_membership" then return items.camp_member == true end
  return false
end

return M
