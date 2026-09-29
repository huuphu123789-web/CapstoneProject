using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // G?i hàm này t? b?t k? ?âu ?? rung camera
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // T?o v? trí l?ch ng?u nhiên
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null; // Ch? frame ti?p theo
        }

        // Tr? camera v? v? trí ban ??u
        transform.localPosition = originalPos;
    }
}