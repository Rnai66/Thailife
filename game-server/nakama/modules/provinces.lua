-- The 77 provinces (จังหวัด) with the latitude and longitude of each one's town (เมือง), for the
-- player's home address (profile_set_address) and distances (travel.lua, fuel.lua). Same order and
-- names as Provinces.cs on the client.

local M = {}

M.LIST = {
  { name = "กรุงเทพมหานคร", lat = 13.75, lon = 100.50 },
  { name = "กระบี่", lat = 8.09, lon = 98.91 },
  { name = "กาญจนบุรี", lat = 14.02, lon = 99.53 },
  { name = "กาฬสินธุ์", lat = 16.43, lon = 103.51 },
  { name = "กำแพงเพชร", lat = 16.48, lon = 99.52 },
  { name = "ขอนแก่น", lat = 16.43, lon = 102.83 },
  { name = "จันทบุรี", lat = 12.61, lon = 102.10 },
  { name = "ฉะเชิงเทรา", lat = 13.69, lon = 101.07 },
  { name = "ชลบุรี", lat = 13.36, lon = 100.98 },
  { name = "ชัยนาท", lat = 15.19, lon = 100.13 },
  { name = "ชัยภูมิ", lat = 15.81, lon = 102.03 },
  { name = "ชุมพร", lat = 10.49, lon = 99.18 },
  { name = "เชียงราย", lat = 19.91, lon = 99.83 },
  { name = "เชียงใหม่", lat = 18.79, lon = 98.98 },
  { name = "ตรัง", lat = 7.56, lon = 99.61 },
  { name = "ตราด", lat = 12.24, lon = 102.52 },
  { name = "ตาก", lat = 16.88, lon = 99.13 },
  { name = "นครนายก", lat = 14.21, lon = 101.21 },
  { name = "นครปฐม", lat = 13.82, lon = 100.06 },
  { name = "นครพนม", lat = 17.41, lon = 104.78 },
  { name = "นครราชสีมา", lat = 14.97, lon = 102.10 },
  { name = "นครศรีธรรมราช", lat = 8.43, lon = 99.96 },
  { name = "นครสวรรค์", lat = 15.70, lon = 100.14 },
  { name = "นนทบุรี", lat = 13.86, lon = 100.51 },
  { name = "นราธิวาส", lat = 6.43, lon = 101.82 },
  { name = "น่าน", lat = 18.78, lon = 100.78 },
  { name = "บึงกาฬ", lat = 18.36, lon = 103.65 },
  { name = "บุรีรัมย์", lat = 14.99, lon = 103.10 },
  { name = "ปทุมธานี", lat = 14.02, lon = 100.53 },
  { name = "ประจวบคีรีขันธ์", lat = 11.81, lon = 99.80 },
  { name = "ปราจีนบุรี", lat = 14.05, lon = 101.37 },
  { name = "ปัตตานี", lat = 6.87, lon = 101.25 },
  { name = "พระนครศรีอยุธยา", lat = 14.35, lon = 100.57 },
  { name = "พะเยา", lat = 19.17, lon = 99.90 },
  { name = "พังงา", lat = 8.45, lon = 98.53 },
  { name = "พัทลุง", lat = 7.62, lon = 100.08 },
  { name = "พิจิตร", lat = 16.44, lon = 100.35 },
  { name = "พิษณุโลก", lat = 16.82, lon = 100.26 },
  { name = "เพชรบุรี", lat = 13.11, lon = 99.94 },
  { name = "เพชรบูรณ์", lat = 16.42, lon = 101.16 },
  { name = "แพร่", lat = 18.14, lon = 100.14 },
  { name = "ภูเก็ต", lat = 7.88, lon = 98.39 },
  { name = "มหาสารคาม", lat = 16.18, lon = 103.30 },
  { name = "มุกดาหาร", lat = 16.54, lon = 104.72 },
  { name = "แม่ฮ่องสอน", lat = 19.30, lon = 97.97 },
  { name = "ยโสธร", lat = 15.79, lon = 104.15 },
  { name = "ยะลา", lat = 6.54, lon = 101.28 },
  { name = "ร้อยเอ็ด", lat = 16.05, lon = 103.65 },
  { name = "ระนอง", lat = 9.96, lon = 98.64 },
  { name = "ระยอง", lat = 12.68, lon = 101.28 },
  { name = "ราชบุรี", lat = 13.54, lon = 99.82 },
  { name = "ลพบุรี", lat = 14.80, lon = 100.65 },
  { name = "ลำปาง", lat = 18.29, lon = 99.49 },
  { name = "ลำพูน", lat = 18.58, lon = 99.01 },
  { name = "เลย", lat = 17.49, lon = 101.72 },
  { name = "ศรีสะเกษ", lat = 15.12, lon = 104.32 },
  { name = "สกลนคร", lat = 17.16, lon = 104.15 },
  { name = "สงขลา", lat = 7.01, lon = 100.47 },
  { name = "สตูล", lat = 6.62, lon = 100.07 },
  { name = "สมุทรปราการ", lat = 13.60, lon = 100.60 },
  { name = "สมุทรสงคราม", lat = 13.41, lon = 100.00 },
  { name = "สมุทรสาคร", lat = 13.55, lon = 100.27 },
  { name = "สระแก้ว", lat = 13.82, lon = 102.07 },
  { name = "สระบุรี", lat = 14.53, lon = 100.91 },
  { name = "สิงห์บุรี", lat = 14.89, lon = 100.40 },
  { name = "สุโขทัย", lat = 17.01, lon = 99.82 },
  { name = "สุพรรณบุรี", lat = 14.47, lon = 100.12 },
  { name = "สุราษฎร์ธานี", lat = 9.14, lon = 99.33 },
  { name = "สุรินทร์", lat = 14.88, lon = 103.49 },
  { name = "หนองคาย", lat = 17.88, lon = 102.74 },
  { name = "หนองบัวลำภู", lat = 17.20, lon = 102.44 },
  { name = "อ่างทอง", lat = 14.59, lon = 100.45 },
  { name = "อำนาจเจริญ", lat = 15.87, lon = 104.63 },
  { name = "อุดรธานี", lat = 17.41, lon = 102.79 },
  { name = "อุตรดิตถ์", lat = 17.63, lon = 100.10 },
  { name = "อุทัยธานี", lat = 15.38, lon = 100.02 },
  { name = "อุบลราชธานี", lat = 15.24, lon = 104.85 },
}

M.BY_NAME = {}
for i, p in ipairs(M.LIST) do M.BY_NAME[p.name] = p; p.index = i end

M.DEFAULT = "สุพรรณบุรี" -- the home province before the player sets an address

-- km as the crow flies
function M.km(a, b)
  local dlat = a.lat - b.lat
  local dlon = (a.lon - b.lon) * math.cos(math.rad((a.lat + b.lat) / 2))
  return 111.2 * math.sqrt(dlat * dlat + dlon * dlon)
end

-- The player's address (storage profile/address), or nil
function M.address(nk, user_id)
  local objects = nk.storage_read({ { collection = "profile", key = "address", user_id = user_id } })
  return (#objects > 0) and objects[1].value or nil
end

-- Where the player's home province town is: { name, lat, lon }
function M.home(nk, user_id)
  local a = M.address(nk, user_id)
  return M.BY_NAME[a and a.province or ""] or M.BY_NAME[M.DEFAULT]
end

return M
