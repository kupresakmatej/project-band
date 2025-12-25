using UnityEngine;

public class DropZone : MonoBehaviour
{
    [SerializeField]
    private string zoneName;

    private Collider zoneCollider;

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
    }

    public bool IsInsideZone(Vector3 worldPosition)
    {
        bool isInside = zoneCollider.bounds.Contains(worldPosition);
        Debug.Log($"Checking if {worldPosition} is inside zone {zoneName}: {isInside}");
        return isInside;
    }


    public bool HandleCardDrop(CardData selectedCard)
    {
        if (selectedCard.cardType.ToString() == zoneName)
        {
            return true;
        }
        return false;
    }
}
