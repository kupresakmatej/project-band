using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardPickup : MonoBehaviour
{
    public float scaleFactor = 1.5f;
    public LayerMask cardLayer;
    public ParticleSystem[] particleEffects; // Array of particle effects
    private Vector3 originalScale;
    private bool isHovered = false;
    private bool isClicked = false;

    [SerializeField]
    private GameObject mainCamera;
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;

    [SerializeField]
    private Vector3 cameraTargetPosition;
    [SerializeField]
    private Vector3 cameraTargetRotation;

    [SerializeField]
    private GameObject deck;

    // Dictionary to map card colors/material names to particle effects
    private Dictionary<string, ParticleSystem> colorToParticleEffect = new Dictionary<string, ParticleSystem>();

    //card ability, rotation helper
    private Ability ability;

    private bool isTeam = true;

    void Start()
    {
        originalScale = transform.localScale;
        originalCameraPos = mainCamera.transform.localPosition;
        originalCameraRot = mainCamera.transform.localRotation;

        // Populate the dictionary with card material names and their corresponding particle effects
        foreach (ParticleSystem particleEffect in particleEffects)
        {
            colorToParticleEffect[particleEffect.name.ToLower()] = particleEffect;
            particleEffect.Stop();
        }
    }

    void Update()
    {
        MouseOverCard();
        HandleInput();

        FocusCameraOnBand();

        if (Input.GetKeyDown(KeyCode.X))
        {
            SceneManager.LoadScene(0);
        }
    }

    private void FocusCameraOnBand()
    {
        // Check if Q key is pressed
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Move and rotate camera to 'support' position
            StartCoroutine(MoveAndRotateCamera("support"));
        }
        // Check if E key is pressed
        else if (Input.GetKeyDown(KeyCode.E))
        {
            // Move and rotate camera to 'attack' position
            StartCoroutine(MoveAndRotateCamera("attack"));
        }
        // Check if Q key is released
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            // Return camera to default position and rotation
            StartCoroutine(ReturnCameraToDefault("support"));
        }
        // Check if E key is released
        else if (Input.GetKeyUp(KeyCode.E))
        {
            // Return camera to default position and rotation
            StartCoroutine(ReturnCameraToDefault("attack"));
        }
    }

    private IEnumerator ReturnCameraToDefault(string type)
    {
        float duration = 1f;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            if(type == "attack")
            {
                mainCamera.transform.position = Vector3.Lerp(cameraTargetPosition, originalCameraPos, t);
                mainCamera.transform.rotation = Quaternion.Lerp(Quaternion.Euler(cameraTargetRotation.x, cameraTargetRotation.y * -1, cameraTargetRotation.z), originalCameraRot, t);
            } else if (type == "support")
            {
                mainCamera.transform.position = Vector3.Lerp(cameraTargetPosition, originalCameraPos, t);
                mainCamera.transform.rotation = Quaternion.Lerp(Quaternion.Euler(cameraTargetRotation), originalCameraRot, t);
            }
            yield return null;
        }
    }

    private IEnumerator MoveAndRotateCamera(string card)
    {
        float duration = 0.5f;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            if(card == "attack")
            {
                mainCamera.transform.position = Vector3.Lerp(originalCameraPos, cameraTargetPosition, t);
                mainCamera.transform.rotation = Quaternion.Lerp(originalCameraRot, Quaternion.Euler(cameraTargetRotation.x, cameraTargetRotation.y * -1, cameraTargetRotation.z), t);
            } else if (card == "support")
            {
                mainCamera.transform.position = Vector3.Lerp(originalCameraPos, cameraTargetPosition, t);
                mainCamera.transform.rotation = Quaternion.Lerp(originalCameraRot, Quaternion.Euler(cameraTargetRotation), t);
            }
            yield return null;
        }

        //mainCamera.transform.position = cameraTargetPosition;
        //mainCamera.transform.localRotation = Quaternion.Euler(cameraTargetRotation);
    }

    private void MouseOverCard()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, cardLayer))
        {
            if (hit.collider.gameObject == gameObject)
            {
                isHovered = true;
                if (!isClicked)
                {
                    transform.localScale = originalScale * scaleFactor;
                }
            }
            else
            {
                isHovered = false;
                if (!isClicked)
                {
                    transform.localScale = originalScale;
                }
            }
        }
        else
        {
            isHovered = false;
            if (!isClicked)
            {
                transform.localScale = originalScale;
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && isHovered)
        {
            isClicked = !isClicked;
            if (isClicked)
            {
                // If the card is clicked, scale it up and start the particle effect
                transform.localScale = originalScale * scaleFactor;
                string cardMaterialName = GetMaterialName(transform.GetComponent<Renderer>().material.name);

                // Get the Ability from the clicked card
                ability = GetComponent<Card>().Ability;  // Assign the Ability from the Card component

                // Check the UsedOn attribute of the ability
                if (ability.UsedOn == UsedOn.Team)
                {
                    // Rotate camera to focus on team
                    StartCoroutine(MoveAndRotateCamera("attack"));
                    isTeam = true;
                }
                else if (ability.UsedOn == UsedOn.Enemies)
                {
                    ActivateParticleEffect(cardMaterialName, true);

                    // Rotate camera to focus on enemies
                    StartCoroutine(MoveAndRotateCamera("support"));
                    isTeam = false;
                }
            }
            else
            {
                // If the card is clicked again, return it to its original scale and stop the particle effect
                transform.localScale = originalScale;
                string cardMaterialName = GetMaterialName(transform.GetComponent<Renderer>().material.name);
                ActivateParticleEffect(cardMaterialName, false); // Stop the particle effect

                if(isTeam)
                {
                    StartCoroutine(ReturnCameraToDefault("attack"));
                } else
                {
                    StartCoroutine(ReturnCameraToDefault("support"));
                }
                
            }
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && isClicked)
        {
            isClicked = false;
            transform.localScale = originalScale;
            string cardMaterialName = GetMaterialName(transform.GetComponent<Renderer>().material.name);
            ActivateParticleEffect(cardMaterialName, false); // Stop the particle effect

            // Reset camera to previous position and rotation
            StartCoroutine(ReturnCameraToDefault(ability.UsedOn.ToString().ToLower()));
        }
        else if (Input.GetMouseButtonDown(0) && isClicked)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                // Determine which tag to set based on the currently active particle effect
                string targetTag = DetermineTargetTag();

                // Check if the hit object matches the determined target tag
                if (hit.collider.CompareTag(targetTag))
                {
                    // Lerp the card to the target object and then return it to the deck
                    StartCoroutine(LerpToTargetAndReturn(hit.collider.gameObject));

                    // Reset camera to previous position and rotation
                    StartCoroutine(ReturnCameraToDefault(ability.UsedOn.ToString().ToLower()));
                }
            }
        }
    }

    private IEnumerator LerpToTargetAndReturn(GameObject target)
    {
        float duration = 0.1f;

        Vector3 targetPosition = target.transform.position;
        Vector3 originalPosition = transform.position;

        // Deactivate particle effects
        DeactivateAllParticleEffects();

        // Detach the card from the camera
        transform.parent = null;

        // Detach other cards of the same color
        DetachCardsOfSameColor();

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.position = Vector3.Lerp(originalPosition, targetPosition, t);
            yield return null;
        }

        // Once the card reaches the target position, set its position to the deck's position
        transform.position = deck.transform.position;
        transform.rotation = deck.transform.rotation;
        transform.localScale = originalScale;

        // Re-parent the card to the deck
        transform.parent = deck.transform;

        // Reactivate the card in the deck
        gameObject.SetActive(true);

        // Deselect the card
        isClicked = false;

        // Deactivate particle effects
        DeactivateAllParticleEffects();

        // Check the color of the placed card and return all cards of that color to the deck
        string cardMaterialName = GetMaterialName(transform.GetComponent<Renderer>().material.name);
        ReturnCardsToDeckByColor(cardMaterialName);

        if (isTeam)
        {
            StartCoroutine(ReturnCameraToDefault("attack"));
        }
        else
        {
            StartCoroutine(ReturnCameraToDefault("support"));
        }
    }

    private void DetachCardsOfSameColor()
    {
        string cardMaterialName = GetMaterialName(transform.GetComponent<Renderer>().material.name);
        CardPickup[] cardsInHand = FindObjectsOfType<CardPickup>();

        foreach (CardPickup card in cardsInHand)
        {
            if (card != this && card.GetComponent<Renderer>().material.name.ToLower().Contains(cardMaterialName.ToLower()))
            {
                card.transform.parent = null;  // Detach the card from its current parent
            }
        }
    }

    private void ReturnCardsToDeckByColor(string color)
    {
        // Find all cards in the hand
        CardPickup[] cardsInHand = FindObjectsOfType<CardPickup>();

        // Iterate through each card in the hand
        foreach (CardPickup card in cardsInHand)
        {
            // Check if the card is of the same color as the placed card and not the placed card itself
            if (card != this && card.GetComponent<Renderer>().material.name.ToLower().Contains(color.ToLower()))
            {
                // Move the card back to the deck's position
                card.transform.position = deck.transform.position;
                card.transform.rotation = deck.transform.rotation;
                card.transform.localScale = originalScale;
            }
        }
    }

    private void DeactivateAllParticleEffects()
    {
        foreach (ParticleSystem particleEffect in particleEffects)
        {
            particleEffect.Stop();
        }
    }

    private string GetMaterialName(string fullName)
    {
        // Remove "(Instance)" from the material name
        int instanceIndex = fullName.IndexOf(" (Instance)");
        if (instanceIndex != -1)
        {
            return fullName.Substring(0, instanceIndex);
        }
        return fullName;
    }

    private void ActivateParticleEffect(string cardMaterialName, bool start)
    {
        foreach (ParticleSystem particleEffect in particleEffects)
        {
            if (particleEffect.name.ToLower().Contains(cardMaterialName.ToLower()))
            {
                if (start)
                    particleEffect.Play();
                else
                    particleEffect.Stop();
            }
        }
    }

    private string DetermineTargetTag()
    {
        foreach (ParticleSystem particleEffect in particleEffects)
        {
            if (particleEffect.isPlaying)
            {
                // Determine the target tag based on the name of the active particle effect
                if(particleEffect.name.ToLower().Contains("red"))
                {
                    return "Guitarist";
                }
                else if (particleEffect.name.ToLower().Contains("blue"))
                {
                    return "Singer";
                }
                else if (particleEffect.name.ToLower().Contains("yellow"))
                {
                    return "Bass";
                }
                else if (particleEffect.name.ToLower().Contains("green"))
                {
                    return "Drummer";
                }
                else if (particleEffect.name.ToLower().Contains("pink"))
                {
                    return "Keyboard";
                }
            }
        }
        return "DefaultTag"; // Default tag if no particle effect is active
    }
}
