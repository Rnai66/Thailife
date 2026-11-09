-- Where you live (บ้านของฉัน): chosen when you start — out in the village (บ้านนอก), in town
-- (ในเมือง) or in Bangkok (กรุงเทพฯ) — and which house there. You wake up at home, sleep there, and
-- your furniture (house.lua) and home shrine go with you: every house has the same 5.6 x 4.6 m room.
--
--   home_get     {}                                   -> {chosen, area, style, move_fee, areas:[...]}
--   home_choose  {"area": "city", "style": "townhouse", "moo": 3?, "lot": 5?}
--                -> the same view; the first choice is free, moving later costs move_fee
--                   (changing only the style of the house on your lot is a move too)
--   lots_get     {"area": "city"?, "moo": 3?}            -> who lives on which lot of one หมู่
--
-- Players from before this was added count as living in the village stilt house until they choose.
--
-- House lots (แปลงบ้าน · ทะเบียนบ้าน): every area has up to 12 หมู่ (copies of the same map, each its own
-- crowd) with LOTS[area] numbered lots each, so a home is "บ้านเลขที่ 5 หมู่ที่ 3". One lot per player,
-- one player per lot: the lot list of each หมู่ is one shared record written with version checks, so two
-- players can never take the same lot. Leave out moo/lot and the first free one is given. A lot whose
-- owner hasn't been seen for IDLE_DAYS is let go (ประกาศขาย); their furniture stays theirs, and the next
-- time they come back they are given a new lot. Homes chosen before lots existed get one on home_get.

local nk = require("nakama")
local data = require("shop_data")

