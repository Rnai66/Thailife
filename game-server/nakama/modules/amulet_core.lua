-- พุทธคุณ: what each amulet does for whoever wears it (ห้อยคอ, at most WORN_MAX at a time).
-- Shared by temple.lua (wear / view), economy.lua, music.lua and quests.lua (money bonuses);
-- the client applies the fight powers (dodge, tougher skin) from the totals temple_get returns.
--
--   klaeo    แคล้วคลาด  chance (%) to slip a punch or kick completely
--   nangniao หนังเหนียว  less damage (%) from punches and kicks
--   pongkan  ป้องกัน     keeps ghosts away from the home at night (adds to "guard")
--   bangta   บังตา       thieves don't see the home (adds to "luck")
--   niyom    มหานิยม     more baht (%) for produce sold at the market and for daily quests
--   sanae    เสน่ห์       more tips (%) when playing music; shown to friends as charm

local nk = require("nakama")

local M = {}

M.WORN_MAX = 3

M.ORDER = { "klaeo", "nangniao", "pongkan", "bangta", "niyom", "sanae" }

M.POWERS = {
  klaeo    = { name = "แคล้วคลาด", cap = 30 },
  nangniao = { name = "หนังเหนียว", cap = 40 },
  pongkan  = { name = "ป้องกัน", cap = 40 },
  bangta   = { name = "บังตา", cap = 40 },
  niyom    = { name = "มหานิยม", cap = 25 },
  sanae    = { name = "เสน่ห์", cap = 30 },
}

-- Amulet id -> power -> points
M.ITEMS = {
  phra_pidta = { bangta = 12, nangniao = 6 },              -- พระปิดตา: มหาอุด ปิดบังภัย
  takrut     = { nangniao = 12, klaeo = 5 },               -- ตะกรุดโทน: คงกระพันชาตรี
  nangphaya  = { sanae = 10, niyom = 5 },                  -- พระนางพญา: เมตตามหานิยม
  somdej     = { klaeo = 6, niyom = 6, pongkan = 5 },      -- พระสมเด็จ: ครอบจักรวาล
  khunphaen  = { sanae = 18, niyom = 7 },                  -- พระขุนแผน: เสน่ห์ เมตตา
  thuat      = { klaeo = 14, pongkan = 8 },                -- หลวงพ่อทวด: แคล้วคลาด เหยียบน้ำทะเลจืด
  phra_rod   = { klaeo = 8, nangniao = 10, pongkan = 6 },  -- พระรอด: แคล้วคลาด คงกระพัน
  rian_raek  = { niyom = 12, klaeo = 8, pongkan = 8 },     -- เหรียญรุ่นแรก: มหานิยม แคล้วคลาด
  phra_kring = { pongkan = 12, klaeo = 10, niyom = 10, bangta = 6 }, -- พระกริ่ง: ป้องกันภัย อายุยืน
}

-- [{id, name, value}] for one amulet (for the client)
function M.list(item_id)
  local p = M.ITEMS[item_id]
  if not p then return nil end
  local out = {}
  for _, id in ipairs(M.ORDER) do
    if p[id] then table.insert(out, { id = id, name = M.POWERS[id].name, value = p[id] }) end
  end
  return out
end

-- Totals for a list of worn amulet ids, each power capped
function M.totals(worn)
  local t = {}
  for _, id in ipairs(M.ORDER) do t[id] = 0 end
  for _, item in ipairs(worn or {}) do
    for id, v in pairs(M.ITEMS[item] or {}) do t[id] = t[id] + v end
  end
  for _, id in ipairs(M.ORDER) do t[id] = math.min(M.POWERS[id].cap, t[id]) end
  return t
end

-- [{id, name, value}] of the totals that are above zero
function M.totals_list(totals)
  local out = {}
  for _, id in ipairs(M.ORDER) do
    if (totals[id] or 0) > 0 then table.insert(out, { id = id, name = M.POWERS[id].name, value = totals[id] }) end
  end
  return out
end

-- The worn amulets of a player (from the temple state)
function M.worn(user_id)
  local objects = nk.storage_read({ { collection = "temple", key = "state", user_id = user_id } })
  local s = (#objects > 0) and objects[1].value or {}
  local worn = {}
  for _, id in ipairs(s.worn or {}) do
    if (s.items or {})[id] then table.insert(worn, id) end
  end
  return worn
end

function M.for_user(user_id)
  return M.totals(M.worn(user_id))
end

-- amount grown by a percentage power of this player (rounded)
function M.boost(user_id, power, amount)
  if amount <= 0 then return amount, 0 end
  local pct = M.for_user(user_id)[power] or 0
  local extra = math.floor(amount * pct / 100 + 0.5)
  return amount + extra, extra
end

return M
