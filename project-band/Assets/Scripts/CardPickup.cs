using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPickup : MonoBehaviour
{
    public float scaleFactor = 1.5f;
    public LayerMask cardLayer;
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        MouseOverCard();
    }

    private void MouseOverCard()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, cardLayer))
        {
            if (hit.collider.gameObject == gameObject)
            {
                transform.localScale = originalScale * scaleFactor;
            }
            else
            {
                transform.localScale = originalScale;
            }
        }
        else
        {
            transform.localScale = originalScale;
        }
    }

}
