using System;
using UnityEngine;
using UnityEngine.UI;

public class UnitFrame : MonoBehaviour
{
    RectTransform rect;
    RectTransform canvasRect;
    Text nameText;
    Image hpFill;
    Text hpText;
    GameObject blockBadge;
    Text blockText;
    Text statusText;
    Func<Vector3> worldAnchor;

    const float Width = 220f;
    const float BarHeight = 22f;

    public static UnitFrame Create(Transform canvas, string name, Color barColor)
    {
        var rect = UIFactory.CreateRect(canvas, name, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, 70));
        rect.pivot = new Vector2(0.5f, 0f);
        var frame = rect.gameObject.AddComponent<UnitFrame>();
        frame.rect = rect;
        frame.canvasRect = canvas.GetComponent<RectTransform>();
        frame.Build(barColor);
        return frame;
    }

    void Build(Color barColor)
    {
        nameText = UIFactory.CreateText(rect, "Name", "", 16, TextAnchor.LowerCenter, new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(Width, 20));
        nameText.color = new Color(1f, 1f, 1f, 0.85f);
        nameText.raycastTarget = false;

        var barBg = UIFactory.CreateRect(rect, "BarBg", new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(Width - 40, BarHeight));
        var bgImg = barBg.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        bgImg.raycastTarget = false;

        var fill = UIFactory.CreateRect(barBg, "Fill", new Vector2(0f, 0.5f), new Vector2(2, 0), new Vector2(Width - 44, BarHeight - 4));
        fill.pivot = new Vector2(0f, 0.5f);
        hpFill = fill.gameObject.AddComponent<Image>();
        hpFill.color = barColor;
        hpFill.raycastTarget = false;

        hpText = UIFactory.CreateText(barBg, "HpText", "", 15, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width - 40, BarHeight));
        hpText.fontStyle = FontStyle.Bold;
        hpText.raycastTarget = false;

        var badge = UIFactory.CreateRect(rect, "Block", new Vector2(1f, 1f), new Vector2(0, -22), new Vector2(36, BarHeight));
        badge.pivot = new Vector2(1f, 1f);
        var badgeImg = badge.gameObject.AddComponent<Image>();
        badgeImg.color = new Color(0.25f, 0.5f, 0.9f, 0.95f);
        badgeImg.raycastTarget = false;
        blockText = UIFactory.CreateText(badge, "Text", "", 15, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, BarHeight));
        blockText.fontStyle = FontStyle.Bold;
        blockText.raycastTarget = false;
        blockBadge = badge.gameObject;
        blockBadge.SetActive(false);

        statusText = UIFactory.CreateText(rect, "Status", "", 14, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0, -46), new Vector2(Width + 80, 24));
        statusText.color = new Color(0.8f, 1f, 0.6f);
        statusText.raycastTarget = false;
    }

    public void SetAnchor(Func<Vector3> anchor)
    {
        worldAnchor = anchor;
        UpdatePosition();
    }

    public void Set(string unitName, int hp, int maxHp, int block, string statuses)
    {
        nameText.text = unitName;
        hpText.text = $"{hp} / {maxHp}";
        float ratio = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
        hpFill.rectTransform.sizeDelta = new Vector2((Width - 44) * ratio, BarHeight - 4);
        blockBadge.SetActive(block > 0);
        blockText.text = block.ToString();
        statusText.text = statuses ?? "";
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
    }

    void LateUpdate()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        if (worldAnchor == null || Camera.main == null) return;
        Vector2 screen = Camera.main.WorldToScreenPoint(worldAnchor());
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
        rect.anchoredPosition = local;
    }
}
