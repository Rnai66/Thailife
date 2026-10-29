-- Player profile: character name (ชื่อตัวละคร), stored as the account's display name, and
-- the character look (หน้าตา), stored as the avatar url "look:g.f.h.c.t.b" (+ ".o" when wearing an outfit)
-- (gender, face, hairstyle, hair colour, top colour, bottom colour; old accounts "skin:N").
--   profile_set_name  {"name": "..."}  -> {name}
--   profile_get_look  {}               -> {code, created, level}
--   profile_set_look  {"code": "g.f.h.c.t.b", "place": "create" | "barber" | "tailor"} -> {code, paid}
--       create: once, free options only · barber (ร้านตัดผม): hairstyle + colour ·
--       tailor (ร้านเสื้อผ้า): top + bottom colours. Paid options need the money and the level.
--   profile_set_skin  {"skin": 0..7}   -> {skin}   (older app versions)
-- Other players read both with GetUsers, so they can't be spoofed through match messages.

local nk = require("nakama")
local data = require("shop_data")

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

-- ── Looks ───────────────────────────────────────────────────────────────────
-- { price, level } per option; index 0 first. Mirrors Look.cs (names and colours live there).
local HAIR_STYLES = { [0] = { 0, 1 }, { 0, 1 }, { 0, 1 }, { 0, 1 }, { 0, 1 }, { 0, 1 },
                      { 150, 2 }, { 300, 3 }, { 400, 5 }, { 600, 7 }, { 100, 2 } }
local HAIR_COLORS = { [0] = { 0, 1 }, { 0, 1 }, { 0, 1 }, { 150, 3 }, { 200, 4 }, { 300, 5 }, { 300, 5 }, { 300, 5 }, { 500, 8 } }
local CLOTH_COLORS = { [0] = { 0, 1 }, { 0, 1 }, { 0, 1 }, { 0, 1 }, { 0, 1 }, { 100, 2 }, { 100, 2 }, { 150, 3 }, { 150, 3 },
                       { 150, 3 }, { 200, 4 }, { 200, 4 }, { 1000, 10 } }
-- Outfits (ชุด) from the clothes shop: bought once, then worn or taken off for free. Mirrors Look.Outfits.
local OUTFITS = { [0] = { 0, 1 }, { 2500, 10 }, { 3000, 8 }, { 800, 3 }, { 1500, 5 }, { 300, 1 }, { 2000, 20 },
                  { 2500, 12 }, { 400, 2 }, { 1800, 15 } }
local SERVICE = { barber = 40, tailor = 60 } -- the haircut or the sewing, on top of the option's price
local LOOK = { collection = "profile", key = "look" }

local function count(t) local n = -1 for k, _ in pairs(t) do if k > n then n = k end end return n + 1 end

local function parse(code)
  local p = {}
  for v in tostring(code or ""):gmatch("[^%.]+") do table.insert(p, math.floor(tonumber(v) or -1)) end
  if #p ~= 6 and #p ~= 7 then return nil end
  local l = { g = p[1], f = p[2], h = p[3], c = p[4], t = p[5], b = p[6], o = p[7] or 0 }
  if l.o < 0 or l.o >= count(OUTFITS) then return nil end
  if l.g < 0 or l.g > 1 or l.f < 0 or l.f > 1 then return nil end
  if l.h < 0 or l.h >= count(HAIR_STYLES) or l.c < 0 or l.c >= count(HAIR_COLORS) then return nil end
  if l.t < 0 or l.t >= count(CLOTH_COLORS) or l.b < 0 or l.b >= count(CLOTH_COLORS) then return nil end
  return l
end

local function code_of(l)
  local code = table.concat({ l.g, l.f, l.h, l.c, l.t, l.b }, ".")
  if (l.o or 0) > 0 then code = code .. "." .. l.o end
  return code
end

local function current(user_id)
  local account = nk.account_get_id(user_id)
  local url = account.user.avatar_url or ""
  local l = parse(url:match("^look:(.+)$"))
  if l then return l end
  local skin = tonumber(url:match("^skin:(%d+)$") or "0") or 0
  return { g = skin >= 2 and 1 or 0, f = skin % 2, h = 0, c = 0, t = 0, b = 0, o = 0 }
