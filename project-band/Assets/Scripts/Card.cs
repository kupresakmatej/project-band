using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Attack,
    Heal,
    Support,
    CrowdControl,
    WildCard
}

public enum TargetType
{
    SingleTarget,
    MultipleTarget
}

[System.Serializable]
public class Card : MonoBehaviour
{
    public string CardName;
    public CardType Type;
    public Ability Ability = new Ability();
}

public class CardAbilities : MonoBehaviour
{
    public List<Card> Cards = new List<Card>();

    void Start()
    {
        InitializeCards();
    }

    void InitializeCards()
    {
        Card attackCard = new Card
        {
            CardName = "Attack Card",
            Type = CardType.Attack,
            Ability = new Ability { Name = "Attack single target", BaseValue = 10, Target = TargetType.SingleTarget }
        };
        Cards.Add(attackCard);
    }

    void Update()
    {

    }
}
