using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Place this on the Door GameObject that acts as an Ending constraint.
/// When the door is successfully unlocked and opened, it triggers an animated
/// cutscene that disables input, fades to black, reveals typewriter ending monologue,
/// and then rolls a credits sequence.
/// </summary>
[RequireComponent(typeof(DoorController))]
public class EndingTrigger : MonoBehaviour
{
    [Header("Ending Dialogue")]
    [Tooltip("The query key for the narrator line in NarratorLinesSO (e.g. 'EndingA').")]
    [SerializeField] private string messageName = "EndingA";

    [Header("Credits Sequence")]
    [TextArea(10, 20)]
    public string creditsText = "MECHANICS\nAbdullah Bin Rashid\nArmaghan Ahmed\n\n\nLEVEL DESIGN\nSaad Riaz\n\n\nDIALOGUES\nShuja\n\n\n\n\n\nTHANK YOU FOR PLAYING";
    
    [Tooltip("How long the credits should take to scroll off screen.")]
    public float creditsScrollDuration = 15f;

    private DoorController _door;
    private bool _hasTriggered = false;

    private void Awake()
    {
        _door = GetComponent<DoorController>();
    }

    private void Update()
    {
        if (!_hasTriggered && _door.IsOpen)
        {
            _hasTriggered = true;
            StartCoroutine(PlayEndingSequence());
        }
    }

    private IEnumerator PlayEndingSequence()
    {
        // 1. Mute immediate interactions by disabling the Player Controller
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            var input = player.GetComponent<PlayerInput>();
            if (input != null) input.DeactivateInput();
            
            var intSys = player.GetComponent<InteractionSystem>();
            if (intSys != null) intSys.ClosePanel(); // Ensure any UI menus are closed
        }

        // Disable mouse/camera panning
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb.GetType().Name == "CinemachineInputAxisController") mb.enabled = false;
        }

        // 2. Dynamically Generate the Cinematic Canvas
        GameObject canvasObj = new GameObject("Ending_Cutscene_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Render atop absolutely everything

        // Background Box
        GameObject bgObj = new GameObject("BlackVignetteFade");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0); 
        RectTransform bgRT = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // 3. Fade completely to Black (Cinematic transition)
        float fadeTime = 2.5f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            bgImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 1f, elapsed / fadeTime));
            yield return null;
        }
        bgImg.color = Color.black;

        // Hold the silence
        yield return new WaitForSeconds(1.5f);

        // 4. Generate the Typewriter Text
        GameObject textObj = new GameObject("MonologueText");
        textObj.transform.SetParent(canvasObj.transform, false);
        Text narratorText = textObj.AddComponent<Text>();
        narratorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        narratorText.fontSize = 32;
        narratorText.fontStyle = FontStyle.Italic;
        narratorText.color = new Color(0.85f, 0.9f, 0.95f, 1f);
        narratorText.alignment = TextAnchor.MiddleCenter;
        narratorText.horizontalOverflow = HorizontalWrapMode.Wrap;
        narratorText.verticalOverflow = VerticalWrapMode.Overflow;
        
        RectTransform textRT = narratorText.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = new Vector2(-150, -150); // Padding

        // Fetch dialog string safely
        string monologueStr = "";
        if (NarratorLinesSO.Instance != null)
        {
            monologueStr = NarratorLinesSO.Instance.GetLine(messageName);
        }
        
        if (string.IsNullOrEmpty(monologueStr) || monologueStr.StartsWith("[Missing"))
        {
            monologueStr = $"[Ending Log: {messageName}]";
        }

        // Type out the characters
        float charInterval = 1f / 30f; // 30 characters per second
        for (int i = 0; i < monologueStr.Length; i++)
        {
            narratorText.text = monologueStr.Substring(0, i + 1);
            yield return new WaitForSeconds(charInterval);
        }

        // Let the player digest the final text
        yield return new WaitForSeconds(6f);

        // Fade Text Out
        elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            narratorText.color = new Color(0.85f, 0.9f, 0.95f, Mathf.Lerp(1f, 0f, elapsed / 2f));
            yield return null;
        }
        
        Destroy(textObj); // Remove monologue object
        yield return new WaitForSeconds(1.5f);

        // 5. Build and Scroll Credits
        GameObject creditsObj = new GameObject("CreditsScrolling");
        creditsObj.transform.SetParent(canvasObj.transform, false);
        Text creditsUI = creditsObj.AddComponent<Text>();
        creditsUI.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        creditsUI.fontSize = 26;
        creditsUI.color = new Color(1, 1, 1, 0); // Start transparent
        creditsUI.alignment = TextAnchor.UpperCenter;
        creditsUI.text = creditsText;

        RectTransform creditsRT = creditsUI.rectTransform;
        creditsRT.anchorMin = new Vector2(0, 0);
        creditsRT.anchorMax = new Vector2(1, 0);
        creditsRT.pivot = new Vector2(0.5f, 1); // Top pivot
        creditsRT.sizeDelta = new Vector2(0, 3000); 
        
        // Scroll Math
        float startY = -150f; // Starts slightly below the bottom of the screen
        float endY = Screen.height + 1500f; // Moves way past the top
        
        elapsed = 0f;
        while (elapsed < creditsScrollDuration)
        {
            elapsed += Time.deltaTime;
            
            // Fade in over the first 2 seconds of scroll
            if (elapsed < 2f)
            {
                creditsUI.color = new Color(1, 1, 1, elapsed / 2f);
            }
            else
            {
                creditsUI.color = Color.white;
            }

            creditsRT.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, endY, elapsed / creditsScrollDuration));
            yield return null;
        }

        // 6. Hard Stop
        yield return new WaitForSeconds(2f);
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
