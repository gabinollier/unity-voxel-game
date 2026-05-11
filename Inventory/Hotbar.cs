using Lean.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Hotbar : MonoBehaviour
{
    [SerializeField] GameObject selector;
    [SerializeField] GameObject hotbarItem;
    [SerializeField] RectTransform hotbarList;
    [SerializeField] float autoRightclickTime = 0.3f;
    LeanToggle _leanToggle;
    Vector2 _scrollValue;
    int _selectedHotbarIndex = 0;
    Dictionary<int, int> _hotbarIndexToInventoryIndex;
    Vector3 _targetedListPosition;
    [NaughtyAttributes.ShowNonSerializedField] Vector3 _defaultListPosition;
    List<HotbarItem> _hotbarItems;
    float _rightClickPressTime;

    private void Awake()
    {
        _leanToggle = GetComponent<LeanToggle>();
    }

    private void Start()
    {
        _defaultListPosition = hotbarList.localPosition;
        _targetedListPosition = hotbarList.localPosition;
        _selectedHotbarIndex = 0;

        Initialize();

        InventoryManager.InventorySizeUpdate.AddListener(Initialize);
        InventoryManager.InventoryUpdate.AddListener(Refresh);

        Refresh();
    }

    void Initialize()
    {
        _hotbarIndexToInventoryIndex = new Dictionary<int, int>();
        _hotbarItems = new List<HotbarItem>();
        hotbarList.DestroyAllChildren();

        for (int i = 0; i < InventoryManager.Capacity; i++)
        {
            HotbarItem item = Instantiate(hotbarItem, hotbarList).GetComponent<HotbarItem>();
            _hotbarItems.Add(item);
        }
    }

    void Refresh()
    {
        _hotbarIndexToInventoryIndex.Clear();

        int hotbarIndex = 0;
        for (int inventoryIndex = 0; inventoryIndex < InventoryManager.Capacity; inventoryIndex++)
        {
            if (!InventoryManager.Inventory[inventoryIndex].HasValue)
                continue;

            _hotbarIndexToInventoryIndex.Add(hotbarIndex, inventoryIndex);

            ItemStack? stack = InventoryManager.Inventory[inventoryIndex];

            _hotbarItems[hotbarIndex].ChangeItem((ItemStack)stack);

            hotbarIndex++;
        }

        for (int i = _hotbarIndexToInventoryIndex.Count; i < InventoryManager.Capacity; i++)
        {
            _hotbarItems[i].Disable();
        }

        if (_selectedHotbarIndex >= _hotbarIndexToInventoryIndex.Count && _hotbarIndexToInventoryIndex.Count > 0)
        {
            GoToSlot(_hotbarIndexToInventoryIndex.Count);
        }

        selector.SetActive(_hotbarIndexToInventoryIndex.Count >= 2);
    }


    private void Update()
    {
        if (!_leanToggle.On || _hotbarIndexToInventoryIndex.Count == 0)
        {
            _scrollValue = Vector2.zero;
            return;
        }

        _scrollValue = Mouse.current.scroll.ReadValue();
        int scroll = (int)_scrollValue.y / 120;
        if (scroll > 0)
        {
            for (int i = 0; i < scroll; i++)
                GoToSlot(_selectedHotbarIndex - 1);
        }
        else if (scroll < 0)
        {
            for (int i = 0; i < -scroll; i++)
                GoToSlot(_selectedHotbarIndex + 1);
        }

        // CLICKS

        if (Mouse.current.middleButton.ReadValue() == 1)
            MiddleClick();

        if (Mouse.current.leftButton.ReadValue() == 1)
            LeftClick();

        if (Mouse.current.rightButton.ReadValue() == 0)
        {
            _rightClickPressTime = autoRightclickTime;
        }
        else if (Mouse.current.rightButton.ReadValue() == 1)
        {
            _rightClickPressTime += Time.deltaTime;
            if (_rightClickPressTime >= autoRightclickTime)
            {
                _rightClickPressTime = 0f;
                RightClick();
            }
        }

        // Shortcuts

        if (InputsManager.Actions.UI.Hotbar1.WasPerformedThisFrame())
            GoToSlot(0);

        if (InputsManager.Actions.UI.Hotbar2.WasPerformedThisFrame())
            GoToSlot(1);

        if (InputsManager.Actions.UI.Hotbar3.WasPerformedThisFrame())
            GoToSlot(2);

        if (InputsManager.Actions.UI.Hotbar4.WasPerformedThisFrame())
            GoToSlot(3);

        if (InputsManager.Actions.UI.Hotbar5.WasPerformedThisFrame())
            GoToSlot(4);

        if (InputsManager.Actions.UI.Hotbar6.WasPerformedThisFrame())
            GoToSlot(5);

        if (InputsManager.Actions.UI.Hotbar7.WasPerformedThisFrame())
            GoToSlot(6);

        if (InputsManager.Actions.UI.Hotbar8.WasPerformedThisFrame())
            GoToSlot(7);

        if (InputsManager.Actions.UI.Hotbar9.WasPerformedThisFrame())
            GoToSlot(8);

        if (InputsManager.Actions.UI.Hotbar10.WasPerformedThisFrame())
            GoToSlot(9);

        hotbarList.localPosition = Vector3.Lerp(hotbarList.localPosition, _targetedListPosition, 25 * Time.deltaTime);
    }


    void GoToSlot(int slot)
    {
        if (slot >= _hotbarIndexToInventoryIndex.Count)
            slot = _hotbarIndexToInventoryIndex.Count - 1;

        if (slot < 0)
            slot = 0;

        _targetedListPosition = _defaultListPosition + (64 * slot * Vector3.left);
        _selectedHotbarIndex = slot;

        int inventorySlot = _hotbarIndexToInventoryIndex[slot];
        PlayerBlockInteractor.Instance.EnableSelectionOutline = InventoryManager.Inventory[inventorySlot].HasValue && InventoryManager.Inventory[inventorySlot].Value.ItemData is ToolItemData;
    }

    void RightClick()
    {
        int inventoryIndex = _hotbarIndexToInventoryIndex[_selectedHotbarIndex];
        InventoryManager.Inventory[inventoryIndex].Value.ItemData.RightClickAction(inventoryIndex);
    }

    void LeftClick()
    {
        int inventoryIndex = _hotbarIndexToInventoryIndex[_selectedHotbarIndex];
        InventoryManager.Inventory[inventoryIndex].Value.ItemData.LeftClickAction(inventoryIndex);
    }

    void MiddleClick()
    {
        if (PlayerBlockInteractor.Instance.SelectedBlock == null)
            return;

        for (int hotbarSlot = 0; hotbarSlot < _hotbarIndexToInventoryIndex.Count; hotbarSlot++)
        {
            int inventorySlot = _hotbarIndexToInventoryIndex[hotbarSlot];
            if (InventoryManager.Inventory[inventorySlot].HasValue
                && InventoryManager.Inventory[inventorySlot].Value.ItemData is BlockItemData
                && (BlockItemData)InventoryManager.Inventory[inventorySlot].Value.ItemData == PlayerBlockInteractor.Instance.SelectedBlock.Item)
            {
                GoToSlot(hotbarSlot);
                return;
            }
        }
    }
}
