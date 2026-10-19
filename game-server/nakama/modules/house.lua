-- Home decoration (แต่งบ้าน): furniture placed in your stilt house's room.
--   house_get    {}                                  -> {owned:[...], placed:[...]}
--   house_place  {"item": "bed", "x": 1.2, "z": -0.5, "rot": 90} -> puts a stored piece in the room
--   house_move   {"id": 3, "x": ..., "z": ..., "rot": ...}
--   house_remove {"id": 3}                            -> back into storage
-- Positions are metres from the middle of the room floor; the room is 5.6 x 4.6 m.

local nk = require("nakama")
local data = require("shop_data")

local LAYOUT = { collection = "house", key = "layout" }
local MAX_PIECES = 30
local BOUND_X, BOUND_Z = 2.5, 2.0

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local function read(user_id)
  local objects = nk.storage_read({ { collection = LAYOUT.collection, key = LAYOUT.key, user_id = user_id } })
  local layout = (#objects > 0) and objects[1].value or {}
  layout.next_id = layout.next_id or 1
  layout.placed = layout.placed or {}
  return layout
end

local function write(user_id, layout)
  nk.storage_write({ {
    collection = LAYOUT.collection, key = LAYOUT.key, user_id = user_id, value = layout,
    permission_read = 1, permission_write = 0,
  } })
end

local function placed_count(layout, item)
  local n = 0
  for _, p in ipairs(layout.placed) do if p.item == item then n = n + 1 end end
  return n
end

local function view(user_id, layout, extra)
  local items = data.read_items(user_id)
  local owned = {}
  for _, id in ipairs(data.FURNITURE_ORDER) do
    local have = items.furniture[id] or 0
    if have > 0 then
      table.insert(owned, { item = id, name = data.FURNITURE[id].name, owned = have, placed = placed_count(layout, id) })
    end
  end
  -- An empty Lua table encodes as {} (not []), so leave empty lists out entirely
  local result = { owned = (#owned > 0) and owned or nil, placed = (#layout.placed > 0) and layout.placed or nil,
                   max_pieces = MAX_PIECES }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  return req
end

-- Validated position/rotation (snapped to 5 cm and 90 degrees)
local function spot(req)
  local x, z = tonumber(req.x), tonumber(req.z)
  if x == nil or z == nil or math.abs(x) > BOUND_X or math.abs(z) > BOUND_Z then
    error({ "outside the room", GRPC_INVALID_ARGUMENT })
  end
  local rot = math.floor((tonumber(req.rot) or 0) / 90 + 0.5) % 4 * 90
  return math.floor(x * 20 + 0.5) / 20, math.floor(z * 20 + 0.5) / 20, rot
end

local function find(layout, id)
  for i, p in ipairs(layout.placed) do
    if p.id == math.floor(tonumber(id) or 0) then return i, p end
  end
  error({ "unknown piece", GRPC_INVALID_ARGUMENT })
end

nk.register_rpc(function(context, payload)
  return view(context.user_id, read(context.user_id))
end, "house_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  if not data.FURNITURE[req.item or ""] then error({ "unknown furniture", GRPC_INVALID_ARGUMENT }) end
  local layout = read(user_id)
  if #layout.placed >= MAX_PIECES then error({ "room full", GRPC_FAILED_PRECONDITION }) end
  local items = data.read_items(user_id)
  if placed_count(layout, req.item) >= (items.furniture[req.item] or 0) then
    error({ "none in storage", GRPC_FAILED_PRECONDITION })
  end
  local x, z, rot = spot(req)
  local piece = { id = layout.next_id, item = req.item, x = x, z = z, rot = rot }
  layout.next_id = layout.next_id + 1
  table.insert(layout.placed, piece)
  write(user_id, layout)
  return view(user_id, layout, { placed_id = piece.id })
end, "house_place")

nk.register_rpc(function(context, payload)
  local req = decode(payload)
  local layout = read(context.user_id)
  local _, piece = find(layout, req.id)
  piece.x, piece.z, piece.rot = spot(req)
  write(context.user_id, layout)
  return view(context.user_id, layout)
end, "house_move")

nk.register_rpc(function(context, payload)
  local req = decode(payload)
  local layout = read(context.user_id)
  local index = find(layout, req.id)
  table.remove(layout.placed, index)
  write(context.user_id, layout)
  return view(context.user_id, layout)
end, "house_remove")
