using UnityEngine;
using UnityEngine.Events;

public struct ItemStack
{
    public ItemData ItemData;
    public int Amount;

    public ItemStack(ItemData itemData, int amout)
    {
        this.ItemData = itemData;
        this.Amount = amout;
    }
}

public static class InventoryManager
{
    const int BASE_CAPACITY = 30;

    public static ItemStack?[] Inventory { get; private set; }
    public static ItemStack? Bag { get; private set; } // on considère que c'est l'ID -1

    public static UnityEvent InventoryUpdate { get; private set; } = new UnityEvent();
    public static UnityEvent InventorySizeUpdate { get; private set; } = new UnityEvent();

    public static int Capacity => BASE_CAPACITY + (Bag.HasValue && Bag.Value.ItemData is BagItemData ? ((BagItemData)Bag.Value.ItemData).Capacity : 0);

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        Inventory = new ItemStack?[Capacity];
    }

    [Command("clear_inventory")]
    static void Clear()
    {
        Inventory = new ItemStack?[Capacity];
        Bag = null;

        InventoryUpdate.Invoke();
    }

    // returns how many items were succesfully added
    public static int TryToAdd(ItemData itemToAdd, int amount)
    {
        int remainingItems = amount;

        for (int i = 0; i < Inventory.Length; i++)
        {
            if (Inventory[i].HasValue)
            {
                if (Inventory[i].Value.ItemData == itemToAdd)
                {
                    int itemsInSlot = Inventory[i].Value.Amount;
                    int totalStackSize = Mathf.Min(itemsInSlot + remainingItems, itemToAdd.MaxStackSize);
                    ItemData itemData = Inventory[i].Value.ItemData;
                    Inventory[i] = new ItemStack(itemData, totalStackSize);
                    remainingItems -= totalStackSize - itemsInSlot;
                    if (remainingItems <= 0)
                        break;
                }
            }
            else
            {
                int stackSize = Mathf.Min(remainingItems, itemToAdd.MaxStackSize);
                Inventory[i] = new ItemStack(itemToAdd, stackSize);
                remainingItems -= stackSize;
                if (remainingItems <= 0)
                    break;
            }
        }

        InventoryUpdate.Invoke();

        return amount - remainingItems;
    }
    [Command("swap-inventory-slots")]
    public static void Swap(int slotA, int slotB)
    {
        if (slotB == -1)
            (slotA, slotB) = (slotB, slotA);  

        // le slot A est le slot du bag 
        if (slotA == -1)
        {
            if (Inventory[slotB].HasValue && Inventory[slotB].Value.ItemData is not BagItemData)
                return;

            (Bag, Inventory[slotB]) = (Inventory[slotB], Bag);

            // change inventory size

            ItemStack?[] oldInventory = Inventory;
            Inventory = new ItemStack?[Capacity];

            for (int i = 0; i < Mathf.Min(oldInventory.Length, Inventory.Length); i++)
            {
                Inventory[i] = oldInventory[i];
            }

            if (Inventory.Length < oldInventory.Length)
            {
                for (int i = Inventory.Length; i < oldInventory.Length; i++)
                {
                    // drop oldInventory[i] 

                    if (oldInventory[i].HasValue)
                        Debug.Log($"drop {oldInventory[i].Value.Amount} {oldInventory[i].Value.ItemData.TechnicalName}");
                }
            }

            InventorySizeUpdate.Invoke();

        }
        else
        {
            (Inventory[slotA], Inventory[slotB]) = (Inventory[slotB], Inventory[slotA]);
        }

        InventoryUpdate.Invoke();
    }

    public static void TryToRemove(int slot, int amount)
    {
        if (slot <= -1)
        {
            Debug.LogError($"You should not try to remove items from slot {slot}.");
            return;
        }

        if (Inventory[slot].HasValue && Inventory[slot].Value.Amount - amount >= 0)
        {
            Inventory[slot] = new ItemStack(Inventory[slot].Value.ItemData, Inventory[slot].Value.Amount - amount);

            if (Inventory[slot].Value.Amount <= 0)
                Inventory[slot] = null;

            InventoryUpdate.Invoke();
        }
    }

    /*
    public static void SpawnItemObject(Vector3 position, LootTable lootTable, bool useRandomDirection = true)
    {
        foreach (ItemStack itemStack in lootTable.GetPackedItemStacks())
        {
            SpawnItemObject(position, itemStack, useRandomDirection);
        }
    }

    public void SpawnItemObject(Vector3 position, ItemStack itemStack, bool useRandomDirection = true)
    {
        GameObject go = Instantiate(itemObjectPrefab, position + Vector3.one * Random.Range(-0.1f, 0.1f), Quaternion.identity);
        go.GetComponent<ItemObject>().SetItemStack(itemStack);
        if (useRandomDirection)
        {
            Vector2 direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * ejectionForce;
            go.GetComponent<Rigidbody>().AddForce(new Vector3(direction.x, ejectionForce, direction.y));
        }
    }*/
}