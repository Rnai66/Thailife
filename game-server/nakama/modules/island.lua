-- The island (เกาะ · อ่าวมาหยา): treasure chests hidden around the bay. Each chest opens once per
-- game day per player and pays a random find; now and then it's the big pirate stash.
--
--   island_treasure  { chest = "cave" | "swing" | "cliff" }  -> { coins, found, next_day }

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")

local STORE = { collection = "island", key = "treasure" }
local GRPC_FAILED_PRECONDITION = 9
local GRPC_INVALID_ARGUMENT = 3

local CHESTS = { cave = true, swing = true, cliff = true }

-- Finds, picked by weight
local FINDS = {
  { weight = 40, min = 40,  max = 90,  text = "เหรียญเก่าในหีบ" },
  { weight = 30, min = 90,  max = 160, text = "เปลือกหอยมุกหายาก ขายได้" },
  { weight = 20, min = 160, max = 260, text = "สร้อยทองของนักเดินเรือ" },
  { weight = 8,  min = 400, max = 600, text = "ขุมทรัพย์โจรสลัด!" },
  { weight = 2,  min = 1000, max = 1000, text = "แผนที่ขุมทรัพย์ทองคำ! โชคใหญ่!" },
}

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function read(user_id)
  local objects = nk.storage_read({ { collection = STORE.collection, key = STORE.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function pick()
  local total = 0
  for _, f in ipairs(FINDS) do total = total + f.weight end
  local r = math.random(1, total)
  for _, f in ipairs(FINDS) do
    r = r - f.weight
    if r <= 0 then return f end
  end
  return FINDS[1]
end

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = (payload and payload ~= "") and nk.json_decode(payload) or {}
  local chest = req.chest
  if not CHESTS[chest] then err("unknown chest", GRPC_INVALID_ARGUMENT) end

  local today = life.game_day(nk.time())
  local opened = read(user_id)
  if opened[chest] == today then err("already opened") end
  opened[chest] = today
  nk.storage_write({ { collection = STORE.collection, key = STORE.key, user_id = user_id, value = opened,
                       permission_read = 1, permission_write = 0 } })

  local find = pick()
  local coins = math.random(find.min, find.max)
  nk.wallet_update(user_id, { [data.CURRENCY] = coins }, { reason = "island_treasure", chest = chest }, true)
  return nk.json_encode({ coins = coins, found = find.text, next_day = today + 1 })
end, "island_treasure")
