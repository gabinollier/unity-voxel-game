using UnityEngine;
using NaughtyAttributes;


public class SchematicCreator : MonoBehaviour
{
    [SerializeField] Vector3Int root;
    [SerializeField] Vector3Int size;
    [SerializeField] bool replaceAirBySchematicVoid = true;
    [SerializeField] Schematic structureToEdit;
    ChunkLoader _chunkLoader;

#if UNITY_EDITOR

    private void Awake()
    {
        _chunkLoader = FindObjectOfType<ChunkLoader>();
    }

    private void OnValidate()
    {
        if (size.x < 1) size.x = 1;
        if (size.y < 1) size.y = 1;
        if (size.z < 1) size.z = 1;
    }

    [Button("Populate Blocks Array", EButtonEnableMode.Playmode)]
    void PopulateBlocksArray()
    {
        ushort[,,] blocks = new ushort[size.x, size.y, size.z];

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    Vector3Int globalPosition = root + new Vector3Int(x, y, z);
                    ushort block = _chunkLoader.GetBlockID(globalPosition);
                    if (replaceAirBySchematicVoid && block == Block.AirID) 
                        block = Block.SchematicVoidID;
                    blocks[x, y, z] = block;
                }
            }
        }
        structureToEdit.EditSchematic(blocks);
    }

    private void OnDrawGizmos()
    {
        // Box
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(root + 0.5f * (Vector3)size, size);

        // Root
        var color = Color.green;
        color.a = .5f;
        Gizmos.color = color;
        Gizmos.DrawCube(root + 0.5f * Vector3.one, Vector3.one);
    }
#endif
}


