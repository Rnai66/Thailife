-- Gold shop (ร้านทอง) and pawn shop (โรงรับจำนำ) in the city.
--
-- Gold: 96.5% bars traded in ครึ่งสลึง (1/8 บาททอง, 1.9 g). The price moves every game hour (90 real
-- seconds) along a slow 5-day swing, a shorter wobble and a little noise, so buying low and selling
-- high makes a profit; the shop sells at the mid price + SPREAD and buys back at mid - SPREAD.
--
-- Pawn: pledge gold, an instrument, a vehicle or land for cash at its market price (gold at today's
-- buy-back price, used goods at a fraction of new). Interest 1.25% per game month, charged by the
-- game day; redeem within TERM_DAYS or the pledge is forfeit (หลุดจำนำ).
--
--   gold_get   {}                                   -> state
--   gold_trade {"side": "buy" | "sell", "units": n}  -> state + done
--   pawn_act   {"action": "pawn", "kind": "gold" | "instrument" | "asset", "ref": id, "units": n}
--              {"action": "redeem", "ticket": id}     -> state + done

local nk = require("nakama")
local life = require("life_core")
local data = require("shop_data")

local CURRENCY = data.CURRENCY
local ACCOUNT = { collection = "gold", key = "account" }
local BANK = { collection = "bank", key = "account" }
local GRPC_INVALID_ARGUMENT = 3
local GRPC_FAILED_PRECONDITION = 9

local BASE = 41000         -- baht per บาททอง around which the price swings
local SPREAD = 150         -- shop sells at mid + SPREAD, buys back at mid - SPREAD (per บาท)
local UNITS_PER_BAHT = 8   -- trading unit: ครึ่งสลึง
local MAX_UNITS = 800      -- 100 บาททอง
local HOUR_MS = life.MINUTE_MS * 60
local HISTORY = 24         -- game hours of price history for the chart

local RATE_MONTH = 0.0125  -- pawn interest per game month
local TERM_DAYS = 120      -- 4 game months to redeem
local MAX_TICKETS = 8
local LTV = { gold = 0.9, instrument = 0.7, car = 0.7, land = 0.7 }       -- loan as a share of market price
local USED = { instrument = 0.7, car = 0.75, land = 1.0 }                  -- market price of used goods vs new

local function err(msg, code) error({ msg, code or GRPC_FAILED_PRECONDITION }) end

