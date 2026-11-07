-- งานก่อสร้าง (building): things you build yourself round your home — fences of several kinds and
-- colours, gates, and bigger pieces (a carport, a garden sala). Each piece sits where you put it
-- relative to your home (x, z in metres from the house, yaw in degrees), within RADIUS of it. You can
-- move it, turn it or repaint it for free, and pull it down for half its price back.
--
--   build_get    {}                                         -> {pieces, catalog, colors, max, radius}
--   build_place  {kind, x, z, yaw, color}                   -> the same view + done; costs the price
--   build_edit   {id, x?, z?, yaw?, color?}                 -> the same view + done; free
--   build_remove {id}                                       -> the same view + done; refunds REFUND
--
-- More kinds (a new house, a garage, …) are added by extending CATALOG; the client draws each kind.

local nk = require("nakama")
local data = require("shop_data")

local STATE = { collection = "build", key = "pieces" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9
local MAX_PIECES, RADIUS, REFUND = 80, 18, 0.5

local CATALOG = {
  { id = "fence_wood",   group = "fence", name = "รั้วไม้ระแนง",        price = 300,   length = 2 },
  { id = "fence_bamboo", group = "fence", name = "รั้วไม้ไผ่",          price = 200,   length = 2 },
  { id = "fence_wire",   group = "fence", name = "รั้วลวดหนาม (ฟาร์ม)", price = 150,   length = 2 },
  { id = "fence_iron",   group = "fence", name = "รั้วเหล็กดัด",        price = 600,   length = 2 },
  { id = "fence_brick",  group = "fence", name = "กำแพงปูน",           price = 900,   length = 2 },
  { id = "gate_wood",    group = "gate",  name = "ประตูรั้วไม้",        price = 800,   length = 2 },
  { id = "gate_iron",    group = "gate",  name = "ประตูรั้วเหล็ก",      price = 1500,  length = 3 },
  { id = "carport",      group = "build", name = "โรงรถ",              price = 15000, length = 3.4 },
  { id = "sala",         group = "build", name = "ศาลาพักผ่อน",        price = 9000,  length = 3 },
  { id = "planter",      group = "build", name = "กระถางไม้ดอก",       price = 250,   length = 1 },
}
local BY_ID = {}
for _, c in ipairs(CATALOG) do BY_ID[c.id] = c end

-- paint: 0 natural, then white, green, red, blue, black, cream
local COLORS = { "สีธรรมชาติ", "ขาว", "เขียว", "แดง", "น้ำเงิน", "ดำ", "ครีม" }

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function read(user_id)
  local objects = nk.storage_read({ { collection = STATE.collection, key = STATE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.pieces = s.pieces or {}
  s.next_id = s.next_id or 1
  return s
end

local function write(user_id, s)
  nk.storage_write({ { collection = STATE.collection, key = STATE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

local function view(s, extra)
  -- an empty Lua table encodes as {} rather than []: leave the list out when there is nothing built
  local r = { pieces = (#s.pieces > 0) and s.pieces or nil, catalog = CATALOG, colors = COLORS, max = MAX_PIECES, radius = RADIUS }
  for k, v in pairs(extra or {}) do r[k] = v end
  return nk.json_encode(r)
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

local function num(v, name)
  v = tonumber(v)
  if v == nil or v ~= v or v == math.huge or v == -math.huge then err("bad " .. name, GRPC_INVALID_ARGUMENT) end
  return v
end

local function round(v, step) return math.floor(v / step + 0.5) * step end

-- position and turn, checked: within RADIUS of the house, snapped to 5 cm and 15 degrees
local function place_args(req, piece)
  local x = round(num(req.x ~= nil and req.x or piece.x, "x"), 0.05)
  local z = round(num(req.z ~= nil and req.z or piece.z, "z"), 0.05)
  if math.abs(x) > RADIUS or math.abs(z) > RADIUS then err("too far") end
  local yaw = round(num(req.yaw ~= nil and req.yaw or piece.yaw, "yaw"), 15) % 360
  local color = math.floor(num(req.color ~= nil and req.color or piece.color, "color"))
  if color < 0 or color >= #COLORS then err("bad color", GRPC_INVALID_ARGUMENT) end
  return x, z, yaw, color
end

local function find(s, id)
  id = math.floor(tonumber(id) or -1)
  for i, p in ipairs(s.pieces) do if p.id == id then return i, p end end
  err("no such piece")
end

local function baht(user_id)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[data.CURRENCY] or 0
end

nk.register_rpc(function(context)
  return view(read(context.user_id))
end, "build_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local c = BY_ID[tostring(req.kind or "")]
  if not c then err("unknown kind", GRPC_INVALID_ARGUMENT) end
  local s = read(user_id)
  if #s.pieces >= MAX_PIECES then err("too many pieces") end
  local x, z, yaw, color = place_args(req, { x = 0, z = 0, yaw = 0, color = 0 })
  if baht(user_id) < c.price then err("not enough baht") end
  nk.wallet_update(user_id, { [data.CURRENCY] = -c.price }, { reason = "build", kind = c.id }, true)
  local piece = { id = s.next_id, kind = c.id, x = x, z = z, yaw = yaw, color = color }
  s.next_id = s.next_id + 1
  table.insert(s.pieces, piece)
  write(user_id, s)
  return view(s, { done = "สร้าง" .. c.name .. "แล้ว · " .. c.price .. " บาท", placed = piece.id })
end, "build_place")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local s = read(user_id)
  local _, piece = find(s, req.id)
  piece.x, piece.z, piece.yaw, piece.color = place_args(req, piece)
  write(user_id, s)
  return view(s, { done = "แก้ไขแล้ว" })
end, "build_edit")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local s = read(user_id)
  local i, piece = find(s, req.id)
  local c = BY_ID[piece.kind]
  local back = c and math.floor(c.price * REFUND) or 0
  table.remove(s.pieces, i)
  write(user_id, s)
  if back > 0 then
    nk.wallet_update(user_id, { [data.CURRENCY] = back }, { reason = "build_remove", kind = piece.kind }, true)
  end
  return view(s, { done = "รื้อถอนแล้ว · ได้คืน " .. back .. " บาท" })
end, "build_remove")