end

local function level_of(user_id)
  local objects = nk.storage_read({ { collection = "career", key = "stats", user_id = user_id } })
  return (#objects > 0 and objects[1].value.level) or 1
end

local function look_record(user_id)
  local objects = nk.storage_read({ { collection = LOOK.collection, key = LOOK.key, user_id = user_id } })
  local r = (#objects > 0) and objects[1].value or {}
  r.outfits = r.outfits or {} -- "id" -> true for outfits bought
  return r
end

local function created(user_id) return look_record(user_id).created == true end

local function owned_list(r)
  local list = {}
  for k, v in pairs(r.outfits) do if v then table.insert(list, tonumber(k)) end end
  table.sort(list)
  if #list == 0 then return nil end -- json_encode turns an empty table into {}, which the client can't read as a list
  return list
end

local function err(msg, code) error({ msg, code or 9 }) end

nk.register_rpc(function(context, payload)
  local l = current(context.user_id)
  local r = look_record(context.user_id)
  return nk.json_encode({ code = code_of(l), created = r.created == true, level = level_of(context.user_id), outfits = owned_list(r) })
end, "profile_get_look")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  local new = parse(req.code)
  if not new then err("invalid look", GRPC_INVALID_ARGUMENT) end
  local old = current(user_id)
  local record = look_record(user_id)
  local place = req.place
  local cost, need = 0, 1
  local function take(opt) cost = cost + opt[1]; if opt[2] > need then need = opt[2] end end

  if place == "create" then
    if created(user_id) then err("already created") end
    for _, opt in ipairs({ HAIR_STYLES[new.h], HAIR_COLORS[new.c], CLOTH_COLORS[new.t], CLOTH_COLORS[new.b] }) do
      if opt[1] > 0 or opt[2] > 1 then err("shop only") end
    end
    if new.o ~= 0 then err("shop only") end
  elseif place == "barber" then
    if new.g ~= old.g or new.f ~= old.f or new.t ~= old.t or new.b ~= old.b or new.o ~= old.o then err("barber does hair only") end
    if new.h == old.h and new.c == old.c then err("no change") end
    cost = SERVICE.barber
    if new.h ~= old.h then take(HAIR_STYLES[new.h]) end
    if new.c ~= old.c then take(HAIR_COLORS[new.c]) end
  elseif place == "tailor" then
    if new.g ~= old.g or new.f ~= old.f or new.h ~= old.h or new.c ~= old.c then err("tailor does clothes only") end
    if new.t == old.t and new.b == old.b and new.o == old.o then err("no change") end
    if new.t ~= old.t or new.b ~= old.b then
      cost = SERVICE.tailor
      if new.t ~= old.t then take(CLOTH_COLORS[new.t]) end
      if new.b ~= old.b then take(CLOTH_COLORS[new.b]) end
    end
    -- A new outfit is bought once; one you already own (or none) is free to put on
    if new.o ~= old.o and new.o > 0 and not record.outfits[tostring(new.o)] then
      take(OUTFITS[new.o])
      record.outfits[tostring(new.o)] = true
    end
  else
    err("unknown place", GRPC_INVALID_ARGUMENT)
  end

  if need > level_of(user_id) then err("level " .. need) end
  if cost > 0 then
    local account = nk.account_get_id(user_id)
    local w = account.wallet or {}
    if type(w) == "string" then w = nk.json_decode(w) end
    if (w[data.CURRENCY] or 0) < cost then err("not enough baht") end
    nk.wallet_update(user_id, { [data.CURRENCY] = -cost }, { reason = "look_" .. place }, true)
  end
  local code = code_of(new)
  nk.account_update_id(user_id, nil, nil, nil, nil, nil, nil, "look:" .. code)
  record.created = true
  nk.storage_write({ { collection = LOOK.collection, key = LOOK.key, user_id = user_id, value = record,
                       permission_read = 1, permission_write = 0 } })
  return nk.json_encode({ code = code, created = true, paid = cost, outfits = owned_list(record) })
end, "profile_set_look")
