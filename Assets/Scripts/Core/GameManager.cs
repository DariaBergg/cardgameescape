using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public CharacterData selectedCharacter;
    public CardVisuals cardVisuals;
    public List<CardData> playerDeck = new List<CardData>();
    public int currentHP;
    public int maxCardsPerTurn = 1;
    public int roomsVisited;

    public int MaxHP => selectedCharacter != null ? selectedCharacter.maxHP : 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CardVisuals.Instance = cardVisuals;
        ResetRun();
    }

    public void ResetRun()
    {
        roomsVisited = 0;
        maxCardsPerTurn = 1;
        if (selectedCharacter != null)
        {
            playerDeck = new List<CardData>(selectedCharacter.startingDeck);
            currentHP = selectedCharacter.maxHP;
        }
    }

    public CardData UpgradeCard(CardData card)
    {
        int index = playerDeck.IndexOf(card);
        if (index < 0) return card;
        var upgraded = card.CreateUpgradedCopy();
        playerDeck[index] = upgraded;
        return upgraded;
    }

    public int EliteChance(EnemyData elite)
    {
        if (elite == null) return 0;
        int chance = 0;
        foreach (var card in playerDeck)
            if (card != null && card.eliteMark == elite) chance += card.eliteChanceBonus;
        return Mathf.Min(100, chance);
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(MaxHP, currentHP + amount);
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
    }
}
