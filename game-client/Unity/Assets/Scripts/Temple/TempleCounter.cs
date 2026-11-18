using UnityEngine;

// Where the temple window opens: the abbot's kuti (mode "abbot"), the amulet pavilion ("amulets") or
// the กฐิน/ผ้าป่า money tree ("merit").
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
        else if (mode == TempleUI.Merit)
        {
            var m = s?.merit;
            string hint = m == null ? "ต้องออนไลน์ก่อน"
                : m.open ? $"{m.name} · ร่วมทำบุญ ได้บารมีและ XP · ยอดทั้งหมู่บ้าน {m.town_total:N0} บาท"
                : $"งานทอดกฐิน/ผ้าป่าครั้งหน้า: {m.next_name} อีก {m.next_in} วัน";
            ContextButton.Show(this, "ร่วมทำบุญ", hint, () => TempleUI.Show(TempleUI.Merit));
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
