using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatUI : MonoBehaviour
{
    CombatManager combat;
    GameObject root;
    RectTransform rootRect;
    Text enemyText;
    RectTransform intentBadge;
    Image intentBadgeBg;
    Image intentIcon;
    Text intentText;
    TooltipTrigger intentTooltip;
    static readonly Vector2 IntentIconSize = new Vector2(96, 96);
    static readonly Vector2 IntentWordSize = new Vector2(200, 44);
    Text logText;
    Text playerText;
    Text statusText;
    Text turnText;
    RectTransform handArea;
    Button endTurnButton;
    RectTransform rewardPanel;
    RectTransform rewardCardsArea;
    RectTransform defeatPanel;
    readonly List<GameObject> cardButtons = new List<GameObject>();

    public static CombatUI Create(CombatManager combat)
    {
        var canvas = UIFactory.CreateCanvas("CombatCanvas", 10);
        var ui = canvas.gameObject.AddComponent<CombatUI>();
        ui.combat = combat;
        ui.Build(canvas.transform);
        return ui;
    }

    void Build(Transform canvas)
    {
        root = new GameObject("Root", typeof(RectTransform));
        rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        enemyText = UIFactory.CreateText(root.transform, "EnemyText", "", 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(700, 70));

        intentBadge = UIFactory.CreateRect(root.transform, "IntentBadge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 44));
        intentBadge.pivot = new Vector2(0.5f, 0f);
        intentBadgeBg = intentBadge.gameObject.AddComponent<Image>();
        intentBadgeBg.color = new Color(0.35f, 0.08f, 0.08f, 0.95f);
        intentTooltip = intentBadge.gameObject.AddComponent<TooltipTrigger>();
        intentText = UIFactory.CreateText(intentBadge, "Label", "", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 44));
        intentText.color = new Color(1f, 0.75f, 0.7f);
        intentText.raycastTarget = false;
        var iconRect = UIFactory.CreateRect(intentBadge, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, IntentIconSize);
        intentIcon = iconRect.gameObject.AddComponent<Image>();
        intentIcon.preserveAspect = true;
        intentIcon.raycastTarget = false;

        logText = UIFactory.CreateText(root.transform, "LogText", "", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(900, 70));
        logText.color = new Color(1f, 0.92f, 0.6f);

        playerText = UIFactory.CreateText(root.transform, "PlayerText", "", 24, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(20, 330), new Vector2(600, 32));
        statusText = UIFactory.CreateText(root.transform, "StatusText", "", 20, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(20, 300), new Vector2(700, 32));
        statusText.color = new Color(0.75f, 1f, 0.5f);
        turnText = UIFactory.CreateText(root.transform, "TurnText", "", 22, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(20, 268), new Vector2(600, 32));

        handArea = UIFactory.CreateRect(root.transform, "HandArea", new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(1000, 260));

        endTurnButton = UIFactory.CreateButton(root.transform, "EndTurnButton", "Пропустить ход", 22, new Vector2(1, 0), new Vector2(-24, 80), new Vector2(200, 56), new Color(0.3f, 0.3f, 0.4f));
        endTurnButton.onClick.AddListener(() => combat.EndTurn());

        rewardPanel = UIFactory.CreateFullscreenPanel(root.transform, "RewardPanel", new Color(0, 0, 0, 0.75f));
        UIFactory.CreateText(rewardPanel, "Title", "Победа! Выбери карту:", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(800, 50));
        rewardCardsArea = UIFactory.CreateRect(rewardPanel, "Cards", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 240));
        var skip = UIFactory.CreateButton(rewardPanel, "SkipButton", "Пропустить", 22, new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(200, 56), new Color(0.3f, 0.3f, 0.3f));
        skip.onClick.AddListener(() => rewardCallback?.Invoke(null));
        rewardPanel.gameObject.SetActive(false);

        defeatPanel = UIFactory.CreateFullscreenPanel(root.transform, "DefeatPanel", new Color(0.2f, 0, 0, 0.85f));
        UIFactory.CreateText(defeatPanel, "Title", "Поражение", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(800, 60));
        var restart = UIFactory.CreateButton(defeatPanel, "RestartButton", "Заново", 26, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(220, 64), new Color(0.5f, 0.2f, 0.2f));
        restart.onClick.AddListener(() => combat.RestartRun());
        defeatPanel.gameObject.SetActive(false);
    }

    public void Show()
    {
        root.SetActive(true);
        rewardPanel.gameObject.SetActive(false);
        defeatPanel.gameObject.SetActive(false);
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    public void Refresh()
    {
        string enemyLine = $"{combat.EnemyName}   HP {combat.EnemyHP}/{combat.EnemyMaxHP}";
        string enemyStatus = combat.EnemyStatusText;
        if (!string.IsNullOrEmpty(enemyStatus)) enemyLine += $"\n<size=22><color=#b8e0ff>{enemyStatus}</color></size>";
        enemyText.text = enemyLine;

        var move = combat.CurrentMove;
        bool showIntent = combat.CombatActive && !combat.EnemyActing && move != null;
        intentBadge.gameObject.SetActive(showIntent);
        if (showIntent)
        {
            intentTooltip.content = move.TooltipText;
            bool hasIcon = move.icon != null;
            intentIcon.gameObject.SetActive(hasIcon);
            intentText.gameObject.SetActive(!hasIcon);
            intentBadgeBg.color = hasIcon ? new Color(0, 0, 0, 0) : new Color(0.35f, 0.08f, 0.08f, 0.95f);
            intentBadge.sizeDelta = hasIcon ? IntentIconSize : IntentWordSize;
            if (hasIcon) intentIcon.sprite = move.icon;
            else intentText.text = move.moveName;
        }

        logText.text = combat.LastEvent;

        var gm = GameManager.Instance;
        playerText.text = $"Ты: HP {gm.currentHP}/{gm.MaxHP}   Блок {combat.PlayerBlock}";
        statusText.text = combat.PlayerStatusText;
        turnText.text = $"Карт за ход: {combat.CardsLeftThisTurn}   Колода {combat.DrawPileCount} / Сброс {combat.DiscardPileCount}";

        endTurnButton.gameObject.SetActive(combat.CanEndTurn && combat.CardsLeftThisTurn > 0);
        RebuildHand();
        PositionIntentBadge();
    }

    void LateUpdate()
    {
        if (intentBadge.gameObject.activeSelf) PositionIntentBadge();
    }

    void PositionIntentBadge()
    {
        if (combat.EnemyTransform == null || Camera.main == null) return;
        Vector3 world = combat.EnemyTop + Vector3.up * 0.15f;
        Vector2 screen = Camera.main.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screen, null, out Vector2 local);
        intentBadge.anchoredPosition = local;
    }

    void RebuildHand()
    {
        foreach (var go in cardButtons) Destroy(go);
        cardButtons.Clear();

        var hand = combat.Hand;
        int count = hand.Count;
        const float spacing = 190f;
        for (int i = 0; i < count; i++)
        {
            var card = hand[i];
            float x = (i - (count - 1) / 2f) * spacing;
            var button = CreateCardButton(handArea, card, new Vector2(x, 0));
            SetCardInteractable(button, combat.CanPlayCard);
            button.onClick.AddListener(() => combat.PlayCard(card));
            cardButtons.Add(button.gameObject);
        }
    }

    static readonly Vector2 HandCardSize = new Vector2(160, 240);
    static readonly Vector2 RewardCardSize = new Vector2(200, 300);

    Button CreateCardButton(Transform parent, CardData card, Vector2 pos)
    {
        return CreateCardButton(parent, card, new Vector2(0.5f, 0), pos, HandCardSize);
    }

    static readonly Color MarkColor = new Color(0.45f, 0.15f, 0.6f, 0.95f);

    Button CreateCardButton(Transform parent, CardData card, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        Button button;
        if (card.artwork == null)
        {
            string text = $"{card.cardName}\n\n<size=17>{card.EffectsSummary}</size>";
            button = UIFactory.CreateButton(parent, "Card_" + card.name, text, 20, anchor, pos, size, ColorFor(card));
        }
        else
        {
            button = UIFactory.CreateButton(parent, "Card_" + card.name, "", 18, anchor, pos, size, new Color(0, 0, 0, 0));
            var art = UIFactory.CreateRect(button.transform, "Art", new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = art.gameObject.AddComponent<Image>();
            img.sprite = card.artwork;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        if (card.IsMarked)
        {
            var badge = UIFactory.CreateRect(button.transform, "MarkBadge", new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(size.x - 12, 34));
            var bg = badge.gameObject.AddComponent<Image>();
            bg.color = MarkColor;
            bg.raycastTarget = false;
            var label = UIFactory.CreateText(badge, "Label", $"{card.eliteMark.enemyName}  +{card.eliteChanceBonus}%", 15, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 16, 34));
            label.color = new Color(0.95f, 0.85f, 1f);
            label.raycastTarget = false;
        }

        button.gameObject.AddComponent<TooltipTrigger>().content = card.TooltipText;
        return button;
    }

    static void SetCardInteractable(Button button, bool interactable)
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

    Action<CardData> rewardCallback;

    public void ShowRewards(List<CardData> rewards, Action<CardData> onChosen)
    {
        rewardCallback = onChosen;
        foreach (Transform child in rewardCardsArea) Destroy(child.gameObject);

        int count = rewards.Count;
        const float spacing = 220f;
        for (int i = 0; i < count; i++)
        {
            var card = rewards[i];
            float x = (i - (count - 1) / 2f) * spacing;
            var button = CreateCardButton(rewardCardsArea, card, new Vector2(0.5f, 0.5f), new Vector2(x, 0), RewardCardSize);
            button.onClick.AddListener(() => rewardCallback?.Invoke(card));
        }
        rewardPanel.gameObject.SetActive(true);
    }

    public void ShowDefeat()
    {
        defeatPanel.gameObject.SetActive(true);
    }
}
