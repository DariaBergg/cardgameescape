using UnityEngine;

public enum MerchantKind
{
    Collector,
    Blacksmith,
    Usurer
}

[CreateAssetMenu(fileName = "NewMerchant", menuName = "CardGame/Merchant")]
public class MerchantData : ScriptableObject
{
    public MerchantKind kind;
    public string displayName;
    [Tooltip("Спрайт персонажа в комнате (стоит справа)")]
    public Sprite sprite;
    [Tooltip("Фон лавки")]
    public Sprite background;
    public Vector3 position = new Vector3(3.5f, -1.2f, 0);
    [Tooltip("Не показывать героя в комнате этого купца")]
    public bool hidePlayer;
    public float spriteHeight = 3.2f;
    public Color placeholderColor = Color.gray;

    public string Id => kind.ToString();
}
