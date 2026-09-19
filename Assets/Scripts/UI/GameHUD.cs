using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    Text roomText;
    Text statsText;
    Text notifyText;
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
        roomText = UIFactory.CreateText(canvas, "RoomText", "", 26, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(20, -16), new Vector2(500, 36));
        statsText = UIFactory.CreateText(canvas, "StatsText", "", 22, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(20, -52), new Vector2(600, 90));
        notifyText = UIFactory.CreateText(canvas, "NotifyText", "", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(900, 60));
        notifyText.color = new Color(1f, 0.9f, 0.5f);
        roomText.raycastTarget = false;
        statsText.raycastTarget = false;
        notifyText.raycastTarget = false;
    }

    static IEnumerable<EnemyData> MarkedElites(GameManager gm)
    {
        var seen = new HashSet<EnemyData>();
        foreach (var card in gm.playerDeck)
            if (card != null && card.eliteMark != null && seen.Add(card.eliteMark))
                yield return card.eliteMark;
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
            string stats = $"HP {gm.currentHP}/{gm.MaxHP}   Колода: {gm.playerDeck.Count}   Комнат: {gm.roomsVisited}";
            foreach (var elite in MarkedElites(gm))
                stats += $"\n<color=#d9a6ff>{elite.enemyName}: +{gm.EliteChance(elite)}% за фиолетовой дверью</color>";
            statsText.text = stats;
        }
        if (Time.time > notifyUntil) notifyText.text = "";
    }
}
