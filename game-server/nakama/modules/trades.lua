-- Profession RPCs (rules in trade_core.lua).
--   trades_get  {}                 -> every profession with its level, XP and rank title, plus the shown one
--   trade_show  {"trade": id | ""} -> show that profession's title by your name ("" = the highest one)

local nk = require("nakama")
local trade = require("trade_core")

local GRPC_INVALID_ARGUMENT = 3

local function view(s)
  local list = {}
  for _, id in ipairs(trade.ORDER) do
    local def, t = trade.TRADES[id], s.list[id]
    local next_at = 0
    for _, at in ipairs(trade.TIERS) do
      if at > t.level then next_at = at break end
    end
    table.insert(list, {
      id = id, name = def.name, how = def.how, level = t.level, xp = t.xp,
      xp_to_next = (t.level >= trade.MAX_LEVEL) and 0 or trade.xp_to_next(t.level),
      rank = trade.rank(id, t.level), next_at = next_at,
      next_rank = next_at > 0 and trade.rank(id, next_at) or "",
    })
  end
  local shown = trade.shown(s)
  return nk.json_encode({
    trades = list, shown = shown, picked = s.shown ~= nil and s.shown ~= "",
    title = trade.rank(shown, s.list[shown].level),
  })
end

nk.register_rpc(function(context, payload)
  return view(trade.read(context.user_id))
end, "trades_get")

nk.register_rpc(function(context, payload)
  local ok, req = pcall(nk.json_decode, payload or "")
  if not ok or type(req) ~= "table" then error({ "invalid payload", GRPC_INVALID_ARGUMENT }) end
  local id = req.trade or ""
  if id ~= "" and not trade.TRADES[id] then error({ "unknown trade", GRPC_INVALID_ARGUMENT }) end
  local s = trade.read(context.user_id)
  s.shown = (id ~= "") and id or nil
  trade.write(context.user_id, s)
  return view(s)
end, "trade_show")
