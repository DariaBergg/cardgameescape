using UnityEngine;
using UnityEngine.UI;

public static class CardView
{
    static readonly Color MarkColor = new Color(0.45f, 0.15f, 0.6f, 0.95f);
    static readonly Color UpgradeColor = new Color(0.75f, 0.55f, 0.1f, 0.95f);

    public static Button Create(Transform parent, CardData card, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        Button button;
        if (card.artwork == null)
        {
            string text = $"{card.cardName}\n\n<size=17>{card.EffectsSummary}</size>";
            button = UIFactory.CreateButton(parent, "Card_" + card.cardName, text, 20, anchor, pos, size, ColorFor(card));
        }
        else
        {
            button = UIFactory.CreateButton(parent, "Card_" + card.cardName, "", 18, anchor, pos, size, new Color(0, 0, 0, 0));
            var art = UIFactory.CreateRect(button.transform, "Art", new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = art.gameObject.AddComponent<Image>();
            img.sprite = card.artwork;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        if (card.upgraded)
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
            var badge = UIFactory.CreateRect(button.transform, "MarkBadge", new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(size.x - 12, 30));
            var bg = badge.gameObject.AddComponent<Image>();
            bg.color = MarkColor;
            bg.raycastTarget = false;
            var label = UIFactory.CreateText(badge, "Label", $"{card.eliteMark.enemyName}  +{card.eliteChanceBonus}%", 14, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 16, 30));
            label.color = new Color(0.95f, 0.85f, 1f);
            label.raycastTarget = false;
        }

        button.gameObject.AddComponent<TooltipTrigger>().content = card.TooltipText;
        return button;
    }

    public static void SetInteractable(Button button, bool interactable)
    {
        button.interactable = interactable;
        var art = button.transform.Find("Art");
        if (art != null) art.GetComponent<Image>().color = interactable ? Color.white : new Color(0.45f, 0.45f, 0.45f);
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
