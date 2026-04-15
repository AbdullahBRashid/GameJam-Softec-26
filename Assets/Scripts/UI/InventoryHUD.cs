using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building Inventory HUD.
/// Shows held attributes as slots with icon tint, name, description, and volatility cost.
/// 
/// Setup: Create an empty GameObject, add this component. Done.
/// </summary>
public class InventoryHUD : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private float padding = 20f;

    [Header("Sizing")]
    [SerializeField] private float slotWidth = 280f;
    [SerializeField] private float slotHeight = 72f;
    [SerializeField] private float slotSpacing = 6f;

    // ── UI References ──
    private Canvas _canvas;
    private RectTransform _rootPanel;
    private Text _headerText;
    private RectTransform _slotsContainer;
    private Text _emptyText;

    // ── Cached ──
    private AttributeInventory _inventory;
    private readonly List<GameObject> _slotObjects = new List<GameObject>();

    // ── Colors ──
    private readonly Color _bgColor = new Color(0.06f, 0.06f, 0.1f, 0.8f);
    private readonly Color _slotBg = new Color(0.12f, 0.12f, 0.18f, 0.9f);
    private readonly Color _textPrimary = new Color(0.9f, 0.92f, 0.95f, 1f);
    private readonly Color _textSecondary = new Color(0.55f, 0.58f, 0.65f, 1f);
    private readonly Color _textDesc = new Color(0.7f, 0.72f, 0.78f, 1f);
    private readonly Color _costPositive = new Color(0.95f, 0.3f, 0.25f, 1f);
    private readonly Color _costNeutral = new Color(0.5f, 0.55f, 0.6f, 1f);
    private readonly Color _headerColor = new Color(0.45f, 0.48f, 0.55f, 1f);

    private void Start()
    {
        // Find the player's inventory
        _inventory = FindFirstObjectByType<AttributeInventory>();
        BuildUI();
        RefreshSlots();
    }

    private void OnEnable()
    {
        GameEventManager.OnAttributePickedUp += OnInventoryChanged;
        GameEventManager.OnAttributeDropped += OnInventoryChanged;
    }

    private void OnDisable()
    {
        GameEventManager.OnAttributePickedUp -= OnInventoryChanged;
        GameEventManager.OnAttributeDropped -= OnInventoryChanged;
    }

    private void OnInventoryChanged(AttributeSO attr)
    {
        RefreshSlots();
    }

    // ═══ Refresh ════════════════════════════════════════════════════

    private void RefreshSlots()
    {
        // Clear old slots
        foreach (var obj in _slotObjects) Destroy(obj);
        _slotObjects.Clear();

        if (_inventory == null || _inventory.Count == 0)
        {
            if (_emptyText != null) _emptyText.gameObject.SetActive(true);
            ResizePanel(0);
            return;
        }

        if (_emptyText != null) _emptyText.gameObject.SetActive(false);

        for (int i = 0; i < _inventory.Count; i++)
        {
            AttributeSO attr = _inventory.GetAt(i);
            if (attr == null) continue;
            CreateSlot(attr);
        }

        ResizePanel(_inventory.Count);
    }

    private void ResizePanel(int slotCount)
    {
        float headerHeight = 36f;
        float contentHeight = slotCount > 0
            ? slotCount * (slotHeight + slotSpacing) + 10f
            : 30f; // space for "empty" text
        float totalHeight = headerHeight + contentHeight + 10f;
        _rootPanel.sizeDelta = new Vector2(slotWidth + 30f, totalHeight);
    }

    // ═══ Slot Builder ═══════════════════════════════════════════════

    private void CreateSlot(AttributeSO attr)
    {
        // ── Slot Container ──
        GameObject slotObj = new GameObject($"Slot_{attr.displayName}");
        slotObj.transform.SetParent(_slotsContainer, false);
        RectTransform slotRT = slotObj.AddComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(slotWidth, slotHeight);
        Image slotBg = slotObj.AddComponent<Image>();
        Color tintedBg = _slotBg;
        tintedBg.r = Mathf.Lerp(tintedBg.r, attr.attributeColor.r, 0.08f);
        tintedBg.g = Mathf.Lerp(tintedBg.g, attr.attributeColor.g, 0.08f);
        tintedBg.b = Mathf.Lerp(tintedBg.b, attr.attributeColor.b, 0.08f);
        slotBg.color = tintedBg;
        slotBg.raycastTarget = false;

        // ── Color Strip (left edge) ──
        GameObject strip = new GameObject("Strip");
        strip.transform.SetParent(slotObj.transform, false);
        RectTransform stripRT = strip.AddComponent<RectTransform>();
        stripRT.anchorMin = new Vector2(0, 0);
        stripRT.anchorMax = new Vector2(0, 1);
        stripRT.pivot = new Vector2(0, 0.5f);
        stripRT.anchoredPosition = Vector2.zero;
        stripRT.sizeDelta = new Vector2(4f, 0);
        Image stripImg = strip.AddComponent<Image>();
        stripImg.color = attr.attributeColor;
        stripImg.raycastTarget = false;

        // ── Name Text ──
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slotObj.transform, false);
        Text nameText = nameObj.AddComponent<Text>();
        nameText.text = attr.displayName;
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 15;
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = _textPrimary;
        nameText.alignment = TextAnchor.UpperLeft;
        nameText.raycastTarget = false;
        RectTransform nameRT = nameText.rectTransform;
        nameRT.anchorMin = new Vector2(0, 1);
        nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0, 1);
        nameRT.anchoredPosition = new Vector2(14, -6);
        nameRT.sizeDelta = new Vector2(-70, 20);

        // ── Description Text ──
        GameObject descObj = new GameObject("Desc");
        descObj.transform.SetParent(slotObj.transform, false);
        Text descText = descObj.AddComponent<Text>();
        descText.text = attr.description;
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 12;
        descText.color = _textDesc;
        descText.alignment = TextAnchor.UpperLeft;
        descText.raycastTarget = false;
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform descRT = descText.rectTransform;
        descRT.anchorMin = new Vector2(0, 0);
        descRT.anchorMax = new Vector2(1, 1);
        descRT.pivot = new Vector2(0, 1);
        descRT.anchoredPosition = new Vector2(14, -28);
        descRT.sizeDelta = new Vector2(-28, -32);

        // ── Cost Badge ──
        float cost = attr.volatilityCost;
        bool wasDef = _inventory.WasDefault(attr);
        string costStr = wasDef ? $"+{cost:F0}" : "+0";
        Color costColor = wasDef ? _costPositive : _costNeutral;

        GameObject costObj = new GameObject("Cost");
        costObj.transform.SetParent(slotObj.transform, false);
        Text costText = costObj.AddComponent<Text>();
        costText.text = costStr;
        costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        costText.fontSize = 14;
        costText.fontStyle = FontStyle.Bold;
        costText.color = costColor;
        costText.alignment = TextAnchor.UpperRight;
        costText.raycastTarget = false;
        RectTransform costRT = costText.rectTransform;
        costRT.anchorMin = new Vector2(1, 1);
        costRT.anchorMax = new Vector2(1, 1);
        costRT.pivot = new Vector2(1, 1);
        costRT.anchoredPosition = new Vector2(-10, -8);
        costRT.sizeDelta = new Vector2(50, 20);

        // ── Type Tag ──
        string typeTag = attr.isPhysicsAttribute ? "PHYSICS" : "STATUS";
        GameObject tagObj = new GameObject("TypeTag");
        tagObj.transform.SetParent(slotObj.transform, false);
        Text tagText = tagObj.AddComponent<Text>();
        tagText.text = typeTag;
        tagText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tagText.fontSize = 10;
        tagText.fontStyle = FontStyle.Bold;
        tagText.color = _textSecondary;
        tagText.alignment = TextAnchor.LowerRight;
        tagText.raycastTarget = false;
        RectTransform tagRT = tagText.rectTransform;
        tagRT.anchorMin = new Vector2(1, 0);
        tagRT.anchorMax = new Vector2(1, 0);
        tagRT.pivot = new Vector2(1, 0);
        tagRT.anchoredPosition = new Vector2(-10, 6);
        tagRT.sizeDelta = new Vector2(60, 14);

        _slotObjects.Add(slotObj);
    }

    // ═══ UI Builder ═════════════════════════════════════════════════

    private void BuildUI()
    {
        // ── Canvas ──
        GameObject canvasObj = new GameObject("InventoryHUD_Canvas");
        canvasObj.transform.SetParent(transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 99;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Root Panel ──
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        _rootPanel = panelObj.AddComponent<RectTransform>();
        _rootPanel.anchorMin = new Vector2(0, 0.5f);
        _rootPanel.anchorMax = new Vector2(0, 0.5f);
        _rootPanel.pivot = new Vector2(0, 0.5f);
        _rootPanel.anchoredPosition = new Vector2(padding, 0);
        _rootPanel.sizeDelta = new Vector2(slotWidth + 30f, 100f);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = _bgColor;
        panelBg.raycastTarget = false;

        // ── Header ──
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(panelObj.transform, false);
        _headerText = headerObj.AddComponent<Text>();
        _headerText.text = "INVENTORY";
        _headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _headerText.fontSize = 13;
        _headerText.fontStyle = FontStyle.Bold;
        _headerText.color = _headerColor;
        _headerText.alignment = TextAnchor.UpperLeft;
        _headerText.raycastTarget = false;
        RectTransform headerRT = _headerText.rectTransform;
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0, 1);
        headerRT.anchoredPosition = new Vector2(12, -8);
        headerRT.sizeDelta = new Vector2(-24, 20);

        // ── Capacity Counter ──
        GameObject capObj = new GameObject("Capacity");
        capObj.transform.SetParent(panelObj.transform, false);
        Text capText = capObj.AddComponent<Text>();
        int count = _inventory != null ? _inventory.Count : 0;
        int max = _inventory != null ? _inventory.MaxCapacity : 5;
        capText.text = $"{count}/{max}";
        capText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        capText.fontSize = 13;
        capText.fontStyle = FontStyle.Bold;
        capText.color = _headerColor;
        capText.alignment = TextAnchor.UpperRight;
        capText.raycastTarget = false;
        RectTransform capRT = capText.rectTransform;
        capRT.anchorMin = new Vector2(0, 1);
        capRT.anchorMax = new Vector2(1, 1);
        capRT.pivot = new Vector2(1, 1);
        capRT.anchoredPosition = new Vector2(-12, -8);
        capRT.sizeDelta = new Vector2(-24, 20);

        // ── Slots Container ──
        GameObject containerObj = new GameObject("Slots");
        containerObj.transform.SetParent(panelObj.transform, false);
        _slotsContainer = containerObj.AddComponent<RectTransform>();
        _slotsContainer.anchorMin = new Vector2(0, 1);
        _slotsContainer.anchorMax = new Vector2(1, 1);
        _slotsContainer.pivot = new Vector2(0.5f, 1);
        _slotsContainer.anchoredPosition = new Vector2(0, -36);
        _slotsContainer.sizeDelta = new Vector2(-20, 0);
        var layoutGroup = containerObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = slotSpacing;
        layoutGroup.padding = new RectOffset(5, 5, 0, 5);
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        var fitter = containerObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Empty Text ──
        GameObject emptyObj = new GameObject("Empty");
        emptyObj.transform.SetParent(_slotsContainer.transform, false);
        _emptyText = emptyObj.AddComponent<Text>();
        _emptyText.text = "Empty";
        _emptyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _emptyText.fontSize = 13;
        _emptyText.color = _textSecondary;
        _emptyText.alignment = TextAnchor.MiddleCenter;
        _emptyText.raycastTarget = false;
        RectTransform emptyRT = _emptyText.rectTransform;
        emptyRT.sizeDelta = new Vector2(slotWidth, 24);
    }
}
