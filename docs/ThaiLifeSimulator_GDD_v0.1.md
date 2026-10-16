# Thai Life Simulator
## Game Design Document และแผนเตรียมโครงการ — v0.1

- **ชื่อโครงการ:** Thai Life Simulator (ชื่อชั่วคราว)
- **เอกสาร:** Game Design Document (GDD), Technical Plan และ Prototype Roadmap
- **เวอร์ชัน:** 0.1 — แผนเริ่มต้น
- **วันที่จัดทำ:** 16 ตุลาคม 2026
- **ผู้พัฒนา:** ผู้พัฒนาคนเดียว (Solo Developer)
- **แพลตฟอร์มเป้าหมาย:** iPhone / iOS และ Android
- **แนวทางงบประมาณ:** เครื่องมือฟรีเป็นหลัก
- **สถานะ:** วางแผนแล้ว; ยังไม่ถือว่าโปรเจกต์ Unity หรือโค้ดได้รับการสร้างและทดสอบจริง

> เอกสารนี้รวบรวมข้อกำหนดและแผนที่ตกลงกันไว้ในบทสนทนา เป็นเอกสารตั้งต้นสำหรับพัฒนา ไม่ใช่หลักฐานว่ามีการติดตั้งเครื่องมือ สร้างเกม หรือทดสอบบนอุปกรณ์แล้ว

---

## 1. วิสัยทัศน์ผลิตภัณฑ์

สร้างเกมจำลองชีวิต 3D ที่มีเอกลักษณ์ประเทศไทย ผู้เล่นสร้างตัวละคร ใช้ชีวิต ทำงาน หาเงิน ปรับปรุงที่อยู่อาศัย สร้างความสัมพันธ์ และพบผู้เล่นคนอื่นในโลกออนไลน์ โดยเริ่มจากต้นแบบขนาดเล็กและขยายอย่างเป็นขั้นตอน

### หลักการออกแบบ

1. **เล่นได้ก่อน ขยายทีหลัง:** ทำให้การเดิน กล้อง และการโต้ตอบพื้นฐานทำงานจริงก่อน
2. **เอกลักษณ์ไทย:** ใช้สถาปัตยกรรม ร้านค้า อาหาร ตลาด และบรรยากาศชุมชนไทยเป็นส่วนหนึ่งของประสบการณ์
3. **เหมาะกับมือถือ:** คุมจำนวนวัตถุ แสง วัสดุ และขนาดพื้นที่เพื่อรักษาประสิทธิภาพ
4. **ออนไลน์อย่างปลอดภัย:** ข้อมูลสำคัญ เช่น เงินและไอเท็ม ต้องตรวจสอบฝั่งเซิร์ฟเวอร์เมื่อมีระบบเหล่านี้
5. **พัฒนาได้โดยคนเดียว:** ลดขอบเขตงาน ใช้ asset ที่มีสิทธิ์ถูกต้อง และหลีกเลี่ยงระบบซับซ้อนใน Prototype

## 2. กลุ่มเป้าหมายและแนวเกม

- ผู้เล่นที่ชอบเกมจำลองชีวิตและแต่งตัวละคร
- ผู้เล่นที่สนใจวิถีชีวิตและสภาพแวดล้อมแบบไทย
- ผู้เล่นที่อยากพบปะและทำกิจกรรมกับเพื่อนออนไลน์
- ผู้เล่นมือถือที่ต้องการเกม 3D ที่ควบคุมง่าย

**แนวเกม:** 3D Life Simulation / Social Sandbox / Online Multiplayer  
**มุมกล้อง:** กล้องบุคคลที่สามติดตามตัวละคร (Third-person follow camera) ใน Prototype  
**รูปแบบการควบคุม:** คีย์บอร์ดระหว่างพัฒนาใน Editor; เพิ่มระบบสัมผัสบนมือถือในขั้นถัดไป

## 3. โลกของเกม

### 3.1 โซนชนบทไทย

แนวคิดสำหรับเวอร์ชันต่อไป:
- บ้านไม้หรือบ้านสวน
- ถนนชุมชนและพื้นที่สีเขียว
- ร้านขายของชำและตลาดท้องถิ่น
- นาข้าวหรือแปลงเกษตร
- กิจกรรมและอาชีพเกี่ยวกับชุมชน

