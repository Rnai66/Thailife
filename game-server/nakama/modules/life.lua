-- Daily life RPCs (energy and sleep). Rules live in life_core.lua.
--   life_get    {} -> {energy, max_energy, sleeping}
--   life_sleep  {} -> start sleeping (energy regenerates fast; actions are blocked)
--   life_wake   {} -> stop sleeping

local nk = require("nakama")
local life = require("life_core")

nk.register_rpc(function(context, payload)
  return nk.json_encode(life.view(life.state(context.user_id)))
end, "life_get")

nk.register_rpc(function(context, payload)
  return nk.json_encode(life.view(life.set_sleeping(context.user_id, true)))
end, "life_sleep")

nk.register_rpc(function(context, payload)
  return nk.json_encode(life.view(life.set_sleeping(context.user_id, false)))
end, "life_wake")
