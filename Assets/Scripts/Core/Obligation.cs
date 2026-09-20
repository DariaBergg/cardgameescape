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
                case ObligationType.Credit: return L.F("Кредит ({0}): расплата через {1} комн.", source, roomsRemaining);
                case ObligationType.PledgeFights: return L.F("Залог ({0}): побед {1}/{2}, осталось {3} комн.", source, fightsWon, fightsRequired, roomsRemaining);
                case ObligationType.PledgeNoHeal: return L.F("Залог ({0}): не лечиться, осталось {1} комн.", source, roomsRemaining);
                default: return type.ToString();
            }
        }
    }
}
