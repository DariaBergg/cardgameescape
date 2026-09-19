using UnityEngine;

[CreateAssetMenu(fileName = "CardVisuals", menuName = "CardGame/Card Visuals")]
public class CardVisuals : ScriptableObject
{
    public static CardVisuals Instance { get; set; }

    public Sprite template;

    [Header("Разметка шаблона (доли от ширины/высоты карты)")]
    public Rect artWindow = new Rect(0.124f, 0.100f, 0.775f, 0.336f);
    public Rect nameArea = new Rect(0.19f, 0.486f, 0.62f, 0.07f);
    public Rect textArea = new Rect(0.15f, 0.638f, 0.70f, 0.22f);

    public Color nameColor = new Color(0.23f, 0.16f, 0.07f);
    public Color textColor = new Color(0.17f, 0.14f, 0.09f);
}
