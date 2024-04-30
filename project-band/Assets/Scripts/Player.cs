using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerType { 
    Bass,
    Drummer,
    Guitar,
    Keyboard,
    Singer
}

public class Player : MonoBehaviour
{
    public PlayerType Type;
    public int Health;
    public int Mana;

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }
}
