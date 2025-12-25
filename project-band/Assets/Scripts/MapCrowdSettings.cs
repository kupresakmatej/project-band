using UnityEngine;

[CreateAssetMenu(fileName = "MapCrowdSettings", menuName = "Crowd/MapCrowdSettings", order = 1)]
public class MapCrowdSettings : ScriptableObject
{
    [SerializeField] private string mapName; // e.g., "Club", "Stadium"
    [SerializeField] private int fansPerCrowdMember = 10; // Fans per crowd member (divisor)
    [SerializeField] private int maxCrowdPerContainer = 100; // Max crowd members per container
    [SerializeField] private int poolSize = 300; // Total pool size for all containers

    public string MapName => mapName;
    public int FansPerCrowdMember => fansPerCrowdMember;
    public int MaxCrowdPerContainer => maxCrowdPerContainer;
    public int PoolSize => poolSize;
}