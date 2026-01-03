using UnityEngine;

public class ArrangeInCircle : MonoBehaviour
{
    public GameObject[] menuItems; // Drag your objects here in Inspector
    public float radius = 8f;
    public float yPosition = 0f;

#if UNITY_EDITOR
    [ContextMenu("Arrange Now")]
    void Arrange()
    {
        if (menuItems.Length == 0) return;

        for (int i = 0; i < menuItems.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / menuItems.Length;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, yPosition, Mathf.Sin(angle) * radius);
            menuItems[i].transform.position = pos;
            menuItems[i].transform.LookAt(transform.position);
            menuItems[i].transform.rotation *= Quaternion.Euler(0, 180, 0);
        }
    }
#endif
}