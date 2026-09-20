using System;
using UnityEngine;
using UnityEngine.UI;

public class UnitFrame : MonoBehaviour
{
    RectTransform rect;
    RectTransform canvasRect;
    Text nameText;
    RectTransform hpFill;
    Text hpText;
    GameObject blockBadge;
    Text blockText;
    Text statusText;
    Func<Vector3> worldAnchor;
    Vector4 inner; // жёлоб внутри рамки в долях (xMin, yMin, xMax, yMax)

    const float Width = 260f;
    const float BarHeight = 34f;

    public static UnitFrame Create(Transform canvas, string name, Color barColor, bool enemy = false)
    {
        var rect = UIFactory.CreateRect(canvas, name, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, 80));
        rect.pivot = new Vector2(0.5f, 0f);
        var frame = rect.gameObject.AddComponent<UnitFrame>();
        frame.rect = rect;
        frame.canvasRect = canvas.GetComponent<RectTransform>();
        frame.Build(barColor, enemy);
        return frame;
    }

    void Build(Color barColor, bool enemy)
    {
        var skin = UISkin.Instance;
        bool skinned = skin != null && skin.hpFrame != null && skin.hpFill != null;
        inner = skinned ? skin.hpFrameInner : new Vector4(0.01f, 0.1f, 0.99f, 0.9f);

        nameText = UIFactory.CreateText(rect, "Name", "", 16, TextAnchor.LowerCenter, new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(Width, 20));
        nameText.color = new Color(1f, 1f, 1f, 0.9f);
        nameText.font = UIFactory.TitleFont;
        nameText.raycastTarget = false;
        UIFactory.AddShadow(nameText);

        // Полоска: без скина — тёмная подложка под заливкой; со скином — рамка поверх заливки
        var barPos = new Vector2(0, -22);
        if (!skinned)
        {
            var barBg = UIFactory.CreateRect(rect, "BarBg", new Vector2(0.5f, 1f), barPos, new Vector2(Width, BarHeight));
            var bgImg = barBg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
            bgImg.raycastTarget = false;
        }

        // Заливка растёт слева направо внутри «жёлоба»
        var fillPos = new Vector2(-Width / 2f + inner.x * Width, barPos.y - BarHeight * (1f - (inner.y + inner.w) / 2f));
        hpFill = UIFactory.CreateRect(rect, "Fill", new Vector2(0.5f, 1f), fillPos, Vector2.zero);
        hpFill.pivot = new Vector2(0f, 0.5f);
        var fillImg = hpFill.gameObject.AddComponent<Image>();
        fillImg.raycastTarget = false;
        UISkin.Apply(fillImg, skinned ? (enemy && skin.hpFillEnemy != null ? skin.hpFillEnemy : skin.hpFill) : null, barColor);

        if (skinned)
        {
            var frame = UIFactory.CreateRect(rect, "Frame", new Vector2(0.5f, 1f), barPos, new Vector2(Width, BarHeight));
            var frameImg = frame.gameObject.AddComponent<Image>();
            frameImg.raycastTarget = false;
            UISkin.Apply(frameImg, skin.hpFrame, Color.clear);
        }

        hpText = UIFactory.CreateText(rect, "HpText", "", 14, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), barPos, new Vector2(Width, BarHeight));
        hpText.fontStyle = FontStyle.Bold;
        hpText.raycastTarget = false;
        UIFactory.AddShadow(hpText);

        // Блок: круглый щит у правого края полоски
        var badge = UIFactory.CreateRect(rect, "Block", new Vector2(0.5f, 1f), new Vector2(Width / 2f + 4f, -22 - BarHeight / 2f), new Vector2(44, 44));
        badge.pivot = new Vector2(0.5f, 0.5f);
        var badgeImg = badge.gameObject.AddComponent<Image>();
        badgeImg.raycastTarget = false;
        if (skin != null && skin.blockIcon != null)
        {
            badgeImg.sprite = skin.blockIcon;
            badgeImg.preserveAspect = true;
        }
        else badgeImg.color = new Color(0.25f, 0.5f, 0.9f, 0.95f);
        blockText = UIFactory.CreateText(badge, "Text", "", 17, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(44, 44));
        blockText.fontStyle = FontStyle.Bold;
        blockText.raycastTarget = false;
        UIFactory.AddShadow(blockText);
        blockBadge = badge.gameObject;
        blockBadge.SetActive(false);

        statusText = UIFactory.CreateText(rect, "Status", "", 14, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0, -22 - BarHeight - 4), new Vector2(Width + 80, 24));
        statusText.color = new Color(0.8f, 1f, 0.6f);
        statusText.raycastTarget = false;
        UIFactory.AddShadow(statusText);
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
        float innerWidth = (inner.z - inner.x) * Width;
        float innerHeight = (inner.w - inner.y) * BarHeight;
        hpFill.sizeDelta = new Vector2(innerWidth * ratio, innerHeight);
        hpFill.gameObject.SetActive(ratio > 0.001f);
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
