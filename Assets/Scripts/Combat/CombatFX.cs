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

    public IEnumerator Flutter(Transform target, float duration, float radiusX = 1.6f, float radiusY = 0.7f)
    {
        if (target == null) yield break;
        Vector3 start = target.position;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (target == null) yield break;
            float k = t / duration * Mathf.PI * 2f;
            target.position = start + new Vector3(Mathf.Sin(k) * radiusX, Mathf.Sin(k * 2f) * radiusY, 0);
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

    // Молния сверху в цель: ломаная линия, мигает и гаснет
    public IEnumerator Lightning(Vector3 target, float duration = 0.35f)
    {
        var go = new GameObject("Lightning");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 25;
        lr.widthMultiplier = 0.12f;
        lr.numCapVertices = 2;
        lr.useWorldSpace = true;

        Vector3 start = new Vector3(target.x + Random.Range(-0.8f, 0.8f), 7.5f, 0);
        const int segments = 9;
        var points = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float k = i / (float)segments;
            var p = Vector3.Lerp(start, target, k);
            if (i > 0 && i < segments) p.x += Random.Range(-0.6f, 0.6f);
            points[i] = p;
        }
        lr.positionCount = points.Length;
        lr.SetPositions(points);

        var glow = new Color(0.75f, 0.9f, 1f, 1f);
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            float flicker = Mathf.PerlinNoise(t * 60f, 0f) * 0.5f + 0.5f;
            var c = new Color(glow.r, glow.g, glow.b, (1f - k) * flicker);
            lr.startColor = Color.white * (1f - k);
            lr.endColor = c;
            lr.widthMultiplier = 0.12f + 0.08f * flicker;
            yield return null;
        }
        Destroy(go);
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
