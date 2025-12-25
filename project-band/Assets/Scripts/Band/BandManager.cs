using UnityEngine;
using System.Collections.Generic;

public class BandManager : MonoBehaviour
{
    [Header("Crowd Generation Settings")]
    [SerializeField] private GameObject crowdMemberPrefab; // Prefab for crowd member
    [SerializeField] private Transform[] crowdContainers; // Containers with BoxCollider triggers
    [SerializeField] private MapCrowdSettings mapSettings; // Map-specific settings

    [Header("Crowd Appearance Settings")]
    [SerializeField] private float minOpacity = 0.2f; // Minimum opacity for crowd
    [SerializeField] private float maxOpacity = 0.4f; // Maximum opacity for crowd
    [SerializeField] private int maxOpacityFans = 1000; // Fans count for max opacity

    private List<GameObject> crowdPool = new List<GameObject>(); // Object pool
    private List<GameObject> activeCrowd = new List<GameObject>(); // Track active crowd members

    void Start()
    {
        // Validate map settings
        if (mapSettings == null)
        {
            Debug.LogError("MapCrowdSettings not assigned in BandManager. Please assign in Inspector.");
            return;
        }

        // Initialize object pool
        InitializePool();

        // Validate containers
        if (crowdContainers.Length == 0)
        {
            Debug.LogError("No crowd containers assigned in BandManager. Please assign containers in Inspector.");
            return;
        }
        for (int i = 0; i < crowdContainers.Length; i++)
        {
            if (crowdContainers[i] == null)
            {
                Debug.LogError($"Crowd container at index {i} is null. Please check Inspector.");
            }
            else
            {
                BoxCollider collider = crowdContainers[i].GetComponent<BoxCollider>();
                if (collider == null || !collider.isTrigger)
                {
                    Debug.LogError($"Container {crowdContainers[i].name} is missing a BoxCollider or it's not set to trigger.");
                }
            }
        }

        // Generate initial crowd
        GenerateCrowd();
    }

    // Initialize object pool
    private void InitializePool()
    {
        int poolSize = mapSettings != null ? mapSettings.PoolSize : 300;
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(crowdMemberPrefab);
            obj.SetActive(false);
            crowdPool.Add(obj);
        }
        Debug.Log($"Initialized object pool with {poolSize} crowd prefabs for map {mapSettings?.MapName}.");
    }

    // Get an inactive object from the pool
    private GameObject GetPooledObject()
    {
        foreach (GameObject obj in crowdPool)
        {
            if (!obj.activeSelf)
            {
                return obj;
            }
        }
        Debug.LogWarning($"Object pool exhausted for map {mapSettings?.MapName}. Increase PoolSize in MapCrowdSettings.");
        return null;
    }

    // Generate crowd based on fans from BandData
    public void GenerateCrowd()
    {
        if (BandData.Instance == null)
        {
            Debug.LogError("BandData singleton not found. Ensure BandData is set up in the BandManagement scene.");
            return;
        }
        if (mapSettings == null)
        {
            Debug.LogError("MapCrowdSettings not assigned. Cannot generate crowd.");
            return;
        }

        // Clear active crowd
        ClearCrowd();

        // Get fans and calculate total crowd members
        int fans = BandData.Instance.Fans;
        int divisor = mapSettings.FansPerCrowdMember;
        int totalCrowdMembers = Mathf.FloorToInt(fans / (float)divisor);
        totalCrowdMembers = Mathf.Min(totalCrowdMembers, mapSettings.MaxCrowdPerContainer * crowdContainers.Length);
        Debug.Log($"Fans: {fans}, Divisor: {divisor}, Total crowd members: {totalCrowdMembers}, Containers: {crowdContainers.Length}");

        // Distribute across containers
        int numContainers = crowdContainers.Length;
        if (numContainers == 0) return;

        int baseCrowdPerContainer = totalCrowdMembers / numContainers;
        int remainder = totalCrowdMembers % numContainers;

        for (int i = 0; i < numContainers; i++)
        {
            if (crowdContainers[i] == null || crowdContainers[i].GetComponent<BoxCollider>() == null || !crowdContainers[i].GetComponent<BoxCollider>().isTrigger)
            {
                Debug.LogWarning($"Skipping container at index {i} (Name: {(crowdContainers[i] != null ? crowdContainers[i].name : "null")}) due to invalid setup.");
                continue;
            }

            int crowdCount = baseCrowdPerContainer + (i < remainder ? 1 : 0);
            crowdCount = Mathf.Min(crowdCount, mapSettings.MaxCrowdPerContainer);
            BoxCollider collider = crowdContainers[i].GetComponent<BoxCollider>();
            Debug.Log($"Spawning {crowdCount} crowd members in {crowdContainers[i].name}");

            for (int j = 0; j < crowdCount; j++)
            {
                GameObject crowdMember = GetPooledObject();
                if (crowdMember == null) continue;

                // Set position within BoxCollider bounds
                Vector3 randomPoint = new Vector3(
                    Random.Range(collider.bounds.min.x, collider.bounds.max.x),
                    collider.bounds.center.y,
                    Random.Range(collider.bounds.min.z, collider.bounds.max.z)
                );

                crowdMember.transform.position = randomPoint;
                crowdMember.transform.SetParent(crowdContainers[i]);
                crowdMember.SetActive(true);
                activeCrowd.Add(crowdMember);

                // Set opacity
                float fansFactor = Mathf.Clamp01(fans / (float)maxOpacityFans);
                float opacity = Mathf.Lerp(minOpacity, maxOpacity, fansFactor * Random.Range(0.8f, 1.2f));
                opacity = Mathf.Clamp01(opacity);
                SetOpacity(crowdMember, opacity);
            }
        }
    }

    // Clear active crowd members
    private void ClearCrowd()
    {
        foreach (GameObject crowdMember in activeCrowd)
        {
            if (crowdMember != null)
            {
                crowdMember.SetActive(false);
            }
        }
        activeCrowd.Clear();
        Debug.Log("Cleared all active crowd members.");
    }

    // Set opacity for a crowd member
    private void SetOpacity(GameObject crowdMember, float opacity)
    {
        SpriteRenderer spriteRenderer = crowdMember.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = opacity;
            spriteRenderer.color = color;
            return;
        }

        Renderer renderer = crowdMember.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            if (mat.HasProperty("_Color"))
            {
                Color color = mat.color;
                color.a = opacity;
                mat.color = color;
            }
            else
            {
                Debug.LogWarning($"Material on {crowdMember.name} does not support color/alpha. Ensure material is set to Transparent.");
            }
        }
        else
        {
            Debug.LogWarning($"No SpriteRenderer or MeshRenderer found on {crowdMember.name}.");
        }
    }

    // Test method to increase fans and regenerate crowd
    public void TestIncreaseFans(int amount)
    {
        if (BandData.Instance != null)
        {
            BandData.Instance.Fans += amount;
            GenerateCrowd();
        }
        else
        {
            Debug.LogError("BandData singleton not found.");
        }
    }
}