using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Экран выбора героя: портреты в каменных рамках, описание и стартовые карты выбранного, кнопка «В путь»
public class CharacterSelectUI : MonoBehaviour
{
    RectTransform panel;
    Text descriptionText;
    RectTransform cardsArea;
    readonly List<Image> frames = new List<Image>();
    readonly List<Image> portraits = new List<Image>();
    List<CharacterData> list;
    CharacterData selected;

    static readonly Vector2 PortraitSize = new Vector2(240, 240);
    static readonly Vector2 CardSize = new Vector2(120, 177);
    static readonly Color Dim = new Color(0.45f, 0.45f, 0.45f, 1f);

    public static CharacterSelectUI Show(Sprite background, List<CharacterData> characters, Action<CharacterData> onChosen)
    {
        var canvas = UIFactory.CreateCanvas("CharacterSelectCanvas", 80);
        var ui = canvas.gameObject.AddComponent<CharacterSelectUI>();
        ui.Build(canvas.transform, background, characters, onChosen);
        return ui;
    }

    void Build(Transform canvas, Sprite background, List<CharacterData> characters, Action<CharacterData> onChosen)
    {
        panel = UIFactory.CreateFullscreenPanel(canvas, "Panel", Color.black);
        var art = UIFactory.CreateFullscreenPanel(panel, "Background", Color.white);
        var artImg = art.GetComponent<Image>();
        artImg.sprite = background;
        artImg.preserveAspect = true;
        artImg.raycastTarget = false;
        UIFactory.CreateFullscreenPanel(panel, "Dim", new Color(0, 0, 0, 0.6f)).GetComponent<Image>().raycastTarget = false;

        list = characters;
        var skin = UISkin.Instance;
        var titlePlate = UIFactory.CreatePanel(panel, "TitlePlate", new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(560, 60), skin != null ? skin.labelTitle : null, Color.clear);
        titlePlate.GetComponent<Image>().raycastTarget = false;
        var title = UIFactory.CreateText(titlePlate, "Title", "Кем идти?", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(480, 50));
        title.font = UIFactory.TitleFont;
        title.color = new Color(0.93f, 0.88f, 0.78f);
        UIFactory.AddShadow(title);

        // Портреты в ряд
        float spacing = PortraitSize.x + 60f;
        float startX = -(characters.Count - 1) * spacing / 2f;
        for (int i = 0; i < characters.Count; i++)
        {
            var character = characters[i];
            var frame = UIFactory.CreatePanel(panel, "Portrait_" + i, new Vector2(0.5f, 0.5f), new Vector2(startX + i * spacing, 140), PortraitSize + new Vector2(40, 40), skin != null ? skin.panelMenu : null, new Color(0.1f, 0.1f, 0.12f));
            var frameImg = frame.GetComponent<Image>();
            frames.Add(frameImg);

            var portrait = UIFactory.CreateRect(frame, "Art", new Vector2(0.5f, 0.5f), Vector2.zero, PortraitSize);
            var portraitImg = portrait.gameObject.AddComponent<Image>();
            portraitImg.sprite = character.portrait != null ? character.portrait : character.sprite;
            portraitImg.preserveAspect = true;
            portraitImg.raycastTarget = false;
            portraits.Add(portraitImg);

            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frameImg;
            button.transition = Selectable.Transition.None; // подсветка выбранного — вручную через цвет рамки
            var captured = character;
            button.onClick.AddListener(() => Select(captured));

            var namePlate = UIFactory.CreatePanel(frame, "NamePlate", new Vector2(0.5f, 0), new Vector2(0, -22), new Vector2(PortraitSize.x + 40, 44), skin != null ? skin.labelTitle : null, Color.clear);
            namePlate.GetComponent<Image>().raycastTarget = false;
            var nameLabel = UIFactory.CreateText(namePlate, "Name", character.characterName, 16, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(PortraitSize.x, 40));
            nameLabel.font = UIFactory.TitleFont;
            nameLabel.resizeTextForBestFit = true;
            nameLabel.resizeTextMinSize = 10;
            nameLabel.resizeTextMaxSize = 16;
            nameLabel.color = new Color(0.93f, 0.88f, 0.78f);
            nameLabel.raycastTarget = false;
            UIFactory.AddShadow(nameLabel);
        }

        // Описание и стартовые карты выбранного героя
        // Описание — на пергаменте, чтобы читалось поверх заставки
        var descPanel = UIFactory.CreatePanel(panel, "DescriptionPanel", new Vector2(0.5f, 0.5f), new Vector2(0, -112), new Vector2(900, 120), skin != null ? skin.panelDialogue : null, new Color(0, 0, 0, 0.7f));
        descPanel.GetComponent<Image>().raycastTarget = false;
        descPanel.GetComponent<Image>().pixelsPerUnitMultiplier = 1.8f; // тоньше рамка для низкой панели
        bool parchment = skin != null && skin.panelDialogue != null;
        descriptionText = UIFactory.CreateText(descPanel, "Description", "", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(820, 96));
        descriptionText.color = parchment ? skin.parchmentText : new Color(0.9f, 0.86f, 0.78f);
        descriptionText.raycastTarget = false;
        cardsArea = UIFactory.CreateRect(panel, "Cards", new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(600, CardSize.y));

        var start = UIFactory.CreateSpriteButton(panel, "Start", "В путь", 30, new Vector2(1, 0), new Vector2(-40, 40), new Vector2(300, 84), skin != null ? skin.mainButton : null, new Color(0.35f, 0.12f, 0.1f, 0.95f));
        start.onClick.AddListener(() =>
        {
            if (selected == null) return;
            Destroy(gameObject);
            onChosen?.Invoke(selected);
        });

        if (characters.Count > 0) Select(characters[0]);
    }

    void Select(CharacterData character)
    {
        selected = character;
        for (int i = 0; i < frames.Count; i++)
        {
            bool active = list[i] == character;
            frames[i].color = active ? Color.white : Dim;
            portraits[i].color = active ? Color.white : Dim;
        }

        descriptionText.text = $"{character.description}\n<color=#8a2a1a><b>Здоровье: {character.maxHP}</b></color>";
        foreach (Transform child in cardsArea) Destroy(child.gameObject);
        var deck = character.startingDeck;
        float spacing = CardSize.x + 20f;
        float startX = -(deck.Count - 1) * spacing / 2f;
        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i] == null) continue;
            var card = CardView.Create(cardsArea, deck[i], new Vector2(0.5f, 0), new Vector2(startX + i * spacing, 0), CardSize);
            card.interactable = false;
        }
    }
}
