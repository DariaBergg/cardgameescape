using UnityEngine;

public class HoverBob : MonoBehaviour
{
    public float amplitude = 0.18f;
    public float speed = 2.2f;
    public float tilt = 3f;

    float phase;

    void Start()
    {
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float t = Time.time * speed + phase;
        transform.localPosition = new Vector3(0, Mathf.Sin(t) * amplitude, 0);
        transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.7f) * tilt);
    }
}
