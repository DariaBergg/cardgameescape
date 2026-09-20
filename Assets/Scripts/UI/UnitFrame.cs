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
    bool isEnemy;
    RectTransform momentumRow;
    RectTransform statusRow;
    readonly System.Collections.Generic.List<GameObject> statusIcons = new System.Collections.Generic.List<GameObject>();
    readonly System.Collections.Generic.List<Image> momentumPips = new System.Collections.Generic.List<Image>();
    static readonly Color PipOn = new Color(0.6f, 0.85f, 1f);
    static readonly Color PipOff = new Color(0.12f, 0.12f, 0.16f, 0.85f);

    const float Width = 260f;
    const float BarHeight = 34f;

    public static UnitFrame Create(Transform canvas, string name, Color barColor, bool enemy = false)
    {
        var rect = UIFactory.CreateRect(canvas, name, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, 80));
        rect.pivot = new Vector2(0.5f, 0f);
        var frame = rect.gameObject.AddComponent<UnitFrame>();
        frame.rect = rect;
        frame.canvasRect = canvas.GetComponent<RectTransform>();
        frame.isEnemy = enemy;
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

        // Шкала Замаха: деления над именем (видны только у героя с Замахом)
        momentumRow = UIFactory.CreateRect(rect, "Momentum", new Vector2(0.5f, 1f), new Vector2(0, 22), new Vector2(Width, 28));
        momentumRow.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f); // ловим наведение на шкалу
        var momentumTip = momentumRow.gameObject.AddComponent<TooltipTrigger>();
        momentumTip.content = Glossary.Explain("Замах");
        momentumRow.gameObject.SetActive(false);

        // Значки статусов под полоской: иконка + число ходов, подсказка по наведению
        statusRow = UIFactory.CreateRect(rect, "Statuses", new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(Width, BarHeight));

        statusText = UIFactory.CreateText(rect, "Status", "", 14, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0, -22 - BarHeight - 48), new Vector2(Width + 80, 24));
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

    string statusSignature = "";

    // Статусы столбиком сбоку от полоски (у героя справа, у врага слева). Перестраиваются только при изменении.
    public void SetStatuses(System.Collections.Generic.List<CombatManager.StatusInfo> statuses)
    {
        var sb = new System.Text.StringBuilder();
        if (statuses != null) foreach (var st in statuses) sb.Append(st.id).Append(':').Append(st.turns).Append('|');
        string signature = sb.ToString();
        if (signature == statusSignature) return;
        statusSignature = signature;

        foreach (var go in statusIcons) Destroy(go);
        statusIcons.Clear();
        if (statuses == null || statuses.Count == 0) return;
        const float size = 46f, gap = 8f;
        float columnX = isEnemy ? Width / 2f + 62f : -Width / 2f - 34f; // герой — слева, враг — справа (за щитом блока)
        var skin = UISkin.Instance;
        for (int i = 0; i < statuses.Count; i++)
        {
            var st = statuses[i];
            var icon = UIFactory.CreateRect(statusRow, "Status_" + st.id, new Vector2(0.5f, 1f), new Vector2(columnX, -i * (size + gap)), new Vector2(size, size));
            icon.pivot = new Vector2(0.5f, 1f);
            var img = icon.gameObject.AddComponent<Image>();
            var sprite = skin != null ? skin.StatusIcon(st.id) : null;
            if (sprite != null) { img.sprite = sprite; img.preserveAspect = true; }
            else
            {
                // Пока нет нарисованной иконки — цветной кружок с подписью
                img.sprite = skin != null ? skin.blockIcon : null;
                img.preserveAspect = true;
                img.color = st.color;
                var label = UIFactory.CreateText(icon, "Label", st.label, 9, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 10, size));
                label.raycastTarget = false;
                UIFactory.AddShadow(label);
            }
            if (st.turns > 0)
            {
                var num = UIFactory.CreateText(icon, "Turns", st.turns.ToString(), 14, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(6, -6), new Vector2(24, 18));
                num.fontStyle = FontStyle.Bold;
                num.raycastTarget = false;
                UIFactory.AddShadow(num, 1f);
            }
            var trigger = icon.gameObject.AddComponent<TooltipTrigger>();
            trigger.content = st.tooltip;
            trigger.preferLeft = !isEnemy;
            statusIcons.Add(icon.gameObject);
        }
    }

    // current < 0 — скрыть шкалу
    public void SetMomentum(int current, int max)
    {
        if (current < 0 || max <= 0) { momentumRow.gameObject.SetActive(false); return; }
        momentumRow.gameObject.SetActive(true);
        const float pipSize = 20f, gap = 10f;
        while (momentumPips.Count < max)
        {
            var pip = UIFactory.CreateRect(momentumRow, "Pip", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(pipSize, pipSize));
            var img = pip.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            var bolt = UISkin.Get(k => k.momentumIcon);
            if (bolt != null) { img.sprite = bolt; img.preserveAspect = true; pip.sizeDelta = new Vector2(pipSize * 1.4f, pipSize * 1.4f); }
            else
            {
                var outline = pip.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.05f, 0.05f, 0.08f, 1f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            momentumPips.Add(img);
        }
        float startX = -(max - 1) * (pipSize + gap) / 2f;
        for (int i = 0; i < momentumPips.Count; i++)
        {
            bool used = i < max;
            momentumPips[i].gameObject.SetActive(used);
            if (!used) continue;
            momentumPips[i].rectTransform.anchoredPosition = new Vector2(startX + i * (pipSize + gap), 0);
            bool bolt = momentumPips[i].sprite != null;
            momentumPips[i].color = i < current ? (bolt ? Color.white : PipOn) : (bolt ? new Color(0.35f, 0.35f, 0.45f, 0.8f) : PipOff);
        }
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
