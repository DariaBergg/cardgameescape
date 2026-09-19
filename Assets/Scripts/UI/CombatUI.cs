using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatUI : MonoBehaviour
{
    CombatManager combat;
    GameObject root;
    RectTransform rootRect;
    UnitFrame enemyFrame;
    RectTransform intentBadge;
    Image intentBadgeBg;
    Image intentIcon;
    Text intentText;
    TooltipTrigger intentTooltip;
    Text drawPileText;
    Text discardPileText;
    RectTransform handArea;
    Button endTurnButton;
    RectTransform rewardPanel;
    RectTransform rewardCardsArea;
    RectTransform defeatPanel;
    readonly List<GameObject> cardButtons = new List<GameObject>();

    static readonly Vector2 IntentIconSize = new Vector2(96, 96);
    static readonly Vector2 IntentWordSize = new Vector2(200, 44);
    static readonly Vector2 HandCardSize = new Vector2(163, 240);
    static readonly Vector2 RewardCardSize = new Vector2(271, 400);
    static readonly Color PillColor = new Color(0.05f, 0.05f, 0.08f, 0.7f);

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

        enemyFrame = UnitFrame.Create(root.transform, "EnemyFrame", new Color(0.85f, 0.25f, 0.25f));
        enemyFrame.SetAnchor(() => combat.EnemyTop + Vector3.up * 0.05f);

        intentBadge = UIFactory.CreateRect(root.transform, "IntentBadge", new Vector2(0.5f, 0.5f), Vector2.zero, IntentWordSize);
        intentBadge.pivot = new Vector2(0.5f, 0f);
        intentBadgeBg = intentBadge.gameObject.AddComponent<Image>();
        intentTooltip = intentBadge.gameObject.AddComponent<TooltipTrigger>();
        intentText = UIFactory.CreateText(intentBadge, "Label", "", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, IntentWordSize);
        intentText.color = new Color(1f, 0.75f, 0.7f);
        intentText.raycastTarget = false;
        var iconRect = UIFactory.CreateRect(intentBadge, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, IntentIconSize);
        intentIcon = iconRect.gameObject.AddComponent<Image>();
        intentIcon.preserveAspect = true;
        intentIcon.raycastTarget = false;

        drawPileText = CreatePill(root.transform, "DrawPile", new Vector2(0, 0), new Vector2(16, 16));
        discardPileText = CreatePill(root.transform, "DiscardPile", new Vector2(1, 0), new Vector2(-16, 16));

        handArea = UIFactory.CreateRect(root.transform, "HandArea", new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(1000, 260));

        endTurnButton = UIFactory.CreateButton(root.transform, "EndTurnButton", "Пропустить ход", 18, new Vector2(1, 0), new Vector2(-16, 60), new Vector2(170, 44), new Color(0.25f, 0.25f, 0.35f, 0.9f));
        endTurnButton.onClick.AddListener(() => combat.EndTurn());

        rewardPanel = UIFactory.CreateFullscreenPanel(root.transform, "RewardPanel", new Color(0, 0, 0, 0.75f));
        UIFactory.CreateText(rewardPanel, "Title", "Победа! Выбери карту", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(800, 50));
        rewardCardsArea = UIFactory.CreateRect(rewardPanel, "Cards", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 420));
        var skip = UIFactory.CreateButton(rewardPanel, "SkipButton", "Пропустить", 20, new Vector2(0.5f, 0.5f), new Vector2(0, -265), new Vector2(180, 48), new Color(0.3f, 0.3f, 0.3f));
        skip.onClick.AddListener(() => rewardCallback?.Invoke(null));
        rewardPanel.gameObject.SetActive(false);

        defeatPanel = UIFactory.CreateFullscreenPanel(root.transform, "DefeatPanel", new Color(0.2f, 0, 0, 0.85f));
        UIFactory.CreateText(defeatPanel, "Title", "Поражение", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(800, 60));
        var restart = UIFactory.CreateButton(defeatPanel, "RestartButton", "Заново", 26, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(220, 64), new Color(0.5f, 0.2f, 0.2f));
        restart.onClick.AddListener(() => combat.RestartRun());
        defeatPanel.gameObject.SetActive(false);
    }

    static Text CreatePill(Transform parent, string name, Vector2 anchor, Vector2 pos)
    {
        var rect = UIFactory.CreateRect(parent, name, anchor, pos, new Vector2(110, 30));
        var bg = rect.gameObject.AddComponent<Image>();
        bg.color = PillColor;
        bg.raycastTarget = false;
        var text = UIFactory.CreateText(rect, "Text", "", 15, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 30));
        text.color = new Color(1f, 1f, 1f, 0.85f);
        text.raycastTarget = false;
        return text;
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
        enemyFrame.Set(combat.EnemyName, combat.EnemyHP, combat.EnemyMaxHP, combat.EnemyBlock, combat.EnemyStatusText);

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

        drawPileText.text = $"Колода {combat.DrawPileCount}";
        discardPileText.text = $"Сброс {combat.DiscardPileCount}";

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
        Vector3 world = combat.EnemyTop + Vector3.up * 1.35f;
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
        const float spacing = 180f;
        for (int i = 0; i < count; i++)
        {
            var card = hand[i];
            float x = (i - (count - 1) / 2f) * spacing;
            var button = CardView.Create(handArea, card, new Vector2(0.5f, 0), new Vector2(x, 0), HandCardSize);
            CardView.SetInteractable(button, combat.CanPlayCard);
            button.onClick.AddListener(() => combat.PlayCard(card));
            cardButtons.Add(button.gameObject);
        }
    }

    Action<CardData> rewardCallback;

    public void ShowRewards(List<CardData> rewards, Action<CardData> onChosen)
    {
        rewardCallback = onChosen;
        foreach (Transform child in rewardCardsArea) Destroy(child.gameObject);

        int count = rewards.Count;
        const float spacing = 310f;
        for (int i = 0; i < count; i++)
        {
            var card = rewards[i];
            float x = (i - (count - 1) / 2f) * spacing;
            var button = CardView.Create(rewardCardsArea, card, new Vector2(0.5f, 0.5f), new Vector2(x, 0), RewardCardSize);
            button.onClick.AddListener(() => rewardCallback?.Invoke(card));
        }
        rewardPanel.gameObject.SetActive(true);
    }

    public void ShowDefeat()
    {
        defeatPanel.gameObject.SetActive(true);
    }
}
