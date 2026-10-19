using UnityEngine;

// Short-lived world-space text (damage numbers) that rises and fades.
public class FloatingText : MonoBehaviour
{
    private const float Lifetime = 0.9f;
    private const float RiseSpeed = 1.2f;

    private TextMesh text;
    private Color startColor;
    private float age;

    public static void Spawn(Vector3 position, string message, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = position;

        var font = ThaiFont.Display; // rounded cartoon lettering (Mitr)
        var mesh = go.AddComponent<TextMesh>();
        mesh.font = font;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        mesh.text = message;
        mesh.characterSize = 0.1f;
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
        c.a = 1f - age / Lifetime;
        text.color = c;

        if (age >= Lifetime) Destroy(gameObject);
    }
}
