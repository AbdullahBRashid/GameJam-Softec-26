using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the Death Screen UI, freezing inputs and allowing the player to retry.
/// </summary>
public class DeathScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The main container for the death screen.")]
    [SerializeField] private GameObject deathPanel;
    
    [Tooltip("The actual button the player clicks to retry.")]
    [SerializeField] private Button retryButton;

    private PlayerMovement _playerMovement;
    private MonoBehaviour _cinemachineInputController;
    private InteractionSystem _interactionSystem;

    private void Awake()
    {
        // Warn if script is on the panel itself and disabled
        if (deathPanel == this.gameObject)
        {
            Debug.LogWarning("[DeathScreenUI] This script should be placed on an always-active parent object (like Canvas), NOT the hidden panel itself, otherwise it won't receive death events!");
        }

        if (deathPanel != null)
            deathPanel.SetActive(false);
            
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerMovement = player.GetComponent<PlayerMovement>();
            _interactionSystem = player.GetComponent<InteractionSystem>();
        }

        // Find Cinemachine input controller
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb.GetType().Name == "CinemachineInputAxisController")
            {
                _cinemachineInputController = mb;
                break;
            }
        }
    }

    private void OnEnable()
    {
        GameEventManager.OnPlayerDied += ShowDeathScreen;
    }

    private void OnDisable()
    {
        GameEventManager.OnPlayerDied -= ShowDeathScreen;
    }

    private void ShowDeathScreen()
    {
        if (_interactionSystem != null && _interactionSystem.IsPanelOpen)
        {
            _interactionSystem.ClosePanel();
        }

        if (deathPanel != null)
            deathPanel.SetActive(true);

        Time.timeScale = 0f;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_playerMovement != null)
            _playerMovement.enabled = false;

        if (_cinemachineInputController != null)
            _cinemachineInputController.enabled = false;
    }

    private void OnRetryClicked()
    {
        if (deathPanel != null)
            deathPanel.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_playerMovement != null)
            _playerMovement.enabled = true;

        if (_cinemachineInputController != null)
            _cinemachineInputController.enabled = true;

        if (StageManager.Instance != null)
        {
            StageManager.Instance.RespawnPlayer();
        }
        else
        {
            Debug.LogWarning("[DeathScreenUI] StageManager Instance is null!");
        }
    }
}
