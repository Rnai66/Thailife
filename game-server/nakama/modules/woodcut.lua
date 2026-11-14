-- ขวาน: buy an axe (ขวานผ่าฟืน, community shop) and you can fell the trees and clear the overgrown
-- scrub (ไม้รก) in the forest behind the tambon canal. A tree gives ท่อนไม้ (logs); split them at the
-- chopping block (ตอผ่าฟืน) by the footbridge into ฟืน (bundles of firewood), worth more than the
-- logs. Both go in your bag (items.goods) and sell at the community shop. A felled tree leaves a
-- stump and grows back after a while (per player, like forage.lua).
--
--   wood_get   {}                 -> {trees: [{id, kind, ready_in}], kinds, has_axe, logs, firewood}
--   wood_chop  {"tree": "wt1"}     -> the same view + {got, name, kind}
--   wood_split {"count": 3}        -> the same view + {split, got}

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")

local STATE = { collection = "woodcut", key = "trees" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9

local KINDS = {
  tree  = { name = "ต้นไม้ใหญ่", min = 3, max = 5, energy = 8, regrow = 60 * 60 * 1000, verb = "โค่นต้นไม้" },
  scrub = { name = "ไม้รก",     min = 1, max = 2, energy = 4, regrow = 20 * 60 * 1000, verb = "ถางไม้รก" },
}
local TREES = {
  wt1 = "tree",  wt2 = "tree",  wt3 = "tree",  wt4 = "tree",  wt5 = "tree",  wt6 = "tree",
  ws1 = "scrub", ws2 = "scrub", ws3 = "scrub", ws4 = "scrub", ws5 = "scrub", ws6 = "scrub",
}
local ORDER = { "wt1", "wt2", "wt3", "wt4", "wt5", "wt6", "ws1", "ws2", "ws3", "ws4", "ws5", "ws6" }
local SPLIT_ENERGY = 2   -- per log
local SPLIT_GIVES = 3    -- bundles of firewood per log
local SPLIT_MAX = 10

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

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

local function view(s, items, extra)
  local now = nk.time()
  local trees = {}
  for _, id in ipairs(ORDER) do
    table.insert(trees, { id = id, kind = TREES[id], ready_in = math.max(0, math.ceil(((s.next[id] or 0) - now) / 1000)) })
  end
  local kinds = {}
  for id, k in pairs(KINDS) do
    table.insert(kinds, { id = id, name = k.name, verb = k.verb, energy = k.energy, min = k.min, max = k.max })
  end
  local r = {
    trees = trees, kinds = kinds, has_axe = items.tools.axe == true,
    logs = items.goods.log or 0, firewood = items.goods.firewood or 0,
    log_price = data.GOODS.log.price, firewood_price = data.GOODS.firewood.price,
    split_energy = SPLIT_ENERGY, split_gives = SPLIT_GIVES, split_max = SPLIT_MAX,
  }
  for k, v in pairs(extra or {}) do r[k] = v end
  return nk.json_encode(r)
end

nk.register_rpc(function(context)
  return view(read(context.user_id), data.read_items(context.user_id))
end, "wood_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local tree = tostring(req.tree or "")
  local kind_id = TREES[tree]
  if not kind_id then err("unknown tree", GRPC_INVALID_ARGUMENT) end
  local items = data.read_items(user_id)
  if not items.tools.axe then err("no axe") end
  local s = read(user_id)
  local now = nk.time()
  if (s.next[tree] or 0) > now then err("not grown") end
  local k = KINDS[kind_id]
  life.spend(user_id, k.energy)
  local got = math.random(k.min, k.max)
  items.goods.log = (items.goods.log or 0) + got
  data.write_items(user_id, items)
  s.next[tree] = now + k.regrow
  write(user_id, s)
  return view(s, items, { got = got, name = data.GOODS.log.name, kind = kind_id })
end, "wood_chop")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local n = math.floor(tonumber(req.count) or 1)
  if n < 1 or n > SPLIT_MAX then err("bad count", GRPC_INVALID_ARGUMENT) end
  local items = data.read_items(user_id)
  if not items.tools.axe then err("no axe") end
  if (items.goods.log or 0) < n then err("not enough logs") end
  life.spend(user_id, SPLIT_ENERGY * n)
  items.goods.log = items.goods.log - n
  if items.goods.log == 0 then items.goods.log = nil end
  items.goods.firewood = (items.goods.firewood or 0) + n * SPLIT_GIVES
  data.write_items(user_id, items)
  return view(read(user_id), items, { split = n, got = n * SPLIT_GIVES, name = data.GOODS.firewood.name })
end, "wood_split")
