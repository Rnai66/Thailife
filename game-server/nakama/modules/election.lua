-- Local elections (เลือกตั้ง อบต.). Every 7 game days is one election: on days 1-5 players register
-- as a candidate at the อบต. office (ส.อบต. or นายก อบต., a fee and a minimum level) and campaign
-- (หาเสียง) at the village hall; on days 6-7 everyone votes at the hall. The winners of one
-- election hold office through the next one and draw a daily salary at the อบต. Villager (NPC)
-- candidates run every time and pick up votes each day, so a player has to campaign to beat them.
-- One shared record per election, written with version checks so simultaneous votes don't clash.
--
--   election_get      {}
--   election_register {"position": "member" | "mayor"}
--   election_campaign {}
--   election_vote     {"position": ..., "candidate": id}
--   election_salary   {}

local nk = require("nakama")
local data = require("shop_data")
local life = require("life_core")
local trade = require("trade_core")
local quest = require("quest_core")

local SYSTEM = "00000000-0000-0000-0000-000000000000"
local COLLECTION = "election"
local ME = { collection = "election", key = "me" }

local CYCLE_DAYS = 7
local REGISTER_DAYS = 5 -- days 1-5 register + campaign, days 6-7 vote
local CAMPAIGNS_PER_DAY = 3
local CAMPAIGN_ENERGY = 10
local PLAYER_VOTE = 10
local WIN_XP = 150

local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local ORDER = { "member", "mayor" }
local POSITIONS = {
  member = { name = "สมาชิกสภา อบต.", short = "ส.อบต.", level = 10, fee = 1000, seats = 2, salary = 400, campaign = 200,
             npcs = 4, base = { 10, 30 }, per_day = { 8, 16 } },
  mayor  = { name = "นายก อบต.", short = "นายก อบต.", level = 25, fee = 5000, seats = 1, salary = 1200, campaign = 500,
             npcs = 3, base = { 30, 60 }, per_day = { 15, 30 } },
}

local NPC_NAMES = { "ลุงสมชาย", "ป้าบุญมี", "ผู้ใหญ่สมศักดิ์", "ครูวิไล", "เสี่ยเปี๊ยก", "กำนันทองดี",
                    "พี่หนึ่งรักถิ่น", "น้าแดงใจดี", "ลุงประยูร", "ป้าสมหญิง", "หมอเอก", "พี่ต้อมคนหนุ่ม" }

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

-- cycle (1-based), day in the cycle (1..7), and the time the current phase ends
local function clock(now)
  local gd = life.game_day(now)
  local cycle = math.floor((gd - 1) / CYCLE_DAYS) + 1
  local day = (gd - 1) % CYCLE_DAYS + 1
  local phase = (day <= REGISTER_DAYS) and "register" or "vote"
  local first = (cycle - 1) * CYCLE_DAYS -- game days before this cycle
  local ends_day = first + ((phase == "register") and REGISTER_DAYS or CYCLE_DAYS)
  local ends_ms = life.EPOCH_MS + ends_day * life.DAY_MS
  return cycle, day, phase, math.max(0, math.ceil((ends_ms - now) / 1000))
end

local function seed(cycle)
  local names = {}
  for i, n in ipairs(NPC_NAMES) do names[i] = n end
  for i = #names, 2, -1 do local j = math.random(1, i); names[i], names[j] = names[j], names[i] end
  local rec = { cycle = cycle, positions = {}, voters = {} }
  local k = 1
  for _, pos in ipairs(ORDER) do
    local def = POSITIONS[pos]
    local list = {}
    for i = 1, def.npcs do
      table.insert(list, { id = "npc" .. i, name = names[k], npc = true, votes = 0,
                           base = math.random(def.base[1], def.base[2]),
                           per_day = math.random(def.per_day[1], def.per_day[2]) })
      k = k + 1
    end
    rec.positions[pos] = list
  end
  return rec
end

-- returns the record (seeded if new) and its version ("*" = not stored yet)
local function read_cycle(cycle, create)
  local objects = nk.storage_read({ { collection = COLLECTION, key = "cycle_" .. cycle, user_id = SYSTEM } })
  if #objects > 0 then return objects[1].value, objects[1].version end
  if not create then return nil, nil end
  return seed(cycle), "*"
end

