using UnityEngine;

public class GeneratorShake : MonoBehaviour
{
    public float shakeAmount = 0.0001f;
    public float shakeSpeed = 30f;

    public bool isRunning = false;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        if (!isRunning)
        {
            transform.localPosition = startPosition;
            transform.localRotation = startRotation;
            return;
        }

        float x = Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f;
        float y = Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f;
        float z = Mathf.PerlinNoise(Time.time * shakeSpeed, Time.time * shakeSpeed) - 0.5f;

        transform.localPosition = startPosition +
            new Vector3(x, y, z) * shakeAmount;

        transform.localRotation = startRotation *
            Quaternion.Euler(x * 2f, y * 2f, z * 2f);
    }
}