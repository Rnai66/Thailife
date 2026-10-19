using UnityEngine;

// Short text over a character's head (ทักทาย): stays a few seconds, then fades.
// One bubble per character; a new phrase replaces the old one.
public class SpeechBubble : MonoBehaviour
{
    private const float Lifetime = 4f;
    private const float FadeTime = 0.6f;

    private TextMesh text, shadow;
    private float age;

    public static void Show(Transform host, string message)
    {
        var bubble = host.GetComponentInChildren<SpeechBubble>();
        if (bubble == null)
        {
            var go = new GameObject("SpeechBubble");
            go.transform.SetParent(host, false);
            go.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            go.AddComponent<FaceCamera>();
            bubble = go.AddComponent<SpeechBubble>();
            bubble.shadow = bubble.CreateText("Shadow", new Vector3(0.02f, -0.02f, 0.01f), new Color(0f, 0f, 0f, 0.8f));
            bubble.text = bubble.CreateText("Text", Vector3.zero, new Color(1f, 0.92f, 0.55f));
        }
        bubble.text.text = bubble.shadow.text = message;
        bubble.age = 0f;
        bubble.Fade(1f);
    }

    private TextMesh CreateText(string name, Vector3 offset, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = offset;
        var font = ThaiFont.Font;
        var mesh = go.AddComponent<TextMesh>();
        mesh.font = font;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        mesh.characterSize = 0.09f;
        mesh.fontSize = 56;
        mesh.fontStyle = FontStyle.Bold;
        mesh.anchor = TextAnchor.LowerCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        return mesh;
    }

    private void Fade(float alpha)
    {
        var c = text.color; c.a = alpha; text.color = c;
        var s = shadow.color; s.a = 0.8f * alpha; shadow.color = s;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age > Lifetime - FadeTime) Fade(Mathf.Clamp01((Lifetime - age) / FadeTime));
        if (age >= Lifetime) Destroy(gameObject);
    }
}