-- Read-modify-write one election record, retrying when someone else wrote it first
local function update_cycle(cycle, fn)
  for _ = 1, 6 do
    local rec, version = read_cycle(cycle, true)
    local result = fn(rec)
    local ok = pcall(nk.storage_write, { { collection = COLLECTION, key = "cycle_" .. cycle, user_id = SYSTEM,
      value = rec, version = version, permission_read = 2, permission_write = 0 } })
    if ok then return rec, result end
  end
  err("election busy, try again")
end

local function votes_of(c, day)
  local v = c.votes or 0
  if c.npc then v = v + c.base + c.per_day * day end
  return v
end

-- candidates sorted by votes as of `day` (7 = final)
local function standings(rec, pos, day)
  local list = {}
  for _, c in ipairs(rec.positions[pos] or {}) do
    table.insert(list, { id = c.id, name = c.name, npc = c.npc or false, votes = votes_of(c, day) })
  end
  table.sort(list, function(a, b) if a.votes ~= b.votes then return a.votes > b.votes end return a.id < b.id end)
  return list
end

-- the position this user won last election, if any
local function office_of(user_id, cycle)
  local prev = read_cycle(cycle - 1, false)
  if not prev then return nil, nil end
  for _, pos in ipairs(ORDER) do
    for i, c in ipairs(standings(prev, pos, CYCLE_DAYS)) do
      if i <= POSITIONS[pos].seats and c.id == user_id then return pos, prev end
    end
  end
  return nil, prev
end

local function registered(rec, user_id)
  for _, pos in ipairs(ORDER) do
    for _, c in ipairs(rec.positions[pos] or {}) do
      if c.id == user_id then return pos, c end
    end
  end
  return nil, nil
end

