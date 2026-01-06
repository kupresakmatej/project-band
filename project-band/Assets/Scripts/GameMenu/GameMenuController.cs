using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;  // For Text

public class GameMenuController : MonoBehaviour
{
    [Header("Menu Settings")]
    public Transform[] menuItems;           // Drag all menu items here in left-to-right order
    public float rotationDuration = 0.6f;   // Smooth rotation time between items
    public float cameraDistance = 8f;       // How far back the camera is from the center
    public float cameraHeight = 4f;         // Camera eye height

    [Header("UI Hint Settings")]
    public GameObject hintPanel;            // Drag your Panel here (optional if you use auto-create)
    public TextMeshProUGUI hintText;        // Drag your TextMeshPro text object here
    public string navigateHint = "← → Arrows - Navigate";
    public string backHint = "ESC - Quit";
    public string selectPlay = "E / Enter - Play";     // Shown when item has a scene
    public string selectEnter = "E / Enter - Select";  // Shown when no scene

    private int currentIndex = 0;
    private bool isRotating = false;
    private Camera mainCam;
    private Transform circleCenter;

    private void Awake()
    {
        mainCam = Camera.main;

        if (menuItems.Length == 0)
        {
            Debug.LogError("Please assign menu items in the inspector!");
            return;
        }

        // Calculate center of all items
        Vector3 center = Vector3.zero;
        foreach (var item in menuItems)
            center += item.position;
        center /= menuItems.Length;

        circleCenter = new GameObject("MenuCenter").transform;
        circleCenter.position = new Vector3(center.x, 0, center.z);

        // Auto-create UI only if you forgot to assign in inspector
        if (hintText == null)
            CreateHintUI();
    }

    private void Start()
    {
        currentIndex = menuItems.Length / 2;  // Start on middle item
        SnapCameraToCurrentItem(true);        // Instant snap at start
        UpdateHint();
        SetHintVisible(true);
    }

    void Update()
    {
        if (isRotating) return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
            MoveToPreviousItem();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            MoveToNextItem();

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            SelectCurrentItem();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    void MoveToNextItem()
    {
        currentIndex = (currentIndex + 1) % menuItems.Length;
        StartCoroutine(RotateToItem(menuItems[currentIndex]));
        UpdateHint();
    }

    void MoveToPreviousItem()
    {
        currentIndex = (currentIndex - 1 + menuItems.Length) % menuItems.Length;
        StartCoroutine(RotateToItem(menuItems[currentIndex]));
        UpdateHint();
    }

    void SelectCurrentItem()
    {
        var item = menuItems[currentIndex];
        var menuItemScript = item.GetComponent<MenuItem>();

        if (menuItemScript != null && !string.IsNullOrEmpty(menuItemScript.sceneToLoad))
        {
            SceneManager.LoadScene(menuItemScript.sceneToLoad);
        }
        else
        {
            Debug.Log($"Selected: {item.name} (no scene to load - open your UI panel here)");
            // Example: enable Options panel, Credits, etc.
        }
    }

    IEnumerator RotateToItem(Transform target)
    {
        isRotating = true;

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        Vector3 directionToItem = (target.position - circleCenter.position).normalized;
        Vector3 targetPos = circleCenter.position - directionToItem * cameraDistance;
        targetPos.y = cameraHeight;

        Quaternion targetRot = Quaternion.LookRotation(target.position - targetPos, Vector3.up);

        float time = 0f;
        while (time < rotationDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / rotationDuration);
            t = EaseOutQuint(t);

            mainCam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        mainCam.transform.position = targetPos;
        mainCam.transform.rotation = targetRot;
        isRotating = false;
    }

    void SnapCameraToCurrentItem(bool instant = false)
    {
        var target = menuItems[currentIndex];
        Vector3 direction = (target.position - circleCenter.position).normalized;
        Vector3 pos = circleCenter.position - direction * cameraDistance;
        pos.y = cameraHeight;

        if (instant)
        {
            mainCam.transform.position = pos;
            mainCam.transform.rotation = Quaternion.LookRotation(target.position - pos, Vector3.up);
        }
        else
        {
            StartCoroutine(RotateToItem(target));
        }
    }

    private void UpdateHint()
    {
        if (hintText == null || menuItems.Length == 0) return;

        var currentItem = menuItems[currentIndex];
        var menuItemScript = currentItem.GetComponent<MenuItem>();
        bool hasScene = menuItemScript != null && !string.IsNullOrEmpty(menuItemScript.sceneToLoad);

        string selectText = hasScene ? selectPlay : selectEnter;
        string itemName = currentItem.name;

        hintText.text = $"{itemName}\n\n{navigateHint}\n{selectText}\n{backHint}";
    }

    private void SetHintVisible(bool visible)
    {
        if (hintPanel != null)
            hintPanel.SetActive(visible);
        else if (hintText != null)
            hintText.gameObject.SetActive(visible);
    }

    private void CreateHintUI()
    {
        GameObject canvasObj = GameObject.Find("Canvas") ?? CreateCanvas();

        GameObject panelObj = new GameObject("HintPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        hintPanel = panelObj;

        var img = panelObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);

        GameObject textObj = new GameObject("HintText");
        textObj.transform.SetParent(panelObj.transform, false);

        hintText = textObj.AddComponent<TextMeshProUGUI>();
        hintText.fontSize = 28;
        hintText.color = Color.white;
        hintText.alignment = TMPro.TextAlignmentOptions.TopLeft;

        // Layout
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(30, -30);
        panelRect.sizeDelta = new Vector2(400, 160);

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 20);
        textRect.offsetMax = new Vector2(-20, -20);
    }

    private GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    private float EaseOutQuint(float t)
    {
        return 1f - Mathf.Pow(1f - t, 5f);
    }
}