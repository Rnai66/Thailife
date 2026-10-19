-- Player profile: character name (ชื่อตัวละคร), stored as the account's display name, and
-- the character look (หน้าตา), stored as the avatar url "skin:N".
--   profile_set_name  {"name": "..."}  -> {name}
--   profile_set_skin  {"skin": 0..7}   -> {skin}
-- Other players read both with GetUsers, so they can't be spoofed through match messages.

local nk = require("nakama")

local GRPC_INVALID_ARGUMENT = 3
local MIN_CHARS, MAX_CHARS = 2, 16
local MAX_SKINS = 8

-- Count UTF-8 characters (Thai letters are 3 bytes each)
local function char_count(s)
  local n = 0
  for i = 1, #s do
    local b = s:byte(i)
    if b < 0x80 or b >= 0xC0 then n = n + 1 end
  end
  return n
end

local function clean(name)
  name = tostring(name or "")
  name = name:gsub("[%c]", "")            -- no control characters / newlines
  name = name:gsub("^%s+", ""):gsub("%s+$", "")
  name = name:gsub("%s%s+", " ")          -- collapse repeated spaces
  return name
end

nk.register_rpc(function(context, payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end

  local name = clean(req.name)
  local n = char_count(name)
  if n < MIN_CHARS or n > MAX_CHARS then
    error({ "name must be " .. MIN_CHARS .. "-" .. MAX_CHARS .. " characters", GRPC_INVALID_ARGUMENT })
  end

  nk.account_update_id(context.user_id, nil, nil, name)
  return nk.json_encode({ name = name })
end, "profile_set_name")

nk.register_rpc(function(context, payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local skin = math.floor(tonumber(req.skin) or -1)
  if skin < 0 or skin >= MAX_SKINS then error({ "unknown skin", GRPC_INVALID_ARGUMENT }) end

  nk.account_update_id(context.user_id, nil, nil, nil, nil, nil, nil, "skin:" .. skin)
  return nk.json_encode({ skin = skin })
end, "profile_set_skin")
