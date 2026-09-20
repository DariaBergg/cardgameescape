using System.Collections.Generic;
using UnityEngine;

public class BlacksmithVisit : MerchantVisit
{
    const string QuestFlag = "Quest_Merchant_Scroll";
    const int PledgeRooms = 5;
    const int PledgeFights = 3;

    protected override void Begin()
    {
        Visits++;
        if (gm.HasFlag(QuestFlag, "Active") && gm.HasItem("SealedScroll"))
            ScrollDialogue();
        else
            Shop();
    }

    void Shop()
    {
        Say(L.T("Кузнец не отрывается от наковальни.\n«Перековать? Или хочешь что-то посерьёзнее — под залог?»"), ShopOptions());
    }

    MerchantUI.Option[] ShopOptions()
    {
        bool hasPledge = gm.HasObligation(ObligationType.PledgeFights);
        return new[]
        {
            Opt(L.T("Перековка"), L.T("Улучшить одну карту из колоды"), () => Reforge(L.T("«Держи. Теперь она бьёт как надо.»"))),
            Opt(L.T("Залог"), L.F("Улучшенная редкая карта. Условие: {0} победы за {1} комнат", PledgeFights, PledgeRooms), Pledge, !hasPledge),
            Leave()
        };
    }

    void Reforge(string afterText)
    {
        ui.Hide();
        DeckPickerUI.Get().Show(L.T("Перековка: какую карту улучшить?"), gm.playerDeck, c => !c.unplayable, card =>
        {
            var upgraded = gm.UpgradeCard(card);
            Done($"{afterText}\n«{L.T(upgraded.cardName)}»: {upgraded.EffectsSummary}");
        }, Shop, upgradePreview: true);
    }

    void Pledge()
    {
        var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 3);
        var upgraded = new List<CardData>();
        foreach (var c in pool) upgraded.Add(c.CreateUpgradedCopy());
        ui.Hide();
        CardChoiceUI.Get().Show(L.F("Залог: выбери карту. {0} победы за {1} комнат — или она ослабнет", PledgeFights, PledgeRooms), upgraded, chosen =>
        {
            if (chosen == null) { Shop(); return; }
            gm.playerDeck.Add(chosen);
            gm.AddObligation(new Obligation
            {
                type = ObligationType.PledgeFights,
                source = L.T(data.displayName),
                roomsRemaining = PledgeRooms,
                fightsRequired = PledgeFights,
                card = chosen
            });
            Done(L.F("«{0} твоя. Пока. Три победы — и забудем про залог.»", L.T(chosen.cardName)));
        });
    }

    void ScrollDialogue()
    {
        Say(L.T("Кузнец смотрит на печать и хмурится.\n«Опять он... Что он тебе за это обещал?»"),
            Opt(L.T("Передать свиток"), L.T("Кузнец бесплатно улучшит одну карту"), () =>
            {
                gm.TakeItem("SealedScroll");
                gm.SetFlag(QuestFlag, "Delivered");
                RoomManager.Instance.ForceMerchant(MerchantKind.Collector);
                Reforge(L.T("«Ладно. За доставку — перекую что скажешь.»\n«Если снова встретишь Собирателя, передай ему: я больше не принимаю его долги.»"));
            }),
            Opt(L.T("Открыть свиток"), L.T("Нарушить просьбу Собирателя и посмотреть, что внутри"), () =>
            {
                gm.TakeItem("SealedScroll");
                gm.GiveItem("OpenedScroll");
                gm.SetFlag(QuestFlag, "Opened");
                RoomManager.Instance.ForceMerchant(MerchantKind.Collector);
                var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 1);
                var card = pool.Count > 0 ? pool[0] : null;
                if (card != null) gm.playerDeck.Add(card);
                Say(L.F("Печать хрустит. Внутри — «{0}».\nКузнец качает головой: «Сорванная печать. Улучшать бесплатно не стану. Но торговать — торгую.»", (card != null ? L.T(card.cardName) : L.T("пусто"))), ShopOptions());
            }));
    }
}
