-- Your own โคกหนองนา (แปลงเกษตร): every player has one farm lot south of the village, along the farm
-- roads, with four corners (โซน): the paddies (นา 1-6, and 7-12 once you own the canal-side land),
-- the fish pond with its three cages, the cow pen and the chicken coop with a field hut. What grows and
-- lives there is still the player's own (farming.lua, fishpond.lua, ranch.lua, chickens.lua): the lot
-- only says where it shows. You can swap which corner holds what (LAYOUT_FEE) and put a fence on each
-- side of the lot (FENCES, paid once per side).
--
--   farmlot_get {"moo": 3?}  -> {mine:{moo, lot, layout, fences, moo_name, address}, moo, moo_name,
--                                 lots:[{no, vacant, owner, owner_id, layout, fences, mine}],
--                                 fences_list:[{id, name, price}], modules:[{id, name}], layout_fee}
--   farmlot_set {"layout": ["pond","rice","cows","coop"]?, "side": "back", "fence": "wood"?}
--   farmlot_move {"moo": 3?, "lot": 7?} -> move your farm (free, everything on it comes along): a
--                village resident to their own หมู่ (so farm and house are neighbours), anyone else to
--                the หมู่ they pick; leave out lot for the first free one there
--
-- Like the house lots (home.lua): 12 หมู่ of FARM_LOTS lots, one shared record per หมู่ written with
-- version checks, idle owners let go after IDLE_DAYS (their animals and rice stay theirs). A village
-- resident's farm is given in their own หมู่ when there's room, so their farm and house are neighbours.

local nk = require("nakama")
local data = require("shop_data")

