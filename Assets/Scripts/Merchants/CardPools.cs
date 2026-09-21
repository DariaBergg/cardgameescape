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

    // Рана в стиле героя, если у него есть своя
    public CardData WoundFor(CharacterData c) => c != null && c.woundCard != null ? c.woundCard : wound;

    // Слабая карта для порчи руки — только из карт текущего героя
    public CardData RandomWeak()
    {
        var pool = weakCards.FindAll(c => c != null && c.AvailableNow);
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }

    public List<CardData> OfRarity(CardRarity rarity, ICollection<CardData> exclude = null)
    {
        var list = new List<CardData>();
        foreach (var c in allCards)
            if (c != null && c.rarity == rarity && !c.unplayable && !c.eventOnly && c.AvailableNow && (exclude == null || !exclude.Contains(c))) list.Add(c);
        return list;
    }

    public List<CardData> RandomOfRarity(CardRarity rarity, int count, ICollection<CardData> exclude = null)
    {
        var pool = OfRarity(rarity, exclude);
        Shuffle(pool);
        if (pool.Count > count) pool.RemoveRange(count, pool.Count - count);
        return pool;
    }

    // Редкие карты из событий: сначала особые «событийные», если их не хватает — обычные редкие
    public List<CardData> EventRares(int count)
    {
        var pool = new List<CardData>();
        foreach (var c in allCards) if (c != null && c.eventOnly && !c.unplayable && c.AvailableNow) pool.Add(c);
        Shuffle(pool);
        if (pool.Count > count) pool.RemoveRange(count, pool.Count - count);
        if (pool.Count < count) pool.AddRange(RandomOfRarity(CardRarity.Rare, count - pool.Count, pool));
        return pool;
    }

    public CardData RandomAny()
    {
        var pool = new List<CardData>();
        foreach (var c in allCards) if (c != null && !c.unplayable && !c.eventOnly && c.AvailableNow) pool.Add(c);
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
