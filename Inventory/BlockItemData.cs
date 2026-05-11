using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu]
public class BlockItemData : ItemData
{
    [field: Header("Block")]
    [field: SerializeField] public Block Block { get; private set; }


#if UNITY_EDITOR

    [Button("Generate Icon from Block")]
    void GenerateIconFromBlock()
    {
        if (Block == null)
            return;

        GameObject obj = new GameObject();
        obj.transform.position = iconPosition;
        obj.transform.rotation = iconRotation;
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        meshFilter.mesh = Block.DisplayMesh;

        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        List<Material> materials = new List<Material>();
        meshRenderer.sharedMaterials = new Material[Block.DisplayMesh.subMeshCount];
        for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
        {
            Material material = new Material(Shader.Find("Shader Graphs/BlockItems"));
            material.mainTexture = Block.textures[i];
            materials.Add(material);
        }

        meshRenderer.SetSharedMaterials(materials);

        GenerateIcon();

        DestroyImmediate(obj);
    }
#endif

    public override void RightClickAction(int indexInInventory)
    {
        if (PlayerBlockInteractor.Instance.TryPlaceBlock(Block))
            InventoryManager.TryToRemove(indexInInventory, 1);
    }

    public override void LeftClickAction(int indexInInventory)
    {
    }

}