local SYSTEM = "00000000-0000-0000-0000-000000000000"
local LOT_STORE = "lots"
local STATE = { collection = "farmlot", key = "lot" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9
local MOOS = 12
local FARM_LOTS = 24 -- drawn in the village map (Homes.FarmLots / FarmLots)
local IDLE_MS = 60 * 24 * 3600 * 1000
local SEEN_EVERY_MS = 6 * 3600 * 1000
local LAYOUT_FEE = 500
local DEFAULT_LAYOUT = { "rice", "pond", "cows", "coop" } -- front left, front right, back left, back right
local MODULES = {
  { id = "rice", name = "แปลงนา" }, { id = "pond", name = "บ่อเลี้ยงปลา" },
  { id = "cows", name = "คอกวัว" }, { id = "coop", name = "เล้าไก่ · เถียงนา" },
}
local SIDES = { front = true, back = true, left = true, right = true }
local FENCES = {
  { id = "none",   name = "ไม่มีรั้ว",        price = 0 },
  { id = "bamboo", name = "รั้วไม้ไผ่",       price = 800 },
  { id = "wire",   name = "รั้วลวดหนาม",     price = 1200 },
  { id = "wood",   name = "รั้วไม้ระแนง",     price = 1500 },
  { id = "wall",   name = "กำแพงปูน",        price = 4000 },
}
local VILLAGE_MOO_NAMES = { "บ้านโคกสะอาด", "บ้านหนองแวง", "บ้านโนนสูง", "บ้านดอนแดง", "บ้านนาดี", "บ้านหนองไผ่",
  "บ้านโพนทอง", "บ้านสร้างแก้ว", "บ้านท่าลาด", "บ้านหัวช้าง", "บ้านนาคำ", "บ้านดงเย็น" }

local NO_FENCES = { front = "none", back = "none", left = "none", right = "none" }

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function fence(id)
  for _, f in ipairs(FENCES) do if f.id == id then return f end end
  return nil
end

local function moo_name(moo) return "หมู่ที่ " .. moo .. " " .. (VILLAGE_MOO_NAMES[moo] or "") end
local function lot_key(moo) return "farm_" .. moo end

local function read(user_id)
  local objects = nk.storage_read({ { collection = STATE.collection, key = STATE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  if type(s.layout) ~= "table" or #s.layout ~= 4 then s.layout = { DEFAULT_LAYOUT[1], DEFAULT_LAYOUT[2], DEFAULT_LAYOUT[3], DEFAULT_LAYOUT[4] } end
  s.fences = type(s.fences) == "table" and s.fences or {}
  for side in pairs(SIDES) do s.fences[side] = fence(s.fences[side]) and s.fences[side] or "none" end
  return s
end

local function write(user_id, s)
  nk.storage_write({ { collection = STATE.collection, key = STATE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

local function display_name(user_id)
  local u = nk.account_get_id(user_id).user or {}
  local name = u.display_name
  if not name or name == "" then name = u.username or "เพื่อนบ้าน" end
  return name
end

local function read_lots(moo)
  local objects = nk.storage_read({ { collection = LOT_STORE, key = lot_key(moo), user_id = SYSTEM } })
  local doc, version = { lots = {} }, "*"
  if #objects > 0 then doc, version = objects[1].value, objects[1].version end
  doc.lots = doc.lots or {}
  local now = nk.time()
  for no, l in pairs(doc.lots) do
    if now - (l.seen or 0) > IDLE_MS then doc.lots[no] = nil end
  end
  return doc, version
end

local function update_lots(moo, fn)
  for _ = 1, 6 do
    local doc, version = read_lots(moo)
    if fn(doc) == false then return false end
    local ok = pcall(nk.storage_write, { { collection = LOT_STORE, key = lot_key(moo), user_id = SYSTEM,
      value = doc, version = version, permission_read = 0, permission_write = 0 } })
    if ok then return true end
  end
  err("busy, try again")
end

-- Put (or refresh) the player's entry on lot `no` of หมู่ `moo`; false if someone else has it
local function put(moo, no, user_id, s)
  local name = display_name(user_id)
  return update_lots(moo, function(doc)
    local cur = doc.lots[tostring(no)]
    if cur and cur.user ~= user_id then return false end
    doc.lots[tostring(no)] = { user = user_id, name = name, layout = s.layout, fences = s.fences, seen = nk.time() }
  end)
end

-- The หมู่ you live in, when your home is in the village (home.lua)
local function home_moo(user_id)
  local objects = nk.storage_read({ { collection = "home", key = "choice", user_id = user_id } })
  local h = (#objects > 0) and objects[1].value or {}
  if h.chosen and h.area == "village" and h.moo then return h.moo end
  return nil
end

-- Has the player given themselves a name yet (profile_set_name)? A farm lot is only given then, so a
-- fresh install that is opened and closed again doesn't take one under a made-up username
local function named(user_id)
  local u = nk.account_get_id(user_id).user or {}
  return u.display_name ~= nil and u.display_name ~= ""
end

-- Make sure the player has a farm lot (your own หมู่ first, else the fullest หมู่ with room); `need`:
-- an action that can't do without one (without a name yet: an error, else nothing given)
local function ensure(user_id, s, need)
  if s.moo and s.lot then
    local doc = read_lots(s.moo)
    local cur = doc.lots[tostring(s.lot)]
    if cur and cur.user == user_id then
      if nk.time() - (cur.seen or 0) > SEEN_EVERY_MS then put(s.moo, s.lot, user_id, s) end
      return
    end
    s.moo, s.lot = nil, nil -- let go meanwhile (idle)
  end
  if not named(user_id) then
    if need then err("set your name first") end
    return
  end
  local want = home_moo(user_id)
  local order = {}
  if want then table.insert(order, want) end
  for m = 1, MOOS do if m ~= want then table.insert(order, m) end end
  for _ = 1, 3 do
    for _, m in ipairs(order) do
      local doc = read_lots(m)
      for no = 1, FARM_LOTS do
        if not doc.lots[tostring(no)] then
          if put(m, no, user_id, s) then
            s.moo, s.lot = m, no
            write(user_id, s)
            return
          end
          break -- taken meanwhile: look again
        end
      end
    end
  end
  err("farms full")
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, (payload and payload ~= "") and payload or "{}")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

-- What each owner built on their farm lot (build.lua pieces with site "farm"), by user
local function farm_pieces(doc)
  local keys = {}
  for _, l in pairs(doc.lots) do
    if l.user then table.insert(keys, { collection = "build", key = "pieces", user_id = l.user }) end
  end
  local by = {}
  if #keys == 0 then return by end
  for _, o in ipairs(nk.storage_read(keys)) do
    local list = {}
    for _, p in ipairs(o.value.pieces or {}) do if p.site == "farm" then table.insert(list, p) end end
    if #list > 0 then by[o.user_id] = list end
  end
  return by
end

local function view(user_id, s, moo, extra)
  local doc = read_lots(moo)
  local built = farm_pieces(doc)
  local lots = {}
  for no = 1, FARM_LOTS do
    local l = doc.lots[tostring(no)]
    table.insert(lots, { no = no, vacant = l == nil, owner = l and l.name or "", owner_id = l and l.user or "",
                         layout = l and l.layout or DEFAULT_LAYOUT, fences = l and l.fences or NO_FENCES,
                         mine = l ~= nil and l.user == user_id, pieces = l and built[l.user] or nil })
  end
  local r = { mine = { moo = s.moo or 0, lot = s.lot or 0, layout = s.layout, fences = s.fences,
                       moo_name = s.moo and moo_name(s.moo) or "",
                       address = s.lot and ("แปลงเกษตรเลขที่ " .. s.lot .. " " .. moo_name(s.moo)) or "" },
              moo = moo, moo_name = moo_name(moo), lot_count = FARM_LOTS, lots = lots,
              fences_list = FENCES, modules = MODULES, layout_fee = LAYOUT_FEE }
  for k, v in pairs(extra or {}) do r[k] = v end
  return nk.json_encode(r)
end

local function pay(user_id, amount, reason)
  if amount <= 0 then return end
  local w = nk.account_get_id(user_id).wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  if (w[data.CURRENCY] or 0) < amount then err("not enough baht") end
  nk.wallet_update(user_id, { [data.CURRENCY] = -amount }, { reason = reason }, true)
end

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local s = read(user_id)
  ensure(user_id, s, true)
  local home = home_moo(user_id)
  local moo = home or math.floor(tonumber(req.moo) or 0)
  if moo < 1 or moo > MOOS then err("bad moo", GRPC_INVALID_ARGUMENT) end
  local lot = req.lot and math.floor(tonumber(req.lot) or 0) or nil
  if lot and (lot < 1 or lot > FARM_LOTS) then err("bad lot", GRPC_INVALID_ARGUMENT) end
  if moo == s.moo and (lot == nil or lot == s.lot) then err("same farm") end
  local old_moo, old_lot = s.moo, s.lot
  local new_lot
  if lot then
    if not put(moo, lot, user_id, s) then err("lot taken") end
    new_lot = lot
  else
    for _ = 1, 3 do
      local doc = read_lots(moo)
      local free
      for no = 1, FARM_LOTS do if not doc.lots[tostring(no)] then free = no break end end
      if not free then err("farms full") end
      if put(moo, free, user_id, s) then new_lot = free break end
    end
    if not new_lot then err("busy, try again") end
  end
  s.moo, s.lot = moo, new_lot
  write(user_id, s)
  update_lots(old_moo, function(doc)
    local cur = doc.lots[tostring(old_lot)]
    if not cur or cur.user ~= user_id then return false end
    doc.lots[tostring(old_lot)] = nil
  end)
  return view(user_id, s, moo, { done = "ย้ายแปลงเกษตรมาที่แปลงเกษตรเลขที่ " .. new_lot .. " " .. moo_name(moo) .. " แล้ว" })
end, "farmlot_move")

nk.register_rpc(function(context, payload)
  local req = decode(payload)
  local s = read(context.user_id)
  ensure(context.user_id, s)
  local moo = tonumber(req.moo)
  if not moo or moo < 1 then moo = s.moo or 1 end -- 0 / none: the หมู่ your farm is in
  moo = math.floor(moo)
  if moo > MOOS then err("bad moo", GRPC_INVALID_ARGUMENT) end
  return view(context.user_id, s, moo)
end, "farmlot_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local s = read(user_id)
  ensure(user_id, s, true)
  local done
  if req.layout ~= nil then
    if type(req.layout) ~= "table" or #req.layout ~= 4 then err("bad layout", GRPC_INVALID_ARGUMENT) end
    local seen, layout = {}, {}
    for i = 1, 4 do
      local id = tostring(req.layout[i])
      local known = false
      for _, m in ipairs(MODULES) do if m.id == id then known = true end end
      if not known or seen[id] then err("bad layout", GRPC_INVALID_ARGUMENT) end
      seen[id] = true
      layout[i] = id
    end
    local same = true
    for i = 1, 4 do if layout[i] ~= s.layout[i] then same = false end end
    if same then err("same layout") end
    pay(user_id, LAYOUT_FEE, "farm_layout")
    s.layout = layout
    done = "จัดโซนแปลงเกษตรใหม่แล้ว"
  elseif req.side ~= nil then
    local side, f = tostring(req.side), fence(tostring(req.fence or ""))
    if not SIDES[side] or not f then err("bad fence", GRPC_INVALID_ARGUMENT) end
    if s.fences[side] == f.id then err("same fence") end
    pay(user_id, f.price, "farm_fence")
    s.fences[side] = f.id
    done = f.id == "none" and "รื้อรั้วแล้ว" or ("สร้าง" .. f.name .. "แล้ว")
  else
    err("nothing to change", GRPC_INVALID_ARGUMENT)
  end
  write(user_id, s)
  put(s.moo, s.lot, user_id, s)
  return view(user_id, s, s.moo, { done = done })
end, "farmlot_set")
