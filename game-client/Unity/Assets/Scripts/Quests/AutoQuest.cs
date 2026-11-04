using System;
using System.Collections;
using Nakama;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ออโต้: the character does today's quests by itself. Turned on with the round "ออโต้" button on the
// HUD, it takes the quests one by one: walks to where each is done (the canal to fish, the banana
// trees to train, the shop, the paddies, the coop, the village hall stage, the school...), riding the
// songthaew when the place is in another scene, then works there while the server finishes the
// quest (quests_auto: some energy, a short wait) and pays the reward. Steering with the joystick or
// keys hands control back. On stays on across scene loads; each scene's Network object carries one.
[RequireComponent(typeof(NakamaConnection))]
public class AutoQuest : MonoBehaviour
{
    public static bool On { get; private set; }

    private static AutoQuest instance;

    private PlayerController player;
    private Coroutine running;
    private GameObject statusRoot;
    private Text statusText;
    private string status = "";
    private float idleUntil;
    private bool toldDone;
    private AudioSource song; // the tune while performing on auto

    public static void Toggle()
    {
        if (On) { Stop("ปิดออโต้แล้ว"); return; }
        var q = QuestClient.Instance;
        if (q == null || !q.IsOnline) { UiKit.FloatAbovePlayer("ต้องออนไลน์ก่อน", new Color(1f, 0.6f, 0.5f)); return; }
        On = true;
        if (instance != null) { instance.toldDone = false; instance.status = "กำลังเริ่ม…"; instance.idleUntil = 0f; }
        UiKit.FloatAbovePlayer("ออโต้: ทำภารกิจประจำวันให้เอง", new Color(0.5f, 0.85f, 1f));
        GameAudio.Play("bell");
    }

    public static void Stop(string message)
    {
        On = false;
        if (instance != null)
        {
            if (instance.running != null) instance.StopCoroutine(instance.running);
            instance.running = null;
            if (instance.player != null) instance.player.AutoMove = Vector3.zero;
            if (instance.song != null) Destroy(instance.song);
            ActivityPose.Clear();
        }
        if (!string.IsNullOrEmpty(message)) UiKit.FloatAbovePlayer(message, new Color(1f, 0.85f, 0.4f));
    }

    private void Awake()
    {
        instance = this;
        BuildStatus();
    }

    private void OnDestroy()
    {
        if (player != null) player.AutoMove = Vector3.zero;
        if (instance == this) instance = null;
        if (statusRoot != null) Destroy(statusRoot);
    }

    private void Update()
    {
        statusRoot.SetActive(On);
        if (!On) return;
        statusText.text = "ออโต้ · " + status;
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        if (player.ManualInput) { Stop("หยุดออโต้ — ควบคุมเอง"); return; }
        if (running == null) running = StartCoroutine(Loop());
    }

    // One long-running loop while auto is on: each pass does (or travels toward) one quest
    private IEnumerator Loop()
    {
        while (On)
        {
            var q = QuestClient.Instance;
            if (player == null || Time.time < idleUntil || q == null || !q.IsOnline) { yield return null; continue; }
            yield return Run();
            yield return null;
        }
        running = null;
    }

    // ── Where each kind of quest is done ───────────────────────────────────────────────────────
    private static Type PlaceType(string kind) => kind switch
    {
        "fish" => typeof(FishingSpot),
        "sell_produce" or "shop" => typeof(ShopCounter),
        "plant" or "harvest" => typeof(FarmPlot),
        "feed" or "sell_eggs" => typeof(ChickenPen),
        "music" => typeof(MusicSpot),
        "train" => typeof(TrainingTarget),
        "study" => typeof(SchoolDesk),
        _ => null, // eat: wherever you are
    };

    private static string PlaceScene(string kind) => kind == "fish" || kind == "study" ? "Tambon" : "ThaiVillage";

    private static string Doing(string kind) => kind switch
    {
        "fish" => "ตกปลา", "sell_produce" => "ขายของ", "shop" => "ซื้อของ", "plant" => "ดำนา", "harvest" => "เกี่ยวข้าว",
        "feed" => "ให้อาหารไก่", "sell_eggs" => "เก็บไข่ขาย", "music" => "เล่นดนตรี", "train" => "ซ้อมมวย",
        "study" => "เข้าเรียน", "eat" => "กินข้าว", _ => "ทำภารกิจ",
    };

