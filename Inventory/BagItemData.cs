using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BagItemData : ItemData
{
    [field:SerializeField] public int Capacity { get; private set; }


    public override void LeftClickAction(int indexInInventory)
    {
    }

    public override void RightClickAction(int indexInInventory)
    {
    }


}