### 3.2 โซนเมืองไทย

แนวคิดสำหรับเวอร์ชันต่อไป:
- คอนโดและที่พักให้เช่า
- ร้านอาหารและร้านค้า
- ตลาดกลางคืน
- สำนักงานและอาชีพในเมือง
- พื้นที่สาธารณะสำหรับพบปะผู้เล่น

สองโซนควรใช้ระบบตัวละคร บัญชี เงิน และไอเท็มร่วมกัน แต่ในทางเทคนิคสามารถแยกเป็น Scene หรือพื้นที่โหลดแยกกันเพื่อลดภาระของมือถือและเซิร์ฟเวอร์

## 4. ขอบเขต Prototype 0.1

### ต้องมี

- ฉาก 3D ขนาดเล็กหนึ่งฉาก
- พื้นที่เดินได้ บ้านจำลอง และต้นไม้จาก primitive objects
- ตัวละครต้นแบบ (เริ่มด้วย Capsule ได้)
- การเดินและหมุนตัว
- กล้องติดตามตัวละคร
- การชนพื้นและสิ่งกีดขวางด้วย Collider
- ทดสอบใน Unity Editor
- หลังระบบพื้นฐานผ่าน: เพิ่มการควบคุมด้วยหน้าจอสัมผัสและทดสอบบน iPhone

### ยังไม่ทำใน Prototype แรก

- Multiplayer
- ระบบบัญชีออนไลน์
- โลกเปิดขนาดใหญ่
- ระบบเศรษฐกิจเต็มรูปแบบ
- แต่งงาน มีลูก และระบบครอบครัวเชิงลึก
- AI สนทนาอิสระ
- ซื้อขายระหว่างผู้เล่น
- ระบบร้านค้าเงินจริงหรือการสร้างรายได้
- ระบบป้องกันโกงเต็มรูปแบบ

**เงื่อนไข:** ไม่เริ่ม Multiplayer จนกว่าการเดิน กล้อง การชน และการทดสอบฉากพื้นฐานจะผ่าน

## 5. เครื่องมือที่วางแผนใช้

### 5.1 Unity 6 LTS + URP

ใช้เป็น Game Engine สำหรับฉาก 3D ฟิสิกส์ แอนิเมชัน UI และ build สำหรับ iOS/Android เลือกเวอร์ชัน LTS ที่ Unity Hub แสดงให้ติดตั้งได้ และบันทึกเลขเวอร์ชันไว้ใน README เพื่อให้สภาพแวดล้อมคงที่

เว็บไซต์: https://unity.com/download

### 5.2 Blender

ใช้สร้างโมเดลบ้าน เฟอร์นิเจอร์ และ asset 3D เมื่อจำเป็น

เว็บไซต์: https://www.blender.org/

### 5.3 Visual Studio Code

ใช้แก้ไขสคริปต์ C# และเอกสาร โดย Unity Editor ยังคงเป็นเครื่องมือหลักในการจัดการ Scene และ Component

เว็บไซต์: https://code.visualstudio.com/

### 5.4 Git

ใช้ควบคุมเวอร์ชันและย้อนกลับการเปลี่ยนแปลง ตรวจสอบการติดตั้งด้วย:

```bash
git --version
```

### 5.5 Xcode

ต้องใช้เพื่อสร้างและทดสอบแอปบน iPhone โดยติดตั้งจาก Mac App Store และตั้งค่า signing ตามวิธีทดสอบที่เลือก

### 5.6 Home Server / Docker

ยังไม่ใช้ใน Prototype พื้นฐาน หลังระบบเกมผ่านการทดสอบจึงพิจารณาใช้ Home Server สำหรับ Multiplayer backend เช่น Nakama และฐานข้อมูล PostgreSQL โดยต้องคำนึงถึง uptime, backup, network security และการเปิดให้เข้าถึงจากอินเทอร์เน็ต

เว็บไซต์อ้างอิง:
- Unity: https://unity.com/
- Blender: https://www.blender.org/
- VS Code: https://code.visualstudio.com/
- Nakama: https://heroiclabs.com/nakama/
- Docker: https://www.docker.com/

