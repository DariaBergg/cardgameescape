using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class EventVisit
{
    protected EventData data;
    protected GameManager gm;
    protected MerchantUI ui;
    protected Action onDone;

    public static void Start(EventData data, Action onDone)
    {
        EventVisit visit;
        switch (data.kind)
        {
            case EventKind.Wanderer: visit = new WandererEvent(); break;
            case EventKind.Crack: visit = new CrackEvent(); break;
            case EventKind.Mirror: visit = new MirrorEvent(); break;
            default: visit = new PitEvent(); break;
        }
        visit.data = data;
        visit.gm = GameManager.Instance;
        visit.ui = MerchantUI.Get();
        visit.ui.SetPanelOffset(data.panelOffset);
        visit.onDone = onDone;
        visit.Begin();
    }

    protected abstract void Begin();

    protected void Say(string text, params MerchantUI.Option[] options)
    {
        ui.Show(L.T(data.title), text, new List<MerchantUI.Option>(options));
    }

    protected MerchantUI.Option Opt(string label, string description, Action action)
    {
        return new MerchantUI.Option { label = label, description = description, action = action };
    }

    protected MerchantUI.Option Finish(string label = null)
    {
        return Opt(label ?? L.T("Дальше"), "", () => { ui.Hide(); onDone?.Invoke(); });
    }

    protected void End(string text)
    {
        Say(text, Finish());
    }

    protected string Hp(int n) => $"−{n} HP";
}

public class WandererEvent : EventVisit
{
    protected override void Begin()
    {
        Say(L.T("У стены сидит ящер в рваном плаще, прижимая руку к боку. Кровь тёмная, дыхание короткое.\n«Эй... подойди. Или не подходи. Мне уже всё равно.»"),
            Opt(L.T("Помочь"), Hp(6) + L.T(": перевязать его рану, оторвав лоскут собственной чешуи. Он отблагодарит"), () =>
            {
                gm.LoseHPSafe(6);
                var pool = CardPools.Instance.EventRares(1);
                var card = pool.Count > 0 ? pool[0] : null;
                if (card != null) gm.playerDeck.Add(card);
                string thanks = L.F("Он выдыхает и суёт тебе в руку потёртую карту.\n«{0}. Мне она больше не пригодится.»", (card != null ? L.T(card.cardName) : L.T("Пусто")));
                if (card != null) { ui.Hide(); CardChoiceUI.Get().Reveal(L.T("Странник отдаёт карту"), new List<CardData> { card }, () => End(thanks)); }
                else End(thanks);
            }),
            Opt(L.T("Обыскать"), L.T("Забрать, что есть. 30% — он не так уж и ранен"), () =>
            {
                if (UnityEngine.Random.Range(0, 100) < 30)
                {
                    ui.Hide();
                    RoomManager.Instance.StartAmbush(L.T("Странник резко выпрямляется. Кровь на плаще — не его."));
                    return;
                }
                var pool = CardPools.Instance.RandomOfRarity(CardRarity.Common, 1);
                var card = pool.Count > 0 ? pool[0] : null;
                if (card != null) gm.playerDeck.Add(card);
                string found = L.F("В сумке — {0}. Странник не сопротивляется. Он просто смотрит.", (card != null ? "«" + L.T(card.cardName) + "»" : L.T("ничего ценного")));
                if (card != null) { ui.Hide(); CardChoiceUI.Get().Reveal(L.T("В сумке странника"), new List<CardData> { card }, () => End(found)); }
                else End(found);
            }),
            Finish(L.T("Пройти мимо")));
    }
}

