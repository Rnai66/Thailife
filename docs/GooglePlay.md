# ThaiLife9 บน Google Play: สิ่งที่ต้องเตรียม

แพ็กเกจ `com.rnai66.thailife` (ชื่อเดียวกับ iOS) · เวอร์ชัน 1.3.1 · แนวตั้ง · Android 10 ขึ้นไป (API 29) · target API 36

## 1. ติดตั้ง Android Build Support ใน Unity (ครั้งเดียว)
Unity Hub → Installs → 6000.6.6f1 → ⚙ → Add modules → ติ๊ก **Android Build Support** พร้อม **OpenJDK** และ **Android SDK & NDK Tools** → Install → เปิด Unity ใหม่

## 2. สร้างกุญแจเซ็นแอป (upload key) ครั้งเดียว เก็บให้ดี
พิมพ์ใน Terminal (จะถามรหัสผ่านและชื่อ ตั้งเอง จดไว้ในที่ปลอดภัย ห้ามหาย):

```bash
mkdir -p ~/keys && "/Applications/Android Studio.app/Contents/jbr/Contents/Home/bin/keytool" -genkeypair -v -keystore ~/keys/thailife-upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

แล้วสร้างไฟล์ `game-client/Unity/UserSettings/AndroidSigning.json` (โฟลเดอร์นี้ไม่ขึ้น git) :

```json
{ "keystore": "/Users/<ชื่อเครื่อง>/keys/thailife-upload.keystore", "alias": "upload", "storePass": "<รหัส>", "keyPass": "<รหัส>" }
```

สำรองไฟล์ `.keystore` และรหัสไว้นอกเครื่องด้วย ถ้าหายต้องขอ Google รีเซ็ต upload key ซึ่งใช้เวลาหลายวัน
(เปิด **Play App Signing** ตอนอัปโหลดครั้งแรก Google จะถือกุญแจจริงของแอปไว้ให้)

## 3. Build
- ลองบนมือถือ Android ของตัวเอง: **Thai Life → Build Android APK (ลองบนมือถือ)** → ได้ `Builds/Android/*.apk` ลากลงเครื่องแล้วติดตั้ง
- ขึ้น Play: **Thai Life → Build Android Release (Google Play .aab)** → ได้ `Builds/Android/ThaiLife9-1.3.1-<versionCode>.aab`
- versionCode = เวลา build `yyMMddHH` เพิ่มขึ้นเองทุกครั้ง

## 4. Play Console
1. สมัคร Google Play Developer (ค่าธรรมเนียมครั้งเดียว 25 USD) ยืนยันตัวตนให้เสร็จ
2. Create app → ชื่อ ThaiLife9 · ภาษาเริ่มต้น ไทย · Game · Free
3. **บัญชีส่วนตัวที่สมัครใหม่**: ต้องทำ **Closed testing ให้มีผู้ทดสอบอย่างน้อย 12 คน ต่อเนื่อง 14 วัน** ก่อนจึงขอเปิด Production ได้ (เริ่มชวนเพื่อนตั้งแต่ตอนนี้)
4. อัปโหลด .aab ใน Testing → Internal testing ก่อน (ทดสอบเองได้ทันที) แล้วค่อย Closed testing

### App content (ตอบตามนี้)
| หัวข้อ | คำตอบ |
|---|---|
| Privacy policy | https://thailife.tail45d319.ts.net/privacy/ |
| Ads | ไม่มีโฆษณา |
| App access | เข้าเล่นได้เลย ไม่ต้องล็อกอิน (บัญชีสร้างจากรหัสเครื่องอัตโนมัติ) |
| Account deletion | มีในเกม: เมนู → ตั้งค่า → บัญชี → ลบบัญชี · ลิงก์เว็บ: หน้า privacy (หัวข้อการลบข้อมูล / อีเมลติดต่อ) |
| Data safety: เก็บข้อมูล | Device or other IDs (ล็อกอินบัญชี) · App activity / in-game progress · ชื่อตัวละคร (User IDs → ชื่อที่ผู้ใช้ตั้ง) — ใช้เพื่อ App functionality, ไม่แชร์ให้บุคคลที่สาม, ส่งแบบเข้ารหัส (https), ผู้ใช้ขอลบได้ |
| Target audience | 13 ปีขึ้นไป (มีผีในตำนาน การชกมวย และเกมงานวัด) |
| Content rating (IARC) | ความรุนแรงแบบการ์ตูน (มวย, ไล่ผี) · มีเรื่องผีน่ากลัวเล็กน้อย · ไม่มีการพนันด้วยเงินจริง ไม่มีซื้อของในแอป · ผู้เล่นโต้ตอบกันได้ มีแชทพิมพ์ (ห้องรวมที่ server กรองคำ + แชทเพื่อน) บล็อก/รายงาน/ระงับแชทได้ · เกมสุ่มที่ต้องจ่ายเงินในงานวัดปิดอยู่ (CHANCE_GAMES=false) จึงไม่มีการพนันจำลอง · ส่งแล้ว 14 พ.ย. 2026 ได้ 12+ (IARC Generic) |
| In-app purchases | ไม่มี |
| Government / Financial / Health | ไม่ใช่ |

### Store listing
ไฟล์พร้อมอัปโหลดอยู่ใน `game-client/Unity/Builds/GooglePlay/`:
- `icon-512.png` (ไอคอน 512×512)
- `feature-graphic-1024x500.png`
- `screenshots/` 6 รูป แนวนอน 2640×1320 (Play กำหนดด้านยาวไม่เกิน 2 เท่าของด้านสั้น) — ถ้ามีรูปจากมือถือ Android แนวตั้งจริง ใส่เพิ่มได้

**ชื่อ:** ThaiLife9 – ชีวิตดีๆ ในแบบไทย

**คำอธิบายสั้น (ไม่เกิน 80 ตัว):**
ใช้ชีวิตในหมู่บ้านไทย ทำนา ตกปลา ต่อยมวย ไปงานวัด เล่นกับเพื่อนออนไลน์

**คำอธิบายเต็ม:**
ThaiLife9 เกมจำลองชีวิตแบบไทยๆ ออนไลน์ เริ่มจากบ้านในหมู่บ้าน แล้วสร้างชีวิตของคุณเอง

• ทำมาหากิน: ทำนา เลี้ยงไก่ ตกปลาคลองและปลาแม่น้ำกว่า 12 ชนิด ตัดไม้ผ่าฟืน เป็นช่างซ่อมรถ นักดนตรี นักมวย
• ชีวิตในวัด: ใส่บาตรพระตอนเช้า นั่งสมาธิ บวชเรียน ร่วมทอดกฐินทอดผ้าป่า บูชาพระเครื่อง เป็นหมอผีปราบผีในตำนาน
• งานประเพณีตลอดปี: งานวัด สงกรานต์ ลอยกระทง ชิงช้าสวรรค์ หนังกลางแปลง ลิเก มวยงานวัด
• บ้านและธุรกิจ: แต่งบ้าน เปิดร้านกาแฟของตัวเอง ฝากเงิน กู้เงิน ซื้อที่ดินและรถ
• เดินทางทั่วไทย: หมู่บ้าน ตำบล ตัวเมือง ชายหาด เกาะ และจังหวัดต่างๆ พร้อมสถานที่เด่นของแต่ละที่
• เล่นกับเพื่อน: เห็นผู้เล่นคนอื่นในฉาก ทักทาย ไหว้ ทำกิจกรรมด้วยกัน

ไม่มีโฆษณา ไม่มีการซื้อในแอป เงินในเกมเป็นเงินสมมติทั้งหมด

**English short:** Live a Thai life: farm, fish, box, visit temple fairs and play online with friends.

## 5. ก่อนส่งตรวจ: ทดสอบบนเครื่อง Android จริง
- เข้าเกม สร้างตัวละคร ต่อเซิร์ฟเวอร์ได้ (https)
- ปุ่มและหน้าต่างพอดีจอ (มือถือ Android มีหลายขนาด / รอยบากกล้อง)
- แชร์การ์ดเลเวลอัป → เปิดหน้าแชร์ของ Android ได้ กลับเข้าเกมแล้วได้รางวัล
- ลบบัญชีได้
- ขนาดไฟล์และเวลาโหลดครั้งแรก
