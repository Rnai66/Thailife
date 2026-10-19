-- Temple-fair attractions (งานวัด) during the traditional festivals (life_core.FESTIVALS):
-- สอยดาว, บิงโก, ปาเป้าลูกโป่ง, ชิงช้าสวรรค์, ม้าหมุน, หนังกลางแปลง, ลิเก. Boxing and the
-- chicken beauty contest run at the same festivals (boxing.lua, chickens.lua).
--   festival_get  {}                -> today's festival (or the next one) + the year's calendar
--   festival_play {"game": "soidao"} -> pays the ticket, rolls the prize on the server

local nk = require("nakama")
local life = require("life_core")
local data = require("shop_data")

local RECORD = { collection = "festival", key = "record" }
local CURRENCY = data.CURRENCY
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local MONTHS = { "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." }

-- price in baht; energy = how much fun restores; once = free shows, once per festival
local GAMES = {
  soidao   = { name = "สอยดาว",          price = 20 },
  bingo    = { name = "บิงโก",           price = 30, big = true },
  kokai    = { name = "มหาลัย ก.ไก่",      price = 30 },
  darts    = { name = "ปาเป้าลูกโป่ง",     price = 10 },
  ferris   = { name = "ชิงช้าสวรรค์",      price = 20, energy = 10, big = true },
  carousel = { name = "ม้าหมุน",          price = 10, energy = 6, big = true },
  movie    = { name = "หนังกลางแปลง",     price = 0,  energy = 12, once = true },
  likay    = { name = "ลิเก",            price = 0,  energy = 12, once = true },
}

-- Prize tables: weight, label, and what it gives
local PRIZES = {
  soidao = {
    { 30, "ลูกชิ้นปิ้ง",              food = "look_chin" },
    { 22, "น้ำอัดลม",                food = "soda" },
    { 15, "เงินรางวัล 50 บาท",        baht = 50 },
    { 12, "ข้าวเหนียวมะม่วง",          food = "mango_sticky" },
    { 10, "กระถางต้นไม้",             furniture = "plant" },
    { 8,  "หมอนข้าง",               furniture = "pillow" },
    { 3,  "ดาวทอง! เงินรางวัล 500 บาท", baht = 500 },
  },
  darts = {
    { 50, "เสียใจด้วย ปาไม่แตก" },
    { 30, "ลูกชิ้นปิ้ง",   food = "look_chin" },
    { 20, "ชานมไข่มุก",   food = "bubble_tea" },
  },
  bingo = {
    { 75, "ไม่ถูกรางวัล" },
    { 20, "บิงโก! 150 บาท", baht = 150 },
    { 5,  "บิงโกแจ็กพอต! 600 บาท", baht = 600 },
  },
}

-- มหาลัย ก.ไก่: a card of 4 Thai consonants; the host draws ก-ฮ from the drum and chalks them
-- on the blackboard until the card is complete. Fewer draws = bigger prize.
local CONSONANTS = { "ก","ข","ฃ","ค","ฅ","ฆ","ง","จ","ฉ","ช","ซ","ฌ","ญ","ฎ","ฏ","ฐ","ฑ","ฒ","ณ","ด","ต","ถ",
                     "ท","ธ","น","บ","ป","ผ","ฝ","พ","ฟ","ภ","ม","ย","ร","ล","ว","ศ","ษ","ส","ห","ฬ","อ","ฮ" }
local KOKAI_PRIZES = { { 20, 400 }, { 28, 100 }, { 34, 30 } } -- done within N draws -> baht

local function kokai()
  local deck = {}
  for i, c in ipairs(CONSONANTS) do deck[i] = c end
  for i = #deck, 2, -1 do local j = math.random(i); deck[i], deck[j] = deck[j], deck[i] end
  local card = { deck[1], deck[2], deck[3], deck[4] }
  for i = #deck, 2, -1 do local j = math.random(i); deck[i], deck[j] = deck[j], deck[i] end
  local need, draws = { [card[1]] = true, [card[2]] = true, [card[3]] = true, [card[4]] = true }, {}
  local left = 4
  for _, c in ipairs(deck) do
    table.insert(draws, c)
    if need[c] then left = left - 1 end
    if left == 0 then break end
  end
  local baht = 0
  for _, p in ipairs(KOKAI_PRIZES) do
    if #draws <= p[1] then baht = p[2]; break end
  end
  return card, draws, baht
end