local STATE = { collection = "home", key = "choice" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9
local MOVE_FEE = 3000

local SYSTEM = "00000000-0000-0000-0000-000000000000"
local LOT_STORE = "lots"
local MOOS = 12
local LOTS = { village = 22, city = 16, bangkok = 29 } -- the lots drawn in each map (Homes / HouseLots)
local IDLE_DAYS = 60
local IDLE_MS = IDLE_DAYS * 24 * 3600 * 1000
local SEEN_EVERY_MS = 6 * 3600 * 1000 -- how often a visit refreshes "last seen" on the lot record
local MOO_NAMES = {
  village = { "บ้านโคกสะอาด", "บ้านหนองแวง", "บ้านโนนสูง", "บ้านดอนแดง", "บ้านนาดี", "บ้านหนองไผ่",
              "บ้านโพนทอง", "บ้านสร้างแก้ว", "บ้านท่าลาด", "บ้านหัวช้าง", "บ้านนาคำ", "บ้านดงเย็น" },
  city    = { "ชุมชนตลาดเก่า", "ชุมชนหลังสถานี", "ชุมชนศรีเมือง", "ชุมชนโพธิ์ทอง", "ชุมชนสระแก้ว", "ชุมชนหนองหาร",
              "ชุมชนวัดกลาง", "ชุมชนบ้านใหม่", "ชุมชนทุ่งศรีเมือง", "ชุมชนหนองประจักษ์", "ชุมชนชัยพฤกษ์", "ชุมชนศรีสุข" },
  bangkok = { "ชุมชนท่าเตียน", "ชุมชนบางลำพู", "ชุมชนนางเลิ้ง", "ชุมชนบ้านบาตร", "ชุมชนสามเพ็ง", "ชุมชนตรอกจันทน์",
              "ชุมชนคลองสาน", "ชุมชนบางกอกน้อย", "ชุมชนวัดอรุณ", "ชุมชนบางโพ", "ชุมชนพระโขนง", "ชุมชนบางรัก" },
}

local AREAS = {
  { id = "village", name = "บ้านนอก · หมู่บ้าน", scene = "ThaiVillage",
    desc = "อากาศดี ใกล้ทุ่งนา วัด เล้าไก่ เหมาะกับผู้เล่นใหม่",
    styles = {
      { id = "stilt",     name = "บ้านไม้ใต้ถุนสูง",        desc = "บ้านไทยดั้งเดิม ชานบ้านกว้าง จอดรถใต้ถุน" },
      { id = "concrete1", name = "บ้านปูนชั้นเดียว",         desc = "บ้านปูนทาสีครีม หลังคากระเบื้อง มีระเบียงหน้าบ้าน" },
      { id = "wood1",     name = "บ้านไม้ชั้นเดียวยกพื้น",    desc = "บ้านไม้สักยกพื้นเตี้ย หลังคาสังกะสี บรรยากาศชนบท" },
      { id = "half2",     name = "บ้านครึ่งปูนครึ่งไม้ 2 ชั้น", desc = "ชั้นล่างปูน ชั้นบนไม้ มีระเบียงชั้นสอง" },
    } },
  { id = "city", name = "ในเมือง", scene = "City",
    desc = "ใกล้ตลาด ร้านค้า โรงพยาบาล งานในเมือง ผับ",
    styles = {
      { id = "townhouse", name = "ทาวน์เฮาส์ 2 ชั้น",    desc = "ห้องแถวในซอยหมู่บ้านในเมือง เดินไปตลาดได้" },
      { id = "city_single", name = "บ้านเดี่ยวในเมือง",  desc = "บ้านเดี่ยวมีรั้ว สนามหญ้าหน้าบ้าน" },
    } },
  { id = "bangkok", name = "กรุงเทพฯ", scene = "Bangkok",
    desc = "เมืองหลวง วัดพระแก้ว แม่น้ำเจ้าพระยา สนามบิน รถไฟฟ้า",
    styles = {
      { id = "townhome", name = "ทาวน์โฮมโมเดิร์น",       desc = "ทาวน์โฮมสไตล์โมเดิร์น หลังคาแบน ใกล้รถไฟฟ้า" },
      { id = "bkk_single", name = "บ้านเดี่ยวหมู่บ้านจัดสรร", desc = "บ้านเดี่ยวสองชั้นในหมู่บ้านจัดสรรริมเมือง" },
    } },
}

local function find(area_id, style_id)
  for _, a in ipairs(AREAS) do
    if a.id == area_id then
      for _, s in ipairs(a.styles) do if s.id == style_id then return a, s end end
    end
  end
  return nil
end

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function read(user_id)
  local objects = nk.storage_read({ { collection = STATE.collection, key = STATE.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function write(user_id, s)
  nk.storage_write({ { collection = STATE.collection, key = STATE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

local function moo_name(area_id, moo)
  local names = MOO_NAMES[area_id] or {}
  return "หมู่ที่ " .. moo .. " " .. (names[moo] or "")
end

local function display_name(user_id)
  local account = nk.account_get_id(user_id)
  local u = account.user or {}
  local name = u.display_name
  if not name or name == "" then name = u.username or "เพื่อนบ้าน" end
  return name
end

local function lot_key(area_id, moo) return area_id .. "_" .. moo end

-- The lot record of one หมู่ and its version ("*" = not stored yet); lots idle too long are let go
local function read_lots(area_id, moo)
  local objects = nk.storage_read({ { collection = LOT_STORE, key = lot_key(area_id, moo), user_id = SYSTEM } })
  local doc, version = { lots = {} }, "*"
  if #objects > 0 then doc, version = objects[1].value, objects[1].version end
  doc.lots = doc.lots or {}
  local now = nk.time()
  for no, l in pairs(doc.lots) do
    if now - (l.seen or 0) > IDLE_MS then doc.lots[no] = nil end
  end
  return doc, version
end

-- Change one หมู่'s record with fn(doc) -> false to give up; retried when someone else wrote it meanwhile
local function update_lots(area_id, moo, fn)
  for _ = 1, 6 do
    local doc, version = read_lots(area_id, moo)
    if fn(doc) == false then return false end
    local ok = pcall(nk.storage_write, { { collection = LOT_STORE, key = lot_key(area_id, moo), user_id = SYSTEM,
      value = doc, version = version, permission_read = 0, permission_write = 0 } })
    if ok then return true end
  end
  err("busy, try again")
end

local function take_lot(area_id, moo, lot, user_id, style_id)
  local name = display_name(user_id)
  return update_lots(area_id, moo, function(doc)
    local cur = doc.lots[tostring(lot)]
    if cur and cur.user ~= user_id then return false end
    doc.lots[tostring(lot)] = { user = user_id, name = name, style = style_id, seen = nk.time() }
  end)
end

local function free_lot(area_id, moo, lot, user_id)
  if not (area_id and moo and lot) then return end
  update_lots(area_id, moo, function(doc)
    local cur = doc.lots[tostring(lot)]
    if not cur or cur.user ~= user_id then return false end
    doc.lots[tostring(lot)] = nil
  end)
end

-- The first free lot: in the หมู่ asked for, else the fullest หมู่ that still has room (neighbours!)
local function find_free(area_id, want_moo)
  local count = LOTS[area_id] or 0
  local order = {}
  if want_moo then table.insert(order, want_moo) end
  for m = 1, MOOS do if m ~= want_moo then table.insert(order, m) end end
  for _, m in ipairs(order) do
    local doc = read_lots(area_id, m)
    for no = 1, count do
      if not doc.lots[tostring(no)] then return m, no end
    end
  end
  return nil
end

-- Give the player a lot (the one asked for, else the first free), retrying if it goes meanwhile
local function claim(area_id, style_id, user_id, moo, lot)
  if lot then
    if not take_lot(area_id, moo, lot, user_id, style_id) then err("lot taken") end
    return moo, lot
  end
  for _ = 1, 5 do
    local m, no = find_free(area_id, moo)
    if not m then err("area full") end
    if take_lot(area_id, m, no, user_id, style_id) then return m, no end
  end
  err("busy, try again")
end

-- Make sure a chosen home has its lot (and the record still has them); refresh "last seen"
local function ensure_lot(user_id, s)
  if not s.chosen then return nil end
  local notice
  if s.moo and s.lot then
    local doc = read_lots(s.area, s.moo)
    local cur = doc.lots[tostring(s.lot)]
    if cur and cur.user == user_id then
      if nk.time() - (cur.seen or 0) > SEEN_EVERY_MS or cur.style ~= s.style then
        take_lot(s.area, s.moo, s.lot, user_id, s.style)
      end
      return nil
    end
    notice = "บ้านเลขที่ " .. s.lot .. " " .. moo_name(s.area, s.moo) .. " ถูกประกาศขายเพราะไม่ได้เข้าเกมนาน · ได้บ้านหลังใหม่แทน"
  end
  s.moo, s.lot = claim(s.area, s.style, user_id, s.moo, nil)
  write(user_id, s)
  return notice
end

local function view(s, extra)
  local area, style = find(s.area or "village", s.style or "stilt")
  local r = { chosen = s.chosen == true, area = area.id, area_name = area.name, scene = area.scene,
              style = style.id, style_name = style.name, move_fee = MOVE_FEE, areas = AREAS,
              moo = s.moo or 0, lot = s.lot or 0, moo_name = s.moo and moo_name(area.id, s.moo) or "",
              address = (s.chosen and s.lot) and ("บ้านเลขที่ " .. s.lot .. " " .. moo_name(area.id, s.moo)) or "" }
  for k, v in pairs(extra or {}) do r[k] = v end
  return nk.json_encode(r)
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

nk.register_rpc(function(context)
  local s = read(context.user_id)
  local notice = ensure_lot(context.user_id, s)
  return view(s, notice and { notice = notice } or nil)
end, "home_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local area, style = find(tostring(req.area or ""), tostring(req.style or ""))
  if not area then err("unknown home", GRPC_INVALID_ARGUMENT) end
  local moo, lot = tonumber(req.moo), tonumber(req.lot)
  if moo and (moo ~= math.floor(moo) or moo < 1 or moo > MOOS) then err("bad moo", GRPC_INVALID_ARGUMENT) end
  if lot and (lot ~= math.floor(lot) or lot < 1 or lot > (LOTS[area.id] or 0) or not moo) then err("bad lot", GRPC_INVALID_ARGUMENT) end
  local s = read(user_id)
  local same_place = s.chosen and s.area == area.id and s.moo ~= nil
    and (moo == nil or moo == s.moo) and (lot == nil or lot == s.lot)
  if same_place and s.style == style.id then err("same home") end
  local paid = 0
  if s.chosen then
    local account = nk.account_get_id(user_id)
    local w = account.wallet or {}
    if type(w) == "string" then w = nk.json_decode(w) end
    if (w[data.CURRENCY] or 0) < MOVE_FEE then err("not enough baht") end
  end
  -- the new lot first (it may be taken), then let the old one go
  local new_moo, new_lot
  if same_place then new_moo, new_lot = s.moo, s.lot
  else new_moo, new_lot = claim(area.id, style.id, user_id, moo or (s.area == area.id and s.moo or nil), lot) end
  take_lot(area.id, new_moo, new_lot, user_id, style.id)
  if not same_place and s.chosen and s.moo and s.lot then free_lot(s.area, s.moo, s.lot, user_id) end
  if s.chosen then
    nk.wallet_update(user_id, { [data.CURRENCY] = -MOVE_FEE }, { reason = "move_home", to = area.id .. "/" .. style.id }, true)
    paid = MOVE_FEE
  end
  s.chosen, s.area, s.style, s.since_ms, s.moo, s.lot = true, area.id, style.id, nk.time(), new_moo, new_lot
  write(user_id, s)
  return view(s, { paid = paid, done = (paid > 0 and "ย้ายบ้านแล้ว! " or "ได้บ้านแล้ว! ") .. style.name
    .. " · บ้านเลขที่ " .. new_lot .. " " .. moo_name(area.id, new_moo) .. " · " .. area.name })
end, "home_choose")

-- One หมู่'s lots (who lives where) and how full every หมู่ of that area is
nk.register_rpc(function(context, payload)
  local req = decode(payload ~= "" and payload or "{}")
  local s = read(context.user_id)
  local area_id = tostring(req.area or s.area or "village")
  if not LOTS[area_id] then err("unknown area", GRPC_INVALID_ARGUMENT) end
  local moo = tonumber(req.moo) or (s.area == area_id and s.moo) or 1
  if moo < 1 or moo > MOOS then err("bad moo", GRPC_INVALID_ARGUMENT) end
  moo = math.floor(moo)
  local count = LOTS[area_id]
  local lots, moos = {}, {}
  local doc = read_lots(area_id, moo)
  for no = 1, count do
    local l = doc.lots[tostring(no)]
    table.insert(lots, { no = no, vacant = l == nil, owner = l and l.name or "", owner_id = l and l.user or "",
                         style = l and l.style or "", mine = l ~= nil and l.user == context.user_id })
  end
  local keys = {}
  for m = 1, MOOS do table.insert(keys, { collection = LOT_STORE, key = lot_key(area_id, m), user_id = SYSTEM }) end
  local used = {}
  for _, o in ipairs(nk.storage_read(keys)) do
    local n, now = 0, nk.time()
    for _, l in pairs(o.value.lots or {}) do if now - (l.seen or 0) <= IDLE_MS then n = n + 1 end end
    used[o.key] = n
  end
  for m = 1, MOOS do
    table.insert(moos, { no = m, name = moo_name(area_id, m), used = used[lot_key(area_id, m)] or 0, total = count })
  end
  return nk.json_encode({ area = area_id, moo = moo, moo_name = moo_name(area_id, moo), lot_count = count,
                          idle_days = IDLE_DAYS, lots = lots, moos = moos })
end, "lots_get")
