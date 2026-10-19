using UnityEngine;

// Likay (ลิเก) performer: stands on the stage and dances (wave / wai) now and then
public class LikayDancer : MonoBehaviour
{
    [SerializeField] private int look;

    private CharacterModel model;
    private float nextMove;

    public void Configure(int lookIndex) => look = lookIndex;

    private void Start()
    {
        model = gameObject.AddComponent<CharacterModel>();
        model.SetSkin(look);
        nextMove = Time.time + Random.value * 2f;
    }

    private void Update()
    {
        if (model == null || Time.time < nextMove) return;
        nextMove = Time.time + Random.Range(2.5f, 4.5f);
        model.Play(Random.value < 0.6f ? PlayerAction.Wave : PlayerAction.Wai);
        transform.Rotate(0f, Random.Range(-40f, 40f), 0f);
    }
}
