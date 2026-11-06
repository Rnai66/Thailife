-- หาของป่า: the dense forest behind the tambon canal (ป่ารกหลังคลอง). Wild mushrooms come up by the
-- termite mounds, bamboo shoots by the bamboo clumps, red ant eggs in the nests up the mango trees and
-- ผักหวานป่า in the undergrowth. Picking a spot costs a little energy and gives a few; the spot grows
-- back after a while. What you pick goes in your bag (items.goods) and sells at the community shop
-- with your fish and rice (shop_sell_fish).
--
--   forage_get   {}                -> {spots: [{id, kind, ready_in}], kinds: [...]}
--   forage_pick  {"spot": "fg1"}    -> the same view + {got, name, kind}

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")

local STATE = { collection = "forage", key = "spots" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9

local ENERGY = 4
local REGROW_MS = 30 * 60 * 1000 -- a picked spot grows back in 30 real minutes

-- kind -> what it gives (items.goods id, data.GOODS has its name and price) and how many
local KINDS = {
  mushroom = { goods = "mushroom",     min = 2, max = 4, verb = "เก็บเห็ดโคน" },
  bamboo   = { goods = "bamboo_shoot", min = 1, max = 3, verb = "ขุดหน่อไม้" },
  ant_eggs = { goods = "ant_eggs",     min = 1, max = 2, verb = "แหย่ไข่มดแดง" },
  pak_wan  = { goods = "pak_wan",      min = 1, max = 3, verb = "เด็ดผักหวานป่า" },
}

local SPOTS = {
  fg1 = "mushroom", fg2 = "bamboo",   fg3 = "pak_wan",  fg4 = "ant_eggs",
  fg5 = "mushroom", fg6 = "bamboo",   fg7 = "pak_wan",  fg8 = "mushroom",
  fg9 = "ant_eggs", fg10 = "bamboo",  fg11 = "mushroom", fg12 = "pak_wan",
}
local ORDER = { "fg1", "fg2", "fg3", "fg4", "fg5", "fg6", "fg7", "fg8", "fg9", "fg10", "fg11", "fg12" }

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function read(user_id)
  local objects = nk.storage_read({ { collection = STATE.collection, key = STATE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.next = s.next or {}
  return s
end

local function write(user_id, s)
  nk.storage_write({ { collection = STATE.collection, key = STATE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

local function view(s, extra)
  local now = nk.time()
  local spots = {}
  for _, id in ipairs(ORDER) do
    local ready = math.max(0, math.ceil(((s.next[id] or 0) - now) / 1000))
    table.insert(spots, { id = id, kind = SPOTS[id], ready_in = ready })
  end
  local kinds = {}
  for id, k in pairs(KINDS) do
    local g = data.GOODS[k.goods]
    table.insert(kinds, { id = id, name = g.name, price = g.price, verb = k.verb })
  end
  local r = { spots = spots, kinds = kinds, energy = ENERGY }
  for k, v in pairs(extra or {}) do r[k] = v end
  return nk.json_encode(r)
end

nk.register_rpc(function(context)
  return view(read(context.user_id))
end, "forage_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  local spot = tostring(req.spot or "")
  local kind_id = SPOTS[spot]
  if not kind_id then err("unknown spot", GRPC_INVALID_ARGUMENT) end
  local s = read(user_id)
  local now = nk.time()
  if (s.next[spot] or 0) > now then err("not grown") end
  life.spend(user_id, ENERGY)

  local k = KINDS[kind_id]
  local got = math.random(k.min, k.max)
  local items = data.read_items(user_id)
  items.goods[k.goods] = (items.goods[k.goods] or 0) + got
  data.write_items(user_id, items)
  s.next[spot] = now + REGROW_MS
  write(user_id, s)
  local g = data.GOODS[k.goods]
  return view(s, { got = got, name = g.name, kind = kind_id, value = got * g.price })
end, "forage_pick")
