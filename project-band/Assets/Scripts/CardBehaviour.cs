using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardUIBehaviour : MonoBehaviour
{
    [SerializeField]
    private GameObject cardPrefab;
    [SerializeField]
    private GameObject cardNameTextPrefab;  // Prefab for displaying card name above the player
    [SerializeField]
    private float animationSpeed = 1f;
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private CardData[] cardDataArray;

    private GameObject[] instantiatedCards;
    private Vector2[] originalPositions;
    private CardData[] selectedCards;

    private int? activeHoveredCardIndex = null;

    void Start()
    {
        instantiatedCards = new GameObject[5];
        originalPositions = new Vector2[5];
        selectedCards = new CardData[5];
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

    public void ShuffleDeckAndDeal()
    {
        ResetDeck();

        selectedCards = new CardData[5];
        for (int i = 0; i < selectedCards.Length; i++)
        {
            int randomIndex = Random.Range(0, cardDataArray.Length);
            selectedCards[i] = cardDataArray[randomIndex];
        }
        for (int i = 0; i < selectedCards.Length; i++)
        {
            Debug.Log(selectedCards[i].cardName);
        }

        StartCoroutine(AnimateCards(selectedCards));
    }

    private IEnumerator AnimateCards(CardData[] selectedCards)
    {
        float duration = animationSpeed;
        float spacing = 150f;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        float startX = -((selectedCards.Length - 1) * spacing) / 2;

        float[] yOffsets = { -30f, -15f, 0f, -15f, -30f };

        float[] rotationOffsets = { 10f, 5f, 0f, -5f, -10f };

        for (int i = 0; i < selectedCards.Length; i++)
        {
            GameObject cardObject = Instantiate(cardPrefab, canvas.transform);
            instantiatedCards[i] = cardObject;

            CardData cardData = selectedCards[i];
            Image cardImage = cardObject.GetComponent<Image>();
            cardImage.sprite = cardData.cardImage;

            TMP_Text[] textComponents = cardObject.GetComponentsInChildren<TMP_Text>();
            textComponents[0].text = cardData.cardName;
            textComponents[1].text = cardData.cardDescription;

            RectTransform rectTransform = cardObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);

            float targetX = startX + i * spacing;
            float targetY = 70f + yOffsets[i];
            Vector2 targetPosition = new Vector2(targetX, targetY);
            Quaternion targetRotation = Quaternion.Euler(0, 0, rotationOffsets[i]);

            originalPositions[i] = targetPosition; // Save original position

            CardHoverHandler hoverHandler = cardObject.AddComponent<CardHoverHandler>();
            hoverHandler.Initialize(i, this);

            Vector2 initialPosition = rectTransform.anchoredPosition;
            Quaternion initialRotation = rectTransform.rotation;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                rectTransform.anchoredPosition = Vector2.Lerp(initialPosition, targetPosition, t);
                rectTransform.rotation = Quaternion.Lerp(initialRotation, targetRotation, t);

                yield return null;
            }

            rectTransform.anchoredPosition = targetPosition;
            rectTransform.rotation = targetRotation;
        }
    }

    public void ResetDeck()
    {
        foreach (GameObject card in instantiatedCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }

        instantiatedCards = new GameObject[5];
        originalPositions = new Vector2[5];
    }

    public void OnCardHovered(int hoveredIndex)
    {
        if (activeHoveredCardIndex == hoveredIndex) return;

        activeHoveredCardIndex = hoveredIndex;

        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (instantiatedCards[i] == null) continue;

            RectTransform rectTransform = instantiatedCards[i].GetComponent<RectTransform>();

            if (i == hoveredIndex)
            {
                rectTransform.anchoredPosition = originalPositions[i] + new Vector2(0, 75f);
            }
            else if (i > hoveredIndex)
            {
                rectTransform.anchoredPosition = originalPositions[i] + new Vector2(75f, 0);
            }
            else
            {
                rectTransform.anchoredPosition = originalPositions[i];
            }
        }
    }

    public void OnCardHoverExit(int hoveredIndex)
    {
        if (activeHoveredCardIndex != hoveredIndex) return;

        activeHoveredCardIndex = null;

        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (instantiatedCards[i] == null) continue;

            RectTransform rectTransform = instantiatedCards[i].GetComponent<RectTransform>();
            rectTransform.anchoredPosition = originalPositions[i];
        }
    }

    public bool TryDropCard(int cardIndex, Vector3 dropPosition)
    {
        Debug.Log("Trying to drop card at position: " + dropPosition);

        Camera camera = Camera.main;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Player hitPlayer = hit.collider.GetComponent<Player>();
            if (hitPlayer != null)
            {
                Debug.Log("Ray hit a player: " + hitPlayer.Type);

                bool canDropCard = HandleCardDrop(selectedCards[cardIndex], hitPlayer);

                if (canDropCard)
                {
                    RemoveCardFromHand(cardIndex);
                    StartCoroutine(DisplayCardNameAbovePlayer(hitPlayer.transform.position, selectedCards[cardIndex]));
                    return true;
                }
                else
                {
                    StartCoroutine(DisplayWrongCard(hitPlayer.transform.position, selectedCards[cardIndex]));
                    return false;
                }
            }
        }

        return false;
    }

    public bool HandleCardDrop(CardData selectedCard, Player player)
    {
        if (selectedCard.cardType == player.Type)
        {
            Debug.Log($"Card {selectedCard.cardName} successfully applied to {player.Type}");

            player.Health += selectedCard.healthBonus;
            player.Damage += selectedCard.damageBonus;

            return true;
        }
        else
        {
            Debug.Log($"Cannot boost {player.Type} with {selectedCard.cardType}");
            return false;
        }
    }

    private void RemoveCardFromHand(int cardIndex)
    {
        // Destroy the card and shift others down
        Destroy(instantiatedCards[cardIndex]);
        instantiatedCards[cardIndex] = null;

        // Shift the remaining cards in the array to close the gap
        for (int i = cardIndex; i < instantiatedCards.Length - 1; i++)
        {
            instantiatedCards[i] = instantiatedCards[i + 1];
            originalPositions[i] = originalPositions[i + 1];  // Shift the original position
        }

        // Clear the last card slot
        instantiatedCards[instantiatedCards.Length - 1] = null;
        originalPositions[instantiatedCards.Length - 1] = Vector2.zero;

        // Recenter remaining cards
        RecenterCards();
    }

    private void RecenterCards()
    {
        float spacing = 150f;  // Adjust as needed
        float startX = -((instantiatedCards.Length - 1) * spacing) / 2;

        // Calculate the new positions for remaining cards
        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (instantiatedCards[i] == null) continue;

            // Reposition each card in the hand
            RectTransform rectTransform = instantiatedCards[i].GetComponent<RectTransform>();
            float targetX = startX + i * spacing;
            Vector2 targetPosition = new Vector2(targetX, originalPositions[i].y);  // Keep the Y position the same

            // Animate the cards to their new positions
            StartCoroutine(AnimateCardReposition(rectTransform, targetPosition));
        }
    }

    private IEnumerator AnimateCardReposition(RectTransform rectTransform, Vector2 targetPosition)
    {
        float duration = animationSpeed;
        Vector2 initialPosition = rectTransform.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            rectTransform.anchoredPosition = Vector2.Lerp(initialPosition, targetPosition, t);

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;  // Ensure the final position is set
    }

    private IEnumerator DisplayCardNameAbovePlayer(Vector3 position, CardData cardData)
    {
        GameObject cardNameObject = Instantiate(cardNameTextPrefab, canvas.transform);
        TMP_Text cardNameText = cardNameObject.GetComponent<TMP_Text>();

        cardNameText.text = cardData.cardName;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, position);

        screenPosition += new Vector2(0, 175f);

        RectTransform rectTransform = cardNameText.GetComponent<RectTransform>();
        rectTransform.position = screenPosition;

        float fadeDuration = 2f;
        float elapsedTime = 0f;

        CanvasGroup canvasGroup = cardNameObject.AddComponent<CanvasGroup>();
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        Destroy(cardNameObject);
    }

    private IEnumerator DisplayWrongCard(Vector3 position, CardData cardData)
    {
        GameObject wrongCardObject = Instantiate(cardNameTextPrefab, canvas.transform);
        TMP_Text cardNameText = wrongCardObject.GetComponent<TMP_Text>();

        cardNameText.text = $"Cannot boost {cardData.cardType} here!";

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, position);
        screenPosition += new Vector2(0, 175f);

        RectTransform rectTransform = wrongCardObject.GetComponent<RectTransform>();
        rectTransform.position = screenPosition;

        float fadeDuration = 2f;
        float elapsedTime = 0f;

        CanvasGroup canvasGroup = wrongCardObject.AddComponent<CanvasGroup>();
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        Destroy(wrongCardObject);
    }

    public Canvas GetCanvas()
    {
        return canvas;
    }

    public void OnCardDragStart(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= selectedCards.Length || selectedCards[cardIndex] == null)
        {
            Debug.LogWarning("Invalid card index or card does not exist.");
            return;
        }

        CardData selectedCard = selectedCards[cardIndex];

        // Pause the highlighter of the previous player before playing the new one
        PausePreviousPlayerHighlighter();

        // Play the particle system for the current card's player
        ActivateAndPlayHighlighter(selectedCard.cardType);
    }

    private void ActivateAndPlayHighlighter(PlayerType playerType)
    {
        Player[] players = FindObjectsOfType<Player>();

        foreach (Player player in players)
        {
            if (player.Type == playerType)
            {
                // Play the particle system for the selected player
                if (player.highlighterEffect.isPaused)
                {
                    player.highlighterEffect.Play();  // Resume the paused effect
                }
                else
                {
                    player.highlighterEffect.Play();  // Start the effect if it's not already playing
                }

                // Track this player for later pausing
                activeHoveredCardIndex = System.Array.FindIndex(selectedCards, card => card.cardType == playerType);

                Debug.Log($"Activating and playing highlighter for {playerType}.");
                break;
            }
        }
    }

    private void PausePreviousPlayerHighlighter()
    {
        if (activeHoveredCardIndex.HasValue)
        {
            int previousIndex = activeHoveredCardIndex.Value;
            Player[] players = FindObjectsOfType<Player>();

            // Check the last hovered player and pause their highlighter effect
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].Type == selectedCards[previousIndex].cardType)
                {
                    if (players[i].highlighterEffect.isPlaying)
                    {
                        players[i].highlighterEffect.Stop(); // Pause the particle system for this player
                        Debug.Log($"Paused highlighter for {players[i].Type}.");
                    }
                    break;
                }
            }
        }
    }

    public void OnCardDragEnd(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= selectedCards.Length || selectedCards[cardIndex] == null)
        {
            Debug.LogWarning("Invalid card index or card does not exist.");
            return;
        }

        CardData selectedCard = selectedCards[cardIndex];

        // Find the corresponding player based on the card type
        Player[] players = FindObjectsOfType<Player>();
        foreach (Player player in players)
        {
            if (player.Type == selectedCard.cardType)
            {
                // Pause the highlighter effect for the player whose card was dropped
                if (player.highlighterEffect.isPlaying)
                {
                    player.highlighterEffect.Stop();
                    Debug.Log($"Paused highlighter for {player.Type} on card drop.");
                }
                break;
            }
        }
    }
}