**หมายเหตุเรื่องค่าใช้จ่าย:** เครื่องมือหลักบางรายการใช้ฟรีตามเงื่อนไขของผู้ให้บริการ แต่การเผยแพร่แอป การเซ็นแอป เซิร์ฟเวอร์ และบริการออนไลน์อาจมีค่าใช้จ่ายหรือข้อกำหนดบัญชีเพิ่มเติม ต้องตรวจสอบเงื่อนไขปัจจุบันก่อนเผยแพร่

## 6. โครงสร้างโฟลเดอร์ที่เสนอ

```text
ThaiLifeSimulator/
├── README.md
├── docs/
│   ├── GDD.md
│   ├── TECHNICAL_DESIGN.md
│   ├── ROADMAP.md
│   └── ASSET_LICENSES.md
├── game-client/
│   └── Unity/
│       └── Assets/
│           ├── Scenes/
│           │   ├── Boot.unity
│           │   ├── CharacterCreator.unity
│           │   └── ThaiVillage.unity
│           ├── Scripts/
│           │   ├── Player/
│           │   ├── Camera/
│           │   ├── Interaction/
│           │   ├── Multiplayer/
│           │   ├── UI/
│           │   └── SaveSystem/
│           ├── Prefabs/
│           ├── Art/
│           ├── Animations/
│           ├── Audio/
│           └── Settings/
├── game-server/
│   ├── docker-compose.yml
│   ├── nakama/
│   └── database/
├── tools/
│   └── asset-import/
└── tests/
    ├── gameplay/
    └── multiplayer/
```

นี่คือโครงสร้างที่เสนอ ไม่ใช่ไฟล์ทั้งหมดที่สร้างไว้แล้ว โฟลเดอร์เซิร์ฟเวอร์จะเติมเมื่อเริ่มงาน backend

## 7. ขั้นตอนสร้างฉากต้นแบบใน Unity

### 7.1 สร้างโปรเจกต์

1. ติดตั้ง Unity Hub จาก https://unity.com/download
2. ติดตั้ง Unity 6 LTS ที่มีให้เลือกใน Hub
3. เลือกเทมเพลต `Universal 3D` (URP)
4. ตั้งชื่อ `ThaiLifeSimulator`
5. แนะนำตำแหน่งโครงการ `~/Developer/ThaiLifeSimulator`
6. สร้าง Scene ชื่อ `ThaiVillage.unity` และเก็บไว้ใน `Assets/Scenes/`

โมดูล iOS Build Support ควรติดตั้งเมื่อต้องการเริ่ม build ไป iPhone ส่วน Android Build Support พร้อม SDK/NDK และ OpenJDK เป็นทางเลือกเตรียมไว้สำหรับภายหลัง

### 7.2 วัตถุในฉาก

สร้างด้วยเมนู `GameObject > 3D Object`:

| GameObject | รูปร่าง | Position | Scale |
|---|---|---|---|
| Ground | Plane | (0, 0, 0) | (5, 1, 5) |
| Player | Capsule | (0, 1, 0) | (1, 1, 1) |
| House | Cube | (4, 1, 4) | (3, 2, 3) |
| TreeTrunk | Cylinder | (-4, 1, 3) | (0.4, 2, 0.4) |
| TreeTop | Sphere | (-4, 3, 3) | (2, 2, 2) |

ตรวจสอบว่า Ground มี Collider และ Player มี `Character Controller` โดยตั้งค่าเริ่มต้นโดยประมาณ:
- Height: 2
- Radius: 0.5
- Center: (0, 1, 0)

อย่าเพิ่ม Rigidbody ให้ Player ในขั้นนี้ เว้นแต่จะออกแบบระบบควบคุมใหม่โดยเฉพาะ

### 7.3 สคริปต์เดิน: `PlayerController.cs`

วางไฟล์ใน `Assets/Scripts/Player/PlayerController.cs`

```csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Prototype input: keyboard in Unity Editor
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 forward = cameraTransform != null
            ? cameraTransform.forward
            : Vector3.forward;

        Vector3 right = cameraTransform != null
            ? cameraTransform.right
            : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * z + right * x;
        move = Vector3.ClampMagnitude(move, 1f);

        if (move.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(move),
                12f * Time.deltaTime
            );
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}
```

