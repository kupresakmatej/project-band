using System.Collections.Generic;
using UnityEngine;

public class BandData : MonoBehaviour
{
    public static BandData Instance { get; private set; }

    [Header("Band Data")]
    [SerializeField] private int fans = 0; // Fans counter (unbounded)
    // Add more fields for future features
    [SerializeField] private int bandMoney = 0;
    [SerializeField] private string[] bandMembers = new string[0];

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Getter and setter for fans
    public int Fans
    {
        get { return fans; }
        set
        {
            fans = Mathf.Max(0, value); // Prevent negative fans
            Debug.Log($"Fans updated to: {fans}");
        }
    }

    // Placeholder methods for future features
    public void AddMoney(int amount)
    {
        bandMoney += amount;
        Debug.Log($"Band Money updated to: {bandMoney}");
    }

    public void AddBandMember(string memberName)
    {
        bandMembers = new List<string>(bandMembers) { memberName }.ToArray();
        Debug.Log($"Added band member: {memberName}");
    }
}