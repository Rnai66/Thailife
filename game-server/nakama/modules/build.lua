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
--   build_lots   {area, moo}                                -> {lots:[{no, user_id, pieces}]}: what everyone on
--                                                              that หมู่'s house lots built, to draw round their houses
--
-- On a house lot (home.lua) everything stays inside your lot: `bounds` (x0, x1, z0, z1, relative to
-- the house) replaces the RADIUS square. Pieces built before lots existed that stick out of it are
-- counted in `outside`: you still see them, the neighbours don't, until you move them in.
--
-- Besides home, a piece can stand on another `site` you hold, measured from that site's middle (its
-- front is local -Z): "farm" your farm lot (farmlot.lua, 26 x 25 m; the neighbours see these too, in
-- farmlot_get), "plain" the 1-rai land in the village and "city" the lot in town (land_core, bought at
-- the bank; only you see these, the parcel is drawn for each owner). The view's `sites` gives the
-- bounds of each one you hold.
--
-- More kinds (a new house, a garage, …) are added by extending CATALOG; the client draws each kind.

local nk = require("nakama")
local data = require("shop_data")
local admin = require("admin_core")
local land = require("land_core")

local STATE = { collection = "build", key = "pieces" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9
local MAX_PIECES, RADIUS, REFUND = 160, 18, 0.5 -- (all sites together)
-- the admin's pieces were built before house lots, round the village's own stilt house: they stay there
local ANCHOR = "village_stilt"
local ADMIN_PIECES, ADMIN_RADIUS = 300, 45 -- the game's own accounts (admin_core): past the lot, and more of it

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

-- House lots (home.lua, drawn by Homes.cs): each lot's width along its soi and depth, and where each
-- style's house stands on its lot (the house is what pieces are measured from)
local function lot_size(area, lot)
  if area == "village" then
    if lot <= 3 then return 14, 14 elseif lot <= 6 then return 12, 12 elseif lot <= 14 then return 12, 14 end
    return 11.5, 14
  elseif area == "city" then
    return 12, (lot <= 8) and 16 or 17
  elseif area == "bangkok" then
    if lot <= 25 then return 12, 16 end
    return 11, 17
  end
  return nil
end
local HOUSE_AT = { stilt = { -0.2, 2 }, concrete1 = { -0.3, 1 }, wood1 = { -0.3, 1 }, half2 = { -0.3, 1 },
                   townhouse = { -2.4, 1.5 }, townhome = { -2.4, 1.5 }, city_single = { 0, 1.5 }, bkk_single = { 0, 1.5 } }
local MARGIN = 0.2

local function bounds_for(home, user_id)
  if admin.is(user_id) then return nil end -- builds within ADMIN_RADIUS instead
  if not (home and home.chosen and home.lot and home.area) then return nil end
  local w, d = lot_size(home.area, home.lot)
  local at = HOUSE_AT[home.style or ""]
  if not w or not at then return nil end
  return { x0 = -w / 2 - at[1] + MARGIN, x1 = w / 2 - at[1] - MARGIN, z0 = -d / 2 - at[2] + MARGIN, z1 = d / 2 - at[2] - MARGIN }
end

-- Other sites: half width and half depth (LandParcels.cs, FarmLotsBuilder.cs), less MARGIN
local SITE_SIZE = { farm = { 13, 12.5 }, plain = { 9.5, 7 }, city = { 8, 6.5 } }

local function site_of(p) return (p.site == nil or p.site == "") and "home" or p.site end

local function holds(user_id, site)
  if site == "farm" then
    local objects = nk.storage_read({ { collection = "farmlot", key = "lot", user_id = user_id } })
    return #objects > 0 and (objects[1].value.lot or 0) > 0
  elseif site == "plain" or site == "city" then
    return land.owned(user_id)[site] == true
  end
  return false
end

local function site_bounds(site)
  local h = SITE_SIZE[site]
  return { x0 = -h[1] + MARGIN, x1 = h[1] - MARGIN, z0 = -h[2] + MARGIN, z1 = h[2] - MARGIN }
end

local function sites_for(user_id)
  local r = {}
  for site in pairs(SITE_SIZE) do if holds(user_id, site) then r[site] = site_bounds(site) end end
  return r
end

local function home_of(user_id)
  local objects = nk.storage_read({ { collection = "home", key = "choice", user_id = user_id } })
  return (#objects > 0) and objects[1].value or nil
end

local function inside(b, x, z, r)
  r = r or RADIUS
  if not b then return math.abs(x) <= r and math.abs(z) <= r end
  return x >= b.x0 and x <= b.x1 and z >= b.z0 and z <= b.z1
end

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

local function view(s, extra, b, user_id)
  -- an empty Lua table encodes as {} rather than []: leave the list out when there is nothing built
  local big = admin.is(user_id)
  local out = 0
  if not big then for _, p in ipairs(s.pieces) do if site_of(p) == "home" and not inside(b, p.x, p.z) then out = out + 1 end end end
  local r = { pieces = (#s.pieces > 0) and s.pieces or nil, catalog = CATALOG, colors = COLORS,
              max = big and ADMIN_PIECES or MAX_PIECES, radius = big and ADMIN_RADIUS or RADIUS,
              bounds = b, outside = out, admin = big,
              anchor = big and ANCHOR or nil, sites = sites_for(user_id) }
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
local function place_args(req, piece, b, r)
  local x = round(num(req.x ~= nil and req.x or piece.x, "x"), 0.05)
  local z = round(num(req.z ~= nil and req.z or piece.z, "z"), 0.05)
  if not inside(b, x, z, r) then err(b and "outside lot" or "too far") end
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
  return view(read(context.user_id), nil, bounds_for(home_of(context.user_id), context.user_id), context.user_id)
end, "build_get")

-- What everyone living on one หมู่'s lots built (only the pieces inside their own lot)
nk.register_rpc(function(context, payload)
  local req = decode(payload)
  local area = tostring(req.area or "")
  local moo = math.floor(tonumber(req.moo) or 0)
  if not lot_size(area, 1) or moo < 1 or moo > 12 then err("bad place", GRPC_INVALID_ARGUMENT) end
  local objects = nk.storage_read({ { collection = "lots", key = area .. "_" .. moo, user_id = "00000000-0000-0000-0000-000000000000" } })
  local doc = (#objects > 0) and objects[1].value or {}
  local owners, keys = {}, {}
  for no, l in pairs(doc.lots or {}) do
    if l.user then
      owners[l.user] = tonumber(no)
      table.insert(keys, { collection = STATE.collection, key = STATE.key, user_id = l.user })
      table.insert(keys, { collection = "home", key = "choice", user_id = l.user })
    end
  end
  local builds, homes = {}, {}
  if #keys > 0 then
    for _, o in ipairs(nk.storage_read(keys)) do
      if o.collection == STATE.collection then builds[o.user_id] = o.value else homes[o.user_id] = o.value end
    end
  end
  local lots = {}
  for user, no in pairs(owners) do
    local h, s = homes[user], builds[user]
    if s and s.pieces and #s.pieces > 0 and h and h.area == area and h.moo == moo and h.lot == no then
      local big = admin.is(user)
      local b = bounds_for(h, user)
      local pieces = {}
      for _, p in ipairs(s.pieces) do
        if site_of(p) == "home" and (big or (b and inside(b, p.x, p.z))) then table.insert(pieces, p) end
      end
      if #pieces > 0 then table.insert(lots, { no = no, user_id = user, pieces = pieces, anchor = big and ANCHOR or nil }) end
    end
  end
  return nk.json_encode({ area = area, moo = moo, lots = (#lots > 0) and lots or nil })
end, "build_lots")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local c = BY_ID[tostring(req.kind or "")]
  if not c then err("unknown kind", GRPC_INVALID_ARGUMENT) end
  local s = read(user_id)
  if #s.pieces >= (admin.is(user_id) and ADMIN_PIECES or MAX_PIECES) then err("too many pieces") end
  local site = tostring(req.site or "")
  if site == "" then site = "home" end
  local b, r
  if site == "home" then
    b, r = bounds_for(home_of(user_id), user_id), admin.is(user_id) and ADMIN_RADIUS or nil
  elseif SITE_SIZE[site] then
    if not holds(user_id, site) then err("not your site") end
    b = site_bounds(site)
  else
    err("bad site", GRPC_INVALID_ARGUMENT)
  end
  local x, z, yaw, color = place_args(req, { x = 0, z = 0, yaw = 0, color = 0 }, b, r)
  if baht(user_id) < c.price then err("not enough baht") end
  nk.wallet_update(user_id, { [data.CURRENCY] = -c.price }, { reason = "build", kind = c.id, site = site }, true)
  local piece = { id = s.next_id, kind = c.id, x = x, z = z, yaw = yaw, color = color, site = site ~= "home" and site or nil }
  s.next_id = s.next_id + 1
  table.insert(s.pieces, piece)
  write(user_id, s)
  return view(s, { done = "สร้าง" .. c.name .. "แล้ว · " .. c.price .. " บาท", placed = piece.id }, bounds_for(home_of(user_id), user_id), user_id)
end, "build_place")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local s = read(user_id)
  local _, piece = find(s, req.id)
  local b = bounds_for(home_of(user_id), user_id)
  -- a piece from before lots that sticks out may still be turned or repainted where it is
  local function moved(v, was) return v ~= nil and math.abs((tonumber(v) or was) - was) > 0.01 end
  local moving = moved(req.x, piece.x) or moved(req.z, piece.z)
  local site = site_of(piece)
  if site == "home" then
    piece.x, piece.z, piece.yaw, piece.color = place_args(req, piece, moving and b or nil, admin.is(user_id) and ADMIN_RADIUS or nil)
  else
    if not SITE_SIZE[site] or not holds(user_id, site) then err("not your site") end
    piece.x, piece.z, piece.yaw, piece.color = place_args(req, piece, site_bounds(site))
  end
  write(user_id, s)
  return view(s, { done = "แก้ไขแล้ว" }, b, user_id)
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
  return view(s, { done = "รื้อถอนแล้ว · ได้คืน " .. back .. " บาท" }, bounds_for(home_of(user_id), user_id), user_id)
end, "build_remove")
