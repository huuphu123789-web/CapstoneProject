using UnityEngine;
using System.Collections;

public class GhostFlicker : MonoBehaviour
{
    public float visibleTime = 0.3f;
    public float invisibleTime = 0.3f;

    private Renderer[] renderers;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        StartCoroutine(Flicker());
    }

    IEnumerator Flicker()
    {
        while (true)
        {
            // Hiện
            SetVisible(true);
            yield return new WaitForSeconds(visibleTime);

            // Biến mất
            SetVisible(false);
            yield return new WaitForSeconds(invisibleTime);
        }
    }

    void SetVisible(bool value)
    {
        foreach (Renderer r in renderers)
            r.enabled = value;
    }
}