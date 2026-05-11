using UnityEngine;
using BinGa.Serialization;
using NaughtyAttributes;

[CreateAssetMenu()]
[System.Serializable]
public class Schematic : ScriptableObject, ISerializationCallbackReceiver
{
    [field : SerializeField, ReadOnly] public Vector3Int Size { get; private set; }

    [SerializeField, ReadOnly] ushort[] blocksSerialized;
    
    ushort[,,] _blocks;

    bool ValidateSize()
    {
        return (
            0 <= Size.x && Size.x <= Chunk.CHUNK_SIZE + 1 &&
            0 <= Size.z && Size.z <= Chunk.CHUNK_SIZE + 1);
    }

    bool IsBlockInSchematic(Vector3Int positionInSchematic)
    {
        return (
            0 <= positionInSchematic.x && positionInSchematic.x < Size.x &&
            0 <= positionInSchematic.y && positionInSchematic.y < Size.y &&
            0 <= positionInSchematic.z && positionInSchematic.z < Size.z);
    }

    public ushort GetBlockID(Vector3Int positionInSchematic)
    {
        if (!IsBlockInSchematic(positionInSchematic))
            return Block.SchematicVoidID;

        return _blocks[positionInSchematic.x, positionInSchematic.y, positionInSchematic.z];
    }



    public void OnBeforeSerialize()
    {
        blocksSerialized = Serializer.Convert(_blocks);
    }

    public void OnAfterDeserialize()
    {
        _blocks = Serializer.Convert(blocksSerialized, Size);
    }

#if UNITY_EDITOR

    // Do not use in any case but to create the schematic, in the editor
    public void EditSchematic(ushort[,,] blocks)
    {
        Debug.Log("Edited blocks array of the structure.");
        _blocks = blocks;
        Size = new Vector3Int(_blocks.GetLength(0), _blocks.GetLength(1), _blocks.GetLength(2));
        OnBeforeSerialize();
        UnityEditor.EditorUtility.SetDirty(this);
    }

#endif
}