local function roll(table_)
  local total = 0
  for _, p in ipairs(table_) do total = total + p[1] end
  local r = math.random() * total
  for _, p in ipairs(table_) do
    r = r - p[1]
    if r <= 0 then return p end
  end
  return table_[#table_]
end

local function fair_always_open(env) return env ~= nil and env.fair_always_open == "true" end

local function read(user_id)
  local objects = nk.storage_read({ { collection = RECORD.collection, key = RECORD.key, user_id = user_id } })
  local r = (#objects > 0) and objects[1].value or {}
  r.watched = r.watched or {}
  return r
end

local WHERE = {
  village = "หมู่บ้าน (ลานวัด)", small = "ตำบล", big = "เมือง", beach = "ริมทะเล", both = "ทุกที่",
}
local KIND = { tradition = "งานประเพณี", monthly = "งานประจำเดือน", national = "ระดับประเทศ", test = "ทดสอบ" }

local function where_of(f)
  if f.kind == "tradition" then return (f.scale == "big" and "เมือง" or "ตำบล") .. " + วัดในหมู่บ้าน" end
  return WHERE[f.scale] or ""
end

local function view(env, extra)
  local now = nk.time()
  local open, fair_id, ends_in, next_in, f = life.fair(now, fair_always_open(env))
  local calendar = {}
  for _, u in ipairs(life.upcoming(now, 12)) do
    local x = u.event
    local month = math.floor(x.start / 30) + 1
    local day = x.start % 30 + 1
    table.insert(calendar, {
      name = x.name, date = day .. "-" .. (day + x.days - 1) .. " " .. MONTHS[month],
      where = where_of(x), kind = KIND[x.kind] or "", scale = x.scale, open = u.open,
      in_s = math.floor(u.in_ms / 1000), ends_s = math.floor(u.ends_ms / 1000),
    })
  end
  local result = {
    open = open, name = f and f.name or "", id = fair_id, scale = f and f.scale or "",
    kind = f and f.kind or "", temple = f and f.temple or false,
    where = f and where_of(f) or "",
    ends_in_s = math.floor((ends_in or 0) / 1000), next_in_s = math.floor((next_in or 0) / 1000),
    calendar = calendar,
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

nk.register_rpc(function(context, payload)
  return view(context.env)
end, "festival_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local g = GAMES[req.game or ""]
  if not g then error({ "unknown game", GRPC_INVALID_ARGUMENT }) end
  local open, fair_id, _, _, f = life.fair(nk.time(), fair_always_open(context.env))
  if not open then error({ "no festival today", GRPC_FAILED_PRECONDITION }) end
  -- The ferris wheel, carousel and bingo only come to the big city festivals
  if g.big and f.scale ~= "big" and f.scale ~= "both" then error({ "big festivals only", GRPC_FAILED_PRECONDITION }) end

  local r = read(user_id)
  local key = req.game .. ":" .. fair_id
  if g.once and r.watched[key] then error({ "already watched", GRPC_FAILED_PRECONDITION }) end

  if g.price > 0 then
    local account = nk.account_get_id(user_id)
    local wallet = account.wallet or {}
    if type(wallet) == "string" then wallet = nk.json_decode(wallet) end
    if (wallet[CURRENCY] or 0) < g.price then error({ "not enough baht", GRPC_FAILED_PRECONDITION }) end
    nk.wallet_update(user_id, { [CURRENCY] = -g.price }, { reason = "festival", game = req.game }, true)
  end

  local result = { game = req.game, game_name = g.name, paid = g.price }
  if g.energy then
    local s = life.add_energy(user_id, g.energy)
    result.energy = math.floor(s.energy)
    result.gained = g.energy
    result.prize = g.once and "ดูจบแล้ว สนุกมาก!" or "สนุกมาก!"
  end
  if req.game == "kokai" then
    local card, draws, baht = kokai()
    result.card = card
    result.draws = draws
    result.won = baht > 0
    result.prize = baht > 0 and ("ครบใน " .. #draws .. " ตัว! รับ " .. baht .. " บาท") or ("ครบตัวที่ " .. #draws .. " ช้าไป ไม่ได้รางวัล")
    if baht > 0 then nk.wallet_update(user_id, { [CURRENCY] = baht }, { reason = "festival_prize", game = "kokai" }, true) end
  end
  if PRIZES[req.game] then
    local p = roll(PRIZES[req.game])
    result.prize = p[2]
    result.won = p.baht ~= nil or p.food ~= nil or p.furniture ~= nil
    if p.baht then nk.wallet_update(user_id, { [CURRENCY] = p.baht }, { reason = "festival_prize", game = req.game }, true) end
    if p.food or p.furniture then
      local items = data.read_items(user_id)
      if p.food then items.food[p.food] = (items.food[p.food] or 0) + 1 end
      if p.furniture then items.furniture[p.furniture] = (items.furniture[p.furniture] or 0) + 1 end
      data.write_items(user_id, items)
    end
  end
  if g.once then
    r.watched[key] = true
    nk.storage_write({ { collection = RECORD.collection, key = RECORD.key, user_id = user_id, value = r,
                         permission_read = 1, permission_write = 0 } })
  end
  return view(context.env, result)
end, "festival_play")
