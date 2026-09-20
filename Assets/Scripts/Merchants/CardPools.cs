using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardPools", menuName = "CardGame/Card Pools")]
public class CardPools : ScriptableObject
{
    public static CardPools Instance { get; set; }

    [Tooltip("Все карты, которые могут выдавать купцы и сундуки")]
    public List<CardData> allCards = new List<CardData>();
    public CardData wound;
    public CardData debt;
    [Tooltip("Слабые карты, которыми враги подменяют карты в руке")]
    public List<CardData> weakCards = new List<CardData>();

    public CardData RandomWeak() => weakCards.Count > 0 ? weakCards[Random.Range(0, weakCards.Count)] : null;

    public List<CardData> OfRarity(CardRarity rarity, ICollection<CardData> exclude = null)
    {
        var list = new List<CardData>();
        foreach (var c in allCards)
            if (c != null && c.rarity == rarity && !c.unplayable && c.AvailableNow && (exclude == null || !exclude.Contains(c))) list.Add(c);
        return list;
    }

    public List<CardData> RandomOfRarity(CardRarity rarity, int count, ICollection<CardData> exclude = null)
    {
        var pool = OfRarity(rarity, exclude);
        Shuffle(pool);
        if (pool.Count > count) pool.RemoveRange(count, pool.Count - count);
        return pool;
    }

    public CardData RandomAny()
    {
        var pool = new List<CardData>();
        foreach (var c in allCards) if (c != null && !c.unplayable && c.AvailableNow) pool.Add(c);
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
