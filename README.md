# Thai Life Simulator

เกมจำลองชีวิต 3D แบบไทย สำหรับ iOS / Android (Solo dev) — ดูแผนเต็มที่ `docs/ThaiLifeSimulator_GDD_v0.1.md`

## สถานะ: Prototype 0.1 — กำลังเริ่ม

- [x] โครงสร้างโฟลเดอร์ตาม GDD
- [x] `PlayerController.cs` — เดิน/หมุนตัว, รองรับทั้ง Input Manager เดิมและ Input System ใหม่, มีช่อง `ExternalMoveInput` ไว้ต่อจอยสัมผัส
- [x] `CameraFollow.cs` — กล้องติดตาม
- [x] `ThaiVillageSceneBuilder.cs` — สร้างฉาก ThaiVillage อัตโนมัติ (พื้น, บ้าน+หลังคา, ต้นไม้, Player, กล้อง, แสง) และตั้ง URP ให้ถ้ายังไม่มี
- [x] เปิดใน Unity ได้ ไม่มี compile error, ฉาก ThaiVillage สร้างแล้ว
- [x] กด Play ผ่านเกณฑ์ใน Editor: เดิน, หมุนตัว, กล้องตาม, ไม่ตกพื้น, ชนสิ่งกีดขวาง, Console ไม่มี error (16 ต.ค. 2026)
- [x] จอยเสมือน (ครึ่งซ้าย) + ลากหมุนกล้อง (ครึ่งขวา) — ทดสอบด้วยเมาส์ใน Editor (16 ต.ค. 2026)
- [x] ปุ่ม A/B/C/D แบบ PlayStation: กระโดด / เตะ / ต่อย / ป้องกัน (ภาพชั่วคราว) + ซิงก์ท่าผ่านเน็ต — ฐานสำหรับอาชีพนักมวย
- [x] ทดสอบบน iPhone 16 Pro Max จริง: จอย, ปุ่ม, เล่นข้ามเครื่องกับ Mac ผ่าน Wi-Fi (16 ต.ค. 2026)
- [x] ระบบชกมวย v1: ต่อย 10 / เตะ 18 / ป้องกันลด 80%, กระเด็น, HP 100, KO 3 วิ แล้วฟื้น, ซิงก์ผ่านเน็ต, กระสอบทราย (16 ต.ค. 2026)
- [x] อาชีพนักมวย v1: XP/เลเวลบนเซิร์ฟเวอร์ (career.lua, กันโกง), ซ้อม 3 ระดับ ต้นกล้วย Lv1 → กระสอบ Lv3 → ค่ายมวย Lv6, ฉายา, ดาเมจ/HP ตามเลเวล (16 ต.ค. 2026)
- [x] หมู่บ้านไทยจาก primitive: บ้านไม้ใต้ถุนสูง, ศาลพระภูมิ, ร้านโชห่วย, ถนน, ต้นกล้วย, ค่ายมวย
- [x] แผนที่ 3 ระดับ เดินทางด้วยป้ายรถสองแถว (แต่ละที่ = ฉาก + ห้องออนไลน์ของตัวเอง):
  - หมู่บ้าน: บ้าน, ศาลพระภูมิ, ร้านค้าชุมชน, วัด, ศาลากลางหมู่บ้าน + หอกระจายข่าว, โคก หนอง นา
  - ตำบล: อบต., รพ.สต., โรงเรียน, สวนสาธารณะ, คลองตกปลา (fishing.lua — เซิร์ฟเวอร์สุ่มเวลาและชนิดปลา)
  - เมือง: หอนาฬิกา, ตึกแถว, ค่ายมวย (ซ้อมระดับ 3)
