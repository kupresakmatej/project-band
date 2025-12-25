using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerType
{
    Bass,
    Drummer,
    Guitar,
    Keyboard,
    Singer
}

public class Player : MonoBehaviour
{
    public PlayerType Type;
    public ParticleSystem highlighterEffect;
    public int MaxHealth = 100;
    public int Health = 100;
    public int Damage;

    public HealthBar healthBar;

    private void Start()
    {
        if (!CompareTag("Ally") && !CompareTag("Enemy"))
        {
            Debug.LogWarning("Player " + gameObject.name + " has no 'Ally' or 'Enemy' tag!");
        }

        Health = MaxHealth;
        healthBar.SetMaxHealth(MaxHealth);
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health < 0) Health = 0;

        healthBar.SetHealth(Health);
    }

    public void Heal(int amount)
    {
        if (Health <= 0) return; // can't heal dead players

        Health += amount;
        if (Health > MaxHealth) Health = MaxHealth;

        healthBar.SetHealth(Health);
    }

    public bool IsAlive() => Health > 0;
}