using UnityEngine;

public enum ObligationType
{
    Credit,
    PledgeFights,
    PledgeNoHeal
}

[System.Serializable]
public class Obligation
{
    public ObligationType type;
    public string source;
    public int roomsRemaining;
    public int fightsRequired;
    public int fightsWon;
    public int hpCost;
    public CardData card;

    public string HudText
    {
        get
        {
            switch (type)
            {
                case ObligationType.Credit: return $"Кредит ({source}): расплата через {roomsRemaining} комн.";
                case ObligationType.PledgeFights: return $"Залог ({source}): побед {fightsWon}/{fightsRequired}, осталось {roomsRemaining} комн.";
                case ObligationType.PledgeNoHeal: return $"Залог ({source}): не лечиться, осталось {roomsRemaining} комн.";
                default: return type.ToString();
            }
        }
    }
}
