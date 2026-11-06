-- Where you live (บ้านของฉัน): chosen when you start — out in the village (บ้านนอก), in town
-- (ในเมือง) or in Bangkok (กรุงเทพฯ) — and which house there. You wake up at home, sleep there, and
-- your furniture (house.lua) and home shrine go with you: every house has the same 5.6 x 4.6 m room.
--
--   home_get     {}                                   -> {chosen, area, style, move_fee, areas:[...]}
--   home_choose  {"area": "city", "style": "townhouse"} -> the same view; the first choice is free,
--                                                         moving later costs move_fee
--
-- Players from before this was added count as living in the village stilt house until they choose.

local nk = require("nakama")
local data = require("shop_data")

local STATE = { collection = "home", key = "choice" }
local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9
local MOVE_FEE = 3000

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

local function view(s, extra)
  local area, style = find(s.area or "village", s.style or "stilt")
  local r = { chosen = s.chosen == true, area = area.id, area_name = area.name, scene = area.scene,
              style = style.id, style_name = style.name, move_fee = MOVE_FEE, areas = AREAS }
  for k, v in pairs(extra or {}) do r[k] = v end
  return nk.json_encode(r)
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

nk.register_rpc(function(context)
  return view(read(context.user_id))
end, "home_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local area, style = find(tostring(req.area or ""), tostring(req.style or ""))
  if not area then err("unknown home", GRPC_INVALID_ARGUMENT) end
  local s = read(user_id)
  if s.chosen and s.area == area.id and s.style == style.id then err("same home") end
  local paid = 0
  if s.chosen then
    local account = nk.account_get_id(user_id)
    local w = account.wallet or {}
    if type(w) == "string" then w = nk.json_decode(w) end
    if (w[data.CURRENCY] or 0) < MOVE_FEE then err("not enough baht") end
    nk.wallet_update(user_id, { [data.CURRENCY] = -MOVE_FEE }, { reason = "move_home", to = area.id .. "/" .. style.id }, true)
    paid = MOVE_FEE
  end
  s.chosen, s.area, s.style, s.since_ms = true, area.id, style.id, nk.time()
  write(user_id, s)
  return view(s, { paid = paid, done = (paid > 0 and "ย้ายบ้านแล้ว! " or "ได้บ้านแล้ว! ") .. style.name .. " · " .. area.name })
end, "home_choose")
