using UnityEngine;
using UnityEngine.UI;

public static class CardView
{
    static readonly Color MarkColor = new Color(0.45f, 0.15f, 0.6f, 0.95f);
    static readonly Color UpgradeColor = new Color(0.75f, 0.55f, 0.1f, 0.95f);

    public static Button Create(Transform parent, CardData card, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        Button button;
        var visuals = CardVisuals.Instance;
        bool composite = visuals != null && visuals.template != null;

        if (composite)
        {
            button = UIFactory.CreateButton(parent, "Card_" + card.cardName, "", 18, anchor, pos, size, new Color(0, 0, 0, 0));
            BuildComposite(button.transform, card, size, visuals);
        }
        else if (card.artwork != null)
        {
            button = UIFactory.CreateButton(parent, "Card_" + card.cardName, "", 18, anchor, pos, size, new Color(0, 0, 0, 0));
            var art = UIFactory.CreateRect(button.transform, "Art", new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = art.gameObject.AddComponent<Image>();
            img.sprite = card.artwork;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        else
        {
            string text = $"{card.cardName}\n\n<size=17>{card.EffectsSummary}</size>";
            button = UIFactory.CreateButton(parent, "Card_" + card.cardName, text, 20, anchor, pos, size, ColorFor(card));
        }

        if (card.upgraded && !composite)
        {
            var badge = UIFactory.CreateRect(button.transform, "UpgradeBadge", new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(size.x - 12, 28));
            badge.pivot = new Vector2(0.5f, 1f);
            var bg = badge.gameObject.AddComponent<Image>();
            bg.color = UpgradeColor;
            bg.raycastTarget = false;
            var label = UIFactory.CreateText(badge, "Label", $"+  {card.EffectsSummary}", 14, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 16, 28));
            label.color = new Color(1f, 0.97f, 0.85f);
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
        }

        if (card.IsMarked)
        {
            var badge = composite
                ? PlaceRect(button.transform, "MarkBadge", new Rect(0.12f, 0.9f, 0.76f, 0.068f), size)
                : UIFactory.CreateRect(button.transform, "MarkBadge", new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(size.x - 12, 30));
            var bg = badge.gameObject.AddComponent<Image>();
            bg.color = MarkColor;
            bg.raycastTarget = false;
            var label = UIFactory.CreateText(badge, "Label", $"{card.eliteMark.enemyName}  +{card.eliteChanceBonus}%", 14, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, badge.sizeDelta);
            label.color = new Color(0.95f, 0.85f, 1f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 6;
            label.resizeTextMaxSize = 14;
            label.raycastTarget = false;
        }

        button.gameObject.AddComponent<TooltipTrigger>().content = card.TooltipText;
        return button;
    }

    static void BuildComposite(Transform parent, CardData card, Vector2 size, CardVisuals visuals)
    {
        var window = PlaceRect(parent, "ArtWindow", visuals.artWindow, size);
        window.gameObject.AddComponent<RectMask2D>();
        if (card.illustration != null)
        {
            var art = UIFactory.CreateRect(window, "Art", new Vector2(0.5f, 0.5f), Vector2.zero, CoverSize(window.sizeDelta, card.illustration));
            var artImg = art.gameObject.AddComponent<Image>();
            artImg.sprite = card.illustration;
            artImg.raycastTarget = false;
        }
        else
        {
            var fill = window.gameObject.AddComponent<Image>();
            fill.color = ColorFor(card);
            fill.raycastTarget = false;
        }

        var frame = UIFactory.CreateRect(parent, "Template", new Vector2(0.5f, 0.5f), Vector2.zero, size);
        var frameImg = frame.gameObject.AddComponent<Image>();
        frameImg.sprite = visuals.template;
        frameImg.preserveAspect = true;
        frameImg.raycastTarget = false;

        var nameRect = PlaceRect(parent, "Name", visuals.nameArea, size);
        var name = nameRect.gameObject.AddComponent<Text>();
        name.font = UIFactory.Font;
        name.fontStyle = FontStyle.Bold;
        name.alignment = TextAnchor.MiddleCenter;
        name.color = visuals.nameColor;
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = 6;
        name.resizeTextMaxSize = Mathf.RoundToInt(size.x * 0.085f);
        name.horizontalOverflow = HorizontalWrapMode.Wrap;
        name.verticalOverflow = VerticalWrapMode.Truncate;
        name.text = card.cardName;
        name.raycastTarget = false;

        var textRect = PlaceRect(parent, "Rules", visuals.textArea, size);
        var rules = textRect.gameObject.AddComponent<Text>();
        rules.font = UIFactory.Font;
        rules.alignment = TextAnchor.MiddleCenter;
        rules.color = visuals.textColor;
        rules.resizeTextForBestFit = true;
        rules.resizeTextMinSize = 6;
        rules.resizeTextMaxSize = Mathf.RoundToInt(size.x * 0.08f);
        rules.horizontalOverflow = HorizontalWrapMode.Wrap;
        rules.verticalOverflow = VerticalWrapMode.Truncate;
        rules.text = card.RulesText;
        rules.raycastTarget = false;
    }

    static RectTransform PlaceRect(Transform parent, string name, Rect fraction, Vector2 size)
    {
        var rect = UIFactory.CreateRect(parent, name, new Vector2(0, 1), new Vector2(fraction.x * size.x, -fraction.y * size.y), new Vector2(fraction.width * size.x, fraction.height * size.y));
        rect.pivot = new Vector2(0, 1);
        return rect;
    }

    static Vector2 CoverSize(Vector2 window, Sprite sprite)
    {
        float aspect = sprite.rect.width / sprite.rect.height;
        return window.x / window.y > aspect
            ? new Vector2(window.x, window.x / aspect)
            : new Vector2(window.y * aspect, window.y);
    }

    public static void AddUpgradePreview(Button button, CardData card)
    {
        var name = button.transform.Find("Name")?.GetComponent<Text>();
        var rules = button.transform.Find("Rules")?.GetComponent<Text>();
        if (name == null || rules == null) return;
        var preview = button.gameObject.AddComponent<UpgradePreview>();
        preview.Init(name, rules, card);
    }

    class UpgradePreview : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        Text nameText;
        Text rulesText;
        string originalName;
        string originalRules;
        string previewName;
        string previewRules;

        public void Init(Text name, Text rules, CardData card)
        {
            nameText = name;
            rulesText = rules;
            originalName = card.cardName;
            originalRules = card.RulesText;
            previewName = card.cardName + "+";
            previewRules = card.UpgradePreviewRulesText();
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            nameText.text = previewName;
            rulesText.text = previewRules;
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            nameText.text = originalName;
            rulesText.text = originalRules;
        }
    }

    public static void SetInteractable(Button button, bool interactable)
    {
        button.interactable = interactable;
        var tint = interactable ? Color.white : new Color(0.45f, 0.45f, 0.45f);
        foreach (var name in new[] { "Art", "Template", "ArtWindow/Art" })
        {
            var t = button.transform.Find(name);
            if (t != null) t.GetComponent<Image>().color = tint;
        }
    }

    static Color ColorFor(CardData card)
    {
        if (card.effects.Count == 0) return Color.gray;
        switch (card.effects[0].type)
        {
            case CardEffectType.Damage: return new Color(0.65f, 0.25f, 0.2f);
            case CardEffectType.Block: return new Color(0.2f, 0.4f, 0.7f);
            case CardEffectType.Heal: return new Color(0.2f, 0.6f, 0.3f);
            case CardEffectType.PoisonEnemy: return new Color(0.3f, 0.55f, 0.2f);
            case CardEffectType.WeakenEnemy: return new Color(0.5f, 0.3f, 0.6f);
            default: return Color.gray;
        }
    }
}
