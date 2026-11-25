using UnityEngine;

// What goes on inside a passer-by (Villager): a name, an age and a job, and four numbers that drift
// as the day goes by — พลังงาน (walking wears it down, standing about brings it back), ความหิว, อารมณ์
// and สุขภาพ. They show in how the person moves (tired or hurt: slower, a limp) and in what they think
// (a thought now and then over their head, from how they feel and the time of day).
// Walk up to one and "คุย": they turn to you and answer the way they feel; pester them and they get
// cross. Cars hurt: a car on the road (TrafficCar) or the one you're driving going past at speed
// knocks them flat — down for a few seconds, then up again, limping and not pleased with you.
[RequireComponent(typeof(Villager))]
public class NpcMind : MonoBehaviour
{
    private static readonly string[] Men = { "สมชาย", "สมศักดิ์", "ประยุทธ", "วิชัย", "สุรชัย", "อนุชา", "ธนากร", "บุญมี", "ชาตรี", "ณัฐพล", "เอกชัย", "สมพงษ์" };
    private static readonly string[] Women = { "สมศรี", "มาลี", "บุษบา", "ปราณี", "วันเพ็ญ", "สุภาพร", "กาญจนา", "นงลักษณ์", "พิมพ์ชนก", "อรุณี", "จันทร์เพ็ญ", "ลำดวน" };
    private static readonly string[] Jobs = { "แม่ค้า", "พ่อค้า", "ชาวนา", "พนักงานบริษัท", "นักศึกษา", "ครู", "วินมอเตอร์ไซค์", "ช่างซ่อมรถ", "พยาบาล", "เกษียณแล้ว", "นักท่องเที่ยว", "ร้านขายของชำ" };

    public string Name { get; private set; }
    public string Job { get; private set; }
    public int Age { get; private set; }
    public float Energy { get; private set; } = 80f;   // 0..100
    public float Hunger { get; private set; } = 20f;   // 0 full .. 100 starving
    public float Mood { get; private set; } = 65f;     // 0 furious .. 100 delighted
    public float Health { get; private set; } = 100f;  // 0..100
    public bool Down => downUntil > Time.time;

    // How fast they walk compared with a fresh, healthy person
    public float SpeedFactor => Down ? 0f : (Health < 60f ? 0.55f : 1f) * (Energy < 25f ? 0.7f : 1f);

    private static NpcMind talking;           // the one the "คุย" button is for (the nearest)
    private static Transform player;
    private static PlayerController playerCtl;

    private CharacterModel model;
    private Vector3 lastPos;
    private float nextThought, nextHitCheck, downUntil, lastTalk;
    private int pestered;
    private Quaternion standRot;
    private float standY;

    private void Start()
    {
        int seed = Mathf.Abs(GetInstanceHash());
        var rng = new System.Random(seed);
        bool woman = rng.Next(2) == 0;
        Name = woman ? Women[rng.Next(Women.Length)] : Men[rng.Next(Men.Length)];
        Job = Jobs[rng.Next(Jobs.Length)];
        Age = 16 + rng.Next(60);
        Energy = 40f + rng.Next(60);
        Hunger = rng.Next(70);
        Mood = 35f + rng.Next(60);
        model = GetComponent<CharacterModel>();
        lastPos = transform.position;
        nextThought = Time.time + 8f + rng.Next(30);
    }

    private int GetInstanceHash() => (transform.position.x * 73856093f + transform.position.z * 19349663f + name.GetHashCode()).GetHashCode();

