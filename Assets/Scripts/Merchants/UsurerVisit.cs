using System.Collections.Generic;
using UnityEngine;

public class UsurerVisit : MerchantVisit
{
    const string QuestFlag = "Quest_Merchant_Scroll";
    const string PaidFlag = "Usurer_CreditPaid";
    const int CreditRooms = 5;
    const int PledgeRooms = 5;

    int CreditCost => gm.HasRelic("BrokenSeal") ? 10 : 8;
    bool FirstCreditFree => gm.HasRelic("BrokenSeal") && !gm.HasFlag("Usurer_SealCreditUsed", "1");

    protected override void Begin()
    {
        Visits++;
        if (gm.HasFlag(QuestFlag, "SealGiven") && gm.HasItem("BrokenSeal"))
            SealDialogue();
        else
            Shop();
    }

    void Shop()
    {
        Say("Ростовщик вежливо улыбается, не поднимая глаз от книги.\n«Всё имеет цену. Не всегда её платят сразу.»", ShopOptions());
    }

    MerchantUI.Option[] ShopOptions()
    {
        bool paidBefore = gm.HasFlag(PaidFlag, "1");
        string creditDesc = FirstCreditFree
            ? "Редкая карта. Сломанная печать покрывает первую плату."
            : paidBefore ? $"Редкая карта. Через {CreditRooms} комнат: −{CreditCost} HP." : $"Редкая карта. Через {CreditRooms} комнат Ростовщик потребует плату кровью.";
        return new[]
        {
            Opt("Кредит", creditDesc, Credit, !gm.HasObligation(ObligationType.Credit)),
            Opt("Залог", $"Улучшенная редкая карта. Условие: {PledgeRooms} комнат без лечения", Pledge, !gm.HasObligation(ObligationType.PledgeNoHeal)),
            Opt("Кровавый договор", "+5 к максимальному HP, в колоду ложится Долг (не удаляется)", BloodPact),
            Leave()
        };
    }

    void Credit()
    {
        var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 3);
        ui.Hide();
        CardChoiceUI.Get().Show("Кредит: возьми карту", pool, chosen =>
        {
            if (chosen == null) { Shop(); return; }
            gm.playerDeck.Add(chosen);
            if (FirstCreditFree)
            {
                gm.SetFlag("Usurer_SealCreditUsed", "1");
                Done("Ростовщик косится на печать у тебя на поясе.\n«Считай, что эта — за старый долг. Следующая будет дороже.»");
                return;
            }
            gm.AddObligation(new Obligation
            {
                type = ObligationType.Credit,
                source = data.displayName,
                roomsRemaining = CreditRooms,
                hpCost = CreditCost
            });
            gm.SetFlag(PaidFlag, "1");
            Done("«Возьми. Вернёшь, когда придёт срок. Я всегда получаю своё.»");
        });
    }

    void Pledge()
    {
        var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 3);
        var upgraded = new List<CardData>();
        foreach (var c in pool) upgraded.Add(c.CreateUpgradedCopy());
        ui.Hide();
        CardChoiceUI.Get().Show($"Залог: выбери карту. {PledgeRooms} комнат без лечения — или она ослабнет", upgraded, chosen =>
        {
            if (chosen == null) { Shop(); return; }
            gm.playerDeck.Add(chosen);
            gm.AddObligation(new Obligation
            {
                type = ObligationType.PledgeNoHeal,
                source = data.displayName,
                roomsRemaining = PledgeRooms,
                card = chosen
            });
            Done("«Береги мою вещь. И не пытайся залечить то, что должно болеть.»");
        });
    }

    void BloodPact()
    {
        gm.AddMaxHP(5);
        gm.playerDeck.Add(CardPools.Instance.debt);
        Done("Ростовщик делает пометку в книге.\n«Теперь ты можешь выдержать больше. Когда-нибудь и я попрошу кое-что взамен.»");
    }

    void SealDialogue()
    {
        Say("Ростовщик медленно закрывает книгу.\n«Давно я не видел эту печать. Значит, старые долги всё-таки пережили своих хозяев.»",
            Opt("Отдать печать", "Закрыть старый долг. Ростовщик окажет одну услугу", GiveSeal),
            Opt("Оставить себе", "Печать станет реликвией: первый кредит без платы. Но сделки Ростовщика станут хуже", KeepSeal));
    }

    void GiveSeal()
    {
        gm.TakeItem("BrokenSeal");
        gm.SetFlag(QuestFlag, "SealDelivered");
        RoomManager.Instance.ForceMerchant(MerchantKind.Collector);

        var options = new List<MerchantUI.Option>();
        var debt = gm.playerDeck.Find(c => c != null && c.permanent);
        var credit = gm.obligations.Find(o => o.type == ObligationType.Credit);
        var pledge = gm.obligations.Find(o => o.type == ObligationType.PledgeNoHeal);

        if (debt != null) options.Add(Opt("Списать Долг", $"Удалить «{debt.cardName}» из колоды", () => { gm.RemoveCard(debt, true); AfterSeal(); }));
        if (credit != null) options.Add(Opt("Простить кредит", "Отменить активный кредит без потери HP", () => { gm.CancelObligation(credit); AfterSeal(); }));
        if (pledge != null) options.Add(Opt("Снять залог", "Улучшенная карта остаётся без условия", () => { gm.CancelObligation(pledge); AfterSeal(); }));
        if (options.Count == 0)
        {
            options.Add(Opt("Удалить плохую карту", "Убрать одну карту из колоды", () =>
            {
                ui.Hide();
                DeckPickerUI.Get().Show("Какую карту убрать?", gm.playerDeck, null, card => { gm.RemoveCard(card, true); AfterSeal(); }, AfterSeal);
            }));
        }

        Say("Ростовщик прячет печать в ящик стола.\n«Долг закрыт. Одна услуга — за счёт заведения.»", options.ToArray());
    }

    void AfterSeal()
    {
        Say("«Скажи ему: долг закрыт. Услуга — нет.»", ShopOptions());
    }

    void KeepSeal()
    {
        gm.TakeItem("BrokenSeal");
        gm.GiveRelic("BrokenSeal");
        gm.SetFlag(QuestFlag, "SealKept");
        RoomManager.Instance.ForceMerchant(MerchantKind.Collector);
        Say("Ростовщик смотрит на печать долгим взглядом и ничего не говорит.\n«Как угодно. Вещи вроде этой сами выбирают хозяина. Но мои условия теперь... иные.»", ShopOptions());
    }
}
