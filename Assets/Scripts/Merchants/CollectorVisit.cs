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
        Say(L.T("Собиратель перебирает связки карт, не глядя на тебя.\n«Услуга за услугу. Если встретишь Кузнеца — передай ему этот запечатанный свиток. Не открывай его.»"),
            Opt(L.T("Взять свиток"), L.T("Квестовый предмет, места в колоде не занимает. Собиратель окажет услугу: подарит карту на выбор"), () =>
            {
                gm.GiveItem("SealedScroll");
                gm.SetFlag(QuestFlag, "Active");
                RoomManager.Instance.ForceMerchant(MerchantKind.Blacksmith);
                ui.Hide();
                // Услуга за услугу: подарок — 1 из 3 обычных карт
                var gift = CardPools.Instance.RandomOfRarity(CardRarity.Common, 3);
                CardChoiceUI.Get().Show(L.T("Услуга Собирателя: выбери карту в подарок"), gift, chosen =>
                {
                    if (chosen != null) gm.playerDeck.Add(chosen);
                    Say(L.T("«Кузнец сам найдётся. Они всегда находятся, когда должны.»\nА пока — обычные дела."), ShopOptions());
                });
            }),
            Opt(L.T("Отказаться"), L.T("Собиратель обидится: заберёт одну обычную карту «в счёт услуг», а сделки станут хуже"), () =>
            {
                gm.SetFlag(QuestFlag, "Declined");
                // Плата за отказ: одна случайная обычная карта уходит Собирателю
                var commons = gm.playerDeck.FindAll(c => c != null && c.rarity == CardRarity.Common && !c.permanent && !c.unplayable);
                if (commons.Count > 0)
                {
                    var taken = commons[UnityEngine.Random.Range(0, commons.Count)];
                    gm.RemoveCard(taken);
                    Say(L.F("Собиратель молча вынимает из твоей колоды «{0}».\n«В счёт будущих услуг. Торговать будем — но на моих условиях.»", L.T(taken.cardName)), ShopOptions());
                }
                else Say(L.T("Собиратель пожимает плечами.\n«Как хочешь. Но торговать будем на моих условиях.»"), ShopOptions());
            }));
    }

    void SecondMeeting(bool honest)
    {
        Say(L.T("Собиратель поднимает глаза от свитков.\n«Ну? Что сказал Кузнец?»"),
            Opt(honest ? L.T("«Он больше не принимает твои долги.»") : L.T("«Я... открыл свиток.»"), "", () =>
            {
                if (honest)
                {
                    Say(L.T("Собиратель усмехается.\n«Так и знал. Что ж, ты сделал больше, чем большинство. Для таких у меня есть особая услуга.»"),
                        Opt(L.T("Коллекционерский обмен"), L.T("Удалить 2 карты из колоды — выбрать 1 редкую из 3"), CollectorsExchange),
                        Opt(L.T("Отказаться"), "", GiveSeal));
                }
                else
                {
                    Say(L.T("Собиратель долго молчит.\n«Любопытство — дорогое удовольствие. Особого обмена не будет. Но дело есть дело.»"),
                        Opt(L.T("Дальше"), "", GiveSeal));
                }
            }));
    }

    void CollectorsExchange()
    {
        ui.Hide();
        DeckPickerUI.Get().Show(L.T("Обмен: первая карта на удаление"), gm.playerDeck, c => !c.permanent, first =>
        {
            gm.RemoveCard(first);
            DeckPickerUI.Get().Show(L.T("Обмен: вторая карта на удаление"), gm.playerDeck, c => !c.permanent, second =>
            {
                gm.RemoveCard(second);
                var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 3);
                CardChoiceUI.Get().Show(L.T("Выбери редкую карту"), pool, chosen =>
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
        Say(L.T("Собиратель достаёт из-под плаща расколотый кусок воска в оправе.\n«Сломанная печать. Отнеси её Ростовщику. Он поймёт.»\nА пока — обычные дела."), ShopOptions());
    }

    void ThirdMeetingDelivered()
    {
        Say(L.T("Собиратель встречает тебя у входа.\n«Ну? Он взял печать?»"),
            Opt(L.T("«Долг закрыт. Услуга — нет.»"), "", () =>
            {
                Say(L.T("Собиратель на секунду замолкает.\n«Конечно. Было бы слишком просто, если бы он просто отпустил долг.»\n«Ты прошёл всё это ради моих старых счетов. Я не люблю быть должным. Последний обмен — только для тебя.»"),
                    Opt(L.T("Последний обмен"), L.T("Отдай одну карту — выбери 1 из 3 улучшенных редких"), FinalExchange),
                    Opt(L.T("Отказаться"), "", () =>
                    {
                        FinishQuest("Completed");
                        Say(L.T("«Разумно. Не всякая вещь становится ценнее после обмена.»"), ShopOptions());
                    }));
            }));
    }

    void FinalExchange()
    {
        ui.Hide();
        DeckPickerUI.Get().Show(L.T("Последний обмен: какую карту отдать?"), gm.playerDeck, c => !c.permanent, given =>
        {
            var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 3);
            var upgraded = new List<CardData>();
            foreach (var c in pool) upgraded.Add(c.CreateUpgradedCopy());
            CardChoiceUI.Get().Show(L.T("Выбери улучшенную редкую карту"), upgraded, chosen =>
            {
                gm.ReplaceCard(given, chosen);
                FinishQuest("Completed");
                Done(L.T("«Теперь мы в расчёте. Почти.»"));
            }, allowSkip: false);
        }, () =>
        {
            FinishQuest("Completed");
            Say(L.T("«Разумно. Не всякая вещь становится ценнее после обмена.»"), ShopOptions());
        });
    }

    void ThirdMeetingKept()
    {
        Say(L.T("Собиратель окидывает тебя взглядом и задерживается на печати.\n«Полагаю, печать до него не дошла.»"),
            Opt(L.T("«Я оставил её себе.»"), "", () =>
            {
                FinishQuest("CompletedKeptSeal");
                Say(L.T("«Хм. Значит, ты нашёл ей лучшее применение.»\nСобиратель пожимает плечами и возвращается к товару."), ShopOptions());
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
        if (quest == "Completed") return L.T("«С долгами покончено. Теперь можем говорить о действительно ценных вещах.»");
        if (quest == "CompletedKeptSeal") return L.T("«Береги эту печать. Такие вещи имеют привычку требовать больше, чем обещают.»");
        return L.T("Собиратель раскладывает карты веером.\n«Обмен, контракт или удача вслепую. Выбирай.»");
    }

    bool Declined => gm.HasFlag(QuestFlag, "Declined");
    int ExchangeChoices => Declined ? 2 : 3;

    // Отказавшемуся — урезанная лавка: обмен похуже и лотерея вместо контракта
    MerchantUI.Option[] ShopOptions()
    {
        if (Declined)
            return new[]
            {
                Opt(L.T("Обмен"), L.T("Отдай карту — выбери 1 из 2 той же редкости"), Exchange),
                Opt(L.T("Неизвестный свиток"), L.T("Отдай обычную карту — получи случайную обычную карту"), UnknownScroll),
                Leave()
            };
        return new[]
        {
            Opt(L.T("Обмен"), L.T("Отдай карту — выбери 1 из 3 той же редкости"), Exchange),
            Opt(L.T("Контракт"), L.T("Редкая карта бесплатно, но в колоду добавится Рана"), Contract),
            Leave()
        };
    }

    void Exchange()
    {
        ui.Hide();
        DeckPickerUI.Get().Show(L.T("Обмен: какую карту отдать?"), gm.playerDeck, c => !c.permanent, given =>
        {
            var pool = CardPools.Instance.RandomOfRarity(given.rarity, ExchangeChoices, new List<CardData> { given.BaseCard });
            if (pool.Count == 0) { Shop(); return; }
            CardChoiceUI.Get().Show(L.T("Выбери карту взамен"), pool, chosen =>
            {
                if (chosen == null) { Shop(); return; }
                gm.ReplaceCard(given, chosen);
                Done(L.F("«{0}. Хороший выбор. Или нет — время покажет.»", L.T(chosen.cardName)));
            });
        }, Shop);
    }

    void Contract()
    {
        var pool = CardPools.Instance.RandomOfRarity(CardRarity.Rare, 1);
        if (pool.Count == 0) { Shop(); return; }
        var rare = pool[0];
        var wound = CardPools.Instance.WoundFor(gm.selectedCharacter);
        Say(L.F("«{0}. Бесплатно. Почти: в колоду ляжет Рана. Мелочь, правда?»", L.T(rare.cardName)),
            OptWithTooltip(L.T("Подписать"), L.F("Получить «{0}» и «{1}»", L.T(rare.cardName), L.T(wound.cardName)), rare.TooltipText + "\n\n" + wound.TooltipText, () =>
            {
                gm.playerDeck.Add(rare);
                gm.playerDeck.Add(wound);
                ui.Hide();
                CardChoiceUI.Get().Reveal(L.T("Контракт подписан"), new List<CardData> { rare, wound }, () => Done(L.T("«Приятно иметь дело с решительными.»")));
            }),
            Opt(L.T("Отказаться"), "", Shop));
    }

    void UnknownScroll()
    {
        ui.Hide();
        DeckPickerUI.Get().Show(L.T("Неизвестный свиток: отдай обычную карту"), gm.playerDeck, c => c.rarity == CardRarity.Common && !c.permanent && !c.unplayable, given =>
        {
            var pool = CardPools.Instance.RandomOfRarity(CardRarity.Common, 1, new List<CardData> { given.BaseCard });
            if (pool.Count == 0) { Shop(); return; }
            var card = pool[0];
            gm.ReplaceCard(given, card);
            CardChoiceUI.Get().Reveal(L.T("Свиток рассыпается в руках. Внутри:"), new List<CardData> { card }, () => Done(L.T("«Удача любит настойчивых.»")));
        }, Shop);
    }
}
