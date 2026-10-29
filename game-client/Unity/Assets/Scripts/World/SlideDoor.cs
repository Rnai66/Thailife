using UnityEngine;

// A pair of centre-opening sliding doors (lift cabins and landings, skytrain cars): the leaves are
// children of this object and slide apart along its local X. Set Open and they glide there.
public class SlideDoor : MonoBehaviour
{
    [SerializeField] private Transform[] leaves;
    [SerializeField] private float travel = 0.55f;
    [SerializeField] private float seconds = 0.6f;

    private Vector3[] closed;
    private float amount;

    public bool Open { get; set; }
    public bool Moving => Mathf.Abs(amount - (Open ? 1f : 0f)) > 0.001f;

    public void Configure(Transform left, Transform right, float slide)
    {
        leaves = new[] { left, right };
        travel = slide;
    }

    private void Awake()
    {
        if (leaves == null) return;
        closed = new Vector3[leaves.Length];
        for (int i = 0; i < leaves.Length; i++) if (leaves[i] != null) closed[i] = leaves[i].localPosition;
    }

    private void Update()
    {
        if (closed == null || !Moving) return;
        amount = Mathf.MoveTowards(amount, Open ? 1f : 0f, Time.deltaTime / seconds);
        float k = Mathf.SmoothStep(0f, 1f, amount);
        for (int i = 0; i < leaves.Length; i++)
        {
            if (leaves[i] == null) continue;
            float side = closed[i].x < 0f ? -1f : 1f;
            leaves[i].localPosition = closed[i] + new Vector3(side * travel * k, 0f, 0f);
        }
    }
}