สคริปต์นี้ใช้ Input Manager แบบเดิมเพื่อให้ทดลองด้วยคีย์บอร์ดได้รวดเร็ว ถ้าโปรเจกต์ตั้งค่า Input System แบบใหม่อย่างเดียว อาจเกิดข้อผิดพลาดได้ ในกรณีนั้นให้เลือก `Both` ที่ `Edit > Project Settings > Player > Active Input Handling` หากตัวเลือกนี้มีให้ใช้ หรือเปลี่ยนโค้ดไปใช้ Input System package อย่างถูกต้อง

### 7.4 สคริปต์กล้อง: `CameraFollow.cs`

วางไฟล์ใน `Assets/Scripts/Camera/CameraFollow.cs`

```csharp
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -6f);
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float lookHeight = 1.2f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.LookAt(
            target.position + Vector3.up * lookHeight
        );
    }
}
```

### 7.5 ผูก Component

1. เพิ่ม `PlayerController` ให้ Player
2. เพิ่ม `Character Controller` ให้ Player
3. เพิ่ม `CameraFollow` ให้ Main Camera
4. ลาก Player ไปใส่ `Target` ของ CameraFollow
5. ลาก Main Camera ไปใส่ `Camera Transform` ของ PlayerController
6. บันทึก Scene แล้วกด Play

### 7.6 ขอบเขตของโค้ดปัจจุบัน

- ควบคุมด้วย WASD หรือปุ่มลูกศรใน Unity Editor
- กล้องติดตามตัวละครแบบง่าย
- ยังไม่มีจอยเสมือน
- ยังไม่มีแอนิเมชันเดิน
- ยังไม่มีระบบบันทึก
- ยังไม่มี Multiplayer
- ยังไม่ได้ยืนยันว่าคอมไพล์หรือทดสอบสำเร็จในเครื่องของผู้ใช้

## 8. Roadmap 12 สัปดาห์

สมมติฐาน: ใช้เวลาประมาณ 10–15 ชั่วโมงต่อสัปดาห์ ระยะเวลาจริงขึ้นกับประสบการณ์และปัญหาที่พบ

### สัปดาห์ 1–2: พื้นฐาน
- ติดตั้ง Unity และ Git
- สร้างโปรเจกต์ URP
- สร้างฉากและตัวละคร
- ทำให้เดินและกล้องติดตามได้

**ผลลัพธ์:** เล่นใน Unity Editor ได้

### สัปดาห์ 3–4: การควบคุมและ iPhone
- เพิ่มระบบสัมผัสหรือ virtual joystick
- ปรับ UI สำหรับหน้าจอมือถือ
- ติดตั้ง Xcode และตั้งค่า iOS build
- ทดสอบบน iPhone จริง

**ผลลัพธ์:** เดินในฉากบนมือถือได้

### สัปดาห์ 5–6: ฉากชุมชนไทย
- ปรับบ้าน ถนน ต้นไม้ และร้านค้า
- สร้างวัสดุและแสงอย่างประหยัด
- ตรวจสอบ frame rate และขนาด build

**ผลลัพธ์:** ฉากต้นแบบที่มีเอกลักษณ์ไทย

### สัปดาห์ 7–9: Multiplayer (เริ่มเมื่อพื้นฐานผ่านเท่านั้น)
- เลือก backend และติดตั้งในสภาพแวดล้อมทดสอบ
- เชื่อมต่อ client
- ซิงก์ตำแหน่งและการเคลื่อนไหว
- ทดสอบผู้เล่นสองเครื่องและการหลุดการเชื่อมต่อ

**ผลลัพธ์:** ผู้เล่นสองเครื่องพบกันได้ในฉากเดียวกัน

### สัปดาห์ 10–11: ปฏิสัมพันธ์และการบันทึก
- เพิ่มการทักทายและการโต้ตอบกับวัตถุ
- เพิ่มข้อมูลผู้เล่นแบบง่าย
- ตรวจสอบการบันทึกและการกลับเข้าเกม

### สัปดาห์ 12: ทดสอบภายใน
- แก้ข้อผิดพลาด
- ตรวจสอบประสิทธิภาพ
- ทดสอบบน iPhone และ Android หากมีอุปกรณ์
- ทำรายการปัญหาก่อนเพิ่มขอบเขต

## 9. แผนระบบออนไลน์ในอนาคต

เมื่อผ่าน Prototype พื้นฐานแล้ว จึงพิจารณา:

