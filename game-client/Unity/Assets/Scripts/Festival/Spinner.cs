using UnityEngine;

// Turns around a local axis (ferris wheel, carousel). Children named "Cabin" stay upright.
public class Spinner : MonoBehaviour
{
    [SerializeField] private Vector3 axis = Vector3.right;
    [SerializeField] private float degreesPerSecond = 12f;

    public void Configure(Vector3 localAxis, float speed)
    {
        axis = localAxis;
        degreesPerSecond = speed;
    }

    private void Update()
    {
        transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
        foreach (Transform child in transform)
            if (child.name == "Cabin") child.rotation = Quaternion.Euler(0f, transform.parent != null ? transform.parent.eulerAngles.y : 0f, 0f);
    }
}
