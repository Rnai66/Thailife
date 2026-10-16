# Thai Life Simulator

เกมจำลองชีวิต 3D แบบไทย สำหรับ iOS / Android (Solo dev) — ดูแผนเต็มที่ `docs/ThaiLifeSimulator_GDD_v0.1.md`

## สถานะ: Prototype 0.1 — กำลังเริ่ม

- [x] โครงสร้างโฟลเดอร์ตาม GDD
- [x] `PlayerController.cs` — เดิน/หมุนตัว, รองรับทั้ง Input Manager เดิมและ Input System ใหม่, มีช่อง `ExternalMoveInput` ไว้ต่อจอยสัมผัส
- [x] `CameraFollow.cs` — กล้องติดตาม
- [x] `ThaiVillageSceneBuilder.cs` — สร้างฉาก ThaiVillage อัตโนมัติ (พื้น, บ้าน+หลังคา, ต้นไม้, Player, กล้อง, แสง) และตั้ง URP ให้ถ้ายังไม่มี
- [x] เปิดใน Unity ได้ ไม่มี compile error, ฉาก ThaiVillage สร้างแล้ว
- [x] กด Play ผ่านเกณฑ์ใน Editor: เดิน, หมุนตัว, กล้องตาม, ไม่ตกพื้น, ชนสิ่งกีดขวาง, Console ไม่มี error (16 ต.ค. 2026)
- [ ] จอยสัมผัส + ทดสอบบน iPhone
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

## Multiplayer (Nakama)

- Server ระหว่างพัฒนา: **Nakama บนเครื่องตัวเองผ่าน Docker** (ฟรี) — `game-server/docker-compose.yml`
  - เริ่ม: `cd game-server && docker compose up -d` · หยุด: `docker compose down`
  - Admin console: http://127.0.0.1:7351
  - Unity config: scheme `http`, host `127.0.0.1` (iPhone ใช้ IP ของ Mac ในวง Wi-Fi), port `7350`, server key ตาม `.env` (ค่าเริ่มต้น `defaultkey`)
- ทดสอบ 2 คนบน Mac: เมนู **Thai Life → Build & Run Mac Test Client** แล้วกด Play ใน Editor (Editor = profile `editor`, build = `-profile mac2`)
- Heroic Cloud (org `rnaisys`): Development deployment ราคา $420/เดือน — ยังไม่ใช้จนกว่าจะเปิดให้คนนอกเล่น
- SDK: `com.heroiclabs.nakama-unity` v3.22.1 (ติดตั้งผ่าน Packages/manifest.json)
- ตั้งค่า: เมนู **Thai Life → Setup Nakama Connection** แล้วกรอก Host / Server Key ใน `Assets/Settings/NakamaConfig.asset` (ไฟล์นี้ไม่เข้า Git)

## สภาพแวดล้อม
- Unity: 6000.6.6f1 (URP 17.6.0, Input System 1.20.0)
- Xcode: ติดตั้งแล้ว