- [x] เศรษฐกิจ v1: ขายปลาที่ร้านค้าชุมชนได้เงินบาท (economy.lua, Nakama wallet — client แก้ยอดเงินไม่ได้) (17 ต.ค. 2026)
- [x] เศรษฐกิจ v2: ซื้อเบ็ด (ไฟเบอร์/คาร์บอน) และสมาชิกค่ายมวย — เซิร์ฟเวอร์ตรวจราคา/ยอดเงิน (shop_data.lua)
- [x] ชีวิตประจำวัน: เวลาในเกม (1 วัน = 24 นาทีจริง) + กลางวัน/กลางคืน, พลังงาน, นอนบนเสื่อที่ชานบ้าน (life.lua)
- [x] อาชีพชาวนา: ซื้อพันธุ์ข้าว (กข6 / หอมมะลิ 105) → ไถ → ดำนา → รดน้ำ → เกี่ยว → เก็บเข้ายุ้งข้าว (ขยายได้ถึง 500 กก.) → ขายที่ร้านค้าชุมชน (farming.lua)
- [x] เลี้ยงไก่: ซื้อแม่ไก่/พ่อไก่/อาหาร, ออกไข่ตามเวลาจริง, ไข่มีเชื้อฟักเป็นลูก (ความสวยสืบจากพ่อแม่), รายได้หลัก = ขายไข่และพันธุ์ไก่ (chickens.lua)
- [x] งานวัดคอโค เดือนละครั้ง (วันที่ 1-2 ของเดือนในเกม = ทุก 12 ชม. จริง): ประกวดไก่สวย รางวัล 1,000/500/250 ฿ · ทดสอบ: `FAIR_ALWAYS_OPEN=true` ใน game-server/.env
- [x] ร้านค้าแบบตะกร้า จ่ายครั้งเดียว (shop_checkout ตรวจทั้งตะกร้า ผิดข้อเดียวไม่ซื้อเลย)
- [x] ของกินเติมพลังงาน: ร้านค้าชุมชน + ร้านข้าวมันไก่/ร้านกาแฟในเมือง, กระเป๋าอาหาร กดปุ่ม "กิน" บน HUD (life_eat)
- [x] นักดนตรี: 8 เครื่องดนตรี (พิณ แคน ซอ ระนาด กีต้าร์ กลอง เบส คีย์บอร์ด), เกมกดตามจังหวะ 3 ช่อง, ทิป + XP (music.lua) · เวที: ศาลากลางหมู่บ้าน, งานวัด, อีเวนต์ตำบล (Lv5), อีเวนต์เมือง (Lv8), ย่านผับ เพื่อชีวิต/บลูส์/อินดี้ (Lv10, 19:00-03:00, ร้านละครั้งต่อวัน) · ทดสอบผับ: `PUBS_ALWAYS_OPEN=true`
- [x] เปลี่ยนชื่อสถานที่ หนองบัว → คอโค
- [x] Multiplayer ขั้น 1: Unity เชื่อมต่อ Nakama local (Docker) ได้ — device auth + realtime socket (16 ต.ค. 2026)
- [x] Multiplayer ขั้น 2: เห็นผู้เล่นอื่นและซิงก์ตำแหน่ง (relayed match "thaivillage", ทดสอบ Editor + Mac build) (16 ต.ค. 2026)


## เริ่มต้นใช้งาน

1. ติดตั้ง **Unity Hub** จาก https://unity.com/download แล้วติดตั้ง **Unity 6 LTS** (เพิ่มโมดูล *iOS Build Support* ด้วยถ้าจะทดสอบบน iPhone)
2. Unity Hub → **Projects → Add → Add project from disk** → เลือกโฟลเดอร์ `game-client/Unity`
3. เลือกเวอร์ชัน Unity 6 LTS ที่ติดตั้งไว้แล้วเปิด (ครั้งแรกจะใช้เวลาสักพักในการ import)
   - ถ้าถามเรื่องเปิดใช้ Input System backend ใหม่และรีสตาร์ต → ตอบ **Yes** ได้ (สคริปต์รองรับทั้งสองแบบ)
4. ฉาก `Assets/Scenes/ThaiVillage.unity` จะถูกสร้างอัตโนมัติ — ถ้าไม่ขึ้น ใช้เมนู **Thai Life → Build ThaiVillage Scene**
5. กด **Play** แล้วเดินด้วย **WASD / ลูกศร**

### ถ้าเจอปัญหา
- **วัตถุเป็นสีชมพู** → เมนู **Thai Life → Setup URP** แล้วลบ `Assets/Art/Materials` และ Build ฉากใหม่
- **Package error ตอนเปิด** → Window → Package Manager แล้วอัปเดต *Universal RP* / *Input System* เป็นเวอร์ชันที่ Unity แนะนำ
- คัดลอก error สีแดงจาก Console มาถามต่อได้

