using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Нарисованные элементы интерфейса (9-slice спрайты). Если чего-то нет — UI рисуется цветными прямоугольниками.
[CreateAssetMenu(fileName = "UISkin", menuName = "CardGame/UI Skin")]
public class UISkin : ScriptableObject
{
    public static UISkin Instance { get; set; }

    [Header("Шрифты")]
    [Tooltip("Основной текст: правила карт, реплики, подсказки")]
    public Font bodyFont;
    [Tooltip("Заголовки: названия карт, кнопки, таблички")]
    public Font titleFont;

    [Header("Кнопки")]
    public Sprite mainButton;
    public Sprite button;
    public Sprite closeButton;

    [Header("Панели")]
    public Sprite panelDialogue;
    public Sprite panelMenu;
    public Sprite labelTitle;

    [Header("Полоски здоровья")]
    public Sprite hpFrame;
    public Sprite hpFill;
    public Sprite hpFillEnemy;
    [Tooltip("Прозрачный «жёлоб» внутри рамки, в долях от её размера (xMin, yMin, xMax, yMax)")]
    public Vector4 hpFrameInner = new Vector4(0.087f, 0.305f, 0.912f, 0.694f);
    public Sprite blockIcon;

    [Header("Иконки статусов (id: poison, burn, rage, thorns, weak, molten, noblock, noattack, hand, hidden, combo, retaliation)")]
    public List<StatusIconEntry> statusIcons = new List<StatusIconEntry>();
    [System.Serializable] public class StatusIconEntry { public string id; public Sprite sprite; }
    public Sprite StatusIcon(string id)
    {
        foreach (var e in statusIcons) if (e.id == id && e.sprite != null) return e.sprite;
        return null;
    }

    [Header("Цвет текста на пергаменте")]
    public Color parchmentText = new Color(0.22f, 0.15f, 0.09f);
    public Color parchmentTitle = new Color(0.45f, 0.14f, 0.08f);

    public static Sprite Get(System.Func<UISkin, Sprite> pick) => Instance != null ? pick(Instance) : null;

    // Ставит спрайт на Image как 9-slice; без спрайта оставляет заливку цветом
    public static bool Apply(Image img, Sprite sprite, Color fallback)
    {
        if (sprite == null)
        {
            img.sprite = null;
            img.color = fallback;
            return false;
        }
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        return true;
    }
}
