using UnityEngine;

// The board at the front of a parcel of land: walk up to it to see the land (LandUI) — whose it is,
// what to build on it, the rent waiting and, at the crop field, the buyer for the harvest.
[RequireComponent(typeof(Collider))]
public class LandCounter : MonoBehaviour
{
    [SerializeField] private string parcel = "plain"; // plain | rice | city

    public void Configure(string parcelId) => parcel = parcelId;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        var s = LandClient.Instance?.Current;
        bool mine = s != null && (parcel == "plain" ? s.own_plain : parcel == "rice" ? s.own_rice : s.own_city);
        string hint = !mine ? "ที่ดินแปลงนี้ขายอยู่ · ซื้อได้ที่ธนาคารในเมือง"
            : parcel == "rice" ? "ที่นาของคุณ · เดินเข้าแปลงนาเพื่อไถ ดำนา รดน้ำ เกี่ยวข้าว"
            : "ที่ดินของคุณ · สร้าง ทำไร่ เก็บค่าเช่า ขายผลผลิต";
        ContextButton.Show(this, mine ? "ที่ดินของคุณ" : "ดูที่ดิน", hint, () => LandUI.Show(parcel));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        ContextButton.Hide(this);
        LandUI.Hide();
    }
}
