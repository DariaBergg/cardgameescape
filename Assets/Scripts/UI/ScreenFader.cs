using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    static ScreenFader instance;
    Image overlay;

    public static ScreenFader Get()
    {
        if (instance != null) return instance;
        var canvas = UIFactory.CreateCanvas("FadeCanvas", 90);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        instance = canvas.gameObject.AddComponent<ScreenFader>();
        var rect = UIFactory.CreateFullscreenPanel(canvas.transform, "Overlay", new Color(0, 0, 0, 0));
        instance.overlay = rect.GetComponent<Image>();
        instance.overlay.raycastTarget = false;
        return instance;
    }

    public IEnumerator FadeTo(float alpha, float duration)
    {
        float start = overlay.color.a;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            overlay.color = new Color(0, 0, 0, Mathf.Lerp(start, alpha, t / duration));
            yield return null;
        }
        overlay.color = new Color(0, 0, 0, alpha);
    }
}
