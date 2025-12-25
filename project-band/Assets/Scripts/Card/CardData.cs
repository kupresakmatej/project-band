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
    [Range(1, 5)] public int maxMultiTargets = 1; // Configurable number of targets for multi-target cards
    [SerializeField] public GameObject hitEffectPrefab;

    public CardData(string name, Sprite image, string description, PlayerType type, int dmgBonus, int hpBonus, int multiTargets = 1)
    {
        cardName = name;
        cardImage = image;
        cardDescription = description;
        cardType = type;
        damageBonus = dmgBonus;
        healthBonus = hpBonus;
        maxMultiTargets = multiTargets;
    }
}