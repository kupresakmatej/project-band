using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UsedOn
{
    Team,
    Enemies
}

[System.Serializable]
public class Ability
{
    public string Name;
    public int BaseValue;
    public int AdditionalValue;
    public TargetType Target;
    public UsedOn UsedOn;
}