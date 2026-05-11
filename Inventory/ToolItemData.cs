using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ToolItemData : ItemData
{
    [SerializeField] float speedMultiplicator = 1;
    [SerializeField] ToolCategory category = ToolCategory.Pickaxe; 

    public override void LeftClickAction(int indexInInventory)
    {
        PlayerBlockInteractor.Instance.TryDestroyBlock(speedMultiplicator, category);
    }

    public override void RightClickAction(int indexInInventory)
    {
    }
}

public enum ToolCategory
{
    Shovel,
    Axe,
    Pickaxe
}