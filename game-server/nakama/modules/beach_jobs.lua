-- Jobs at the beach (งานพิเศษริมทะเล): take tourists out on the longtail boat (พานักท่องเที่ยว
-- นั่งเรือหางยาว). The server times the trip: you can't be paid before the boat could be back.
--
--   tour_start   {}  -> costs energy, starts the trip
--   tour_finish  {}  -> pays the fare plus a tip

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")

local TOUR = { collection = "beach_jobs", key = "tour" }
local GRPC_FAILED_PRECONDITION = 9

local TOUR_ENERGY = 10
local TOUR_MIN_MS = 30000   -- the trip takes ~40 s; allow for lag
local TOUR_MAX_MS = 180000  -- a trip left hanging longer than this doesn't pay
local FARE = 180
local MAX_TIP = 80

local function err(msg) error({ msg, GRPC_FAILED_PRECONDITION }) end

local function read(user_id)
  local objects = nk.storage_read({ { collection = TOUR.collection, key = TOUR.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function write(user_id, v)
  nk.storage_write({ { collection = TOUR.collection, key = TOUR.key, user_id = user_id, value = v,
                       permission_read = 1, permission_write = 0 } })
end

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  life.spend(user_id, TOUR_ENERGY)
  write(user_id, { start_ms = nk.time() })
  return nk.json_encode({ fare = FARE, energy = TOUR_ENERGY })
end, "tour_start")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local t = read(user_id)
  if not t.start_ms then err("no trip") end
  local elapsed = nk.time() - t.start_ms
  if elapsed < TOUR_MIN_MS then err("too early") end
  write(user_id, {})
  if elapsed > TOUR_MAX_MS then err("trip expired") end
  local tip = math.random(0, MAX_TIP)
  nk.wallet_update(user_id, { [data.CURRENCY] = FARE + tip }, { reason = "boat_tour" }, true)
  return nk.json_encode({ fare = FARE, tip = tip, earned = FARE + tip,
                          done = "พานักท่องเที่ยวเที่ยวทะเลเสร็จ ได้ค่าเรือ " .. FARE .. " + ทิป " .. tip .. " บาท" })
end, "tour_finish")
