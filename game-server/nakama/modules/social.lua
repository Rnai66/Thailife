-- Talking to other players, friends and marriage (แชท เพื่อน แต่งงาน).
--
--   chat_send      {"text": "...", "to": user_id?}       -> {ok}  to = a friend: a private message;
--                                                                 none: the whole server ("world" room)
--   chat_report    {"user_id": "...", "text": "..."}     -> {ok}  kept for the admin to look at
--   marry_get      {}                                    -> view
--   marry_propose  {"to": user_id}                       -> view  (buys the ring; friends only, both single)
--   marry_answer   {"from": user_id, "accept": bool}     -> view  (a "no" gives the ring money back)
--   marry_divorce  {}                                    -> view
--   gift_get       {"to": user_id}                       -> what you can give that friend: baht, today's
--                                                         limits, and the giftable things in your bag
--   gift_send      {"to": user_id, "baht": n}            -> โอนเงินให้เพื่อน (one transaction for both wallets)
--                  {"to": user_id, "item": id, "count": n} -> มอบของขวัญ: food, seeds, rice, field crops, fish,
--                                                         chicken feed, fair goods (the friend's barn / field
--                                                         store must have room)
--
-- Friends themselves are Nakama's own (add / accept / remove / block from the client); Nakama sends the
-- friend-request notifications. Messages are cleaned here (length, rate, rude words) and sent by the
-- server, so the client can't post into the room directly. Notifications this module sends (code):
--   100 a message from a friend · 110 a proposal · 111 accepted · 112 declined · 113 divorced · 120 a gift

local nk = require("nakama")
local data = require("shop_data")
local land = require("land_core")

local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9
local ROOM = "world"
local MAX_LEN = 120
local GAP_MS = 1500           -- at most one message every 1.5 s
local RING = 5000             -- the engagement ring (แหวนหมั้น)
local FRIEND, BLOCKED = 0, 3  -- Nakama friend states

local NOTE = { chat = 100, proposal = 110, accepted = 111, declined = 112, divorced = 113, gift = 120 }

local RUDE = { "เหี้ย", "ควย", "เย็ด", "แตด", "กะหรี่", "ร่าน", "อีดอก", "สัส", "fuck", "shit", "bitch", "cunt", "dick", "pussy" }

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