## โครงสร้าง

```
docs/                  GDD, ASSET_LICENSES
game-client/Unity/     Unity project (Assets, Packages)
  Assets/Scripts/      Player, Camera, Interaction, Multiplayer, UI, SaveSystem
  Assets/Editor/       เครื่องมือใน Editor (ตัวสร้างฉาก)
game-server/           Nakama + PostgreSQL ผ่าน Docker (docker-compose.yml)
tools/, tests/         (ภายหลัง)
```

## การควบคุม

| | จอบนมือถือ | คีย์บอร์ด | Gamepad |
|---|---|---|---|
| เดิน | จอยครึ่งซ้าย | WASD / ลูกศร | Left stick |
| หมุนกล้อง | ลากครึ่งขวา | — | — |
| กระโดด | A (ล่าง) | Space | South (✕) |
| เตะ | B (ขวา) | K | East (○) |
| ต่อย | C (ซ้าย) | J | West (□) |
| ป้องกัน (ค้าง) | D (บน) | L | North (△) |

สร้าง/อัปเดตทุกฉาก: **Thai Life → Build Whole World** · ถ้าฉากขาดอะไรไป: เมนู **Thai Life → Repair Scene** (เติมเฉพาะส่วนที่หาย ไม่ลบอะไร) · ตั้งค่า UI: **Thai Life → Add Mobile Controls** · กระสอบทรายซ้อม: **Thai Life → Add Punching Bag**

## Multiplayer (Nakama)

- Server ระหว่างพัฒนา: **Nakama บนเครื่องตัวเองผ่าน Docker** (ฟรี) — `game-server/docker-compose.yml`
  - เริ่ม: `cd game-server && docker compose up -d` · หยุด: `docker compose down`
  - Admin console: http://127.0.0.1:7351
  - Unity config: scheme `http`, host `Chanas-MacBook-Pro.local` (ชื่อเครื่อง — ไม่เปลี่ยนตาม IP), port `7350`, server key ตาม `.env` (ค่าเริ่มต้น `defaultkey`)
- ทดสอบ 2 คนบน Mac: เมนู **Thai Life → Build & Run Mac Test Client** แล้วกด Play ใน Editor (Editor = profile `editor`, build = `-profile mac2`)
- Heroic Cloud (org `rnaisys`): Development deployment ราคา $420/เดือน — ยังไม่ใช้จนกว่าจะเปิดให้คนนอกเล่น
- SDK: `com.heroiclabs.nakama-unity` v3.22.1 (ติดตั้งผ่าน Packages/manifest.json)
- ตั้งค่า: เมนู **Thai Life → Setup Nakama Connection** แล้วกรอก Host / Server Key ใน `Assets/Settings/NakamaConfig.asset` (ไฟล์นี้ไม่เข้า Git)

## Build ลง iPhone

1. Unity: เมนู **Thai Life → Build iOS (Xcode project)** → ได้ `Builds/iOS` (Bundle ID `com.rnai66.thailife`, Team `H4X9654279`)
2. คอมไพล์ + ติดตั้ง (iPhone เสียบสาย/ปลดล็อก):
   ```bash
   cd game-client/Unity/Builds/iOS
   xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Debug -destination "id=<UDID>" -derivedDataPath ../iOS_DerivedData -allowProvisioningUpdates build
   xcrun devicectl device install app --device <UDID> ../iOS_DerivedData/Build/Products/Debug-iphoneos/ThaiLife.app
   ```
   หรือเปิด `Unity-iPhone.xcodeproj` ใน Xcode แล้วกด Run
3. iPhone ต้องอยู่ Wi-Fi เดียวกับ Mac และอนุญาต Local Network; `NakamaConfig.host` = IP ของ Mac
4. ถ้าติดตั้ง iOS Build Support ระหว่างที่ Unity เปิดอยู่ ต้องรีสตาร์ต Unity ก่อน build

## สภาพแวดล้อม
- Unity: 6000.6.6f1 (URP 17.6.0, Input System 1.20.0)
- Xcode: ติดตั้งแล้ว