- Unity client สำหรับ iOS/Android
- Multiplayer backend เช่น Nakama
- PostgreSQL สำหรับข้อมูลถาวรที่จำเป็น
- Docker Compose สำหรับสภาพแวดล้อมทดสอบ
- ระบบบัญชีผู้เล่นและการจัดการ session
- ระบบสำรองข้อมูลและบันทึก log
- การป้องกันการแก้ไขข้อมูลจาก client

หลักความปลอดภัย: เซิร์ฟเวอร์ต้องตรวจสอบการเปลี่ยนเงิน ไอเท็ม และการซื้อขาย ไม่เชื่อค่าจาก client โดยตรง อย่าเปิดฐานข้อมูลหรือแผงควบคุมให้เข้าถึงจากอินเทอร์เน็ตโดยไม่มีการควบคุมสิทธิ์และการป้องกันที่เหมาะสม

## 10. เกณฑ์ผ่าน Prototype

- [ ] Scene `ThaiVillage` เปิดได้โดยไม่มี compile errors
- [ ] Player เดินและหมุนตัวได้
- [ ] กล้องติดตาม Player
- [ ] Player ไม่ตกทะลุพื้น
- [ ] Player ชนสิ่งกีดขวางได้
- [ ] ไม่มี error สีแดงใน Console
- [ ] เพิ่มการควบคุมแบบสัมผัสและทดสอบบน iPhone
- [ ] ตรวจสอบการทำงานบนอุปกรณ์จริงก่อนเริ่ม Multiplayer

## 11. Asset และลิขสิทธิ์

ก่อนนำโมเดล เสียง ฟอนต์ หรือภาพจากภายนอกมาใช้:
- บันทึกชื่อ asset ผู้สร้าง URL และชนิดใบอนุญาตใน `docs/ASSET_LICENSES.md`
- ตรวจสอบสิทธิ์การใช้เชิงพาณิชย์และข้อกำหนดการให้เครดิต
- หลีกเลี่ยงการนำ asset จากเกม The Sims หรือเกมอื่นมาใช้โดยไม่มีสิทธิ์
- แยก asset ที่สร้างเองออกจาก asset ที่ดาวน์โหลดมา

## 12. ความเสี่ยงและข้อควรระวัง

1. **ขอบเขตใหญ่เกินไป:** เริ่มจากฉากเดียวและตัวละครต้นแบบ
2. **Multiplayer ซับซ้อน:** อย่าเริ่มก่อน movement และ input เสถียร
3. **ประสิทธิภาพมือถือ:** ใช้โมเดลและวัสดุแบบประหยัด และทดสอบบน iPhone จริง
4. **ระบบออนไลน์มีค่าใช้จ่าย:** ประเมินตามจำนวนผู้เล่นและปริมาณข้อมูลก่อนเปิดบริการ
5. **ความเข้ากันได้ของ Input:** โค้ดต้นแบบใช้ Input Manager เดิม ต้องตรวจสอบการตั้งค่า Unity
6. **ข้อมูลสูญหาย:** เพิ่มระบบ backup เมื่อเริ่มเก็บข้อมูลผู้เล่นจริง
7. **ลิขสิทธิ์:** ตรวจสอบ asset และข้อกำหนดของบริการทั้งหมดก่อนเผยแพร่

## 13. ขั้นตอนถัดไป

1. ติดตั้ง Unity Hub และ Unity 6 LTS
2. สร้างโปรเจกต์ `ThaiLifeSimulator` ด้วย Universal 3D (URP)
3. สร้าง `ThaiVillage.unity`
4. สร้าง Ground, Player, House และ Tree จาก primitive objects
5. เพิ่มสคริปต์เดินและกล้อง
6. กด Play และแก้ปัญหาจาก Console
7. เมื่อผ่านแล้วจึงเพิ่มการควบคุมสัมผัสและทดสอบบน iPhone
8. เริ่ม Multiplayer หลังผ่านเกณฑ์พื้นฐานเท่านั้น

---

**สถานะล่าสุดของงาน:** เอกสารแผนและโค้ดตัวอย่างถูกรวบรวมไว้ในเอกสารนี้แล้ว ขั้นตอนติดตั้งและทดสอบในเครื่องจริงยังต้องดำเนินการโดยผู้พัฒนา
