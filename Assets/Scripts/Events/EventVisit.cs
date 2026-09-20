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
        visit.onDone = onDone;
        visit.Begin();
    }

    protected abstract void Begin();

    protected void Say(string text, params MerchantUI.Option[] options)
    {
        ui.Show(data.title, text, new List<MerchantUI.Option>(options));
    }

    protected MerchantUI.Option Opt(string label, string description, Action action)
    {
        return new MerchantUI.Option { label = label, description = description, action = action };
    }

    protected MerchantUI.Option Finish(string label = "Дальше")
    {
        return Opt(label, "", () => { ui.Hide(); onDone?.Invoke(); });
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
        Say("У стены сидит ящер в рваном плаще, прижимая руку к боку. Кровь тёмная, дыхание короткое.\n«Эй... подойди. Или не подходи. Мне уже всё равно.»",
            Opt("Помочь", Hp(6) + ": перевязать раной со своей чешуи. Он отблагодарит", () =>
            {
                gm.LoseHPSafe(6);
                var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 1);
                var card = pool.Count > 0 ? pool[0] : null;
                if (card != null) gm.playerDeck.Add(card);
                End($"Он выдыхает и суёт тебе в руку потёртую карту.\n«{(card != null ? card.cardName : "Пусто")}. Мне она больше не пригодится.»");
            }),
            Opt("Обыскать", "Забрать, что есть. 30% — он не так уж и ранен", () =>
            {
                if (UnityEngine.Random.Range(0, 100) < 30)
                {
                    ui.Hide();
                    RoomManager.Instance.StartAmbush("Странник резко выпрямляется. Кровь на плаще — не его.");
                    return;
                }
                var pool = CardPools.Instance.RandomOfRarity(CardRarity.Common, 1);
                var card = pool.Count > 0 ? pool[0] : null;
                if (card != null) gm.playerDeck.Add(card);
                End($"В сумке — {(card != null ? "«" + card.cardName + "»" : "ничего ценного")}. Странник не сопротивляется. Он просто смотрит.");
            }),
            Finish("Пройти мимо"));
    }
}

public class CrackEvent : EventVisit
{
    protected override void Begin()
    {
        Say("В стене трещина шириной в ладонь. Из глубины тянет холодом, и что-то там блестит.",
            Opt("Протиснуться", Hp(4) + ": чешуя оставит кусочки на камне. Внутри — тайник", () =>
            {
                gm.LoseHPSafe(4);
                ui.Hide();
                OfferChest("Ты протискиваешься, обдирая бока. Внутри — небольшой тайник.");
            }),
            Opt("Выжечь проход", "Дыхание расширит трещину. 50% — тайник, 50% — обвал: " + Hp(10), () =>
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    ui.Hide();
                    OfferChest("Камень трескается от жара и осыпается. Проход свободен — и тайник цел.");
                }
                else
                {
                    gm.LoseHPSafe(10);
                    End("Стена дрожит и обрушивается. Ты едва успеваешь отскочить; тайник погребён под камнями.");
                }
            }),
            Finish("Уйти"));
    }

    void OfferChest(string intro)
    {
        var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 3);
        CardChoiceUI.Get().Show("Тайник: выбери карту", pool, chosen =>
        {
            if (chosen != null) gm.playerDeck.Add(chosen);
            End(intro + (chosen != null ? $"\nТы забираешь «{chosen.cardName}»." : "\nТы ничего не берёшь."));
        });
    }
}

public class MirrorEvent : EventVisit
{
    protected override void Begin()
    {
        Say("Посреди комнаты стоит зеркало из чёрного стекла. Отражение смотрит на тебя, но не повторяет твоих движений.",
            Opt("Коснуться", "Одна случайная карта станет сильнее, другая — слабее", () =>
            {
                var candidates = gm.playerDeck.FindAll(c => !c.unplayable);
                if (candidates.Count < 2) { End("Зеркало молчит. Отражение отворачивается."); return; }
                var up = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                candidates.Remove(up);
                var down = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                var upgraded = gm.UpgradeCard(up);
                var weakened = down.CreateWeakenedCopy();
                gm.ReplaceCard(down, weakened);
                End($"Стекло холодное, как лёд. Ты чувствуешь, как что-то перетекает.\n«{upgraded.cardName}» стала сильнее, «{weakened.cardName}» — слабее.");
            }),
            Opt("Разбить", Hp(5) + ": осколки. Все метки Короля исчезнут с карт, бонусы останутся", () =>
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
                    ? $"Зеркало разлетается вдребезги. Осколки режут руки, но с карт сходит чужой знак: {cleared} мет. снято."
                    : "Зеркало разлетается вдребезги. Осколки режут руки. Меток на картах и так не было.");
            }),
            Finish("Уйти"));
    }
}

public class PitEvent : EventVisit
{
    protected override void Begin()
    {
        gm.LoseHPSafe(5);
        var card = CardPools.Instance.RandomAny();
        if (card != null) gm.playerDeck.Add(card);
        End($"Плита под ногой проваливается. Ты летишь вниз и приземляешься на кости — {Hp(5)}.\nВ пыли рядом лежит «{(card != null ? card.cardName : "пусто")}». Наверх ведут выбитые в стене ступени.");
    }
}