    // The nearest place for this kind of quest: its trigger zone, or (banana trees, the punching bag)
    // the solid part you hit. Chicken work happens at the coop gate.
    private Collider FindPlace(string kind, Type type)
    {
        if (kind == "feed" || kind == "sell_eggs")
        {
            var gate = GameObject.Find("CoopGateZone");
            if (gate != null && gate.GetComponent<Collider>() != null) return gate.GetComponent<Collider>();
        }
        if (type == null) return null;
        Collider best = null;
        float bestD = float.MaxValue;
        foreach (var o in FindObjectsByType(type))
        {
            var host = (Component)o;
            var c = kind == "train" ? Solid(host) : host.GetComponent<Collider>() ?? host.GetComponentInChildren<Collider>();
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
            float d = (c.bounds.center - player.transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = c; }
        }
        return best;
    }

    private static Collider Solid(Component host)
    {
        foreach (var c in host.GetComponentsInChildren<Collider>())
            if (!c.isTrigger) return c;
        return null;
    }

    // ── The loop: next quest, go there, do it, repeat ─────────────────────────────────────────
    private IEnumerator Run()
    {
        var quests = QuestClient.Instance;
        var s = quests.Current;
        QuestClient.Quest next = null;
        if (s?.quests != null)
        {
            foreach (var q in s.quests)
                if (q.done && !q.claimed) { yield return Await(quests.ClaimAsync(q.id), "รับรางวัล"); yield break; }
            foreach (var q in s.quests) if (q.auto) { next = q; break; }
            if (next == null) foreach (var q in s.quests) if (!q.done) { next = q; break; }
        }
        if (next == null)
        {
            if (s != null && s.all_done && !s.bonus_claimed) { yield return Await(quests.ClaimBonusAsync(), "รับโบนัส"); yield break; }
            // Nothing left today: say so once, then wait for the next game day's quests and carry on
            float left = s != null ? s.resets_in_s - quests.Age : 0f;
            status = $"ภารกิจวันนี้ครบแล้ว · ภารกิจใหม่ในอีก {Mathf.Max(0, (int)left) / 60}:{Mathf.Max(0, (int)left) % 60:00} นาที";
            if (!toldDone)
            {
                toldDone = true;
                UiKit.FloatAbovePlayer("ภารกิจวันนี้ครบแล้ว — รอภารกิจใหม่", new Color(1f, 0.85f, 0.4f));
            }
            if (left < -2f) quests.Refresh();
            idleUntil = Time.time + 1f;
            yield break;
        }

        toldDone = false;
        string doing = Doing(next.kind);
        var type = PlaceType(next.kind);
        var place = FindPlace(next.kind, type);
        string scene = SceneManager.GetActiveScene().name;
        if (place == null && type != null && scene != PlaceScene(next.kind))
        {
            // Ride the songthaew to where it's done; the AutoQuest there carries on
            var stop = GameObject.Find("BusStop_ป้ายรถสองแถว");
            if (stop != null)
            {
                status = $"ไปขึ้นรถสองแถว เพื่อไป{doing}";
                yield return WalkTo(stop.GetComponentInChildren<Collider>(), stop.transform.position);
            }
            foreach (var l in Locations.All)
                if (l.Scene == PlaceScene(next.kind)) { status = $"นั่งรถไป{doing}"; Locations.TravelTo(l); break; }
            idleUntil = Time.time + 10f;
            yield break;
        }

        if (place != null && !next.auto)
        {
            status = $"เดินไป{doing} · {next.title}";
            yield return WalkTo(place, place.bounds.center);
        }
        player.AutoMove = Vector3.zero;
        // Walking in opens each place's window; keep the view clear while the character works
        if (next.kind == "feed" || next.kind == "sell_eggs") CoopUI.Hide();
        else if (next.kind == "music") MusicUI.Hide();
        else if (next.kind == "shop" || next.kind == "sell_produce") ShopUI.Hide();
        else if (next.kind == "study") SchoolUI.Hide();

        // Start the work on the server (energy now, done after a short wait)
        if (!next.auto)
        {
            Exception error = null;
            var call = quests.AutoAsync(next.id);
            while (!call.IsCompleted) yield return null;
            if (call.IsFaulted) error = call.Exception?.InnerException;
            if (error != null)
            {
                string m = error is ApiResponseException api ? api.Message : "";
                if (m.Contains("not enough energy")) { Stop("ออโต้หยุด: พลังงานไม่พอ — พักหรือกินข้าวก่อน"); yield break; }
                if (m.Contains("sleeping")) { Stop("ออโต้หยุด: กำลังนอนอยู่"); yield break; }
                quests.Refresh();
                idleUntil = Time.time + 3f;
                yield break;
            }
        }

        // Work at it until the server says it's done
        var model = player.GetComponent<CharacterModel>();
        var actions = player.GetComponent<PlayerActions>();
        bool kick = true;
        float started = Time.time, nextMove = 0f, nextWord = 0f;
        if (next.kind == "fish") ActivityPose.Hold("rod", place != null ? place.bounds.center : (Vector3?)null);
        if (next.kind == "music")
        {
            ActivityPose.Hold("instrument");
            SynthSounds.Prepare("morlam");
        }
        while (On)
        {
            var q = Find(quests.Current, next.id);
            if (q == null || q.done || q.kind != next.kind) break;
            float left = q.auto_left_s - quests.Age;
            status = $"กำลัง{doing}… อีก {Mathf.Max(0, Mathf.CeilToInt(left))} วินาที";
            if (place != null && next.kind != "music") Face(place.bounds.center);
            if (next.kind == "music" && song == null)
            {
                // a หมอลำ tune from the stage while the character strums along
                var clip = SynthSounds.TryGet("morlam");
                if (clip != null)
                {
                    song = player.gameObject.AddComponent<AudioSource>();
                    song.clip = clip;
                    song.loop = true;
                    song.spatialBlend = 0.6f;
                    song.volume = 0.7f;
                    song.Play();
                }
            }
            if (Time.time >= nextMove)
            {
                nextMove = Time.time + MoveEvery(next.kind);
                switch (next.kind)
                {
                    case "train": // real kicks and punches on the banana tree / bag: it sways, and the training counts
                        if (actions != null) actions.Press(kick ? ActionInput.Kick : ActionInput.Punch);
                        kick = !kick;
                        break;
                    case "fish": ActivityPose.Do(kick ? "cast" : "reel", 2.5f); kick = !kick; break;
                    case "plant": ActivityPose.Do("plant", 1.6f); break;
                    case "harvest": ActivityPose.Do("sweep", 1.4f); break;
                    case "feed": case "sell_eggs": ActivityPose.Do("scatter", 1.6f); break;
                    case "music": ActivityPose.Do("strum", MoveEvery("music") + 0.5f); break;
                    default: if (model != null) model.Play(next.kind == "study" ? PlayerAction.Wai : PlayerAction.Wave); break;
                }
            }
            if (Time.time >= nextWord)
            {
                nextWord = Time.time + 6f;
                FloatingText.Spawn(player.transform.position + Vector3.up * 2.2f, doing + "…", new Color(0.6f, 0.9f, 1f));
            }
            if (Time.time - started > (quests.Current?.auto_seconds ?? 20) + 40f) { quests.Refresh(); started = Time.time; }
            yield return null;
        }
        if (song != null) Destroy(song);
        song = null;
        ActivityPose.Clear();
        idleUntil = Time.time + 1.5f;
    }

