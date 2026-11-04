-- Talking to other players, friends and marriage (แชท เพื่อน แต่งงาน).
--
--   chat_send      {"text": "...", "to": user_id?}       -> {ok}  to = a friend: a private message;
--                                                                 none: the whole server ("world" room)
--   chat_report    {"user_id": "...", "text": "..."}     -> {ok}  kept for the admin to look at
--   marry_get      {}                                    -> view
--   marry_propose  {"to": user_id}                       -> view  (buys the ring; friends only, both single)
--   marry_answer   {"from": user_id, "accept": bool}     -> view  (a "no" gives the ring money back)
--   marry_divorce  {}                                    -> view
--
-- Friends themselves are Nakama's own (add / accept / remove / block from the client); Nakama sends the
-- friend-request notifications. Messages are cleaned here (length, rate, rude words) and sent by the
-- server, so the client can't post into the room directly. Notifications this module sends (code):
--   100 a message from a friend · 110 a proposal · 111 accepted · 112 declined · 113 divorced

local nk = require("nakama")
local data = require("shop_data")

local GRPC_INVALID_ARGUMENT, GRPC_FAILED_PRECONDITION = 3, 9
local ROOM = "world"
local MAX_LEN = 120
local GAP_MS = 1500           -- at most one message every 1.5 s
local RING = 5000             -- the engagement ring (แหวนหมั้น)
local FRIEND, BLOCKED = 0, 3  -- Nakama friend states

local NOTE = { chat = 100, proposal = 110, accepted = 111, declined = 112, divorced = 113 }

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
