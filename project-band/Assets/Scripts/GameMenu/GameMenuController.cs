using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;  // For Text

public class GameMenuController : MonoBehaviour
{
    [Header("Animation Settings")]
    public float totalAnimationDuration = 2.0f;
    public float returnDuration = 1.5f;
    public float initialHeight = 20f;
    public float zoomedHeight = 6f;
    public float forwardDollyDistance = 4f;
    public float finalFOV = 40f;

    [Header("UI Hint Settings")]
    public GameObject hintPanel;           // Optional: assign a UI Panel in inspector
    public Text hintText;                  // Assign a UI Text or TextMeshProUGUI (see notes below)
    public string backHint = "ESC - Back";
    public string enterHintBase = "E - ";   // Will become "E - Play" or "E - Enter"

    [Header("References")]
    public Transform circleCenter;

    private Transform selectedItem;
    private bool isAnimating = false;
    private bool isInItemView = false;

    private Camera mainCam;
    private float originalFOV;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Awake()
    {
        mainCam = Camera.main;
        originalFOV = mainCam.fieldOfView;

        if (circleCenter == null)
            circleCenter = new GameObject("CircleCenter").transform;

        // Auto-create hint UI if not assigned
        if (hintText == null)
            CreateHintUI();
    }

    private void Start()
    {
        if (initialHeight <= 0f)
            initialHeight = mainCam.transform.position.y;

        originalPosition = new Vector3(circleCenter.position.x, initialHeight, circleCenter.position.z);
        originalRotation = Quaternion.Euler(90f, 0f, 0f);

        mainCam.transform.position = originalPosition;
        mainCam.transform.rotation = originalRotation;

        // Hide hint at start
        SetHintVisible(false);
    }

    void Update()
    {
        if (isAnimating) return;

        // Click to select item
        if (!isInItemView && Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                selectedItem = hit.transform;
                StartCoroutine(AnimateToItem(selectedItem));
            }
        }

        // ESC = back
        if (isInItemView && Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(ReturnToTopView());
        }

        // E = load scene
        if (isInItemView && Input.GetKeyDown(KeyCode.E))
        {
            MenuItem itemScript = selectedItem?.GetComponent<MenuItem>();
            if (itemScript != null && !string.IsNullOrEmpty(itemScript.sceneToLoad))
            {
                SceneManager.LoadScene(itemScript.sceneToLoad);
            }
        }
    }

    IEnumerator AnimateToItem(Transform target)
    {
        isAnimating = true;
        isInItemView = true;

        Vector3 centerPos = circleCenter.position;
        Vector3 toTargetXZ = target.position - centerPos;
        toTargetXZ.y = 0;
        Vector3 forwardDirection = toTargetXZ.normalized;

        Vector3 finalPos = new Vector3(
            centerPos.x + forwardDirection.x * forwardDollyDistance,
            zoomedHeight,
            centerPos.z + forwardDirection.z * forwardDollyDistance
        );

        Quaternion finalRot = Quaternion.LookRotation(target.position - finalPos, Vector3.up);

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        float time = 0f;
        while (time < totalAnimationDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / totalAnimationDuration);
            t = EaseOutQuint(t);

            mainCam.transform.position = Vector3.Lerp(startPos, finalPos, t);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, finalRot, t);
            mainCam.fieldOfView = Mathf.Lerp(originalFOV, finalFOV, t);

            yield return null;
        }

        // Final position
        mainCam.transform.position = finalPos;
        mainCam.transform.rotation = finalRot;
        mainCam.fieldOfView = finalFOV;

        // Show hint when animation is done
        UpdateAndShowHint(target);

        isAnimating = false;
    }

    IEnumerator ReturnToTopView()
    {
        isAnimating = true;
        SetHintVisible(false);

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;
        float startFOV = mainCam.fieldOfView;

        float time = 0f;
        while (time < returnDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / returnDuration);
            t = EaseOutQuint(t);

            mainCam.transform.position = Vector3.Lerp(startPos, originalPosition, t);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, originalRotation, t);
            mainCam.fieldOfView = Mathf.Lerp(startFOV, originalFOV, t);

            yield return null;
        }

        mainCam.transform.position = originalPosition;
        mainCam.transform.rotation = originalRotation;
        mainCam.fieldOfView = originalFOV;

        isInItemView = false;
        selectedItem = null;
        isAnimating = false;
    }

    private void UpdateAndShowHint(Transform target)
    {
        MenuItem itemScript = target.GetComponent<MenuItem>();
        bool hasScene = itemScript != null && !string.IsNullOrEmpty(itemScript.sceneToLoad);

        string enterText = hasScene ? "Play" : "Enter"; // Or "Select", "Open", etc.

        if (hintText != null)
        {
            hintText.text = $"{backHint}\n{enterHintBase}{enterText}";
        }

        SetHintVisible(true);
    }

    private void SetHintVisible(bool visible)
    {
        if (hintPanel != null)
            hintPanel.SetActive(visible);
        else if (hintText != null)
            hintText.gameObject.SetActive(visible);
    }

    // Auto-create simple UI if nothing is assigned in inspector
    private void CreateHintUI()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject panelObj = new GameObject("HintPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        hintPanel = panelObj;

        // Background panel (optional semi-transparent)
        Image img = panelObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.5f);

        GameObject textObj = new GameObject("HintText");
        textObj.transform.SetParent(panelObj.transform, false);

        hintText = textObj.AddComponent<Text>();
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 24;
        hintText.color = Color.white;
        hintText.alignment = TextAnchor.UpperLeft;

        // Position top-left
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(20, -20);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15, 15);
        textRect.offsetMax = new Vector2(-15, -15);

        panelRect.sizeDelta = new Vector2(250, 100);
    }

    private float EaseOutQuint(float t)
    {
        return 1f - Mathf.Pow(1f - t, 5f);
    }
}