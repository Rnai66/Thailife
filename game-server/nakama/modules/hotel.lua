-- Hotels at the beach (โรงแรม / รีสอร์ท): book a room for one game day (36 real minutes). In your room
-- you can sleep for free (life_sleep {"spot": "hotel"} checks the booking). Better rooms come with
-- breakfast in your food bag, and the suite also restores your energy as you check in.
--
--   hotel_get   {}
--   hotel_book  {"tier": "standard"|"deluxe"|"suite", "hotel": name}

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")

local BOOKING = { collection = "hotel", key = "booking" }
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local TIERS = {
  standard = { name = "ห้องมาตรฐาน",        price = 350,  perks = "เตียงนุ่ม นอนฟรีทั้งวัน" },
  deluxe   = { name = "ห้องดีลักซ์วิวทะเล",    price = 900,  perks = "วิวทะเล + อาหารเช้า 1 ชุด", breakfast = 1 },
  suite    = { name = "สวีทริมหาด",          price = 2500, perks = "ห้องใหญ่หรู + อาหารเช้า 2 ชุด + พลังงานเต็มทันที", breakfast = 2, full_energy = true },
}
local TIER_ORDER = { "standard", "deluxe", "suite" }
local BREAKFAST = { "khao_man_gai", "coffee" } -- one of each per set

local M = {}

local function read(user_id)
  local objects = nk.storage_read({ { collection = BOOKING.collection, key = BOOKING.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

-- The active booking or nil
function M.active(user_id)
  local b = read(user_id)
  if b.tier and (b.until_ms or 0) > nk.time() then return b end
  return nil
end

local function view(user_id, done)
  local b = M.active(user_id)
  local tiers = {}
  for _, id in ipairs(TIER_ORDER) do
    local t = TIERS[id]
    table.insert(tiers, { id = id, name = t.name, price = t.price, perks = t.perks })
  end
  return nk.json_encode({
    tiers = tiers, done = done,
    tier = b and b.tier or "", room = b and TIERS[b.tier].name or "", hotel = b and b.hotel or "",
    ends_in_s = b and math.floor((b.until_ms - nk.time()) / 1000) or 0,
  })
end

nk.register_rpc(function(context, payload)
  return view(context.user_id)
end, "hotel_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local tier = TIERS[req.tier or ""]
  if not tier then error({ "unknown room", GRPC_INVALID_ARGUMENT }) end
  if M.active(user_id) then error({ "already booked", GRPC_FAILED_PRECONDITION }) end

  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  if (w[data.CURRENCY] or 0) < tier.price then error({ "not enough baht", GRPC_FAILED_PRECONDITION }) end
  nk.wallet_update(user_id, { [data.CURRENCY] = -tier.price }, { reason = "hotel", tier = req.tier }, true)

  local hotel = type(req.hotel) == "string" and string.sub(req.hotel, 1, 60) or ""
  nk.storage_write({ { collection = BOOKING.collection, key = BOOKING.key, user_id = user_id,
                       value = { tier = req.tier, hotel = hotel, until_ms = nk.time() + life.DAY_MS },
                       permission_read = 1, permission_write = 0 } })
  if tier.breakfast then
    local items = data.read_items(user_id)
    for _, food in ipairs(BREAKFAST) do items.food[food] = (items.food[food] or 0) + tier.breakfast end
    data.write_items(user_id, items)
  end
  if tier.full_energy then life.add_energy(user_id, life.MAX_ENERGY) end
  return view(user_id, "จองห้อง" .. tier.name .. " แล้ว — เข้าห้องพักได้เลย (1 วันในเกม)")
end, "hotel_book")

return M
