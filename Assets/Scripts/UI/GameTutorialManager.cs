using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameTutorialManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject tutorialPanel;
    
    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button closeButton;   // Top Right
    [SerializeField] private Button finishButton;  // Last Page
    
    [Header("Text Display")]
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicator;

    [Header("Tutorial Data")]
    [SerializeField, TextArea(10, 20)] 
    private List<string> rulePages = new List<string>();

    private int _currentIndex = 0;

    public static GameTutorialManager Instance { get; private set; }
    private System.Action _onCloseCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private System.Collections.IEnumerator Start()
    {
        // 1. Setup Button Listeners
        nextButton.onClick.AddListener(() => {
            Debug.Log("Next Clicked");
            OnNextClicked();
        });
        
        backButton.onClick.AddListener(() => {
            Debug.Log("Back Clicked");
            OnBackClicked();
        });
        
        closeButton.onClick.AddListener(CloseTutorial);
        finishButton.onClick.AddListener(CloseTutorial);

        // 2. Pause Game
        Time.timeScale = 0f;
        RefreshPage();

        // Wait one frame to ensure PlayerMovement.Start() has already executed
        yield return null;

        // Unlock Cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnNextClicked()
    {
        if (_currentIndex < rulePages.Count - 1)
        {
            _currentIndex++;
            RefreshPage();
        }
    }

    private void OnBackClicked()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            RefreshPage();
        }
    }

    private void RefreshPage()
    {
        // Update Content
        bodyText.text = rulePages[_currentIndex];
        
        // Update Page Number (e.g., "1 / 5")
        if (pageIndicator != null)
            pageIndicator.text = $"{_currentIndex + 1} / {rulePages.Count}";

        // Button Visibility
        backButton.gameObject.SetActive(_currentIndex > 0);
        
        // Toggle Next vs Finish
        bool isLastPage = (_currentIndex == rulePages.Count - 1);
        nextButton.gameObject.SetActive(!isLastPage);
        finishButton.gameObject.SetActive(isLastPage);
    }

    public void OpenTutorial(System.Action onClose = null)
    {
        _onCloseCallback = onClose;
        tutorialPanel.SetActive(true);
        _currentIndex = 0;
        RefreshPage();
    }

    private void CloseTutorial()
    {
        // 1. Close UI
        tutorialPanel.SetActive(false);

        if (_onCloseCallback != null)
        {
            // If opened from pause menu or another system
            _onCloseCallback.Invoke();
            _onCloseCallback = null;
        }
        else
        {
            // 2. Resume Game (Initial startup case)
            Time.timeScale = 1f;
            
            // 3. Relock Cursor (if first person)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("Tutorial Closed. Game Physics and Time Resumed.");
        }
    }
}