local function decode(payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then err("invalid payload", GRPC_INVALID_ARGUMENT) end
  return req
end

local function wallet(user_id)
  local account = nk.account_get_id(user_id)
  local w = account.wallet or {}
  if type(w) == "string" then w = nk.json_decode(w) end
  return w[CURRENCY] or 0
end

local function pay(user_id, delta, reason)
  if delta ~= 0 then nk.wallet_update(user_id, { [CURRENCY] = delta }, { reason = reason }, true) end
end

local function game_days(now) return (now - life.EPOCH_MS) / life.DAY_MS end

-- ── Price ─────────────────────────────────────────────────────────────────────
local function noise(h)
  local x = math.sin(h * 12.9898) * 43758.5453
  return x - math.floor(x)
end

-- Mid price (baht per บาททอง) in game hour h, in steps of 50 baht like a Thai gold board
local function mid_at(h)
  local swing = 0.06 * math.sin(2 * math.pi * h / 120) + 0.02 * math.sin(2 * math.pi * h / 17 + 1.3) + 0.01 * (2 * noise(h) - 1)
  return math.floor(BASE * (1 + swing) / 50 + 0.5) * 50
end

local function hour_now(now) return math.floor((now - life.EPOCH_MS) / HOUR_MS) end

local function prices(now)
  local h = hour_now(now)
  local mid = mid_at(h)
  local p = {
    mid = mid, sell_baht = mid + SPREAD, buy_baht = mid - SPREAD,       -- per บาททอง (shop sells / shop buys)
    unit_sell = math.floor((mid + SPREAD) / UNITS_PER_BAHT),            -- per ครึ่งสลึง
    unit_buy = math.floor((mid - SPREAD) / UNITS_PER_BAHT),
    change = mid - mid_at(h - 1),
    next_s = math.max(1, math.ceil(((h + 1) * HOUR_MS - (now - life.EPOCH_MS)) / 1000)),
    history = {},
  }
  for i = HISTORY - 1, 0, -1 do table.insert(p.history, mid_at(h - i)) end
  return p
end

-- ── Storage ───────────────────────────────────────────────────────────────────
local function read(user_id)
  local objects = nk.storage_read({ { collection = ACCOUNT.collection, key = ACCOUNT.key, user_id = user_id } })
  local a = (#objects > 0) and objects[1].value or {}
  a.units = a.units or 0      -- ครึ่งสลึง held
  a.cost = a.cost or 0        -- baht paid for the units held (for the average cost and profit)
  a.tickets = a.tickets or {} -- pawn tickets
  a.next_id = a.next_id or 1
  return a
end

local function write(user_id, a)
  nk.storage_write({ { collection = ACCOUNT.collection, key = ACCOUNT.key, user_id = user_id, value = a,
                       permission_read = 1, permission_write = 0 } })
end

local function read_bank(user_id)
  local objects = nk.storage_read({ { collection = BANK.collection, key = BANK.key, user_id = user_id } })
  local b = (#objects > 0) and objects[1].value or {}
  b.assets = b.assets or {}
  return b
end

local function write_bank(user_id, b)
  nk.storage_write({ { collection = BANK.collection, key = BANK.key, user_id = user_id, value = b,
                       permission_read = 1, permission_write = 0 } })
end

-- ── Pawn ──────────────────────────────────────────────────────────────────────
local function interest(t, now)
  local days = math.max(1, math.ceil(game_days(now) - t.at))
  return math.ceil(t.loan * RATE_MONTH * days / life.MONTH_DAYS)
end

-- Tickets past their term are forfeit: the pledge stays with the shop
local function forfeit(a, now)
  local lost = {}
  local keep = {}
  for _, t in ipairs(a.tickets) do
    if game_days(now) - t.at > TERM_DAYS then table.insert(lost, t.name) else table.insert(keep, t) end
  end
  a.tickets = keep
  return lost
end

local function instrument_price(id)
  for _, e in ipairs(data.CATALOG) do if e.instrument == id then return e.price end end
  return 200 -- the พิณ everyone starts with
end

-- What can be pledged now, at what market price and loan
local function pawnables(user_id, a, p)
  local list = {}
  if a.units > 0 then
    local market = p.unit_buy * a.units
    table.insert(list, { kind = "gold", ref = "gold", name = "ทองคำแท่ง " .. a.units .. " ครึ่งสลึง", units = a.units,
                         unit_loan = math.floor(p.unit_buy * LTV.gold), market = market, loan = math.floor(market * LTV.gold) })
  end
  local items = data.read_items(user_id)
  local owned = 0
  for _, _ in pairs(items.instruments) do owned = owned + 1 end
  for _, id in ipairs(data.INSTRUMENT_ORDER) do
    if items.instruments[id] and owned > 1 then -- keep at least one to play
      local market = math.floor(instrument_price(id) * USED.instrument)
      table.insert(list, { kind = "instrument", ref = id, name = data.INSTRUMENTS[id].name, market = market,
                           loan = math.floor(market * LTV.instrument) })
    end
  end
  local bank = read_bank(user_id)
  for _, id in ipairs(data.BANK_ASSET_ORDER) do
    local x = data.BANK_ASSETS[id]
    if bank.assets[id] and x then
      local market = math.floor(x.price * USED[x.kind])
      table.insert(list, { kind = "asset", ref = id, name = x.kind == "land" and ("โฉนด" .. x.name) or x.name, market = market,
                           loan = math.floor(market * LTV[x.kind]) })
    end
  end
  return list
end

local function view(user_id, a, now, extra)
  local p = prices(now)
  local tickets = {}
  for _, t in ipairs(a.tickets) do
    local due = interest(t, now)
    table.insert(tickets, { id = t.id, name = t.name, kind = t.kind, loan = t.loan, market = t.market, interest = due,
                            redeem = t.loan + due, days_left = math.max(0, math.floor(TERM_DAYS - (game_days(now) - t.at))) })
  end
  local v = {
    price = p, units = a.units, cost = a.cost, value = a.units * p.unit_buy, baht = wallet(user_id),
    unit_name = "ครึ่งสลึง", units_per_baht = UNITS_PER_BAHT, max_units = MAX_UNITS,
    tickets = tickets, pawnables = pawnables(user_id, a, p), rate_month = RATE_MONTH, term_days = TERM_DAYS, max_tickets = MAX_TICKETS,
  }
  v.profit = v.value - a.cost
  -- json_encode turns an empty table into {}, which the client can't read as a list: leave them out
  if #v.tickets == 0 then v.tickets = nil end
  if #v.pawnables == 0 then v.pawnables = nil end
  for k, x in pairs(extra or {}) do v[k] = x end
  return v
end

local function load(user_id, now)
  local a = read(user_id)
  local lost = forfeit(a, now)
  if #lost > 0 then write(user_id, a) end
  return a, lost
end

-- ── RPCs ──────────────────────────────────────────────────────────────────────
nk.register_rpc(function(context, payload)
  local now = nk.time()
  local a, lost = load(context.user_id, now)
  local extra = nil
  if #lost > 0 then extra = { done = "หลุดจำนำ: " .. table.concat(lost, ", ") } end
  return nk.json_encode(view(context.user_id, a, now, extra))
end, "gold_get")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local units = math.floor(tonumber(req.units) or 0)
  if units <= 0 or units > MAX_UNITS then err("bad units", GRPC_INVALID_ARGUMENT) end
  local now = nk.time()
  local a = load(user_id, now)
  local p = prices(now)
  local done
  if req.side == "buy" then
    if a.units + units > MAX_UNITS then err("too much gold") end
    local cost = p.unit_sell * units
    if wallet(user_id) < cost then err("not enough baht") end
    pay(user_id, -cost, "gold_buy")
    a.units = a.units + units
    a.cost = a.cost + cost
    done = string.format("ซื้อทอง %d ครึ่งสลึง -%d ฿", units, cost)
  elseif req.side == "sell" then
    if units > a.units then err("not enough gold") end
    local got = p.unit_buy * units
    local basis = math.floor(a.cost * units / a.units + 0.5)
    pay(user_id, got, "gold_sell")
    a.cost = a.cost - basis
    a.units = a.units - units
    local gain = got - basis
    done = string.format("ขายทอง %d ครึ่งสลึง +%d ฿ (%s%d ฿)", units, got, gain >= 0 and "กำไร " or "ขาดทุน ", math.abs(gain))
  else
    err("bad side", GRPC_INVALID_ARGUMENT)
  end
  write(user_id, a)
  return nk.json_encode(view(user_id, a, now, { done = done }))
end, "gold_trade")

nk.register_rpc(function(context, payload)
  local user_id = context.user_id
  local req = decode(payload)
  local now = nk.time()
  local a = load(user_id, now)
  local done

  if req.action == "pawn" then
    if #a.tickets >= MAX_TICKETS then err("too many tickets") end
    local p = prices(now)
    local found
    for _, x in ipairs(pawnables(user_id, a, p)) do
      if x.kind == req.kind and x.ref == req.ref then found = x end
    end
    if not found then err("not owned") end
    local t = { id = a.next_id, kind = found.kind, ref = found.ref, name = found.name, at = game_days(now) }
    if found.kind == "gold" then
      local units = math.floor(tonumber(req.units) or a.units)
      if units <= 0 or units > a.units then err("not enough gold") end
      local basis = math.floor(a.cost * units / a.units + 0.5)
      t.units, t.cost = units, basis
      t.name = "ทองคำแท่ง " .. units .. " ครึ่งสลึง"
      t.market = p.unit_buy * units
      t.loan = math.floor(t.market * LTV.gold)
      a.units = a.units - units
      a.cost = a.cost - basis
    elseif found.kind == "instrument" then
      local items = data.read_items(user_id)
      items.instruments[found.ref] = nil
      if items.instrument == found.ref then -- play another one you still own
        for _, id in ipairs(data.INSTRUMENT_ORDER) do if items.instruments[id] then items.instrument = id break end end
      end
      data.write_items(user_id, items)
      t.market, t.loan = found.market, found.loan
    else
      local bank = read_bank(user_id)
      bank.assets[found.ref] = nil
      write_bank(user_id, bank)
      t.market, t.loan = found.market, found.loan
    end
    a.next_id = a.next_id + 1
    table.insert(a.tickets, t)
    pay(user_id, t.loan, "pawn")
    done = string.format("จำนำ %s รับเงิน %d ฿ · ไถ่คืนภายใน %d วัน", t.name, t.loan, TERM_DAYS)

  elseif req.action == "redeem" then
    local id = math.floor(tonumber(req.ticket) or 0)
    local index
    for i, t in ipairs(a.tickets) do if t.id == id then index = i end end
    if not index then err("no ticket") end
    local t = a.tickets[index]
    local total = t.loan + interest(t, now)
    if wallet(user_id) < total then err("not enough baht") end
    pay(user_id, -total, "pawn_redeem")
    if t.kind == "gold" then
      a.units = a.units + t.units
      a.cost = a.cost + (t.cost or 0)
    elseif t.kind == "instrument" then
      local items = data.read_items(user_id)
      items.instruments[t.ref] = true
      data.write_items(user_id, items)
    else
      local bank = read_bank(user_id)
      bank.assets[t.ref] = true
      write_bank(user_id, bank)
    end
    table.remove(a.tickets, index)
    done = string.format("ไถ่ถอน %s -%d ฿", t.name, total)
  else
    err("bad action", GRPC_INVALID_ARGUMENT)
  end

  write(user_id, a)
  return nk.json_encode(view(user_id, a, now, { done = done }))
end, "pawn_act")
