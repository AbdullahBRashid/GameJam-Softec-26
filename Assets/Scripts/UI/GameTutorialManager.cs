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

    private void Start()
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
        
        closeButton.onClick.AddListener(StartGame);
        finishButton.onClick.AddListener(StartGame);

        // 2. Pause Game and Unlock Cursor
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshPage();
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

    private void StartGame()
    {
        // 1. Close UI
        tutorialPanel.SetActive(false);

        // 2. Resume Game
        Time.timeScale = 1f;
        
        // 3. Relock Cursor (if first person)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Tutorial Closed. Game Physics and Time Resumed.");
    }
}