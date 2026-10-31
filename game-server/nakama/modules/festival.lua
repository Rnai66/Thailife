-- Temple-fair attractions (งานวัด) during the traditional festivals (life_core.FESTIVALS):
-- สอยดาว, บิงโก, ปาโป่ง, ชิงช้าสวรรค์, ม้าหมุน, หนังกลางแปลง, ลิเก. Boxing and the
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
  soidao   = { name = "สอยดาว",          price = 40, chance = true },
  bingo    = { name = "บิงโก",           price = 30, big = true, chance = true },
  kokai    = { name = "มหาลัย ก.ไก่",      price = 30, chance = true },
  darts    = { name = "ปาโป่ง",           price = 10, chance = true },
  ferris   = { name = "ชิงช้าสวรรค์",      price = 20, energy = 10, big = true },
  carousel = { name = "ม้าหมุน",          price = 10, energy = 6, big = true },
  movie    = { name = "หนังกลางแปลง",     price = 0,  energy = 12, once = true },
  likay    = { name = "ลิเก",            price = 0,  energy = 12, once = true },
  -- the village temple fair ground (ลานอเนกประสงค์ across from the temple)
  ferris_village = { name = "ชิงช้าสวรรค์",  price = 20, energy = 10 },
  morlam   = { name = "หมอลำ · กันตรึม",    price = 0,  energy = 15, once = true }, -- older app versions
  ramwong  = { name = "รำวง",            price = 20, energy = 8 },  -- a round on the village rum wong stage
}

-- สอยดาว: pick a folded paper ticket off the dry tree; the number on it wins one of the prizes
-- on the shelves. Most are goods people gave the temple as merit (ของทำบุญ); a few are big ones.
-- { weight, label, what it gives, icon }
local SOIDAO = {
  { 18, "มาม่า 1 แพ็ค",       food = "mama",          icon = "food_mama" },
  { 12, "สบู่ก้อน",           goods = "soap",         icon = "sell_goods_soap" },
  { 11, "ยาสีฟัน",           goods = "toothpaste",   icon = "sell_goods_toothpaste" },
  { 10, "ผงซักฟอก",          goods = "detergent",    icon = "sell_goods_detergent" },
  { 9,  "น้ำปลา",            goods = "fish_sauce",   icon = "sell_goods_fish_sauce" },
  { 8,  "ช้อนส้อม 1 โหล",     goods = "spoons",       icon = "sell_goods_spoons" },
  { 7,  "กะละมัง",           goods = "basin",        icon = "sell_goods_basin" },
  { 6,  "เสื่อ",              furniture = "mat",      icon = "furn_mat" },
  { 5,  "กระติกน้ำ",          goods = "flask",        icon = "sell_goods_flask" },
  { 4,  "กระเป๋าเป้",         goods = "backpack",     icon = "sell_goods_backpack" },
  { 2.5, "ฟูกนอน",            goods = "futon",        icon = "sell_goods_futon",       big = true },
  { 2,  "หม้อหุงข้าว",        goods = "rice_cooker",  icon = "sell_goods_rice_cooker", big = true },
  { 0.8, "รถจักรยาน!",        bicycle = true,         icon = "goods_bicycle",          big = true },
  { 0.4, "โทรทัศน์! รางวัลใหญ่", furniture = "tv",       icon = "furn_tv",                big = true },
  { 0.25, "ตู้เย็น! รางวัลใหญ่",   furniture = "fridge",   icon = "furn_fridge",            big = true },
}

-- ปาโป่ง: three darts at the balloon board; how many pop decides the prize
local DART_HIT = 0.55
local DART_PRIZES = {
  [0] = { "เสียใจด้วย ไม่แตกสักลูก" },
  [1] = { "ลูกชิ้นปิ้ง",   food = "look_chin",   icon = "food_look_chin" },
  [2] = { "ชานมไข่มุก",   food = "bubble_tea",  icon = "food_bubble_tea" },
  [3] = { "ตุ๊กตาหมี! แตกครบ 3 ลูก", goods = "teddy", icon = "sell_goods_teddy" },
}

-- บิงโก: a 5x5 card (B 1-15, I 16-30, N 31-45, G 46-60, O 61-75, free middle); the caller draws
-- balls until a row, column or diagonal is complete. Fewer calls = bigger prize.
local BINGO_MAX_CALLS = 34
local BINGO_PRIZES = { { 22, 500 }, { 28, 120 }, { 34, 40 } } -- line within N calls -> baht

