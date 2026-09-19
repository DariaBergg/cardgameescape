using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    Text roomText;
    Text eliteText;
    Text goldText;
    Text itemsText;
    Text notifyText;
    UnitFrame playerFrame;
    SpriteRenderer playerRenderer;
    float notifyUntil;

    public static GameHUD Create()
    {
        var canvas = UIFactory.CreateCanvas("HUDCanvas", 0);
        var hud = canvas.gameObject.AddComponent<GameHUD>();
        hud.Build(canvas.transform);
        return hud;
    }

    void Build(Transform canvas)
    {
        roomText = UIFactory.CreateText(canvas, "RoomText", "", 20, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(20, -14), new Vector2(500, 28));
        roomText.color = new Color(1f, 1f, 1f, 0.75f);
        eliteText = UIFactory.CreateText(canvas, "EliteText", "", 16, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(20, -42), new Vector2(600, 120));
        eliteText.color = new Color(0.85f, 0.65f, 1f);
        goldText = UIFactory.CreateText(canvas, "GoldText", "", 20, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-160, -20), new Vector2(200, 30));
        goldText.color = new Color(1f, 0.85f, 0.35f);
        goldText.raycastTarget = false;
        itemsText = UIFactory.CreateText(canvas, "ItemsText", "", 15, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-16, -60), new Vector2(400, 80));
        itemsText.color = new Color(0.9f, 0.9f, 0.75f);
        itemsText.raycastTarget = false;
        notifyText = UIFactory.CreateText(canvas, "NotifyText", "", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(900, 60));
        notifyText.color = new Color(1f, 0.9f, 0.5f);
        roomText.raycastTarget = false;
        eliteText.raycastTarget = false;
        notifyText.raycastTarget = false;

        playerFrame = UnitFrame.Create(canvas, "PlayerFrame", new Color(0.3f, 0.8f, 0.35f));
        playerFrame.SetAnchor(PlayerTop);

        exitButton = UIFactory.CreateButton(canvas, "ExitButton", "Выйти", 22, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(220, 56), new Color(0.3f, 0.3f, 0.4f, 0.95f));
        exitButton.onClick.AddListener(() => { var cb = onExit; HideExitButton(); cb?.Invoke(); });
        exitButton.gameObject.SetActive(false);

        deckButton = UIFactory.CreateButton(canvas, "DeckButton", "Колода", 18, new Vector2(1, 1), new Vector2(-16, -14), new Vector2(130, 40), new Color(0.25f, 0.25f, 0.35f, 0.9f));
        deckButton.onClick.AddListener(OpenDeck);
        deckButton.gameObject.SetActive(false);
    }

    Button deckButton;
    public int deckUnlockCombats = 3;

    void OpenDeck()
    {
        var gm = GameManager.Instance;
        DeckPickerUI.Get().Show($"Колода — {gm.playerDeck.Count} карт", gm.playerDeck, card => false, null, null, closeLabel: "Закрыть");
    }

    Button exitButton;
    System.Action onExit;

    public void ShowExitButton(System.Action callback)
    {
        onExit = callback;
        exitButton.gameObject.SetActive(true);
    }

    public void HideExitButton()
    {
        onExit = null;
        exitButton.gameObject.SetActive(false);
    }

    Vector3 PlayerTop()
    {
        if (playerRenderer == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            playerRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
        }
        if (playerRenderer == null) return Vector3.zero;
        var b = playerRenderer.bounds;
        return new Vector3(b.center.x, b.max.y + 0.05f, 0);
    }

    static IEnumerable<EnemyData> MarkedElites(GameManager gm)
    {
        var seen = new HashSet<EnemyData>();
        foreach (var card in gm.playerDeck)
            if (card != null && card.eliteMark != null && seen.Add(card.eliteMark))
                yield return card.eliteMark;
    }

    public static string ItemName(string id)
    {
        switch (id)
        {
            case "SealedScroll": return "Запечатанный свиток";
            case "OpenedScroll": return "Вскрытый свиток";
            case "BrokenSeal": return "Сломанная печать";
            default: return id;
        }
    }

    public void SetRoom(string roomName)
    {
        roomText.text = roomName;
    }

    public void Notify(string message, float seconds = 3f)
    {
        notifyText.text = message;
        notifyUntil = Time.time + seconds;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            var combat = CombatManager.Instance;
            bool inCombat = combat != null && combat.CombatActive;
            string name = gm.selectedCharacter != null ? gm.selectedCharacter.characterName : "";
            playerFrame.Set(name, gm.currentHP, gm.MaxHP, inCombat ? combat.PlayerBlock : 0, inCombat ? combat.PlayerStatusText : "");

            string elites = "";
            foreach (var elite in MarkedElites(gm))
                elites += $"{elite.enemyName}: +{gm.EliteChance(elite)}% за фиолетовой дверью\n";
            foreach (var o in gm.obligations)
                elites += $"<color=#ffb070>{o.HudText}</color>\n";
            eliteText.text = elites;

            goldText.text = gm.gold > 0 ? $"Золото: {gm.gold}" : "";
            string held = "";
            foreach (var item in gm.items) held += ItemName(item) + "\n";
            foreach (var relic in gm.relics) held += "<color=#ffd27f>" + ItemName(relic) + "</color>\n";
            itemsText.text = held;

            deckButton.gameObject.SetActive(gm.combatsWon >= deckUnlockCombats && !inCombat);
        }
        if (Time.time > notifyUntil) notifyText.text = "";
    }
}
