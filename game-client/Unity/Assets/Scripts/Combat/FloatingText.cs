using UnityEngine;

// Short-lived world-space text (damage numbers) that rises and fades.
public class FloatingText : MonoBehaviour
{
    private const float Lifetime = 1.6f;
    private const float RiseSpeed = 0.7f;

    private TextMesh text;
    private Color startColor;
    private float age;

    public static void Spawn(Vector3 position, string message, Color color)
    {
        // Words (done!, level up, what auto is doing) go to a readable pill on the screen; only short
        // numbers like "+50 ฿" or damage float over the head
        if (message != null && message.Trim().Length > 10)
        {
            ScreenToast.Show(message, color);
            return;
        }
        var go = new GameObject("FloatingText");
        go.transform.position = position;

        var font = ThaiFont.Display; // rounded cartoon lettering (Mitr)
        var mesh = go.AddComponent<TextMesh>();
        mesh.font = font;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        mesh.text = message;
        mesh.characterSize = 0.07f;
        mesh.fontSize = 56;
        mesh.fontStyle = FontStyle.Normal;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.color = color;

        go.AddComponent<FaceCamera>();
        var floating = go.AddComponent<FloatingText>();
        floating.text = mesh;
        floating.startColor = color;
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

        var c = startColor;
        c.a = Mathf.Clamp01((Lifetime - age) / (Lifetime * 0.4f)); // solid, then fades over the last part
        text.color = c;

        if (age >= Lifetime) Destroy(gameObject);
    }
}