local function bingo()
  local card = {}
  for col = 0, 4 do
    local pool = {}
    for n = col * 15 + 1, col * 15 + 15 do table.insert(pool, n) end
    for i = #pool, 2, -1 do local j = math.random(i); pool[i], pool[j] = pool[j], pool[i] end
    for row = 0, 4 do card[row * 5 + col + 1] = pool[row + 1] end
  end
  card[13] = 0 -- free space
  local balls = {}
  for n = 1, 75 do balls[n] = n end
  for i = #balls, 2, -1 do local j = math.random(i); balls[i], balls[j] = balls[j], balls[i] end
  local marked = { [13] = true }
  local where = {}
  for i, n in ipairs(card) do if n > 0 then where[n] = i end end
  local function line()
    for r = 0, 4 do
      local ok = true
      for c = 0, 4 do if not marked[r * 5 + c + 1] then ok = false end end
      if ok then return true end
    end
    for c = 0, 4 do
      local ok = true
      for r = 0, 4 do if not marked[r * 5 + c + 1] then ok = false end end
      if ok then return true end
    end
    local d1, d2 = true, true
    for i = 0, 4 do
      if not marked[i * 5 + i + 1] then d1 = false end
      if not marked[i * 5 + (4 - i) + 1] then d2 = false end
    end
    return d1 or d2
  end
  local calls, done = {}, false
  for _, n in ipairs(balls) do
    table.insert(calls, n)
    if where[n] then marked[where[n]] = true end
    if line() then done = true; break end
    if #calls >= BINGO_MAX_CALLS then break end
  end
  local baht = 0
  if done then
    for _, p in ipairs(BINGO_PRIZES) do
      if #calls <= p[1] then baht = p[2]; break end
    end
  end
  return card, calls, done, baht
end

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

-- Hand over a prize: food or goods into the bag, furniture into the house store, a bicycle to the bank
-- account (a second one is paid out instead), or baht
local function award(user_id, p, result)
  if p.baht then nk.wallet_update(user_id, { [CURRENCY] = p.baht }, { reason = "festival_prize" }, true) end
  if p.bicycle then
    local objects = nk.storage_read({ { collection = "bank", key = "account", user_id = user_id } })
    local a = (#objects > 0) and objects[1].value or {}
    a.assets = a.assets or {}
    if a.assets.car_bicycle then
      nk.wallet_update(user_id, { [CURRENCY] = 1500 }, { reason = "festival_prize", note = "second bicycle sold" }, true)
      result.prize = "รถจักรยาน! (มีแล้ว ขายต่อได้ 1,500 บาท)"
    else
      a.assets.car_bicycle = true
      nk.storage_write({ { collection = "bank", key = "account", user_id = user_id, value = a, permission_read = 1, permission_write = 0 } })
      result.prize = "รถจักรยาน! เดินทางเร็วขึ้น 10%"
    end
  end
  if p.food or p.furniture or p.goods then
    local items = data.read_items(user_id)
    if p.food then items.food[p.food] = (items.food[p.food] or 0) + 1 end
    if p.furniture then items.furniture[p.furniture] = (items.furniture[p.furniture] or 0) + 1 end
    if p.goods then items.goods[p.goods] = (items.goods[p.goods] or 0) + 1 end
    data.write_items(user_id, items)
  end
end

local function fair_always_open(env) return env ~= nil and env.fair_always_open == "true" end

-- Paid games with a random prize count as simulated gambling, which Apple won't take from an
-- individual developer: they stay closed unless CHANCE_GAMES=true (game-server/.env)
local function chance_games(env) return env ~= nil and env.chance_games == "true" end

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
    calendar = calendar, chance_games = chance_games(env),
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
  if g.chance and not chance_games(context.env) then error({ "game closed", GRPC_FAILED_PRECONDITION }) end
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
  if req.game == "soidao" then
    local p = roll(SOIDAO)
    result.ticket = string.format("%03d", math.random(1, 500))
    result.prize = p[2]
    result.icon = p.icon
    result.big = p.big == true
    result.won = true
    award(user_id, p, result)
  end
  if req.game == "darts" then
    local throws, pops = {}, 0
    for i = 1, 3 do
      local hit = math.random() < DART_HIT
      throws[i] = hit
      if hit then pops = pops + 1 end
    end
    local p = DART_PRIZES[pops]
    result.throws = throws
    result.pops = pops
    result.prize = p[1]
    result.icon = p.icon or ""
    result.won = pops > 0
    award(user_id, p, result)
  end
  if req.game == "bingo" then
    local card, calls, done, baht = bingo()
    result.bingo_card = card
    result.calls = calls
    result.won = baht > 0
    result.prize = baht > 0 and ("บิงโก! ครบแถวใน " .. #calls .. " ลูก รับ " .. baht .. " บาท")
                   or (done and ("ครบแถวลูกที่ " .. #calls .. " ช้าไปนิด ไม่ได้รางวัล") or "ยังไม่ครบแถว เสียใจด้วย")
    if baht > 0 then nk.wallet_update(user_id, { [CURRENCY] = baht }, { reason = "festival_prize", game = "bingo" }, true) end
  end
  if g.once then
    r.watched[key] = true
    nk.storage_write({ { collection = RECORD.collection, key = RECORD.key, user_id = user_id, value = r,
                         permission_read = 1, permission_write = 0 } })
  end
  return view(context.env, result)
end, "festival_play")
