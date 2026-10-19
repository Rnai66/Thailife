using UnityEngine;
using UnityEngine.SceneManagement;

// Plays the game's sounds — recorded Kenney takes where there are some, synthesized otherwise:
// one-shots (GameAudio.Play), soft background music the player picks (SoundUI), and the ambience
// that follows the player — footsteps, crickets at night, waves at the beach, ranat music near a
// fair that's on, and each place's own sound (AmbientZone). Created automatically, kept across scenes.
public class GameAudio : MonoBehaviour
{
    private const string TrackKey = "audio.track", MusicKey = "audio.music", SfxKey = "audio.sfx";

    private static GameAudio instance;

    public static Transform Player => instance != null ? instance.player : null;

    // -1 = no music; otherwise an index into SynthMusic.Tracks
    public static int Track
    {
        get => PlayerPrefs.GetInt(TrackKey, 0);
        set { PlayerPrefs.SetInt(TrackKey, value); PlayerPrefs.Save(); if (instance != null) instance.wantedTrack = value; }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.5f);
        set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
        set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    private AudioSource oneShots, crickets, waves, fairMusic, concertMusic, bgm;
    private Transform player;
    private CharacterController body;
    private FestivalGate[] gates = new FestivalGate[0];
    private float nextStep, nextScan, music = 0.5f, sfx = 0.8f;
    private int playingTrack = -2, wantedTrack;
    private bool beach, city;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("GameAudio");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameAudio>();
    }

    // A sound at a point in the world (punches, place sounds…); pass null for a flat UI sound.
    public static void Play(string name, Vector3? at = null, float volume = 1f, float range = 30f)
    {
        if (instance == null) return;
        var clip = KenneySounds.Get(name) ?? (Long(name) ? SynthSounds.TryGet(name)
            : SynthSounds.Get(name.StartsWith("step") ? "step" : name.Replace("_heavy", "")));
        if (clip == null) return;
        volume *= instance.sfx;
        if (at.HasValue)
        {
            var go = new GameObject("Sfx_" + name);
            go.transform.position = at.Value;
            var src = Source(go, clip, false, volume, spatial: true);
            src.maxDistance = range;
            src.pitch = Long(name) ? 1f : Random.Range(0.92f, 1.08f);
            src.Play();
            Destroy(go, clip.length + 0.1f);
        }
        else
        {
            instance.oneShots.pitch = Random.Range(0.95f, 1.05f);
            instance.oneShots.PlayOneShot(clip, volume);
        }
    }

    // Sounds too long to make in one frame (generated on a worker thread)
    private static bool Long(string name) =>
        name == "chant" || name.EndsWith("_bell") || name == "buffalo" || name == "cow" || name.StartsWith("bgm");

    private static AudioSource Source(GameObject go, AudioClip clip, bool loop, float volume, bool spatial)
    {
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = loop;
        src.volume = volume;
        src.playOnAwake = false;
        src.spatialBlend = spatial ? 1f : 0f;
        src.minDistance = 3f;
        src.maxDistance = 30f;
        src.rolloffMode = AudioRolloffMode.Linear;
        return src;
    }

    private void Awake()
    {
        oneShots = Source(gameObject, null, false, 1f, false);
        crickets = Source(gameObject, SynthSounds.Get("crickets"), true, 0f, false);
        waves = Source(gameObject, SynthSounds.Get("waves"), true, 0f, false);
        fairMusic = Source(gameObject, SynthSounds.Get("ranat"), true, 0f, false);
        bgm = Source(gameObject, null, true, 0f, false);
        concertMusic = Source(gameObject, null, true, 0f, false);
        crickets.Play();
        waves.Play();
        fairMusic.Play();
        wantedTrack = Track;
        if (wantedTrack >= 0) SynthSounds.Prepare("bgm" + wantedTrack);
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        body = null;
        nextScan = 0f;
        beach = scene.name == "Beach";
        if (beach) SynthSounds.Prepare("concert");
        city = scene.name == "City";
        AmbientPlaces.Attach(scene);
    }

    private void Update()
    {
        if (player == null && Time.time >= nextScan)
        {
            nextScan = Time.time + 1f;
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
                body = pc.GetComponent<CharacterController>();
                gates = FindObjectsByType<FestivalGate>(FindObjectsSortMode.None);
            }
        }

        music = MusicVolume;
        sfx = SfxVolume;
        float dt = Time.deltaTime;
        float fair = FairVolume(false), concert = FairVolume(true);
        Fade(crickets, GameClock.IsNight ? 0.35f * sfx : 0f, dt);
        Fade(waves, beach ? 0.5f * sfx : 0f, dt);
        Fade(fairMusic, fair * sfx, dt);
        Concert(concert * 1.3f * sfx, dt);
        Music(dt, Mathf.Max(fair, concert));
        Footsteps();
    }

    // Background music: switch tracks when the player picks one; dip it under the fair's ranat
    private void Music(float dt, float fair)
    {
        if (wantedTrack != playingTrack)
        {
            if (wantedTrack < 0)
            {
                bgm.Stop();
                playingTrack = wantedTrack;
            }
            else
            {
                var clip = SynthSounds.TryGet("bgm" + wantedTrack);
                if (clip != null)
                {
                    bgm.clip = clip;
                    bgm.volume = 0f;
                    bgm.Play();
                    playingTrack = wantedTrack;
                }
            }
        }
        float target = playingTrack >= 0 ? 0.4f * music * (1f - Mathf.Clamp01(fair * 2f)) : 0f;
        bgm.volume = Mathf.MoveTowards(bgm.volume, target, dt * 0.4f);
    }

    private void Footsteps()
    {
        if (body == null || !body.enabled || !body.isGrounded) return;
        Vector3 v = body.velocity;
        float speed = new Vector2(v.x, v.z).magnitude;
        if (speed < 0.6f || Time.time < nextStep) return;
        nextStep = Time.time + Mathf.Clamp(1.4f / speed, 0.24f, 0.55f);
        Play(StepSound(), player.position, 0.45f);
    }

    // Wood on floors, decks and piers; concrete on roads and in the city; sand at the beach; grass elsewhere
    private string StepSound()
    {
        if (Physics.Raycast(player.position + Vector3.up * 0.3f, Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore))
        {
            string n = hit.collider.name;
            if (n.Contains("Deck") || n.Contains("Floor") || n.Contains("Plank") || n.Contains("Porch") || n.Contains("Stair") || n.Contains("Platform"))
                return "step_wood";
            if (n.Contains("Road") || n.Contains("Lane") || n.Contains("Concrete") || n.Contains("Path") || n.Contains("Sidewalk"))
                return "step_concrete";
        }
        return beach ? "step_sand" : city ? "step_concrete" : "step_grass";
    }

    // The beach concert's band loop (made on a worker thread the first time)
    private void Concert(float target, float dt)
    {
        if (concertMusic.clip == null)
        {
            if (target <= 0f) return;
            var clip = SynthSounds.TryGet("concert");
            if (clip == null) return;
            concertMusic.clip = clip;
            concertMusic.Play();
        }
        Fade(concertMusic, target, dt);
    }

    // Loudest when standing among the stalls (or in front of the concert stage) of an event that's on, silent 45 m away
    private float FairVolume(bool concert)
    {
        if (player == null) return 0f;
        float best = 0f;
        foreach (var g in gates)
        {
            if (g == null || (g.Scale == "beach") != concert) continue;
            var attractions = g.transform.Find("Attractions");
            if (attractions == null || !attractions.gameObject.activeInHierarchy) continue;
            float d = Vector3.Distance(player.position, attractions.position);
            best = Mathf.Max(best, Mathf.Clamp01(1f - (d - 10f) / 35f));
        }
        return best * 0.45f;
    }

    private static void Fade(AudioSource src, float target, float dt) =>
        src.volume = Mathf.MoveTowards(src.volume, target, dt * 0.5f);
}
