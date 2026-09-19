using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "CardGame/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    [Tooltip("Квадратная иллюстрация: вставляется в окно шаблона")]
    public Sprite illustration;
    [Tooltip("Готовая картинка всей карты (старый вариант, используется если нет иллюстрации)")]
    public Sprite artwork;
    public List<CardEffect> effects = new List<CardEffect>();

    [Header("Редкость и особые свойства")]
    public CardRarity rarity = CardRarity.Common;
    [Tooltip("Нельзя разыграть: карта-балласт (Рана, Долг)")]
    public bool unplayable;
    [Tooltip("Нельзя удалить обычными способами (Долг)")]
    public bool permanent;
    [Tooltip("Во что превращается при провале залога (если пусто — снимается усиление)")]
    public CardData failVariant;

    [Header("Метка элиты")]
    public EnemyData eliteMark;
    [Range(0, 100)] public int eliteChanceBonus;

    [System.NonSerialized] public int upgradeLevel;
    [System.NonSerialized] public CardData baseCard;

    public bool upgraded => upgradeLevel > 0;
    public CardData BaseCard => baseCard != null ? baseCard : this;
    public bool IsBad => unplayable;
    public bool IsAttack => HasEffect(CardEffectType.Damage) || HasEffect(CardEffectType.PierceDamage);
    public bool IsDefense => HasEffect(CardEffectType.Block);

    public bool IsMarked => eliteMark != null;

    public bool HasEffect(CardEffectType type)
    {
        foreach (var e in effects) if (e.type == type) return true;
        return false;
    }

    public CardData CreateUpgradedCopy()
    {
        var copy = Instantiate(this);
        copy.name = name;
        copy.cardName = cardName;
        copy.upgradeLevel = upgradeLevel + 1;
        copy.baseCard = BaseCard;
        copy.effects = new List<CardEffect>();
        foreach (var e in effects)
        {
            var u = new CardEffect { type = e.type, value = e.value, hits = e.hits, turns = e.turns, condition = e.condition };
            switch (e.type)
            {
                case CardEffectType.Damage: u.value += 2; break;
                case CardEffectType.Block: u.value += 2; break;
                case CardEffectType.Heal: u.value += 3; break;
                case CardEffectType.PoisonEnemy: u.value += 1; break;
                case CardEffectType.WeakenEnemy: u.turns += 1; break;
                case CardEffectType.BurnEnemy: u.value += 1; break;
                case CardEffectType.PierceDamage: u.value += 2; break;
                case CardEffectType.Thorns: u.value += 2; break;
                case CardEffectType.SelfDamage: u.value = Mathf.Max(0, u.value - 1); break;
                case CardEffectType.MoltenGuard: u.value += 1; break;
            }
            copy.effects.Add(u);
        }
        return copy;
    }

    public string EffectsSummary => string.Join(", ", effects.Select(e => e.Summary));

    public string RulesText => string.Join("\n", effects.Select(e => e.RulesText));

    public string ShortText => unplayable ? "Нельзя разыграть" : string.Join("\n", effects.Select(e => Capitalize(e.Summary)));

    public string UpgradePreviewShortText()
    {
        var upgraded = CreateUpgradedCopy();
        var lines = new List<string>();
        for (int i = 0; i < upgraded.effects.Count; i++)
            lines.Add(Capitalize(upgraded.effects[i].SummaryComparedTo(effects[i])));
        DestroyImmediate(upgraded);
        return string.Join("\n", lines);
    }

    public string UpgradePreviewTooltipText()
    {
        var upgraded = CreateUpgradedCopy();
        var lines = new List<string>();
        for (int i = 0; i < upgraded.effects.Count; i++)
            lines.Add(upgraded.effects[i].RulesTextComparedTo(effects[i]));
        DestroyImmediate(upgraded);
        string text = $"<b>{cardName}</b>\n<color=#ffd27f>{string.Join("\n", lines)}</color>";
        if (!string.IsNullOrEmpty(description)) text += $"\n\n<i>{description}</i>";
        if (IsMarked) text += $"\n\n<color=#d9a6ff>{MarkSummary}</color>";
        return text;
    }

    static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

    public string UpgradePreviewRulesText()
    {
        var upgraded = CreateUpgradedCopy();
        var lines = new List<string>();
        for (int i = 0; i < upgraded.effects.Count; i++)
            lines.Add(upgraded.effects[i].RulesTextComparedTo(effects[i]));
        DestroyImmediate(upgraded);
        return string.Join("\n", lines);
    }

    public string MarkSummary => IsMarked ? $"Метка: {eliteMark.enemyName} (+{eliteChanceBonus}% к шансу встречи)" : "";

    public string TooltipText
    {
        get
        {
            string rules = unplayable ? "Нельзя разыграть. Занимает место в руке." : RulesText;
            string text = $"<b>{cardName}</b> <color=#aaaaaa>({(rarity == CardRarity.Rare ? "редкая" : "обычная")})</color>\n<color=#ffd27f>{rules}</color>";
            if (permanent) text += "\n<color=#ff9090>Нельзя удалить обычным способом.</color>";
            if (!string.IsNullOrEmpty(description)) text += $"\n\n<i>{description}</i>";
            if (IsMarked) text += $"\n\n<color=#d9a6ff>{MarkSummary}</color>\nКаждая такая карта в колоде повышает шанс, что за фиолетовой дверью окажется {eliteMark.enemyName}.";
            return text;
        }
    }
}