local function read(user_id, key)
  local objects = nk.storage_read({ { collection = "social", key = key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function write(user_id, key, value)
  nk.storage_write({ { collection = "social", key = key, user_id = user_id, value = value,
                       permission_read = 1, permission_write = 0 } })
end

-- Another module's record (the fish basket, inventory/fish)
local function read_any(user_id, collection, key)
  local objects = nk.storage_read({ { collection = collection, key = key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function write_any(user_id, collection, key, value)
  nk.storage_write({ { collection = collection, key = key, user_id = user_id, value = value,
                       permission_read = 1, permission_write = 0 } })
end

local function name_of(user_id)
  local ok, account = pcall(nk.account_get_id, user_id)
  if not ok or not account then return nil end
  local u = account.user or {}
  local name = u.display_name
  if name == nil or name == "" then name = u.username end
  return name
end

local function baht(user_id)
  local w = nk.account_get_id(user_id).wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[data.CURRENCY] or 0
end

-- The state `other` has in user_id's friend list (nil = not there)
local function friend_state(user_id, other)
  local cursor
  repeat
    local friends, next_cursor = nk.friends_list(user_id, 100, nil, cursor)
    for _, f in ipairs(friends or {}) do
      local u = f.user or {}
      if (u.user_id or u.id) == other then return f.state end
    end
    cursor = next_cursor
  until cursor == nil or cursor == ""
  return nil
end

-- Trim, keep it short, drop control characters, cover rude words
local function clean(text)
  text = tostring(text or ""):gsub("[%c]", " "):gsub("^%s+", ""):gsub("%s+$", "")
  if #text > MAX_LEN * 3 then text = text:sub(1, MAX_LEN * 3) end
  -- cut back to whole UTF-8 characters and at most MAX_LEN of them
  local out, n = {}, 0
  for ch in text:gmatch("[%z\1-\127\194-\244][\128-\191]*") do
    n = n + 1
    if n > MAX_LEN then break end
    out[n] = ch
  end
  text = table.concat(out)
  local lower = text:lower()
  for _, word in ipairs(RUDE) do
    local from = 1
    while true do
      local i, j = lower:find(word, from, true)
      if not i then break end
      text = text:sub(1, i - 1) .. "***" .. text:sub(j + 1)
      lower = lower:sub(1, i - 1) .. "***" .. lower:sub(j + 1)
      from = i + 3
    end
  end
  return text
end

local function notify(to, subject, code, content, sender)
  nk.notification_send(to, subject, content, code, sender, true)
end

-- ── Chat ──────────────────────────────────────────────────────────────────────────────────
nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local text = clean(req.text)
  if text == "" then err("empty", GRPC_INVALID_ARGUMENT) end

  local st = read(user_id, "chat")
  local now = nk.time()
  if st.muted_until and now < st.muted_until then err("muted") end
  if st.last_ms and now - st.last_ms < GAP_MS then err("too fast") end
  st.last_ms = now
  write(user_id, "chat", st)

  local name = name_of(user_id) or "ผู้เล่น"
  local to = req.to
  if type(to) == "string" and to ~= "" then
    if to == user_id then err("self", GRPC_INVALID_ARGUMENT) end
    if friend_state(user_id, to) ~= FRIEND then err("not friends") end
    -- quietly dropped when they blocked us
    if friend_state(to, user_id) ~= BLOCKED then
      notify(to, "แชทจาก " .. name, NOTE.chat, { text = text, from = user_id, name = name }, user_id)
    end
    return nk.json_encode({ ok = true, text = text })
  end

  local channel = nk.channel_id_build(user_id, ROOM, 1)
  nk.channel_message_send(channel, { text = text, name = name }, user_id, context.username, true)
  return nk.json_encode({ ok = true, text = text })
end, "chat_send")

nk.register_rpc(function(context, payload)
  local req = decode(payload)
  local who = tostring(req.user_id or "")
  if who == "" or who == context.user_id then err("unknown player", GRPC_INVALID_ARGUMENT) end
  local now = nk.time()
  nk.storage_write({ { collection = "chat_reports", key = string.format("%d_%s", now, context.user_id:sub(1, 8)),
                       user_id = who, value = { by = context.user_id, text = clean(req.text), ms = now },
                       permission_read = 0, permission_write = 0 } })
  return nk.json_encode({ ok = true })
end, "chat_report")

-- Clients join the room to listen; only the server posts into it
nk.register_rt_before(function() return nil end, "ChannelMessageSend")
nk.register_rt_before(function() return nil end, "ChannelMessageUpdate")
nk.register_rt_before(function() return nil end, "ChannelMessageRemove")

-- ── Marriage ──────────────────────────────────────────────────────────────────────────────
local function marriage(user_id)
  local m = read(user_id, "marriage")
  m.proposals = m.proposals or {} -- from user id -> { name, ms }
  return m
end

local function view(user_id, m, extra)
  local proposals = {}
  for from, p in pairs(m.proposals) do table.insert(proposals, { user_id = from, name = p.name, ms = p.ms }) end
  table.sort(proposals, function(a, b) return a.ms > b.ms end)
  local result = {
    spouse_id = m.spouse, spouse_name = m.spouse_name, since_ms = m.since,
    proposed_to = m.sent, proposed_name = m.sent_name, ring_cost = RING,
    proposals = (#proposals > 0) and proposals or nil,
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

nk.register_rpc(function(context, payload)
  local m = marriage(context.user_id)
  return view(context.user_id, m)
end, "marry_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local to = tostring(decode(payload).to or "")
  if to == "" or to == user_id then err("unknown player", GRPC_INVALID_ARGUMENT) end
  local m = marriage(user_id)
  if m.spouse then err("already married") end
  if m.sent then err("already proposed") end
  if friend_state(user_id, to) ~= FRIEND then err("not friends") end
  local them = marriage(to)
  if them.spouse then err("they are married") end
  if baht(user_id) < RING then err("not enough baht") end
  nk.wallet_update(user_id, { [data.CURRENCY] = -RING }, { reason = "ring", to = to }, true)
  local name, their_name = name_of(user_id) or "ผู้เล่น", name_of(to) or "ผู้เล่น"
  them.proposals[user_id] = { name = name, ms = nk.time() }
  m.sent, m.sent_name = to, their_name
  write(to, "marriage", them)
  write(user_id, "marriage", m)
  notify(to, name .. " ขอคุณแต่งงาน!", NOTE.proposal, { from = user_id, name = name }, user_id)
  return view(user_id, m, { done = "มอบแหวนขอ" .. their_name .. "แต่งงานแล้ว รอคำตอบนะ" })
end, "marry_propose")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local from = tostring(req.from or "")
  local m = marriage(user_id)
  local p = m.proposals[from]
  if not p then err("no proposal") end
  m.proposals[from] = nil
  local them = marriage(from)
  local name = name_of(user_id) or "ผู้เล่น"
  if them.sent == user_id then them.sent, them.sent_name = nil, nil end

  if req.accept == true then
    if m.spouse then err("already married") end
    if them.spouse then
      write(user_id, "marriage", m)
      err("they are married")
    end
    local now = nk.time()
    m.spouse, m.spouse_name, m.since = from, p.name, now
    them.spouse, them.spouse_name, them.since = user_id, name, now
    -- anyone else waiting on either of us gets their ring back
    for other in pairs(m.proposals) do
      local o = marriage(other)
      if o.sent == user_id then
        o.sent, o.sent_name = nil, nil
        write(other, "marriage", o)
        nk.wallet_update(other, { [data.CURRENCY] = RING }, { reason = "ring_back" }, true)
        notify(other, name .. " แต่งงานไปแล้ว · ได้ค่าแหวนคืน", NOTE.declined, { from = user_id, name = name }, user_id)
      end
    end
    m.proposals = {}
    if m.sent and m.sent ~= from then
      local o = marriage(m.sent)
      o.proposals[user_id] = nil
      write(m.sent, "marriage", o)
      nk.wallet_update(user_id, { [data.CURRENCY] = RING }, { reason = "ring_back" }, true)
    end
    m.sent, m.sent_name = nil, nil
    write(user_id, "marriage", m)
    write(from, "marriage", them)
    notify(from, name .. " ตอบตกลงแต่งงาน!", NOTE.accepted, { from = user_id, name = name }, user_id)
    return view(user_id, m, { done = "แต่งงานกับ" .. p.name .. "แล้ว! ยินดีด้วย" })
  end

  write(user_id, "marriage", m)
  write(from, "marriage", them)
  nk.wallet_update(from, { [data.CURRENCY] = RING }, { reason = "ring_back" }, true)
  notify(from, name .. " ปฏิเสธคำขอแต่งงาน · ได้ค่าแหวนคืน", NOTE.declined, { from = user_id, name = name }, user_id)
  return view(user_id, m, { done = "ปฏิเสธคำขอแต่งงานแล้ว" })
end, "marry_answer")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local m = marriage(user_id)
  if not m.spouse then err("not married") end
  local spouse = m.spouse
  local them = marriage(spouse)
  local name = name_of(user_id) or "ผู้เล่น"
  if them.spouse == user_id then them.spouse, them.spouse_name, them.since = nil, nil, nil end
  m.spouse, m.spouse_name, m.since = nil, nil, nil
  write(spouse, "marriage", them)
  write(user_id, "marriage", m)
  notify(spouse, name .. " ขอหย่าแล้ว", NOTE.divorced, { from = user_id, name = name }, user_id)
  return view(user_id, m, { done = "หย่าแล้ว" })
end, "marry_divorce")

-- ── Gifts and money between friends (ของขวัญ · โอนเงิน) ───────────────────────────────────
-- Friends only, both ways (not when they blocked you). A real day caps how much money and how many
-- gifts one player sends, so a second account can't pump a main one.
local GIFT = { min_baht = 10, max_baht = 50000, day_baht = 100000, day_sends = 30, max_count = 999 }
local LAND_BARN = 3000 -- farming.lua: the canal-side land's own barn space

local SEA_FISH = { mackerel = "ปลาทู", squid = "ปลาหมึก", seabass = "ปลากะพงขาว", grouper = "ปลาเก๋า", kingfish = "ปลาอินทรี" }
local function fish_name(id)
  for _, f in ipairs(data.FISH) do if f.id == id then return f.name end end
  for _, f in ipairs(data.RIVER_FISH) do if f.id == id then return f.name end end
  return SEA_FISH[id]
end

local function fish_inv(user_id)
  local v = read_any(user_id, "inventory", "fish")
  v.counts = v.counts or {}
  v.total = v.total or 0
  return v
end

-- Everything a player owns that can be given: what it is, where it's kept
local function bag(user_id)
  return { items = data.read_items(user_id), crops = data.read_barn(user_id), fish = fish_inv(user_id) }
end

-- id -> {name, unit, get(b) -> count, add(b, n) -> nil | refusal}
local function thing(id)
  local kind, key = id:match("^(%a+)_(.+)$")
  if id == "feed" then
    return { name = "อาหารไก่", unit = "ถุง", get = function(b) return b.items.feed or 0 end,
             add = function(b, n) b.items.feed = (b.items.feed or 0) + n end, store = "items" }
  end
  if kind == "food" and data.FOODS[key] then
    return { name = data.FOODS[key].name, unit = "ชิ้น", get = function(b) return b.items.food[key] or 0 end,
             add = function(b, n) b.items.food[key] = (b.items.food[key] or 0) + n end, store = "items" }
  elseif kind == "seed" and data.RICE[key] then
    return { name = "เมล็ด" .. data.RICE[key].name, unit = "ถุง", get = function(b) return b.items.seeds[key] or 0 end,
             add = function(b, n) b.items.seeds[key] = (b.items.seeds[key] or 0) + n end, store = "items" }
  elseif kind == "fseed" and data.FIELD_CROPS[key] then
    return { name = "พันธุ์" .. data.FIELD_CROPS[key].name, unit = "ชุด",
             get = function(b) return (b.items.field_seeds or {})[key] or 0 end,
             add = function(b, n) b.items.field_seeds = b.items.field_seeds or {}; b.items.field_seeds[key] = (b.items.field_seeds[key] or 0) + n end,
             store = "items" }
  elseif kind == "goods" and data.GOODS[key] then
    return { name = data.GOODS[key].name, unit = "ชิ้น", get = function(b) return b.items.goods[key] or 0 end,
             add = function(b, n) b.items.goods[key] = (b.items.goods[key] or 0) + n end, store = "items" }
  elseif kind == "rice" and data.RICE[key] then
    return { name = data.RICE[key].name, unit = "กก.", get = function(b) return b.crops.rice[key] or 0 end,
             add = function(b, n, user_id)
               local total = 0
               for _, v in ipairs(data.RICE_ORDER) do total = total + (b.crops.rice[v] or 0) end
               local room = b.items.barn_capacity + (land.owned(user_id).rice and LAND_BARN or 0)
               if n > 0 and total + n > room then return "barn full" end
               b.crops.rice[key] = (b.crops.rice[key] or 0) + n
             end, store = "crops" }
  elseif kind == "crop" and data.FIELD_CROPS[key] then
    return { name = data.FIELD_CROPS[key].name, unit = data.FIELD_CROPS[key].unit, get = function(b) return b.crops.field[key] or 0 end,
             add = function(b, n)
               local total = 0
               for _, v in pairs(b.crops.field) do total = total + (tonumber(v) or 0) end
               if n > 0 and total + n > data.FIELD_STORE then return "store full" end
               b.crops.field[key] = (b.crops.field[key] or 0) + n
             end, store = "crops" }
  elseif kind == "fish" and fish_name(key) then
    return { name = fish_name(key), unit = "ตัว", get = function(b) return b.fish.counts[key] or 0 end,
             add = function(b, n) b.fish.counts[key] = (b.fish.counts[key] or 0) + n; b.fish.total = math.max(0, b.fish.total + n) end,
             store = "fish" }
  end
  return nil
end

local function giftable(b)
  local list = {}
  local function add(id)
    local t = thing(id)
    local n = t and t.get(b) or 0
    if n > 0 then table.insert(list, { id = id, name = t.name, count = math.floor(n), unit = t.unit }) end
  end
  for _, id in ipairs(data.FOOD_ORDER) do add("food_" .. id) end
  for _, v in ipairs(data.RICE_ORDER) do add("seed_" .. v); add("rice_" .. v) end
  for _, c in ipairs(data.FIELD_ORDER or {}) do add("fseed_" .. c); add("crop_" .. c) end
  add("feed")
  for _, f in ipairs(data.FISH) do add("fish_" .. f.id) end
  for _, f in ipairs(data.RIVER_FISH) do add("fish_" .. f.id) end
  for id in pairs(SEA_FISH) do add("fish_" .. id) end
  for _, g in ipairs(data.GOODS_ORDER) do add("goods_" .. g) end
  return list
end

local function save(user_id, b, store)
  if store == "items" then data.write_items(user_id, b.items)
  elseif store == "crops" then data.write_barn(user_id, b.crops)
  else write_any(user_id, "inventory", "fish", b.fish) end
end

local function gift_day(user_id)
  local g = read(user_id, "gifts")
  local today = math.floor(nk.time() / 86400000)
  if g.day ~= today then g = { day = today, baht = 0, sends = 0 } end
  return g
end

local function gift_view(user_id, to, extra)
  local g = gift_day(user_id)
  local r = { to = to, to_name = name_of(to) or "เพื่อน", baht = baht(user_id),
              min_baht = GIFT.min_baht, max_baht = GIFT.max_baht,
              baht_left = math.max(0, GIFT.day_baht - g.baht), sends_left = math.max(0, GIFT.day_sends - g.sends) }
  local items = giftable(bag(user_id))
  if #items > 0 then r.items = items end -- an empty Lua table would encode as {}
  for k, v in pairs(extra or {}) do r[k] = v end
  return nk.json_encode(r)
end

local function check_friend(user_id, to)
  if to == "" or to == user_id then err("unknown player", GRPC_INVALID_ARGUMENT) end
  if friend_state(user_id, to) ~= FRIEND or friend_state(to, user_id) == BLOCKED then err("not friends") end
end

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local to = tostring(decode(payload).to or "")
  check_friend(user_id, to)
  return gift_view(user_id, to)
end, "gift_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local to = tostring(req.to or "")
  check_friend(user_id, to)
  local g = gift_day(user_id)
  if g.sends >= GIFT.day_sends then err("too many gifts") end
  local name, their_name = name_of(user_id) or "ผู้เล่น", name_of(to) or "เพื่อน"
  local what

  if req.baht ~= nil then
    local amount = math.floor(tonumber(req.baht) or 0)
    if amount < GIFT.min_baht or amount > GIFT.max_baht then err("bad amount", GRPC_INVALID_ARGUMENT) end
    if g.baht + amount > GIFT.day_baht then err("day limit") end
    if baht(user_id) < amount then err("not enough baht") end
    nk.wallets_update({
      { user_id = user_id, changeset = { [data.CURRENCY] = -amount }, metadata = { reason = "gift_baht_to", to = to } },
      { user_id = to, changeset = { [data.CURRENCY] = amount }, metadata = { reason = "gift_baht_from", from = user_id } },
    }, true)
    g.baht = g.baht + amount
    what = amount .. " บาท"
  else
    local id = tostring(req.item or "")
    local t = thing(id)
    if not t then err("cannot give", GRPC_INVALID_ARGUMENT) end
    local n = math.floor(tonumber(req.count) or 0)
    if n < 1 or n > GIFT.max_count then err("bad count", GRPC_INVALID_ARGUMENT) end
    local mine, theirs = bag(user_id), bag(to)
    if t.get(mine) < n then err("not enough items") end
    local refused = t.add(theirs, n, to)
    if refused then err(refused) end
    t.add(mine, -n, user_id)
    save(to, theirs, t.store)
    save(user_id, mine, t.store)
    what = t.name .. " " .. n .. " " .. t.unit
  end

  g.sends = g.sends + 1
  write(user_id, "gifts", g)
  notify(to, name .. " ส่งของขวัญให้คุณ: " .. what, NOTE.gift, { from = user_id, name = name, what = what }, user_id)
  return gift_view(user_id, to, { done = "ส่ง" .. what .. " ให้" .. their_name .. "แล้ว" })
end, "gift_send")
