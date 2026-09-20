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
        Say("Кузнец не отрывается от наковальни.\n«Перековать? Или хочешь что-то посерьёзнее — под залог?»", ShopOptions());
    }

    MerchantUI.Option[] ShopOptions()
    {
        bool hasPledge = gm.HasObligation(ObligationType.PledgeFights);
        return new[]
        {
            Opt("Перековка", "Улучшить одну карту из колоды", () => Reforge("«Держи. Теперь она бьёт как надо.»")),
            Opt("Залог", $"Улучшенная редкая карта. Условие: {PledgeFights} победы за {PledgeRooms} комнат", Pledge, !hasPledge),
            Leave()
        };
    }

    void Reforge(string afterText)
    {
        ui.Hide();
        DeckPickerUI.Get().Show("Перековка: какую карту улучшить?", gm.playerDeck, c => !c.unplayable, card =>
        {
            var upgraded = gm.UpgradeCard(card);
            Done($"{afterText}\n«{upgraded.cardName}»: {upgraded.EffectsSummary}");
        }, Shop, upgradePreview: true);
    }

    void Pledge()
    {
        var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 3);
        var upgraded = new List<CardData>();
        foreach (var c in pool) upgraded.Add(c.CreateUpgradedCopy());
        ui.Hide();
        CardChoiceUI.Get().Show($"Залог: выбери карту. {PledgeFights} победы за {PledgeRooms} комнат — или она ослабнет", upgraded, chosen =>
        {
            if (chosen == null) { Shop(); return; }
            gm.playerDeck.Add(chosen);
            gm.AddObligation(new Obligation
            {
                type = ObligationType.PledgeFights,
                source = data.displayName,
                roomsRemaining = PledgeRooms,
                fightsRequired = PledgeFights,
                card = chosen
            });
            Done($"«{chosen.cardName} твоя. Пока. Три победы — и забудем про залог.»");
        });
    }

    void ScrollDialogue()
    {
        Say("Кузнец смотрит на печать и хмурится.\n«Опять он... Что он тебе за это обещал?»",
            Opt("Передать свиток", "Кузнец бесплатно улучшит одну карту", () =>
            {
                gm.TakeItem("SealedScroll");
                gm.SetFlag(QuestFlag, "Delivered");
                RoomManager.Instance.ForceMerchant(MerchantKind.Collector);
                Reforge("«Ладно. За доставку — перекую что скажешь.»\n«Если снова встретишь Собирателя, передай ему: я больше не принимаю его долги.»");
            }),
            Opt("Открыть свиток", "Нарушить просьбу Собирателя и посмотреть, что внутри", () =>
            {
                gm.TakeItem("SealedScroll");
                gm.GiveItem("OpenedScroll");
                gm.SetFlag(QuestFlag, "Opened");
                RoomManager.Instance.ForceMerchant(MerchantKind.Collector);
                var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 1);
                var card = pool.Count > 0 ? pool[0] : null;
                if (card != null) gm.playerDeck.Add(card);
                Say($"Печать хрустит. Внутри — «{(card != null ? card.cardName : "пусто")}».\nКузнец качает головой: «Сорванная печать. Улучшать бесплатно не стану. Но торговать — торгую.»", ShopOptions());
            }));
    }
}
