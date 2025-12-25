using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public class CardUIBehaviour : MonoBehaviour
{
    [SerializeField]
    private GameObject cardPrefab;
    [SerializeField]
    private GameObject cardNameTextPrefab;
    [SerializeField]
    private float animationSpeed = 1f;
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private float flyDuration = 0.5f; // Duration of card fly animation
    // New: For shrinking and wobbly effects
    [SerializeField]
    private float shrinkScale = 0.5f; // Target scale at end of animation (0.5 = 50% size)
    [SerializeField]
    private float wobbleAmplitude = 15f; // Max rotation angle for wobble (degrees)
    [SerializeField]
    private float wobbleFrequency = 5f; // How fast the wobble oscillates

    private GameObject[] instantiatedCards = new GameObject[5];
    private Vector2[] originalPositions = new Vector2[5];
    private CardData[] selectedCards = new CardData[5];
    private int? selectedCardIndex = null;
    private List<Player> selectedTargets = new List<Player>(); // For multi-target cards

    private int? activeHoveredCardIndex = null;

    // Layer mask for the "Player" layer
    private int playerLayerMask;
    private bool inputEnabled = true;

    void Start()
    {
        // Initialize only if not already set (optional, since we initialize above)
        if (instantiatedCards == null) instantiatedCards = new GameObject[5];
        if (originalPositions == null) originalPositions = new Vector2[5];
        if (selectedCards == null) selectedCards = new CardData[5];
        // Set the layer mask to only include the "Player" layer
        playerLayerMask = 1 << LayerMask.NameToLayer("Player");
    }

    void Update()
    {
        if (!inputEnabled) return;

        // if (Input.GetKeyDown(KeyCode.F))
        // {
        //     ShuffleDeckAndDeal();
        // }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetDeck();
        }
        if (Input.GetMouseButtonDown(0) && selectedCardIndex.HasValue)
        {
            Vector2 mousePosition = Input.mousePosition;
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = mousePosition };
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            if (raycastResults.Count == 0) // No UI hit, proceed to 3D world
            {
                Ray ray = Camera.main.ScreenPointToRay(mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, playerLayerMask))
                {
                    Player hitPlayer = hit.collider.GetComponent<Player>();
                    if (hitPlayer != null && IsValidTarget(hitPlayer))
                    {
                        selectedTargets.Add(hitPlayer);
                        CardData card = selectedCards[selectedCardIndex.Value];
                        Debug.Log($"Selected target: {hitPlayer.name}, Targets count: {selectedTargets.Count}, Max targets: {card.maxMultiTargets}, IsMultiTarget: {IsMultiTargetCard(card)}");
                        if (!IsMultiTargetCard(card) && selectedTargets.Count == 1 || IsMultiTargetCard(card) && selectedTargets.Count >= card.maxMultiTargets)
                        {
                            StartCoroutine(AnimateToTargetsAndApply(selectedCardIndex.Value));
                        }
                    }
                }
                else
                {
                    Debug.Log("Raycast did not hit any Player layer object.");
                }
            }
            else
            {
                Debug.Log("Click hit a UI element.");
            }
        }
    }

    public void SetHand(CardData[] hand)
    {
        ResetDeck();
        selectedCards = hand;
        StartCoroutine(AnimateCards(selectedCards));
    }

    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
    }

    // public void ShuffleDeckAndDeal()
    // {
    //     ResetDeck();
    //     selectedCards = new CardData[5];
    //     for (int i = 0; i < selectedCards.Length; i++)
    //     {
    //         int randomIndex = Random.Range(0, cardDataArray.Length);
    //         selectedCards[i] = cardDataArray[randomIndex];
    //     }
    //     StartCoroutine(AnimateCards(selectedCards));
    // }

    private IEnumerator AnimateCards(CardData[] selectedCards)
    {
        float duration = animationSpeed;
        float spacing = 150f;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float startX = -((selectedCards.Length - 1) * spacing) / 2;
        float[] yOffsets = { -120f, -90f, -80f, -90f, -120f };
        float[] rotationOffsets = { 10f, 5f, 0f, -5f, -10f };

        for (int i = 0; i < selectedCards.Length; i++)
        {
            if (selectedCards[i] == null) continue; // Skip null cards

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

            originalPositions[i] = targetPosition;

            CardHoverHandler hoverHandler = cardObject.GetComponent<CardHoverHandler>();
            if (hoverHandler == null) hoverHandler = cardObject.AddComponent<CardHoverHandler>();
            hoverHandler.Initialize(i, this);
            hoverHandler.SetAnimationState(true); // Prevent hover during animation

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
            hoverHandler.SetAnimationState(false); // Allow hover after animation
        }
    }

    public void ResetDeck()
    {
        // Guard against null
        if (instantiatedCards != null)
        {
            foreach (GameObject card in instantiatedCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
        }

        instantiatedCards = new GameObject[5];
        originalPositions = new Vector2[5];
        selectedCards = new CardData[5]; // Ensure selectedCards is reset
    }

    public void OnCardClicked(int clickedIndex)
    {
        if (selectedCardIndex == clickedIndex)
        {
            selectedCardIndex = null;
            selectedTargets.Clear();
        }
        else
        {
            selectedCardIndex = clickedIndex;
            selectedTargets.Clear();
        }
        UpdateCardDisplay();
    }

    public void OnCardHovered(int hoveredIndex)
    {
        if (activeHoveredCardIndex == hoveredIndex || selectedCardIndex.HasValue) return;

        activeHoveredCardIndex = hoveredIndex;

        UpdateCardDisplay();
    }

    public void OnCardHoverExit(int hoveredIndex)
    {
        if (activeHoveredCardIndex != hoveredIndex || selectedCardIndex.HasValue) return;

        activeHoveredCardIndex = null;

        UpdateCardDisplay();
    }

    public Canvas GetCanvas()
    {
        return canvas;
    }

    private bool IsValidTarget(Player target)
    {
        if (selectedCardIndex == null) return false;
        CardData card = selectedCards[selectedCardIndex.Value];
        bool isHealing = card.healthBonus > 0;
        bool isAttacking = card.damageBonus > 0;

        if (isHealing && target.CompareTag("Ally")) return true;
        if (isAttacking && target.CompareTag("Enemy")) return true;
        return false;
    }

    private bool IsMultiTargetCard(CardData card)
    {
        return card.cardType == PlayerType.Drummer || card.cardType == PlayerType.Singer;
    }

    private void ApplyCardEffect(int cardIndex)
    {
        // Logic moved to AnimateToTargetsAndApply and ApplyEffectToTargets
    }

    private void ApplyEffectToTargets(int cardIndex)
    {
        CardData card = selectedCards[cardIndex];
        Debug.Log($"Applying effect for {card.cardName}, Targets: {selectedTargets.Count}");
        Player[] allPlayers = FindObjectsOfType<Player>();
        List<Player> allyTeam = new List<Player>();
        List<Player> enemyTeam = new List<Player>();

        foreach (Player player in allPlayers)
        {
            if (player.CompareTag("Ally")) allyTeam.Add(player);
            else if (player.CompareTag("Enemy")) enemyTeam.Add(player);
        }

        switch (card.cardType)
        {
            case PlayerType.Bass:
                if (card.healthBonus > 0 && selectedTargets.Count == 1 && selectedTargets[0].CompareTag("Ally"))
                {
                    selectedTargets[0].Heal(card.healthBonus);
                    Debug.Log($"Healed {selectedTargets[0].name} by {card.healthBonus}");
                }
                else if (card.damageBonus > 0 && selectedTargets.Count == 1 && selectedTargets[0].CompareTag("Enemy"))
                {
                    selectedTargets[0].TakeDamage(card.damageBonus);
                    Debug.Log($"Damaged {selectedTargets[0].name} by {card.damageBonus}");
                }
                break;

            case PlayerType.Drummer:
                if (selectedTargets.Count > 0 && selectedTargets.Count <= card.maxMultiTargets && selectedTargets.All(t => t.CompareTag("Enemy")))
                {
                    foreach (Player target in selectedTargets)
                    {
                        target.TakeDamage(card.damageBonus);
                        Debug.Log($"Damaged {target.name} by {card.damageBonus}");
                    }
                }
                break;

            case PlayerType.Guitar:
                if (selectedTargets.Count == 1 && selectedTargets[0].CompareTag("Enemy"))
                {
                    selectedTargets[0].TakeDamage(card.damageBonus);
                    Debug.Log($"Damaged {selectedTargets[0].name} by {card.damageBonus}");
                }
                break;

            case PlayerType.Keyboard:
                if (card.damageBonus > 0 && selectedTargets.Count == 1 && selectedTargets[0].CompareTag("Enemy"))
                {
                    selectedTargets[0].TakeDamage(card.damageBonus);
                    Debug.Log($"Damaged {selectedTargets[0].name} by {card.damageBonus}");
                }
                else if (card.healthBonus > 0 && selectedTargets.Count == 1 && selectedTargets[0].CompareTag("Ally"))
                {
                    selectedTargets[0].Heal(2 * card.healthBonus);
                    Debug.Log($"Healed {selectedTargets[0].name} by {2 * card.healthBonus}");
                }
                break;

            case PlayerType.Singer:
                if (card.healthBonus > 0 && selectedTargets.Count == 1 && selectedTargets[0].CompareTag("Ally"))
                {
                    selectedTargets[0].Heal(card.healthBonus);
                    Debug.Log($"Healed {selectedTargets[0].name} by {card.healthBonus}");
                }
                else if (card.damageBonus > 0 && selectedTargets.Count == 1 && selectedTargets[0].CompareTag("Enemy"))
                {
                    selectedTargets[0].TakeDamage(card.damageBonus);
                    Debug.Log($"Damaged {selectedTargets[0].name} by {card.damageBonus}");
                }
                else if (card.damageBonus > 0 && selectedTargets.Count == 1 && selectedTargets[0].CompareTag("Enemy"))
                {
                    selectedTargets[0].TakeDamage(2 * card.damageBonus);
                    Debug.Log($"Damaged {selectedTargets[0].name} by {2 * card.damageBonus}");
                }
                else if (selectedTargets.Count > 0 && selectedTargets.Count <= card.maxMultiTargets && selectedTargets.All(t => t.CompareTag("Enemy")))
                {
                    foreach (Player target in selectedTargets)
                    {
                        target.TakeDamage(card.damageBonus);
                        Debug.Log($"Damaged {target.name} by {card.damageBonus}");
                    }
                }
                break;
        }
    }

    private IEnumerator AnimateToTargetsAndApply(int cardIndex)
    {
        inputEnabled = false; // Prevent input during animation

        CardData card = selectedCards[cardIndex];
        GameObject selectedCardGO = instantiatedCards[cardIndex];
        RectTransform selectedRect = selectedCardGO.GetComponent<RectTransform>();

        // Convert target world positions to screen positions
        List<Vector3> targetScreenPos = selectedTargets.Select(t => Camera.main.WorldToScreenPoint(t.transform.position)).ToList();

        List<GameObject> clones = new List<GameObject>();
        List<Coroutine> animCoroutines = new List<Coroutine>();

        // Create and animate a clone for each target
        for (int i = 0; i < selectedTargets.Count; i++)
        {
            GameObject clone = Instantiate(selectedCardGO, canvas.transform);
            clone.transform.SetAsLastSibling(); // Ensure on top of other UI
            RectTransform cloneRect = clone.GetComponent<RectTransform>();
            cloneRect.position = selectedRect.position; // Start from selected card's position
            clones.Add(clone);
            animCoroutines.Add(StartCoroutine(AnimateClone(cloneRect, targetScreenPos[i])));
        }

        // Wait for all animations to complete
        foreach (var cor in animCoroutines)
        {
            yield return cor;
        }

        // Spawn hit effects at each target's 3D position
        for (int i = 0; i < selectedTargets.Count; i++)
        {
            if (card.hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(card.hitEffectPrefab, selectedTargets[i].transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        // Destroy animation clones
        foreach (var clone in clones)
        {
            Destroy(clone);
        }

        // Apply the card's effect
        ApplyEffectToTargets(cardIndex);

        // Display card name above each target
        // foreach (Player target in selectedTargets)
        // {
        //     StartCoroutine(DisplayCardNameAbovePlayer(target.transform.position, card));
        // }

        // Remove the used card and duplicates
        yield return StartCoroutine(RemoveCardsWithFade(cardIndex, card.cardType));

        // End turn if hand is empty
        if (GetActiveCardCount() == 0)
        {
            GameManager.Instance.EndPlayerTurn();
        }

        selectedCardIndex = null;
        selectedTargets.Clear();
        UpdateCardDisplay();
        inputEnabled = true;
    }

    private IEnumerator AnimateClone(RectTransform rt, Vector3 endPos)
    {
        Vector3 startPos = rt.position;
        Vector3 startScale = rt.localScale; // Original scale (typically Vector3.one)
        Vector3 targetScale = Vector3.one * shrinkScale; // Target scale for shrinking
        float elapsed = 0f;
        Vector3 endPosition = endPos;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing for position and scale

            // Update position
            rt.position = Vector3.Lerp(startPos, endPosition, t);

            // Update scale (shrinking effect)
            rt.localScale = Vector3.Lerp(startScale, targetScale, t);

            // Update rotation (wobbly effect)
            float wobble = Mathf.Sin(t * Mathf.PI * wobbleFrequency) * wobbleAmplitude;
            rt.rotation = Quaternion.Euler(0f, 0f, wobble);

            yield return null;
        }

        // Ensure final position, scale, and rotation
        rt.position = endPosition;
        rt.localScale = targetScale;
        rt.rotation = Quaternion.Euler(0f, 0f, 0f); // Reset rotation at end
    }

    private IEnumerator RemoveCardsWithFade(int cardIndex, PlayerType usedType)
    {
        List<GameObject> cardsToFade = new List<GameObject>();
        List<int> indicesToClear = new List<int>();

        // Collect the used card and its duplicates
        if (instantiatedCards[cardIndex] != null)
        {
            cardsToFade.Add(instantiatedCards[cardIndex]);
            indicesToClear.Add(cardIndex);
        }
        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (i != cardIndex && instantiatedCards[i] != null && selectedCards[i] != null && selectedCards[i].cardType == usedType)
            {
                cardsToFade.Add(instantiatedCards[i]);
                indicesToClear.Add(i);
            }
        }

        // Fade out all collected cards
        List<Coroutine> fadeCoroutines = new List<Coroutine>();
        for (int i = 0; i < cardsToFade.Count; i++)
        {
            int index = indicesToClear[i];
            GameObject card = cardsToFade[i];
            fadeCoroutines.Add(StartCoroutine(FadeOutCard(card, () =>
            {
                if (index >= 0 && index < instantiatedCards.Length && instantiatedCards[index] != null)
                {
                    Destroy(instantiatedCards[index]);
                    instantiatedCards[index] = null;
                    selectedCards[index] = null;
                }
            })));
        }

        // Wait for all fade coroutines to complete
        foreach (var coroutine in fadeCoroutines)
        {
            yield return coroutine;
        }

        // Shift and recenter after all fades are done
        ShiftCardsAfterRemoval();
        RecenterCards();
    }

    private void ShiftCardsAfterRemoval()
    {
        List<int> activeIndices = new List<int>();
        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (instantiatedCards[i] != null)
            {
                activeIndices.Add(i);
            }
        }

        for (int i = 0; i < activeIndices.Count; i++)
        {
            int oldIndex = activeIndices[i];
            if (i != oldIndex)
            {
                instantiatedCards[i] = instantiatedCards[oldIndex];
                originalPositions[i] = originalPositions[oldIndex];
                selectedCards[i] = selectedCards[oldIndex];
                instantiatedCards[oldIndex] = null;
                originalPositions[oldIndex] = Vector2.zero;
                selectedCards[oldIndex] = null;
            }
        }

        // Clear remaining slots
        for (int i = activeIndices.Count; i < instantiatedCards.Length; i++)
        {
            instantiatedCards[i] = null;
            originalPositions[i] = Vector2.zero;
            selectedCards[i] = null;
        }

        Debug.Log("After ShiftCardsAfterRemoval: " + string.Join(", ", instantiatedCards.Select(c => c != null ? c.name : "null")));
    }

    private IEnumerator FadeOutCard(GameObject card, System.Action onComplete)
    {
        if (card == null) yield break;

        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = card.AddComponent<CanvasGroup>();

        float fadeDuration = 0.2f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        onComplete?.Invoke();
    }

    private IEnumerator AnimateCardReposition(RectTransform rectTransform, Vector2 targetPosition, Quaternion targetRotation)
    {
        float duration = animationSpeed;
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

    private void UpdateCardDisplay()
    {
        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (instantiatedCards[i] == null) continue;

            RectTransform rectTransform = instantiatedCards[i].GetComponent<RectTransform>();
            if (i == selectedCardIndex || (activeHoveredCardIndex.HasValue && i == activeHoveredCardIndex))
            {
                rectTransform.anchoredPosition = originalPositions[i] + new Vector2(0, 75f); // Lift selected or hovered card
            }
            else
            {
                rectTransform.anchoredPosition = originalPositions[i]; // Reset others
            }
        }
    }

    private void RecenterCards()
    {
        int activeCardCount = 0;
        List<int> activeIndices = new List<int>();
        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (instantiatedCards[i] != null)
            {
                activeCardCount++;
                activeIndices.Add(i);
                Debug.Log($"Active card at index {i}: {instantiatedCards[i].name}, Position: {originalPositions[i]}");
            }
        }

        Debug.Log($"RecenterCards: Active card count = {activeCardCount}");

        if (activeCardCount == 0) return;

        float spacing = 150f;
        float startX = -((activeCardCount - 1) * spacing) / 2;

        // Dynamically calculate Y offsets based on active card count
        float[] yOffsets = CalculateYOffsets(activeCardCount);
        float[] rotationOffsets = CalculateRotations(activeCardCount);

        // Reassign positions and animate
        for (int i = 0; i < activeCardCount; i++)
        {
            int originalIndex = activeIndices[i];
            RectTransform rectTransform = instantiatedCards[originalIndex].GetComponent<RectTransform>();
            float targetX = startX + i * spacing;
            float targetY = 70f + yOffsets[i];
            Vector2 targetPosition = new Vector2(targetX, targetY);
            Quaternion targetRotation = Quaternion.Euler(0, 0, rotationOffsets[i]);

            originalPositions[originalIndex] = targetPosition; // Update position
            Debug.Log($"Recalculating position for index {originalIndex} to {targetPosition}");

            CardHoverHandler hoverHandler = instantiatedCards[originalIndex].GetComponent<CardHoverHandler>();
            if (hoverHandler != null)
            {
                hoverHandler.Initialize(i, this); // Reassign index to match new order
            }

            StartCoroutine(AnimateCardReposition(rectTransform, targetPosition, targetRotation));
        }
    }

    private float[] CalculateYOffsets(int cardCount)
    {
        float[] baseYOffsets = { -120f, -90f, -80f, -90f, -120f }; // Original Y offsets
        float[] yOffsets = new float[5]; // Match array size for consistency

        if (cardCount == 1)
        {
            yOffsets[0] = -80f; // Centered with no offset for single card
        }
        else if (cardCount == 2)
        {
            yOffsets[0] = baseYOffsets[0] * 0.75f; // Scaled down -90f
            yOffsets[1] = baseYOffsets[4] * 0.75f; // Scaled down -90f
        }
        else if (cardCount == 3)
        {
            yOffsets[0] = baseYOffsets[0] * 0.75f; // -90f
            yOffsets[1] = baseYOffsets[2] * 0.75f; // -60f
            yOffsets[2] = baseYOffsets[4] * 0.75f; // -90f
        }
        else if (cardCount == 4)
        {
            yOffsets[0] = baseYOffsets[0] * 0.85f; // -102f
            yOffsets[1] = baseYOffsets[1] * 0.85f; // -76.5f
            yOffsets[2] = baseYOffsets[3] * 0.85f; // -76.5f
            yOffsets[3] = baseYOffsets[4] * 0.85f; // -102f
        }
        else if (cardCount == 5)
        {
            yOffsets[0] = baseYOffsets[0]; // -120f
            yOffsets[1] = baseYOffsets[1]; // -90f
            yOffsets[2] = baseYOffsets[2]; // -80f
            yOffsets[3] = baseYOffsets[3]; // -90f
            yOffsets[4] = baseYOffsets[4]; // -120f
        }

        return yOffsets;
    }

    private float[] CalculateRotations(int cardCount)
    {
        float[] baseRotations = { 10f, 5f, 0f, -5f, -10f }; // Original rotations
        float[] rotations = new float[5]; // Match array size for consistency

        if (cardCount == 1)
        {
            rotations[0] = 0f; // No rotation for single card
        }
        else if (cardCount == 2)
        {
            rotations[0] = baseRotations[0]; // 10f
            rotations[1] = baseRotations[4]; // -10f
        }
        else if (cardCount == 3)
        {
            rotations[0] = baseRotations[0]; // 10f
            rotations[1] = baseRotations[2]; // 0f
            rotations[2] = baseRotations[4]; // -10f
        }
        else if (cardCount == 4)
        {
            rotations[0] = baseRotations[0]; // 10f
            rotations[1] = baseRotations[1]; // 5f
            rotations[2] = baseRotations[3]; // -5f
            rotations[3] = baseRotations[4]; // -10f
        }
        else if (cardCount == 5)
        {
            rotations[0] = baseRotations[0]; // 10f
            rotations[1] = baseRotations[1]; // 5f
            rotations[2] = baseRotations[2]; // 0f
            rotations[3] = baseRotations[3]; // -5f
            rotations[4] = baseRotations[4]; // -10f
        }

        return rotations;
    }

    public int GetActiveCardCount()
    {
        int count = 0;
        for (int i = 0; i < instantiatedCards.Length; i++)
        {
            if (instantiatedCards[i] != null) count++;
        }
        return count;
    }
}