public class CrackEvent : EventVisit
{
    protected override void Begin()
    {
        Say(L.T("В стене трещина шириной в ладонь. Из глубины тянет холодом, и что-то там блестит."),
            Opt(L.T("Протиснуться"), Hp(4) + L.T(": чешуя оставит кусочки на камне. Внутри — тайник"), () =>
            {
                gm.LoseHPSafe(4);
                ui.Hide();
                OfferChest(L.T("Ты протискиваешься, обдирая бока. Внутри — небольшой тайник."));
            }),
            Opt(L.T("Выжечь проход"), L.T("Дыхание расширит трещину. 50% — тайник, 50% — обвал: ") + Hp(10), () =>
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    ui.Hide();
                    OfferChest(L.T("Камень трескается от жара и осыпается. Проход свободен — и тайник цел."));
                }
                else
                {
                    gm.LoseHPSafe(10);
                    End(L.T("Стена дрожит и обрушивается. Ты едва успеваешь отскочить; тайник погребён под камнями."));
                }
            }),
            Finish(L.T("Уйти")));
    }

    void OfferChest(string intro)
    {
        var pool = CardPools.Instance.EventRares(3);
        CardChoiceUI.Get().Show(L.T("Тайник: выбери карту"), pool, chosen =>
        {
            if (chosen != null) gm.playerDeck.Add(chosen);
            End(intro + (chosen != null ? L.F("\nТы забираешь «{0}».", L.T(chosen.cardName)) : L.T("\nТы ничего не берёшь.")));
        });
    }
}

public class MirrorEvent : EventVisit
{
    protected override void Begin()
    {
        Say(L.T("Посреди комнаты стоит зеркало из чёрного стекла. Отражение смотрит на тебя, но не повторяет твоих движений."),
            Opt(L.T("Коснуться"), L.T("Одна случайная карта станет сильнее, другая — слабее"), () =>
            {
                var candidates = gm.playerDeck.FindAll(c => !c.unplayable);
                if (candidates.Count < 2) { End(L.T("Зеркало молчит. Отражение отворачивается.")); return; }
                var up = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                candidates.Remove(up);
                var down = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                var upgraded = gm.UpgradeCard(up);
                var weakened = down.CreateWeakenedCopy();
                gm.ReplaceCard(down, weakened);
                ui.Hide();
                string outcome = L.F("Стекло холодное, как лёд. Ты чувствуешь, как что-то перетекает.\n«{0}» стала сильнее, «{1}» — слабее.", L.T(upgraded.cardName), L.T(weakened.cardName));
                // Показываем «было → стало» для обеих карт
                DeckPickerUI.Get().Show(
                    L.T("Зеркало: было → стало"),
                    new List<CardData> { up, upgraded, down, weakened },
                    null,
                    _ => End(outcome),
                    () => End(outcome),
                    closeLabel: L.T("Дальше"));
            }),
            Opt(L.T("Разбить"), Hp(5) + L.T(": осколки. Все метки Короля исчезнут с карт, бонусы останутся"), () =>
            {
                gm.LoseHPSafe(5);
                RoomManager.Instance.HideRoomNpc();
                int cleared = 0;
                for (int i = 0; i < gm.playerDeck.Count; i++)
                {
                    if (!gm.playerDeck[i].IsMarked) continue;
                    gm.playerDeck[i] = gm.playerDeck[i].CreateUnmarkedCopy();
                    cleared++;
                }
                End(cleared > 0
                    ? L.F("Зеркало разлетается вдребезги. Осколки режут руки, но с карт сходит чужой знак: {0} мет. снято.", cleared)
                    : L.T("Зеркало разлетается вдребезги. Осколки режут руки. Меток на картах и так не было."));
            }),
            Finish(L.T("Уйти")));
    }
}

public class PitEvent : EventVisit
{
    protected override void Begin()
    {
        gm.LoseHPSafe(5);
        var card = CardPools.Instance.RandomAny();
        if (card != null) gm.playerDeck.Add(card);
        if (card != null) { CardChoiceUI.Get().Reveal(L.T("В пыли лежит карта"), new List<CardData> { card }, () => PitText(card)); return; }
        PitText(card);
    }

    void PitText(CardData card)
    {
        Say(L.F("Плита под ногой проваливается. Ты летишь вниз и приземляешься на кости — {0}.\nВ пыли рядом лежит «{1}». Наверх ведут выбитые в стене ступени.", Hp(5), (card != null ? L.T(card.cardName) : L.T("пусто"))), Finish(L.T("Выбраться")));
    }
}
