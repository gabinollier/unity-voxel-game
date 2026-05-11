using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryMenu : MonoBehaviour
{
    [Header("List")]
    [SerializeField] GameObject slotPrefab;
    [SerializeField] Transform slotsParent;
    [SerializeField] Transform equippableSlotsParent;
    [SerializeField] ItemDataList itemDataList;
    [SerializeField] Scrollbar scrollbar;
    [SerializeField] Transform body;
    InventorySlot[] _slots;
    InventorySlot _bagSlot;

    private void Start()
    {
        Initialize();

        _bagSlot = Instantiate(slotPrefab, equippableSlotsParent).GetComponent<InventorySlot>();
        _bagSlot.SetParentUsedWhenDragging(body);

        InventoryManager.InventorySizeUpdate.AddListener(Initialize);
        InventoryManager.InventoryUpdate.AddListener(Refresh);

        Refresh();
    }

    void Initialize()
    {
        slotsParent.DestroyAllChildren();
        _slots = new InventorySlot[InventoryManager.Capacity];

        for (int i = 0; i < InventoryManager.Inventory.Length; i++)
        {
            InventorySlot slot = Instantiate(slotPrefab, slotsParent).GetComponent<InventorySlot>();
            slot.SetParentUsedWhenDragging(body);
            _slots[i] = slot;
            slot.ChangeItem(InventoryManager.Inventory[i], i);
        }
    }

    [Command("refresh_inventory")]
    public void Refresh()
    {
        for (int i = 0; i < InventoryManager.Capacity; i++)
        {
            _slots[i].ChangeItem(InventoryManager.Inventory[i], i);
        }

        _bagSlot.ChangeItem(InventoryManager.Bag.HasValue ? InventoryManager.Bag.Value : null, -1);
    }

    [Command("give", "Give amount items to the player", displaySuccessMessage: false)]
    void Give(string itemName, int amount)
    {
        if (amount <= 0)
            return;

        ItemData itemData = itemDataList.ItemDatas.Find(x => string.Equals(x.TechnicalName, itemName));

        if (itemData == null)
        {
            CommandConsole.LogError($"Item '{itemName}' does not exist.");
            return;
        }

        int numberOfSuccess = InventoryManager.TryToAdd(itemData, amount);

        if (numberOfSuccess != 0)
            CommandConsole.LogSuccess($"Succesfully gived {numberOfSuccess} {itemData.TechnicalName}.");

        if (numberOfSuccess != amount)
            CommandConsole.LogError($"Inventory is full.");
    }

    [Command("give-all")]
    void GiveAll()
    {
        bool fail = false;
        foreach (ItemData item in itemDataList.ItemDatas)
        {
            int successes = InventoryManager.TryToAdd(item, item.MaxStackSize);
            if (successes == 0) 
                fail = true;
        }

        if (fail)
            CommandConsole.LogError($"Inventory is full.");

    }

    private void Update()
    {
        scrollbar.value = Mathf.Clamp01(scrollbar.value + Mouse.current.scroll.ReadValue().y * 0.0004f);
    }

    [Command("print-inventory")]
    void PrintInventoryCommand()
    {
        for (int i = 0; i < InventoryManager.Inventory.Length; i++)
        {
            if (InventoryManager.Inventory[i].HasValue)
                CommandConsole.Log($"{i} : {InventoryManager.Inventory[i].Value.Amount} {InventoryManager.Inventory[i].Value.ItemData.TechnicalName}");
            else
                CommandConsole.Log($"{i} : null");
        }

        if (InventoryManager.Bag.HasValue)
            CommandConsole.Log($"Bag : {InventoryManager.Bag.Value.Amount} {InventoryManager.Bag.Value.ItemData.TechnicalName}");
        else
            CommandConsole.Log($"Bag : null");
    }

}
