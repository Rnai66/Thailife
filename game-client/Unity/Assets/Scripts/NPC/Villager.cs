using UnityEngine;

// A villager (ชาวบ้าน) strolling inside its spawner's area: walks to a random spot, looks
// around for a while, now and then says something. Uses the downloaded character looks
// (CharacterModel animates walking from the movement). Local only: each player sees their own.
public class Villager : MonoBehaviour
{
    private const float WalkSpeed = 1.3f;

    private VillagerSpawner home;
    private Vector3 target;
    private float waitUntil, nextChat;
    private string[] phrases;

    public void Init(VillagerSpawner spawner, int look, string[] lines)
    {
        home = spawner;
        phrases = lines;
        var model = gameObject.AddComponent<CharacterModel>();
        model.SetSkin(look);
        if (!model.HasModel)
        {
            // No character files on this machine: a simple stand-in so the place still feels busy
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);
            body.GetComponent<Renderer>().material.color = Color.HSVToRGB(Random.value, 0.45f, 0.85f);
        }
        PickTarget();
        nextChat = Time.time + Random.Range(6f, 25f);
    }

    private void PickTarget()
    {
        target = home.RandomPoint();
        waitUntil = 0f;
    }

    private void Update()
    {
        if (home == null) return;
        if (waitUntil > 0f)
        {
            if (Time.time >= waitUntil) PickTarget();
        }
        else
        {
            Vector3 to = target - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.2f)
            {
                waitUntil = Time.time + Random.Range(1.5f, 6f); // stop and look at the stalls
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 6f * Time.deltaTime);
                transform.position += to.normalized * Mathf.Min(WalkSpeed * Time.deltaTime, to.magnitude);
            }
        }

        if (phrases != null && phrases.Length > 0 && Time.time >= nextChat)
        {
            nextChat = Time.time + Random.Range(15f, 40f);
            SpeechBubble.Show(transform, phrases[Random.Range(0, phrases.Length)]);
        }
    }
}
