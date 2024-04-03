using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardBehaviour : MonoBehaviour
{
    [SerializeField]
    private GameObject[] cards;
    [SerializeField]
    private float animationSpeed;

    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;

    void Start()
    {
        StoreOriginalPositionsAndRotations();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ShuffleDeckAndDeal();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetDeck();
        }
    }

    private void StoreOriginalPositionsAndRotations()
    {
        originalPositions = new Vector3[cards.Length];
        originalRotations = new Quaternion[cards.Length];

        for (int i = 0; i < cards.Length; i++)
        {
            originalPositions[i] = cards[i].transform.position;
            originalRotations[i] = cards[i].transform.rotation;
        }
    }

    public void ShuffleDeckAndDeal()
    {
        ShuffleArray(cards);

        int numberOfCardsToDeal = Mathf.Min(cards.Length, 5);
        GameObject[] cardsToDeal = new GameObject[numberOfCardsToDeal];
        for (int i = 0; i < numberOfCardsToDeal; i++)
        {
            cardsToDeal[i] = cards[i];
        }

        StartCoroutine(AnimateCards(cardsToDeal));
    }

    private IEnumerator AnimateCards(GameObject[] cards)
    {
        float duration = animationSpeed;
        float[] xPositions = { -2.2f, -1.1f, 0f, 1.1f, 2.2f };
        Vector3 basePosition = new Vector3(-2.2f, 4.5f, -11.71f);
        float radius = 0.15f;
        float angleIncrement = Mathf.PI / (cards.Length - 1);

        for (int i = 0; i < cards.Length; i++)
        {
            GameObject card = cards[i];

            float angle = Mathf.PI / 2 - angleIncrement * i;
            float offsetY = Mathf.Cos(angle) * radius;

            Vector3 targetPosition = new Vector3(xPositions[i], basePosition.y + offsetY, basePosition.z);
            Quaternion targetRotation = Quaternion.Euler(-77.45f + angle * -1, 0 + angle * -5, 0 + angle * -2.5f);

            Quaternion initialRotation = card.transform.rotation;

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                card.transform.position = Vector3.Lerp(originalPositions[i], targetPosition, t);
                card.transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, t);
                yield return null;
            }

            card.transform.position = targetPosition;
            card.transform.rotation = targetRotation;
        }
    }

    private void ShuffleArray(GameObject[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            GameObject temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }

    public void ResetDeck()
    {
        StartCoroutine(AnimateReset(cards));
    }

    private IEnumerator AnimateReset(GameObject[] cards)
    {
        float duration = animationSpeed;

        for (int i = 0; i < cards.Length; i++)
        {
            GameObject card = cards[i];
            Vector3 initialPosition = card.transform.position;
            Quaternion initialRotation = card.transform.rotation;

            Vector3 targetPosition = originalPositions[i];
            Quaternion targetRotation = originalRotations[i];

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                card.transform.position = Vector3.Lerp(initialPosition, targetPosition, t);
                card.transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, t);
                yield return null;
            }

            card.transform.position = targetPosition;
            card.transform.rotation = targetRotation;
        }
    }
}
