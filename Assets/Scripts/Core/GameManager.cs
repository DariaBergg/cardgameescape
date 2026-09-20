using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public CharacterData selectedCharacter;
    [Tooltip("Герои на экране выбора")]
    public List<CharacterData> characters = new List<CharacterData>();
    public CardVisuals cardVisuals;
    public CardPools cardPools;
    public UISkin uiSkin;
    public List<CardData> playerDeck = new List<CardData>();
    public int currentHP;
    public int maxCardsPerTurn = 1;
    public int roomsVisited;
    public int combatsWon;

    [Header("Забег")]
    public int gold;
    public int maxHPBonus;
    public List<string> items = new List<string>();
    public List<string> relics = new List<string>();
    public List<Obligation> obligations = new List<Obligation>();
    public Dictionary<string, string> flags = new Dictionary<string, string>();

    public event Action OnHealed;
    public event Action<Obligation, bool> OnObligationResolved;

    public int MaxHP => (selectedCharacter != null ? selectedCharacter.maxHP : 0) + maxHPBonus;

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
        CardPools.Instance = cardPools;
        UISkin.Instance = uiSkin;
        ResetRun();
    }

    public void ResetRun()
    {
        roomsVisited = 0;
        combatsWon = 0;
        maxCardsPerTurn = 1;
        gold = 0;
        maxHPBonus = 0;
        items.Clear();
        relics.Clear();
        obligations.Clear();
        flags.Clear();
        bonusCardCooldownUntilRoom = 0;
        if (selectedCharacter != null)
        {
            playerDeck = new List<CardData>(selectedCharacter.startingDeck);
            currentHP = selectedCharacter.maxHP;
        }
    }

    public void SelectCharacter(CharacterData character)
    {
        selectedCharacter = character;
        ResetRun();
    }

    // --- Cards ---

    public bool HasCard(CardData card) => card != null && playerDeck.Exists(c => c != null && c.BaseCard == card.BaseCard);

    // Особая карта героя (Ярость): выпадает ли шанс предложить её сейчас. Копий может быть несколько,
    // но после выдачи несколько комнат её не предлагают.
    int bonusCardCooldownUntilRoom;

    public CardData RollBonusCard()
    {
        var c = selectedCharacter;
        if (c == null || c.bonusCard == null || roomsVisited < bonusCardCooldownUntilRoom) return null;
        return UnityEngine.Random.Range(0, 100) < c.bonusCardChance ? c.bonusCard : null;
    }

    public void OnBonusCardTaken()
    {
        if (selectedCharacter != null) bonusCardCooldownUntilRoom = roomsVisited + selectedCharacter.bonusCardCooldownRooms;
    }

    public CardData UpgradeCard(CardData card)
    {
        int index = playerDeck.IndexOf(card);
        if (index < 0) return card;
        var upgraded = card.CreateUpgradedCopy();
        playerDeck[index] = upgraded;
        return upgraded;
    }

    public void ReplaceCard(CardData oldCard, CardData newCard)
    {
        int index = playerDeck.IndexOf(oldCard);
        if (index < 0) return;
        if (newCard == null) playerDeck.RemoveAt(index);
        else playerDeck[index] = newCard;
    }

    public bool RemoveCard(CardData card, bool force = false)
    {
        if (card == null || (card.permanent && !force)) return false;
        return playerDeck.Remove(card);
    }

    public int EliteChance(EnemyData elite)
    {
        if (elite == null) return 0;
        int chance = 0;
        foreach (var card in playerDeck)
            if (card != null && card.eliteMark == elite) chance += card.eliteChanceBonus;
        return Mathf.Min(100, chance);
    }

    // --- HP ---

    public void Heal(int amount)
    {
        int before = currentHP;
        currentHP = Mathf.Min(MaxHP, currentHP + amount);
        if (currentHP > before)
        {
            OnPlayerHealed();
            OnHealed?.Invoke();
        }
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
    }

    public void LoseHPSafe(int amount)
    {
        currentHP = Mathf.Max(1, currentHP - amount);
    }

    public void AddMaxHP(int amount)
    {
        maxHPBonus += amount;
        currentHP = Mathf.Clamp(currentHP + amount, 1, MaxHP);
    }

    // --- Flags, items, relics ---

    public string Flag(string key) => flags.TryGetValue(key, out var v) ? v : null;
    public void SetFlag(string key, string value) => flags[key] = value;
    public bool HasFlag(string key, string value) => Flag(key) == value;

    public bool HasItem(string id) => items.Contains(id);
    public void GiveItem(string id) { if (!items.Contains(id)) items.Add(id); }
    public void TakeItem(string id) => items.Remove(id);

    public bool HasRelic(string id) => relics.Contains(id);
    public void GiveRelic(string id) { if (!relics.Contains(id)) relics.Add(id); }

    // --- Obligations ---

    public void AddObligation(Obligation obligation) => obligations.Add(obligation);

    public bool HasObligation(ObligationType type) => obligations.Exists(o => o.type == type);

    public void OnRoomEntered()
    {
        for (int i = obligations.Count - 1; i >= 0; i--)
        {
            var o = obligations[i];
            o.roomsRemaining--;
            if (o.roomsRemaining > 0) continue;
            obligations.RemoveAt(i);
            ResolveObligation(o);
        }
    }

    public void OnCombatWon()
    {
        combatsWon++;
        foreach (var o in obligations)
            if (o.type == ObligationType.PledgeFights) o.fightsWon++;
    }

    public void OnPlayerHealed()
    {
        for (int i = obligations.Count - 1; i >= 0; i--)
        {
            var o = obligations[i];
            if (o.type != ObligationType.PledgeNoHeal) continue;
            obligations.RemoveAt(i);
            FailPledge(o);
        }
    }

    void ResolveObligation(Obligation o)
    {
        switch (o.type)
        {
            case ObligationType.Credit:
                currentHP = Mathf.Max(1, currentHP - o.hpCost);
                OnObligationResolved?.Invoke(o, false);
                break;
            case ObligationType.PledgeFights:
                if (o.fightsWon >= o.fightsRequired) OnObligationResolved?.Invoke(o, true);
                else FailPledge(o);
                break;
            case ObligationType.PledgeNoHeal:
                OnObligationResolved?.Invoke(o, true);
                break;
        }
    }

    void FailPledge(Obligation o)
    {
        if (o.card != null && playerDeck.Contains(o.card))
        {
            var replacement = o.card.failVariant != null ? o.card.failVariant : o.card.BaseCard.failVariant != null ? o.card.BaseCard.failVariant : o.card.BaseCard;
            ReplaceCard(o.card, replacement);
        }
        OnObligationResolved?.Invoke(o, false);
    }

    public void CancelObligation(Obligation o)
    {
        obligations.Remove(o);
    }
}
