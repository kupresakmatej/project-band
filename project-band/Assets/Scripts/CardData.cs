using UnityEngine;

[System.Serializable]
public class CardData
{
    public Sprite cardImage;
    public string cardName;
    public string cardDescription;
    public PlayerType cardType;

    public int damageBonus;
    public int healthBonus;

    public CardData(string name, Sprite image, string description, PlayerType type, int dmgBonus, int hpBonus)
    {
        cardName = name;
        cardImage = image;
        cardDescription = description;
        cardType = type;
        damageBonus = dmgBonus;
        healthBonus = hpBonus;
    }
}
