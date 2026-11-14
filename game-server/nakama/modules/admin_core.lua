-- The game's own accounts (ผู้สร้างเกม): special rights other players don't have, such as building
-- past the edge of their house lot (build.lua). Kept in code, not in the player's data.

local M = {}

local ADMINS = {
  ["3fe8ebfd-99ec-41b5-a276-5316ebd9ad69"] = "Rnai", -- the creator's iPhone account
}

function M.is(user_id) return ADMINS[user_id or ""] ~= nil end

-- Their user ids
function M.ids()
  local r = {}
  for id in pairs(ADMINS) do table.insert(r, id) end
  return r
end

return M
