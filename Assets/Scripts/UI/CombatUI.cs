using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatUI : MonoBehaviour
{
    CombatManager combat;
    GameObject root;
    RectTransform rootRect;
    UnitFrame enemyFrame;
    UnitFrame minionFrame;
    RectTransform minionBadge;
    TooltipTrigger minionBadgeTooltip;
    RectTransform targetMarker;
    RectTransform intentBadge;
    Image intentBadgeBg;
    Image intentIcon;
    Text intentNumber;
    Outline intentOutline;
    const int HeavyHitThreshold = 6;
    Text intentText;
    TooltipTrigger intentTooltip;
    Text drawPileText;
    Text discardPileText;
    RectTransform handArea;
    Button endTurnButton;
    RectTransform rewardPanel;
    RectTransform rewardCardsArea;
    RectTransform defeatPanel;

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

        enemyFrame = UnitFrame.Create(root.transform, "EnemyFrame", new Color(0.85f, 0.25f, 0.25f), enemy: true);
        enemyFrame.SetAnchor(() => combat.EnemyTop + Vector3.up * 0.05f);
        minionFrame = UnitFrame.Create(root.transform, "MinionFrame", new Color(0.85f, 0.25f, 0.25f), enemy: true);
        minionFrame.SetAnchor(() => combat.MinionTop + Vector3.up * 0.05f);
        minionFrame.transform.localScale = Vector3.one * 0.7f;
        minionFrame.Show(false);

        // Намерение малыша: иконка удара главного врага, поменьше
        minionBadge = UIFactory.CreateRect(root.transform, "MinionIntent", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
        minionBadge.pivot = new Vector2(0.5f, 0f);
        var mbImg = minionBadge.gameObject.AddComponent<Image>();
        mbImg.preserveAspect = true;
        minionBadgeTooltip = minionBadge.gameObject.AddComponent<TooltipTrigger>();
        minionBadge.gameObject.SetActive(false);

        // Метка цели: красный ромб над тем, кого бьют карты
        targetMarker = UIFactory.CreateRect(root.transform, "TargetMarker", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
        targetMarker.pivot = new Vector2(0.5f, 0f);
        targetMarker.localRotation = Quaternion.Euler(0, 0, 45);
        var tmImg = targetMarker.gameObject.AddComponent<Image>();
        tmImg.color = new Color(1f, 0.25f, 0.2f, 0.95f);
        tmImg.raycastTarget = false;
        UIFactory.AddShadow(tmImg, 1f);
        targetMarker.gameObject.SetActive(false);

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
        intentOutline = iconRect.gameObject.AddComponent<Outline>();
        intentOutline.effectColor = new Color(1f, 0.2f, 0.15f, 0.95f);
        intentOutline.effectDistance = new Vector2(5, -5);
        intentOutline.enabled = false;
        // Число под иконкой: урон / защита, чтобы не лезть в подсказку
        intentNumber = UIFactory.CreateText(intentBadge, "Number", "", 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, -6), new Vector2(160, 30));
        intentNumber.rectTransform.pivot = new Vector2(0.5f, 1f);
        intentNumber.fontStyle = FontStyle.Bold;
        intentNumber.raycastTarget = false;
        UIFactory.AddShadow(intentNumber, 1f);

        drawPileText = CreatePill(root.transform, "DrawPile", new Vector2(0, 0), new Vector2(16, 16));
        discardPileText = CreatePill(root.transform, "DiscardPile", new Vector2(1, 0), new Vector2(-16, 16));
        var discardImg = discardPileText.transform.parent.GetComponent<Image>();
        discardImg.raycastTarget = true;
        var discardButton = discardImg.gameObject.AddComponent<Button>();
        discardButton.targetGraphic = discardImg;
        discardButton.onClick.AddListener(OpenDiscardPile);

        handArea = UIFactory.CreateRect(root.transform, "HandArea", new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(1000, 260));

        endTurnButton = UIFactory.CreateButton(root.transform, "EndTurnButton", L.T("Пропустить ход"), 18, new Vector2(1, 0), new Vector2(-16, 60), new Vector2(170, 44), new Color(0.25f, 0.25f, 0.35f, 0.9f));
        endTurnButton.onClick.AddListener(() => combat.EndTurn());

        rewardPanel = UIFactory.CreateFullscreenPanel(root.transform, "RewardPanel", new Color(0, 0, 0, 0.75f));
        var rewardPlate = UIFactory.CreatePanel(rewardPanel, "TitlePlate", new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(880, 64), UISkin.Get(k => k.labelTitle), Color.clear);
        rewardPlate.GetComponent<Image>().raycastTarget = false;
        var rewardTitle = UIFactory.CreateText(rewardPlate, "Title", L.T("Победа! Выбери карту"), 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(780, 50));
        rewardTitle.color = new Color(0.93f, 0.88f, 0.78f);
        rewardTitle.font = UIFactory.TitleFont;
        UIFactory.AddShadow(rewardTitle);
        rewardCardsArea = UIFactory.CreateRect(rewardPanel, "Cards", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 420));
        var skip = UIFactory.CreateButton(rewardPanel, "SkipButton", L.T("Пропустить"), 20, new Vector2(0.5f, 0.5f), new Vector2(0, -265), new Vector2(180, 48), new Color(0.3f, 0.3f, 0.3f));
        skip.onClick.AddListener(() => rewardCallback?.Invoke(null));
        rewardPanel.gameObject.SetActive(false);

        defeatPanel = UIFactory.CreateFullscreenPanel(root.transform, "DefeatPanel", new Color(0.2f, 0, 0, 0.85f));
        UIFactory.CreateText(defeatPanel, "Title", L.T("Поражение"), 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(800, 60));
        var restart = UIFactory.CreateButton(defeatPanel, "RestartButton", L.T("Заново"), 26, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(220, 64), new Color(0.5f, 0.2f, 0.2f));
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

    void OpenDiscardPile()
    {
        if (combat.DiscardPile.Count == 0) return;
        DeckPickerUI.Get().Show(L.F("Сброс — {0} карт", combat.DiscardPile.Count), new List<CardData>(combat.DiscardPile), c => false, null, null, closeLabel: L.T("Закрыть"));
    }

    public void Show()
    {
        root.SetActive(true);
        enemyFrame.Show(true);
        rewardPanel.gameObject.SetActive(false);
        defeatPanel.gameObject.SetActive(false);
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    public void Refresh()
    {
        enemyFrame.Set(combat.EnemyName, combat.EnemyHP, combat.EnemyMaxHP, combat.EnemyBlock, "");
        enemyFrame.SetStatuses(combat.EnemyStatuses);
        minionFrame.Show(combat.MinionAlive);
        if (combat.MinionAlive) minionFrame.Set(L.F("Малыш ({0})", combat.MinionDamage), combat.MinionHP, combat.MinionMaxHP, 0, "");
        bool showMinionIntent = combat.MinionAlive && combat.CombatActive && !combat.EnemyActing;
        minionBadge.gameObject.SetActive(showMinionIntent);
        if (showMinionIntent)
        {
            Sprite icon = null;
            if (combat.CurrentEnemy != null) foreach (var m in combat.CurrentEnemy.moves) if (m.damage > 0 && m.icon != null) { icon = m.icon; break; }
            var img = minionBadge.GetComponent<Image>();
            img.sprite = icon; img.enabled = icon != null;
            minionBadgeTooltip.content = L.F("<size=22><b>Малыш</b></size>\n\n<color=#ffb3a7>Атака {0}</color>\n\nБьёт после главного врага. Кликни по нему, чтобы направить на него атаки.", combat.MinionDamage);
        }
        targetMarker.gameObject.SetActive(combat.CombatActive && combat.MinionAlive);

        var move = combat.CurrentMove;
        bool showIntent = combat.CombatActive && !combat.EnemyActing && move != null;
        intentBadge.gameObject.SetActive(showIntent);
        if (showIntent)
        {
            intentTooltip.content = move.TooltipTextWithBonus(combat.PendingDamageBonus);
            intentTooltip.extra = Glossary.ForMove(move);
            bool hasIcon = move.icon != null;
            intentIcon.gameObject.SetActive(hasIcon);
            intentText.gameObject.SetActive(!hasIcon);
            intentBadgeBg.color = hasIcon ? new Color(0, 0, 0, 0) : new Color(0.35f, 0.08f, 0.08f, 0.95f);
            intentBadge.sizeDelta = hasIcon ? IntentIconSize : IntentWordSize;
            if (hasIcon) intentIcon.sprite = move.icon;
            else intentText.text = L.T(move.moveName);
            intentNumber.text = "";
            // Сильная атака — красный контур вокруг иконки
            bool heavy = move.damage + combat.PendingDamageBonus >= HeavyHitThreshold;
            intentOutline.enabled = heavy;
        }

        drawPileText.text = L.F("Колода {0}", combat.DrawPileCount);
        discardPileText.text = L.F("Сброс {0}", combat.DiscardPileCount);

        endTurnButton.gameObject.SetActive(combat.CanEndTurn && combat.CardsLeftThisTurn > 0);
        RebuildHand();
        PositionIntentBadge();
    }

    void LateUpdate()
    {
        if (intentBadge.gameObject.activeSelf) PositionIntentBadge();
        if (minionBadge.gameObject.activeSelf) PlaceAtWorld(minionBadge, combat.MinionTop + Vector3.up * 1.0f);
        if (targetMarker.gameObject.activeSelf) PlaceAtWorld(targetMarker, (combat.TargetIsMinion ? combat.MinionTop + Vector3.up * 0.75f : combat.EnemyTop + Vector3.up * 1.05f) + Vector3.left * 0.9f);
    }

    void PlaceAtWorld(RectTransform rt, Vector3 world)
    {
        if (Camera.main == null) return;
        Vector2 screen = Camera.main.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screen, null, out Vector2 local);
        rt.anchoredPosition = local;
    }

    // Короткая числовая подпись к намерению: «5», «2×3», «защ. 6»
    static string IntentNumbers(EnemyMove move, int bonus)
    {
        var parts = new List<string>();
        if (move.damage > 0)
        {
            int dmg = move.damage + bonus;
            parts.Add(move.hits > 1 ? $"<color=#ff9a8a>{dmg}×{move.hits}</color>" : $"<color=#ff9a8a>{dmg}</color>");
        }
        if (move.block > 0) parts.Add(L.F("<color=#9fd0ff>защ. {0}</color>", move.block));
        if (move.poisonTurns > 0) parts.Add(L.F("<color=#9de08a>яд {0}×{1}</color>", move.poisonDamage, move.poisonTurns));
        if (move.handReduce > 0) parts.Add(L.F("<color=#d9a6ff>−{0} карта</color>", move.handReduce));
        return string.Join("  ", parts);
    }

    void PositionIntentBadge()
    {
        if (combat.EnemyTransform == null || Camera.main == null) return;
        Vector3 world = combat.EnemyTop + Vector3.up * 1.35f;
        Vector2 screen = Camera.main.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screen, null, out Vector2 local);
        intentBadge.anchoredPosition = local;
    }

    class HandCardView
    {
        public CardData card;
        public Button button;
        public RectTransform rect;
        public CanvasGroup group;
        public bool swept;
        public Coroutine motion;
    }

    readonly List<HandCardView> handViews = new List<HandCardView>();
    CardData lastPlayedCard;
    static readonly Vector2 DealOrigin = new Vector2(-620f, -80f);
    static readonly Vector2 DiscardTarget = new Vector2(620f, -80f);
    const float HandSpacing = 180f;
    const float DealDuration = 0.28f;
    const float DealStagger = 0.09f;

    public void NotifyCardPlayed(CardData card) => lastPlayedCard = card;

    public void HideEnemyFrame() { enemyFrame.Show(false); minionFrame.Show(false); minionBadge.gameObject.SetActive(false); targetMarker.gameObject.SetActive(false); }

    public void SweepHand()
    {
        foreach (var view in handViews)
        {
            if (view.swept) continue;
            view.swept = true;
            view.button.interactable = false;
            StartMotion(view, view.rect.anchoredPosition, DiscardTarget, 0.3f, 0f, fadeOut: true, destroy: false);
        }
    }

    void RebuildHand()
    {
        var hand = combat.Hand;
        int count = hand.Count;
        var remaining = new List<HandCardView>(handViews);
        var ordered = new List<HandCardView>();
        int newCount = 0;

        for (int i = 0; i < count; i++)
        {
            var card = hand[i];
            var existing = remaining.Find(v => v.card == card && !v.swept);
            if (existing != null)
            {
                remaining.Remove(existing);
                ordered.Add(existing);
                continue;
            }
            var button = CardView.Create(handArea, card, new Vector2(0.5f, 0), DealOrigin, HandCardSize);
            var captured = card;
            button.onClick.AddListener(() => combat.PlayCard(captured));
            var view = new HandCardView
            {
                card = card,
                button = button,
                rect = button.GetComponent<RectTransform>(),
                group = button.gameObject.AddComponent<CanvasGroup>()
            };
            view.group.alpha = 0f;
            view.button.interactable = false;
            ordered.Add(view);
            StartMotion(view, DealOrigin, SlotPosition(i, count), DealDuration, newCount * DealStagger, fadeOut: false, destroy: false,
                onDone: () => CardView.SetInteractable(view.button, combat.CanPlay(view.card)));
            newCount++;
        }

        foreach (var gone in remaining)
        {
            if (gone.swept) { Destroy(gone.button.gameObject); continue; }
            bool played = gone.card == lastPlayedCard;
            var target = played ? gone.rect.anchoredPosition + new Vector2(0, 260f) : DiscardTarget;
            gone.button.interactable = false;
            StartMotion(gone, gone.rect.anchoredPosition, target, played ? 0.25f : 0.3f, 0f, fadeOut: true, destroy: true);
        }
        lastPlayedCard = null;

        handViews.Clear();
        handViews.AddRange(ordered);
        for (int i = 0; i < ordered.Count; i++)
        {
            var view = ordered[i];
            if (view.swept) continue;
            var slot = SlotPosition(i, count);
            bool arriving = view.group.alpha < 1f;
            if (!arriving && (view.rect.anchoredPosition - slot).sqrMagnitude > 1f)
                StartMotion(view, view.rect.anchoredPosition, slot, 0.2f, 0f, fadeOut: false, destroy: false);
            CardView.SetInteractable(view.button, combat.CanPlay(view.card)); // всегда актуализируем кликабельность
        }
    }

    static Vector2 SlotPosition(int index, int count) => new Vector2((index - (count - 1) / 2f) * HandSpacing, 0f);

    public void StealCard(CardData card)
    {
        var view = handViews.Find(v => v.card == card && !v.swept);
        if (view == null) return;
        handViews.Remove(view);
        view.button.interactable = false;
        StartCoroutine(StealMotion(view));
        RelayoutHand();
    }

    IEnumerator StealMotion(HandCardView view)
    {
        Vector2 start = view.rect.anchoredPosition;
        Vector2 up = start + new Vector2(0, 230f);
        yield return MoveOnly(view, start, up, 0.3f);
        yield return new WaitForSeconds(0.6f);
        if (view.button == null) yield break;
        StartMotion(view, up, DiscardTarget, 0.35f, 0f, fadeOut: true, destroy: true);
    }

    public void CorruptCard(CardData oldCard, CardData newCard)
    {
        var view = handViews.Find(v => v.card == oldCard && !v.swept);
        if (view == null) return;
        int index = handViews.IndexOf(view);
        var slot = view.rect.anchoredPosition;
        handViews.Remove(view);
        view.button.interactable = false;
        StartCoroutine(CorruptMotion(view));

        var button = CardView.Create(handArea, newCard, new Vector2(0.5f, 0), slot, HandCardSize);
        var captured = newCard;
        button.onClick.AddListener(() => combat.PlayCard(captured));
        var fresh = new HandCardView
        {
            card = newCard,
            button = button,
            rect = button.GetComponent<RectTransform>(),
            group = button.gameObject.AddComponent<CanvasGroup>()
        };
        fresh.group.alpha = 0f;
        fresh.button.interactable = false;
        handViews.Insert(Mathf.Clamp(index, 0, handViews.Count), fresh);
        StartMotion(fresh, slot + new Vector2(0, -30f), slot, 0.35f, 0.35f, fadeOut: false, destroy: false,
            onDone: () => CardView.SetInteractable(fresh.button, combat.CanPlay(fresh.card)));
    }

    IEnumerator CorruptMotion(HandCardView view)
    {
        var tint = view.button.transform.Find("Template")?.GetComponent<Image>();
        Vector2 origin = view.rect.anchoredPosition;
        for (float t = 0; t < 0.35f; t += Time.deltaTime)
        {
            if (view.button == null) yield break;
            float k = t / 0.35f;
            view.rect.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 60f) * 8f * (1f - k), 0);
            if (tint != null) tint.color = Color.Lerp(new Color(0.85f, 0.5f, 1f), Color.white, Mathf.PingPong(k * 4f, 1f));
            view.group.alpha = 1f - k;
            yield return null;
        }
        if (view.button != null) Destroy(view.button.gameObject);
    }

    IEnumerator MoveOnly(HandCardView view, Vector2 from, Vector2 to, float duration)
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (view.button == null) yield break;
            float k = 1f - Mathf.Pow(1f - t / duration, 3f);
            view.rect.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            yield return null;
        }
        if (view.button != null) view.rect.anchoredPosition = to;
    }

    void RelayoutHand()
    {
        int count = handViews.Count;
        for (int i = 0; i < count; i++)
        {
            var view = handViews[i];
            if (view.swept) continue;
            var slot = SlotPosition(i, count);
            if ((view.rect.anchoredPosition - slot).sqrMagnitude > 1f)
                StartMotion(view, view.rect.anchoredPosition, slot, 0.2f, 0f, fadeOut: false, destroy: false);
        }
    }

    void StartMotion(HandCardView view, Vector2 from, Vector2 to, float duration, float delay, bool fadeOut, bool destroy, Action onDone = null)
    {
        if (view.motion != null) StopCoroutine(view.motion);
        view.motion = StartCoroutine(Motion(view, from, to, duration, delay, fadeOut, destroy, onDone));
    }

    IEnumerator Motion(HandCardView view, Vector2 from, Vector2 to, float duration, float delay, bool fadeOut, bool destroy, Action onDone)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (view.button == null) yield break;
        float startAlpha = view.group.alpha;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (view.button == null) yield break;
            float k = 1f - Mathf.Pow(1f - t / duration, 3f);
            view.rect.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            // Сдвиг уже видимой карты не должен её гасить (иначе она считается «прилетающей» и остаётся серой)
            view.group.alpha = fadeOut ? Mathf.Lerp(startAlpha, 0f, k) : Mathf.Max(startAlpha, Mathf.Min(1f, k * 2f));
            yield return null;
        }
        if (view.button == null) yield break;
        view.rect.anchoredPosition = to;
        view.group.alpha = fadeOut ? 0f : 1f;
        view.motion = null;
        if (destroy) Destroy(view.button.gameObject);
        else onDone?.Invoke();
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
