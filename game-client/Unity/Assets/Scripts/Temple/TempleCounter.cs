using UnityEngine;

// Where the temple window opens: the abbot's kuti (mode "abbot") or the amulet pavilion ("amulets").
[RequireComponent(typeof(Collider))]
public class TempleCounter : MonoBehaviour
{
    [SerializeField] private string mode = TempleUI.Abbot;

    public void Configure(string which) => mode = which;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        var s = TempleClient.Instance?.Current;
        if (mode == TempleUI.Abbot)
        {
            string hint = s == null ? "ต้องออนไลน์ก่อน"
                : s.ordained ? $"พระเลเวล {s.monk_level} · สึก หรือสึกออกมาเป็นหมอผี (พระเลเวล {s.exorcist_monk_level})"
                : "บวชเป็นพระ · รับหมาวัดและกุมารทองไปเฝ้าบ้าน";
            ContextButton.Show(this, "กราบเจ้าอาวาส", hint, () => TempleUI.Show(TempleUI.Abbot));
        }
        else
            ContextButton.Show(this, "บูชาพระเครื่อง", "พระเครื่องและผ้ายันต์ ขึ้นหิ้งพระที่บ้าน กันผีกันขโมย", () => TempleUI.Show(TempleUI.Amulets));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        TempleUI.Hide();
    }
}
