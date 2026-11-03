-- Shared land helpers (land.lua's RPCs, ranch.lua's cow places, farming.lua's paddies): which land
-- the player owns at the bank and what is built on it. Storage "land"/"parcels":
--   { plain = { build = "field"|"pen"|"rental", since = game day, field = { ["1"] = plot, ... } },
--     city  = { build = "shophouse", since = game day } }

local nk = require("nakama")

local M = {}

M.STORE = { collection = "land", key = "parcels" }
M.PEN_COWS = 8 -- cow places a pen on your plain land adds

-- Which land the player owns (bought at the bank, bank.lua assets)
function M.owned(user_id)
  local objects = nk.storage_read({ { collection = "bank", key = "account", user_id = user_id } })
  local assets = (#objects > 0 and objects[1].value.assets) or {}
  return { plain = assets.land_plain == true, rice = assets.land_rice == true, city = assets.land_city == true }
end

function M.read(user_id)
  local objects = nk.storage_read({ { collection = M.STORE.collection, key = M.STORE.key, user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  s.plain = s.plain or {}
  s.city = s.city or {}
  s.plain.field = s.plain.field or {}
  return s
end

function M.write(user_id, s)
  nk.storage_write({ { collection = M.STORE.collection, key = M.STORE.key, user_id = user_id, value = s,
                       permission_read = 1, permission_write = 0 } })
end

-- Extra cow places from a pen built on your plain land
function M.extra_cows(user_id)
  return (M.read(user_id).plain.build == "pen" and M.owned(user_id).plain) and M.PEN_COWS or 0
end

return M
