-- Account recovery (รหัสกู้คืนบัญชี): accounts are tied to the phone (device ID), so a player who
-- changes phone or deletes the app would lose everything. account_recovery_code gives the player a
-- code to write down; it is linked to their account as a custom ID, so on a new phone the game can
-- log in with it (AuthenticateCustom "recover-<code>") and then link the new phone's device ID.
--
--   account_recovery_get  {}  -> the current code (or "" if none yet)
--   account_recovery_new  {}  -> makes a new code (the old one stops working)
--   account_delete        {"confirm": "DELETE"} -> deletes the account and all its game data (App Store rule)

local nk = require("nakama")

local STORE = { collection = "account", key = "recovery" }
local ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789" -- no 0/O or 1/I to misread

local function read(user_id)
  local objects = nk.storage_read({ { collection = STORE.collection, key = STORE.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function new_code()
  local parts = {}
  for g = 1, 4 do
    local s = ""
    for _ = 1, 4 do
      local i = math.random(1, #ALPHABET)
      s = s .. string.sub(ALPHABET, i, i)
    end
    parts[g] = s
  end
  return table.concat(parts, "-")
end

nk.register_rpc(function(context)
  return nk.json_encode({ code = read(context.user_id).code or "" })
end, "account_recovery_get")

nk.register_rpc(function(context)
  local user_id = context.user_id
  local old = read(user_id).code
  if old then pcall(nk.unlink_custom, user_id, "recover-" .. old) end
  local code
  for _ = 1, 5 do -- a clash with another player's code is astronomically unlikely, but retry anyway
    code = new_code()
    if pcall(nk.link_custom, user_id, "recover-" .. code) then break end
    code = nil
  end
  if not code then error({ "could not create code", 13 }) end
  nk.storage_write({ { collection = STORE.collection, key = STORE.key, user_id = user_id, value = { code = code },
                       permission_read = 1, permission_write = 0 } })
  return nk.json_encode({ code = code })
end, "account_recovery_new")

nk.register_rpc(function(context, payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" or req.confirm ~= "DELETE" then error({ "confirm required", 3 }) end
  nk.account_delete_id(context.user_id, false) -- removes the account, its storage objects and wallet
  return nk.json_encode({ deleted = true })
end, "account_delete")