local function read_me(user_id)
  local objects = nk.storage_read({ { collection = ME.collection, key = ME.key, user_id = user_id } })
  return (#objects > 0) and objects[1].value or {}
end

local function write_me(user_id, v)
  nk.storage_write({ { collection = ME.collection, key = ME.key, user_id = user_id, value = v,
                       permission_read = 1, permission_write = 0 } })
end

local function wallet(user_id)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[data.CURRENCY] or 0
end

local function display_name(user_id)
  local account = nk.account_get_id(user_id)
  local u = account.user or {}
  local name = u.display_name
  if not name or name == "" then name = u.username or "ผู้สมัคร" end
  return name
end

local function view(user_id, extra)
  local now = nk.time()
  local cycle, day, phase, ends_s = clock(now)
  local rec = read_cycle(cycle, false) or seed(cycle) -- unsaved preview is fine for display
  local me = read_me(user_id)
  local today = life.game_day(now)
  local my_pos = registered(rec, user_id)
  local my_votes = (rec.voters or {})[user_id] or {}
  local office, prev = office_of(user_id, cycle)

  local positions = {}
  for _, pos in ipairs(ORDER) do
    local def = POSITIONS[pos]
    local list = standings(rec, pos, day)
    for _, c in ipairs(list) do c.me = (c.id == user_id) end
    table.insert(positions, { id = pos, name = def.name, short = def.short, level = def.level, fee = def.fee,
      seats = def.seats, salary = def.salary, campaign_price = def.campaign, candidates = list,
      my_vote = my_votes[pos] or "" })
  end

  local last = {}
  if prev then
    for _, pos in ipairs(ORDER) do
      local winners = {}
      for i, c in ipairs(standings(prev, pos, CYCLE_DAYS)) do
        if i > POSITIONS[pos].seats then break end
        table.insert(winners, { id = c.id, name = c.name, votes = c.votes, me = c.id == user_id })
      end
      table.insert(last, { id = pos, name = POSITIONS[pos].name, winners = winners })
    end
  end

  local result = {
    cycle = cycle, day = day, days = CYCLE_DAYS, register_days = REGISTER_DAYS, phase = phase, phase_ends_s = ends_s,
    positions = positions, registered = my_pos or "", office = office or "",
    office_name = office and POSITIONS[office].name or "",
    salary_today = office ~= nil and me.salary_day == today,
    campaigns_left = (me.campaign_day == today) and math.max(0, CAMPAIGNS_PER_DAY - (me.campaign_count or 0)) or CAMPAIGNS_PER_DAY,
    last_results = (#last > 0) and last or nil, level = quest.player_level(user_id), politics = trade.read(user_id).list.politician.level,
    energy = CAMPAIGN_ENERGY,
  }
  for k, v in pairs(extra or {}) do result[k] = v end
  return nk.json_encode(result)
end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

nk.register_rpc(function(context, payload)
  return view(context.user_id)
end, "election_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local def = POSITIONS[req.position or ""]
  if not def then err("unknown position", GRPC_INVALID_ARGUMENT) end
  local cycle, _, phase = clock(nk.time())
  if phase ~= "register" then err("registration closed") end
  if quest.player_level(user_id) < def.level then err("level too low") end
  if wallet(user_id) < def.fee then err("not enough baht") end
  local name = display_name(user_id)

  update_cycle(cycle, function(rec)
    if registered(rec, user_id) then err("already registered") end
    table.insert(rec.positions[req.position], { id = user_id, name = name, npc = false, votes = 0 })
  end)
  nk.wallet_update(user_id, { [data.CURRENCY] = -def.fee }, { reason = "election_fee", position = req.position }, true)
  trade.gain(user_id, "politician", 30, true)
  return view(user_id, { done = "สมัครรับเลือกตั้ง " .. def.name .. " แล้ว! ไปหาเสียงที่ศาลากลางบ้าน" })
end, "election_register")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local now = nk.time()
  local cycle = clock(now)
  local today = life.game_day(now)
  local rec = read_cycle(cycle, false)
  local pos = rec and registered(rec, user_id)
  if not pos then err("not a candidate") end
  local def = POSITIONS[pos]
  local me = read_me(user_id)
  if me.campaign_day ~= today then me.campaign_day = today; me.campaign_count = 0 end
  if me.campaign_count >= CAMPAIGNS_PER_DAY then err("campaigned enough today") end
  if wallet(user_id) < def.campaign then err("not enough baht") end
  life.spend(user_id, CAMPAIGN_ENERGY)

  local skill = trade.read(user_id).list.politician.level
  local gained = 8 + math.floor(skill / 2) + math.floor(quest.player_level(user_id) / 5) + math.random(0, 6)
  update_cycle(cycle, function(r)
    local _, c = registered(r, user_id)
    if c then c.votes = (c.votes or 0) + gained end
  end)
  nk.wallet_update(user_id, { [data.CURRENCY] = -def.campaign }, { reason = "election_campaign" }, true)
  me.campaign_count = me.campaign_count + 1
  write_me(user_id, me)
  trade.gain(user_id, "politician", 15, true)
  return view(user_id, { gained = gained, done = "หาเสียงที่ศาลา ได้คะแนนนิยม +" .. gained .. " เสียง" })
end, "election_campaign")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  if not POSITIONS[req.position or ""] then err("unknown position", GRPC_INVALID_ARGUMENT) end
  local cycle, _, phase = clock(nk.time())
  if phase ~= "vote" then err("voting not open") end
  local picked
  update_cycle(cycle, function(rec)
    rec.voters = rec.voters or {}
    local mine = rec.voters[user_id] or {}
    if mine[req.position] then err("already voted") end
    for _, c in ipairs(rec.positions[req.position]) do
      if c.id == req.candidate then picked = c end
    end
    if not picked then err("unknown candidate", GRPC_INVALID_ARGUMENT) end
    picked.votes = (picked.votes or 0) + PLAYER_VOTE
    mine[req.position] = req.candidate
    rec.voters[user_id] = mine
  end)
  trade.gain(user_id, "politician", 5, true)
  return view(user_id, { done = "ลงคะแนนให้ " .. picked.name .. " แล้ว (+" .. PLAYER_VOTE .. " เสียง)" })
end, "election_vote")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local now = nk.time()
  local cycle = clock(now)
  local office = office_of(user_id, cycle)
  if not office then err("not in office") end
  local def = POSITIONS[office]
  local today = life.game_day(now)
  local me = read_me(user_id)
  if me.salary_day == today then err("salary already paid") end
  me.salary_day = today
  local extra = {}
  local xp = 20
  if me.bonus_cycle ~= cycle then me.bonus_cycle = cycle; xp = xp + WIN_XP; extra.bonus = true end
  write_me(user_id, me)
  nk.wallet_update(user_id, { [data.CURRENCY] = def.salary }, { reason = "election_salary", position = office }, true)
  trade.gain(user_id, "politician", xp, true)
  extra.earned = def.salary
  extra.done = "รับเงินเดือน " .. def.name .. " " .. def.salary .. " บาท" .. (extra.bonus and " · โบนัสชนะเลือกตั้ง!" or "")
  return view(user_id, extra)
end, "election_salary")
