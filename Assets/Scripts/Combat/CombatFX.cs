using System.Collections;
using UnityEngine;

public class CombatFX : MonoBehaviour
{
    public static CombatFX Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public IEnumerator Lunge(Transform target, Vector3 offset, float duration)
    {
        if (target == null) yield break;
        Vector3 start = target.position;
        float half = duration * 0.5f;
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            if (target == null) yield break;
            target.position = Vector3.Lerp(start, start + offset, t / half);
            yield return null;
        }
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            if (target == null) yield break;
            target.position = Vector3.Lerp(start + offset, start, t / half);
            yield return null;
        }
        target.position = start;
    }

    public void Flash(SpriteRenderer sr, Color color, float duration = 0.2f)
    {
        if (sr != null) StartCoroutine(FlashRoutine(sr, color, duration));
    }

    IEnumerator FlashRoutine(SpriteRenderer sr, Color color, float duration)
    {
        Color original = sr.color;
        sr.color = color;
        yield return new WaitForSeconds(duration);
        if (sr != null) sr.color = original;
    }

    public void Shake(Transform target, float amount = 0.15f, float duration = 0.25f)
    {
        if (target != null) StartCoroutine(ShakeRoutine(target, amount, duration));
    }

    IEnumerator ShakeRoutine(Transform target, float amount, float duration)
    {
        Vector3 origin = target.position;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (target == null) yield break;
            target.position = origin + (Vector3)(Random.insideUnitCircle * amount);
            yield return null;
        }
        if (target != null) target.position = origin;
    }

    public IEnumerator FadeOut(SpriteRenderer sr, float duration)
    {
        if (sr == null) yield break;
        Color start = sr.color;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            sr.color = new Color(start.r, start.g, start.b, Mathf.Lerp(start.a, 0f, t / duration));
            yield return null;
        }
        if (sr != null) sr.color = new Color(start.r, start.g, start.b, 0f);
    }

    public void FloatingText(Vector3 worldPos, string text, Color color)
    {
        StartCoroutine(FloatingTextRoutine(worldPos, text, color));
    }

    IEnumerator FloatingTextRoutine(Vector3 worldPos, string text, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = worldPos;
        var tm = go.AddComponent<TextMesh>();
        tm.font = UIFactory.Font;
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var mr = go.GetComponent<MeshRenderer>();
        mr.material = UIFactory.Font.material;
        mr.sortingOrder = 20;

        const float duration = 0.9f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            go.transform.position = worldPos + Vector3.up * (k * 1.2f);
            tm.color = new Color(color.r, color.g, color.b, 1f - k * k);
            yield return null;
        }
        Destroy(go);
    }
}
