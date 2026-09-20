using System.Collections.Generic;
using UnityEngine;

public class CollectorVisit : MerchantVisit
{
    const string QuestFlag = "Quest_Merchant_Scroll";

    protected override void Begin()
    {
        Visits++;
        string quest = gm.Flag(QuestFlag);

        if (quest == null && Visits == 1)
            OfferQuest();
        else if (quest == "Delivered" || quest == "Opened")
            SecondMeeting(quest == "Delivered");
        else if (quest == "SealDelivered")
            ThirdMeetingDelivered();
        else if (quest == "SealKept")
            ThirdMeetingKept();
        else
            Shop();
    }

    void OfferQuest()
    {
        Say("Собиратель перебирает связки карт, не глядя на тебя.\n«Услуга за услугу. Если встретишь Кузнеца — передай ему этот запечатанный свиток. Не открывай его.»",
            Opt("Взять свиток", "Квестовый предмет, места в колоде не занимает", () =>
            {
                gm.GiveItem("SealedScroll");
                gm.SetFlag(QuestFlag, "Active");
                RoomManager.Instance.ForceMerchant(MerchantKind.Blacksmith);
                Say("«Кузнец сам найдётся. Они всегда находятся, когда должны.»\nА пока — обычные дела.", ShopOptions());
            }),
            Opt("Отказаться", "Ничего не происходит, торговец работает как обычно", () =>
            {
                gm.SetFlag(QuestFlag, "Declined");
                Shop();
            }));
    }

    void SecondMeeting(bool honest)
    {
        Say("Собиратель поднимает глаза от свитков.\n«Ну? Что сказал Кузнец?»",
            Opt(honest ? "«Он больше не принимает твои долги.»" : "«Я... открыл свиток.»", "", () =>
            {
                if (honest)
                {
                    Say("Собиратель усмехается.\n«Так и знал. Что ж, ты сделал больше, чем большинство. Для таких у меня есть особая услуга.»",
                        Opt("Коллекционерский обмен", "Удалить 2 карты из колоды — выбрать 1 редкую из 3", CollectorsExchange),
                        Opt("Отказаться", "", GiveSeal));
                }
                else
                {
                    Say("Собиратель долго молчит.\n«Любопытство — дорогое удовольствие. Особого обмена не будет. Но дело есть дело.»",
                        Opt("Дальше", "", GiveSeal));
                }
            }));
    }