    private static float MoveEvery(string kind) => kind switch
    {
        "train" => 0.8f, "fish" => 3f, "music" => 6f, "plant" or "harvest" or "feed" or "sell_eggs" => 2f, _ => 4f,
    };

    private static QuestClient.Quest Find(QuestClient.State s, int id)
    {
        if (s?.quests == null) return null;
        foreach (var q in s.quests) if (q.id == id) return q;
        return null;
    }

    private IEnumerator Await(System.Threading.Tasks.Task task, string what)
    {
        status = what;
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) idleUntil = Time.time + 3f;
    }

    // ── Walking ───────────────────────────────────────────────────────────────────────────
    // Head for the place, steer round what's in the way, and if stuck for a while hop over to it
    private IEnumerator WalkTo(Collider place, Vector3 goal)
    {
        float best = float.MaxValue, checkAt = Time.time + 3f, giveUp = Time.time + 75f;
        int stuck = 0, side = 1;
        while (On)
        {
            var pos = player.transform.position;
            if (Arrived(place, goal, pos)) break;
            Vector3 to = goal - pos;
            to.y = 0f;
            float dist = to.magnitude;
            status = status.Split('(')[0].TrimEnd() + $" (อีก {dist:0} ม.)";

            player.AutoMove = Steer(pos, to.normalized, place, ref side);

            if (Time.time >= checkAt)
            {
                checkAt = Time.time + 3f;
                if (dist > best - 0.6f) stuck++; else stuck = 0;
                best = Mathf.Min(best, dist);
                if (stuck == 1) player.Jump();
                if (stuck >= 2 || Time.time > giveUp) { Hop(goal, pos); stuck = 0; best = float.MaxValue; giveUp = Time.time + 75f; }
            }
            yield return null;
        }
        player.AutoMove = Vector3.zero;
    }

    private static bool Arrived(Collider place, Vector3 goal, Vector3 pos)
    {
        Vector3 p = goal;
        if (place != null)
        {
            try { p = place.ClosestPoint(pos); } catch (Exception) { p = place.bounds.center; }
            var b = place.bounds;
            if (pos.x > b.min.x && pos.x < b.max.x && pos.z > b.min.z && pos.z < b.max.z) return true; // inside its zone
        }
        Vector3 d = p - pos;
        d.y = 0f;
        return d.magnitude < 1.1f;
    }

    private Vector3 Steer(Vector3 pos, Vector3 dir, Collider place, ref int side)
    {
        if (Free(pos, dir, place)) return dir;
        foreach (float a in new[] { 35f, 70f, 105f, 140f })
        {
            foreach (int sgn in new[] { side, -side })
            {
                var d = Quaternion.Euler(0f, a * sgn, 0f) * dir;
                if (Free(pos, d, place)) { side = sgn; return d; }
            }
        }
        return dir;
    }

    private bool Free(Vector3 pos, Vector3 dir, Collider place)
    {
        if (!Physics.SphereCast(pos + Vector3.up * 0.9f, 0.3f, dir, out var hit, 1.4f, ~0, QueryTriggerInteraction.Ignore)) return true;
        return hit.collider == place || hit.transform.IsChildOf(player.transform);
    }

    // Can't find a way: step over to just short of the place (on the ground there)
    private void Hop(Vector3 goal, Vector3 from)
    {
        Vector3 back = from - goal;
        back.y = 0f;
        var at = goal + (back.sqrMagnitude > 0.01f ? back.normalized * 1.6f : Vector3.back * 1.6f);
        if (Physics.Raycast(new Vector3(at.x, goal.y + 3f, at.z), Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y + 0.05f;
        else at.y = from.y;
        var look = new Vector3(goal.x - at.x, 0f, goal.z - at.z);
        player.Teleport(at, look.sqrMagnitude > 0.001f ? Quaternion.LookRotation(look) : player.transform.rotation);
    }

    private void Face(Vector3 at)
    {
        var d = at - player.transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.04f)
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 4f);
    }

    // ── Status line under the energy bar ───────────────────────────────────────────────────
    private void BuildStatus()
    {
        var canvasGo = new GameObject("AutoQuestCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var pill = UiKit.Panel("AutoStatus", canvasGo.transform, new Color(0.08f, 0.2f, 0.28f, 0.85f), Vector2.zero, new Vector2(760f, 52f));
        pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 1f);
        pill.anchoredPosition = new Vector2(0f, -110f);
        pill.GetComponent<Image>().raycastTarget = false;
        statusText = UiKit.Label(pill, 24, "", Vector2.zero, new Vector2(740f, 48f), Color.white);
        statusText.raycastTarget = false;
        statusRoot = canvasGo;
        statusRoot.SetActive(false);
    }
}
