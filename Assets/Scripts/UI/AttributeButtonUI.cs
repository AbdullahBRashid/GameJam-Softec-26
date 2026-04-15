using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for attribute buttons in the interaction panel.
/// Attach this to the attribute button prefab.
/// 
/// Expected hierarchy:
///   Button (with this component + Button component)
///     └── Icon (Image, optional)
///     └── Label (TextMeshProUGUI or Text)
///     └── ActionLabel (TextMeshProUGUI or Text — "TAKE" or "APPLY")
/// </summary>
public class AttributeButtonUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMPro.TextMeshProUGUI labelText;
    [SerializeField] private TMPro.TextMeshProUGUI actionText;
    [SerializeField] private Image backgroundImage;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    /// <summary>
    /// Set up this button with attribute data.
    /// </summary>
    public void Setup(AttributeSO attribute, string actionLabel, Color tint, UnityEngine.Events.UnityAction onClick)
    {
        if (_button == null) _button = GetComponent<Button>();

        // Set icon
        if (iconImage != null && attribute.icon != null)
        {
            iconImage.sprite = attribute.icon;
            iconImage.color = Color.white;
        }
        else if (iconImage != null)
        {
            // No icon — tint the icon slot with the attribute color
            iconImage.color = tint;
        }

        // Set label text
        if (labelText != null)
        {
            labelText.text = attribute.displayName;
        }

        // Set action label ("TAKE" or "APPLY")
        if (actionText != null)
        {
            actionText.text = actionLabel;
        }

        // Tint the background
        if (backgroundImage != null)
        {
            Color bg = tint;
            bg.a = 0.3f;
            backgroundImage.color = bg;
        }

        // Set up click handler
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(onClick);
        }
    }
}