    void CollectorsExchange()
    {
        ui.Hide();
        DeckPickerUI.Get().Show("Обмен: первая карта на удаление", gm.playerDeck, c => !c.permanent, first =>
        {
            gm.RemoveCard(first);
            DeckPickerUI.Get().Show("Обмен: вторая карта на удаление", gm.playerDeck, c => !c.permanent, second =>
            {
                gm.RemoveCard(second);
                var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 3);
                CardChoiceUI.Get().Show("Выбери редкую карту", pool, chosen =>
                {
                    if (chosen != null) gm.playerDeck.Add(chosen);
                    GiveSeal();
                }, allowSkip: false);
            }, () => { gm.playerDeck.Add(first); GiveSeal(); });
        }, GiveSeal);
    }

    void GiveSeal()
    {
        gm.GiveItem("BrokenSeal");
        gm.SetFlag(QuestFlag, "SealGiven");
        RoomManager.Instance.ForceMerchant(MerchantKind.Usurer);
        Say("Собиратель достаёт из-под плаща расколотый кусок воска в оправе.\n«Сломанная печать. Отнеси её Ростовщику. Он поймёт.»\nА пока — обычные дела.", ShopOptions());
    }

    void ThirdMeetingDelivered()
    {
        Say("Собиратель встречает тебя у входа.\n«Ну? Он взял печать?»",
            Opt("«Долг закрыт. Услуга — нет.»", "", () =>
            {
                Say("Собиратель на секунду замолкает.\n«Конечно. Было бы слишком просто, если бы он просто отпустил долг.»\n«Ты прошёл всё это ради моих старых счетов. Я не люблю быть должным. Последний обмен — только для тебя.»",
                    Opt("Последний обмен", "Отдай одну карту — выбери 1 из 3 улучшенных редких", FinalExchange),
                    Opt("Отказаться", "", () =>
                    {
                        FinishQuest("Completed");
                        Say("«Разумно. Не всякая вещь становится ценнее после обмена.»", ShopOptions());
                    }));
            }));
    }

    void FinalExchange()
    {
        ui.Hide();
        DeckPickerUI.Get().Show("Последний обмен: какую карту отдать?", gm.playerDeck, c => !c.permanent, given =>
        {
            var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 3);
            var upgraded = new List<CardData>();
            foreach (var c in pool) upgraded.Add(c.CreateUpgradedCopy());
            CardChoiceUI.Get().Show("Выбери улучшенную редкую карту", upgraded, chosen =>
            {
                gm.ReplaceCard(given, chosen);
                FinishQuest("Completed");
                Done("«Теперь мы в расчёте. Почти.»");
            }, allowSkip: false);
        }, () =>
        {
            FinishQuest("Completed");
            Say("«Разумно. Не всякая вещь становится ценнее после обмена.»", ShopOptions());
        });
    }

    void ThirdMeetingKept()
    {
        Say("Собиратель окидывает тебя взглядом и задерживается на печати.\n«Полагаю, печать до него не дошла.»",
            Opt("«Я оставил её себе.»", "", () =>
            {
                FinishQuest("CompletedKeptSeal");
                Say("«Хм. Значит, ты нашёл ей лучшее применение.»\nСобиратель пожимает плечами и возвращается к товару.", ShopOptions());
            }));
    }

    void FinishQuest(string state)
    {
        gm.SetFlag(QuestFlag, state);
    }

    void Shop()
    {
        Say(Greeting(), ShopOptions());
    }

    string Greeting()
    {
        string quest = gm.Flag(QuestFlag);
        if (quest == "Completed") return "«С долгами покончено. Теперь можем говорить о действительно ценных вещах.»";
        if (quest == "CompletedKeptSeal") return "«Береги эту печать. Такие вещи имеют привычку требовать больше, чем обещают.»";
        return "Собиратель раскладывает карты веером.\n«Обмен, контракт или удача вслепую. Выбирай.»";
    }

    MerchantUI.Option[] ShopOptions()
    {
        return new[]
        {
            Opt("Обмен", "Отдай карту — выбери 1 из 3 той же редкости", Exchange),
            Opt("Контракт", "Редкая карта бесплатно, но в колоду добавится Рана", Contract),
            Opt("Неизвестный свиток", "Отдай обычную карту — получи случайную карту, редкость скрыта", UnknownScroll),
            Leave()
        };
    }

    void Exchange()
    {
        ui.Hide();
        DeckPickerUI.Get().Show("Обмен: какую карту отдать?", gm.playerDeck, c => !c.permanent, given =>
        {
            var pool = CardPools.Instance.RandomOfRarity(given.rarity, 3, new List<CardData> { given.BaseCard });
            if (pool.Count == 0) { Shop(); return; }
            CardChoiceUI.Get().Show("Выбери карту взамен", pool, chosen =>
            {
                if (chosen == null) { Shop(); return; }
                gm.ReplaceCard(given, chosen);
                Done($"«{chosen.cardName}. Хороший выбор. Или нет — время покажет.»");
            });
        }, Shop);
    }

    void Contract()
    {
        var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 1);
        if (pool.Count == 0) { Shop(); return; }
        var rare = pool[0];
        var wound = CardPools.Instance.WoundFor(gm.selectedCharacter);
        Say($"«{rare.cardName}. Бесплатно. Почти: в колоду ляжет Рана. Мелочь, правда?»",
            OptWithTooltip("Подписать", $"Получить «{rare.cardName}» и «{wound.cardName}»", rare.TooltipText + "\n\n" + wound.TooltipText, () =>
            {
                gm.playerDeck.Add(rare);
                gm.playerDeck.Add(wound);
                Done("«Приятно иметь дело с решительными.»");
            }),
            Opt("Отказаться", "", Shop));
    }

    void UnknownScroll()
    {
        ui.Hide();
        DeckPickerUI.Get().Show("Неизвестный свиток: отдай обычную карту", gm.playerDeck, c => c.rarity == CardRarity.Common && !c.permanent, given =>
        {
            var card = CardPools.Instance.RandomAny();
            gm.ReplaceCard(given, card);
            string rarity = card.rarity == CardRarity.Rare ? "Редкая. Повезло." : "Обычная. Удача любит настойчивых.";
            Done($"Свиток рассыпается в руках. Внутри — «{card.cardName}».\n«{rarity}»");
        }, Shop);
    }
}