    public string Feeling
    {
        get
        {
            if (Down) return "เจ็บ";
            if (Health < 60f) return "บาดเจ็บ";
            if (Mood < 25f) return "หงุดหงิด";
            if (Hunger > 75f) return "หิวมาก";
            if (Energy < 25f) return "เหนื่อย";
            if (Mood > 75f) return "อารมณ์ดี";
            return "ปกติ";
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        // the day wears on: walking tires, standing rests, everyone gets hungry, moods settle back
        float moved = (transform.position - lastPos).magnitude;
        lastPos = transform.position;
        Energy = Mathf.Clamp(Energy - moved * 0.15f + (moved < 0.001f ? dt * 0.6f : 0f), 0f, 100f);
        Hunger = Mathf.Clamp(Hunger + dt * 0.08f, 0f, 100f);
        Mood = Mathf.MoveTowards(Mood, 60f - Hunger * 0.2f, dt * 0.15f);
        Health = Mathf.Clamp(Health + dt * 0.25f, 0f, 100f);
        if (Hunger > 95f && Random.value < dt * 0.05f) Hunger = 10f; // bought something from a stall

        if (Down) { HoldDown(); return; }
        if (downUntil > 0f) GetUp();

        if (Time.time >= nextHitCheck) { nextHitCheck = Time.time + 0.08f; CheckCars(); }
        if (Time.time >= nextThought) Think();
        OfferTalk();
    }

    // ── Thoughts ─────────────────────────────────────────────────────────────────────────────
    private void Think()
    {
        nextThought = Time.time + Random.Range(25f, 60f);
        if (player != null && (player.position - transform.position).sqrMagnitude > 30f * 30f) return; // nobody to see it
        string t;
        int hour = Mathf.FloorToInt(GameClock.MinuteOfDay / 60f);
        if (Health < 60f) t = Pick("ขายังเจ็บอยู่เลย", "ต้องไปหาหมอแล้วมั้ง", "รถสมัยนี้ขับเร็วจริง");
        else if (Hunger > 75f) t = Pick("หิวจัง อยากกินส้มตำ", "เที่ยงแล้ว กินอะไรดีนะ", "ก๋วยเตี๋ยวสักชามก็ดี");
        else if (Energy < 25f) t = Pick("เหนื่อยจัง อยากนั่งพัก", "เดินมาทั้งวันแล้ว", "กลับบ้านไปนอนดีกว่า");
        else if (Mood < 25f) t = Pick("วันนี้ไม่ใช่วันของเราเลย", "หงุดหงิดจริงๆ", "อย่ามีใครมายุ่งนะ");
        else if (hour < 9) t = Pick("ตื่นเช้าอากาศดีจัง", "ไปตลาดเช้าก่อนดีกว่า", "กาแฟสักแก้วก่อนทำงาน");
        else if (hour >= 18) t = Pick("เย็นแล้ว กลับบ้านดีกว่า", "คืนนี้มีงานวัดไหมนะ", "ไปหาอะไรอร่อยๆ กินดีกว่า");
        else t = Pick("ร้อนจังวันนี้", "ของแพงขึ้นทุกวันเลย", "เดี๋ยวแวะธนาคารหน่อย", "อยากไปเที่ยวทะเลจัง", "ลูกจะกลับบ้านช่วงสงกรานต์", "หวยงวดนี้ออกอะไรนะ");
        SpeechBubble.Show(transform, "(" + t + ")");
    }

    private static string Pick(params string[] lines) => lines[Random.Range(0, lines.Length)];

    // ── Talking ──────────────────────────────────────────────────────────────────────────────
    private void OfferTalk()
    {
        if (player == null)
        {
            playerCtl = FindAnyObjectByType<PlayerController>();
            if (playerCtl == null) return;
            player = playerCtl.transform;
        }
        var flat = player.position - transform.position;
        flat.y = 0f;
        float d = flat.magnitude;
        bool close = d < 2.2f && !playerCtl.VehicleMode && playerCtl.enabled;
        if (talking == this)
        {
            if (!close) { ContextButton.Hide(this); talking = null; }
            return;
        }
        if (!close) return;
        if (talking != null)
        {
            var other = player.position - talking.transform.position;
            other.y = 0f;
            if (other.magnitude <= d + 0.3f) return; // someone nearer already has the button
            ContextButton.Hide(talking);
        }
        if (ContextButton.TakenByOther(this) && talking == null) return; // a shop, a seat, a door: they come first
        talking = this;
        ShowButton();
    }

    private void ShowButton() =>
        ContextButton.Show(this, "คุย",
            $"{Name} · {Age} ปี · {Job} · พลังงาน {Energy:0} · อิ่ม {100f - Hunger:0} · สุขภาพ {Health:0} · {Feeling}", Talk);

    private void Talk()
    {
        if (Down || player == null) return;
        var to = player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
        GetComponent<Villager>()?.Pause(4f);
        pestered = Time.time - lastTalk < 20f ? pestered + 1 : 0;
        lastTalk = Time.time;
        string reply;
        if (pestered >= 3)
        {
            Mood = Mathf.Max(0f, Mood - 15f);
            reply = Pick("พอได้แล้ว! คุยอะไรนักหนา", "รำคาญนะ ไปไกลๆ เลย", "จะเอาอะไรอีกเนี่ย!");
        }
        else if (Health < 60f) reply = Pick("โอ๊ย ยังเจ็บอยู่เลย", "ระวังรถหน่อยนะ ฉันเพิ่งโดนชนมา");
        else if (Mood < 25f) reply = Pick("ไม่อยากคุยตอนนี้", "อย่าเพิ่งมายุ่ง อารมณ์ไม่ดี", "มีอะไรก็ว่ามาเร็วๆ");
        else if (Hunger > 75f) reply = Pick("หิวข้าวจัง แถวนี้มีร้านอร่อยไหม", "ไปกินข้าวด้วยกันไหมล่ะ");
        else if (Energy < 25f) reply = Pick("เหนื่อยจังเลยวันนี้", "ขอนั่งพักก่อนนะ เดินมาไกล");
        else if (Mood > 75f) reply = Pick($"สวัสดีจ้า ฉันชื่อ{Name} เป็น{Job}", "วันนี้อากาศดีนะ ไปเที่ยวไหนมา", "ยิ้มเข้าไว้ วันนี้ต้องดีแน่ๆ");
        else reply = Pick($"สวัสดี ฉัน{Name}", "ไปไหนมาจ๊ะ", $"ทำงาน{Job}มาหลายปีแล้วล่ะ", "แถวนี้มีอะไรกินบ้างนะ");
        if (pestered < 3) Mood = Mathf.Min(100f, Mood + 8f);
        SpeechBubble.Show(transform, reply);
        if (talking == this) ShowButton();
    }

    // ── Cars ─────────────────────────────────────────────────────────────────────────────────
    private void CheckCars()
    {
        var p = transform.position;
        foreach (var car in TrafficCar.Active)
        {
            if (car == null || car.Speed < 2f) continue;
            if (Hits(car.transform, car.HalfLength, p)) { KnockedDown(car.transform, car.Speed); return; }
        }
        // the car or bike you're driving
        if (playerCtl != null && playerCtl.VehicleMode && Mathf.Abs(playerCtl.VehicleSpeed) > 2.5f && Hits(player, 1.6f, p))
            KnockedDown(player, Mathf.Abs(playerCtl.VehicleSpeed), byPlayer: true);
    }

    private static bool Hits(Transform car, float halfLength, Vector3 p)
    {
        var local = car.InverseTransformPoint(p);
        float sx = car.lossyScale.x, sz = car.lossyScale.z;
        return Mathf.Abs(local.x * sx) < 1.05f && Mathf.Abs(local.z * sz) < halfLength + 0.3f && Mathf.Abs(p.y - car.position.y) < 2.5f;
    }

    private void KnockedDown(Transform car, float speed, bool byPlayer = false)
    {
        standRot = transform.rotation;
        standY = transform.position.y;
        Health = Mathf.Max(5f, Health - Mathf.Clamp(speed * 6f, 15f, 70f));
        Mood = Mathf.Max(0f, Mood - 40f);
        downUntil = Time.time + Mathf.Lerp(3f, 7f, Mathf.InverseLerp(2f, 14f, speed));
        // thrown aside, away from the car, and flat on the ground
        var away = transform.position - car.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = car.right;
        away = Vector3.ProjectOnPlane(away, Vector3.up).normalized;
        transform.position += away * Mathf.Clamp(speed * 0.18f, 0.6f, 2.2f);
        transform.rotation = Quaternion.LookRotation(away) * Quaternion.Euler(-88f, 0f, 0f);
        if (model != null) model.Seated = true;
        GameAudio.Play("punch_heavy", transform.position, 0.8f);
        SpeechBubble.Show(transform, Pick("โอ๊ย!", "ช่วยด้วย!", "ขับรถยังไงเนี่ย!"));
        if (byPlayer)
        {
            UiKit.FloatAbovePlayer($"ชน{Name}!", new Color(1f, 0.45f, 0.35f));
        }
        if (talking == this) { ContextButton.Hide(this); talking = null; }
    }

    private void HoldDown()
    {
        // lie still (the walk animation stays off), sunk to the ground
        var pos = transform.position;
        pos.y = standY - 0.85f;
        transform.position = pos;
    }

    private void GetUp()
    {
        downUntil = 0f;
        var pos = transform.position;
        pos.y = standY;
        transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, standRot.eulerAngles.y, 0f));
        if (model != null) model.Seated = false;
        SpeechBubble.Show(transform, Health < 60f ? Pick("โอ๊ย... เจ็บขาไปหมด", "ไม่ไหวแล้ว ต้องไปโรงพยาบาล") : Pick("รอดตายไป", "ระวังหน่อยสิ!"));
        lastPos = transform.position;
    }

    private void OnDisable()
    {
        if (talking == this) { ContextButton.Hide(this); talking = null; }
    }
}
