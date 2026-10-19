# ย้ายเซิร์ฟเวอร์เกมขึ้น Cloud และเปิดทดสอบผ่าน TestFlight

คู่มือนี้พาย้ายเซิร์ฟเวอร์ Nakama จากเครื่อง Mac ไปไว้บน VPS ที่เปิดตลอด 24 ชม.
หลังย้ายแล้ว ผู้เล่นจะเล่นได้จากทุกที่ ไม่ต้องอยู่ Wi-Fi บ้านเดียวกัน

ไฟล์ในโฟลเดอร์นี้:

| ไฟล์ | ใช้ทำอะไร |
|---|---|
| `docker-compose.prod.yml` | Postgres + Nakama + Caddy (HTTPS ฟรีอัตโนมัติ) ไม่มีสวิตช์ทดสอบ |
| `Caddyfile` | ส่ง `https://<โดเมน>` ไปที่ Nakama รวมถึง WebSocket |
| `gen-secrets.sh` | สร้างรหัสลับทั้งหมดแบบสุ่มลง `.env.prod` |
| `backup.sh` | สำรองฐานข้อมูลทุกวัน เก็บ 14 ชุดล่าสุด |
| `.env.prod.example` | ตัวอย่างค่าที่ต้องมี |

## 1. สิ่งที่ต้องเตรียม (คุณต้องสมัครและจ่ายเงินเอง)

1. **VPS:** เลือก Ubuntu 24.04, RAM 2 GB, region **Singapore** (ใกล้ไทยที่สุด)
   เจ้าที่ใช้ได้ เช่น DigitalOcean, Vultr, Linode, AWS Lightsail ราคาประมาณ 10–12 USD ต่อเดือน
   RAM 1 GB ก็พอสำหรับผู้เล่นไม่กี่สิบคน
2. **โดเมน:** เช่น `thailife.yourname.com` ต้องมีโดเมนเพราะ HTTPS ใช้กับ IP ตรง ๆ ไม่ได้
   - ถ้ามีโดเมนอยู่แล้ว: เพิ่ม DNS record ชนิด **A** ชี้ไปที่ IP ของ VPS
   - ถ้าไม่มี: ใช้ subdomain ฟรีจาก DuckDNS แล้วชี้ไปที่ IP ของ VPS ได้
3. **Apple Developer Program** (99 USD ต่อปี) จำเป็นสำหรับ TestFlight และ App Store บัญชีฟรีทำได้แค่ติดตั้งลงเครื่องตัวเองผ่านสาย

## 2. ติดตั้งบน VPS

```bash
# บน VPS (ล็อกอินด้วย SSH)
curl -fsSL https://get.docker.com | sh
sudo mkdir -p /opt/thailife && sudo chown $USER /opt/thailife
```

คัดลอกโฟลเดอร์ `game-server/nakama/modules` และ `game-server/deploy` ขึ้นไปไว้ที่ `/opt/thailife/game-server/`
ใช้ `scp -r` หรือ `git clone` ก็ได้ จากนั้น:

```bash
cd /opt/thailife/game-server/deploy
./gen-secrets.sh thailife.yourname.com      # ใส่โดเมนของคุณ
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d
docker compose -f docker-compose.prod.yml --env-file .env.prod logs -f nakama   # รอเห็น "Startup done"
```

ทดสอบจากเครื่องไหนก็ได้ ถ้าตอบกลับมาเป็น JSON แสดงว่าเปิดสำเร็จ:

```bash
curl https://thailife.yourname.com/healthcheck
```

เปิดไฟร์วอลล์เฉพาะพอร์ต 22 (SSH), 80 และ 443 ห้ามเปิด 5432 และ 7351 สู่อินเทอร์เน็ต

## 3. ตั้งค่าเกมให้ต่อเซิร์ฟเวอร์ใหม่

ใน Unity เปิด `Assets/Settings/NakamaConfig.asset` (ไฟล์นี้ไม่ถูกบันทึกเข้า Git) แล้วตั้ง:

| ช่อง | ค่า |
|---|---|
| scheme | `https` |
| host | `thailife.yourname.com` |
| port | `443` |
| server key | ค่า `NAKAMA_SERVER_KEY` ใน `.env.prod` |
| fallback hosts | เว้นว่าง |

แล้วกด `Thai Life → Build iOS` ตามปกติ

บัญชีผู้เล่นบนเซิร์ฟเวอร์ใหม่เริ่มจากศูนย์ ถ้าต้องการย้ายข้อมูลจากเครื่อง Mac ให้ใช้ `pg_dump` จากเครื่อง Mac แล้วนำไป restore บน VPS ก่อนเปิดให้คนเล่น

## 4. ดูแลเซิร์ฟเวอร์

- **สำรองข้อมูลทุกวันตี 4:** ตั้ง cron ตามบรรทัดในหัวไฟล์ `backup.sh` และควรดาวน์โหลดไฟล์ backup เก็บนอกเครื่อง VPS เป็นระยะ
- **อัปเดตโค้ดเซิร์ฟเวอร์ (ไฟล์ .lua):** คัดลอก `nakama/modules` ชุดใหม่ขึ้นไป แล้วรัน
  `docker compose -f docker-compose.prod.yml --env-file .env.prod restart nakama`
- **เปิดหน้าแอดมิน Nakama:** ทำผ่าน SSH tunnel เท่านั้น
  `ssh -L 7351:127.0.0.1:7351 user@vps` แล้วเปิด http://localhost:7351
  ใช้ชื่อ `admin` และรหัสจาก `.env.prod`

## 5. TestFlight (ให้คนอื่นลองเล่น)

1. สร้างแอปใน App Store Connect ด้วย Bundle ID `com.rnai66.thailife`
2. ใน Xcode เปิด `Builds/iOS/Unity-iPhone.xcodeproj` เลือก **Any iOS Device** แล้วกด **Product → Archive**
3. ในหน้าต่าง Organizer กด **Distribute App → TestFlight & App Store** แล้วกด Upload
4. รอ Apple ประมวลผลประมาณ 10–30 นาที แล้วเพิ่มอีเมลผู้ทดสอบในแท็บ TestFlight
5. ก่อนเปิดให้คนทั่วไป (External testers) ต้องมีลิงก์ **นโยบายความเป็นส่วนตัว** และผ่านการตรวจเบื้องต้นจาก Apple
