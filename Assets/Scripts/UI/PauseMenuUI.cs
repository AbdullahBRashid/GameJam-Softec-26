using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// A self-building Escape/Pause menu.
/// Handles pausing the game, unlocking the cursor, and provides options to resume, reset, or quit.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    [Header("Aesthetics")]
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] private int baseFontSize = 28;

    private GameObject _rootUI;
    private bool _isPaused = false;

    // References to other components to disable
    private MonoBehaviour _cinemachineInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        BuildUI();
        _rootUI.SetActive(false);

        // Find camera controller to disable panning
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb.GetType().Name == "CinemachineInputAxisController")
            {
                _cinemachineInput = mb;
                break;
            }
        }
    }

    private void Update()
    {
        // Detect ESC key via New Input System's Keyboard class for direct polling
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        
        if (_isPaused) PauseGame();
        else ResumeGame();
    }

    private void PauseGame()
    {
        _isPaused = true;
        _rootUI.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_cinemachineInput != null) _cinemachineInput.enabled = false;
        
        // Disable player movement script to prevent input handling
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;
            
            var interaction = player.GetComponent<InteractionSystem>();
            if (interaction != null) interaction.enabled = false;
        }
    }

    public void ResumeGame()
    {
        _isPaused = false;
        _rootUI.SetActive(false);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_cinemachineInput != null) _cinemachineInput.enabled = true;

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = true;
            
            var interaction = player.GetComponent<InteractionSystem>();
            if (interaction != null) interaction.enabled = true;
        }
    }

    private void ResetStage()
    {
        if (StageManager.Instance != null)
        {
            ResumeGame(); // Resume before respawning
            StageManager.Instance.RespawnPlayer();
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ═══ UI Generation ═════════════════════════════════════════════

    private void BuildUI()
    {
        _rootUI = new GameObject("PauseMenu_Canvas");
        // Ensure we don't inherit scale from the PauseSystem object which might be weird
        _rootUI.transform.SetParent(null); 
        
        Canvas canvas = _rootUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        CanvasScaler scaler = _rootUI.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        _rootUI.AddComponent<GraphicRaycaster>();
        
        // Final safety check for identity scale
        _rootUI.transform.localScale = Vector3.one;

        // ── Overlay ──
        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(_rootUI.transform, false);
        Image overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = overlayColor;
        RectTransform overlayRT = overlayImg.rectTransform;
        
        // Force full stretch
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.pivot = new Vector2(0.5f, 0.5f);
        overlayRT.anchoredPosition = Vector2.zero;
        overlayRT.sizeDelta = Vector2.zero;
        overlayRT.localScale = Vector3.one;

        // ── Main Layout ──
        GameObject menuObj = new GameObject("MenuContainer");
        menuObj.transform.SetParent(_rootUI.transform, false);
        
        RectTransform menuRT = menuObj.AddComponent<RectTransform>();
        menuRT.anchorMin = new Vector2(0.5f, 0.5f);
        menuRT.anchorMax = new Vector2(0.5f, 0.5f);
        menuRT.pivot = new Vector2(0.5f, 0.5f);
        menuRT.anchoredPosition = Vector2.zero;
        menuRT.sizeDelta = new Vector2(600, 800);
        menuRT.localScale = Vector3.one;

        VerticalLayoutGroup layout = menuObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 40f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // ── Title ──
        CreateText(menuObj.transform, "SYSTEM PAUSED", baseFontSize + 20, Color.cyan, FontStyle.Bold);

        // ── Buttons ──
        CreateButton(menuObj.transform, "RESUME", ResumeGame);
        CreateButton(menuObj.transform, "RESET STAGE", ResetStage);
        CreateButton(menuObj.transform, "QUIT GAME", QuitGame);
    }

    private void CreateText(Transform parent, string content, int size, Color color, FontStyle style)
    {
        GameObject obj = new GameObject("Text_" + content);
        obj.transform.SetParent(parent, false);
        Text t = obj.AddComponent<Text>();
        t.text = content;
        
        // Try to find the legacy font, fallback to standard Arial
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, size + 20);
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.05f);
        
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(action);

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(1, 1, 1, 0.1f);
        colors.highlightedColor = new Color(1, 1, 1, 0.25f);
        colors.pressedColor = new Color(0, 1, 1, 0.4f); // Cyan punch on click
        btn.colors = colors;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(450, 80);
        rt.localScale = Vector3.one;

        // Label
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text t = txtObj.AddComponent<Text>();
        t.text = label;
        
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        t.fontSize = baseFontSize + 4;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        
        RectTransform txtRT = t.rectTransform;
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;
        txtRT.localScale = Vector3.one;
    }
}
