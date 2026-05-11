using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu()]
public class Structure : ScriptableObject
{
    // La "root" d'une structure est le bloc le plus en bas, à l'ouest et au sud de celle-ci
    // Le "groundedBlock" d'une structure est le bloc qui sera au sol (ex : le bas du tronc pour un arbre)
    // Pour chaque Structure, le monde est divisé en cellules de "CellSize" de large. 
    // La root dans la cellule est déterminée aléatoirement.

    [field: Header("Structure")]
    [ReadOnly] public int Seed;
    [field: SerializeField] public int CellSize { get; private set; }
    [field: SerializeField] public Vector3Int GroundedBlock { get; private set; }
    [field: SerializeField] public Biome[] Biomes { get; private set; }

    [field: Header("Schematics")]
    [field: ValidateInput("Validate", "The sum of spawn chances must not exceed 1.\n Nothing has to be null or empty.\nA schematic Size can't exceed the structure CellSize")]
    [field: SerializeField] public SchematicData[] SchematicDatas { get; private set; }

    bool Validate()
    {
        float sum = 0;
        if (SchematicDatas == null)
            return false;
        if (SchematicDatas.Length == 0)
            return false;

        foreach (SchematicData schematicData in SchematicDatas)
        {
            if (schematicData == null)
                return false;
            if (schematicData.Schematic == null)
                return false;
            if (schematicData.Schematic.Size.x > CellSize ||
                schematicData.Schematic.Size.z > CellSize)
                return false;

            sum += schematicData.SpawnChance;
            if (sum > 1)
                return false;
        }

        if (Biomes == null)
            return false;
        if (Biomes.Length == 0)
            return false;

        foreach (Biome biome in Biomes)
        {
            if (biome == null)
                return false;
        }

        return true;
    }

    public Schematic GetSchematic(float random01)
    {
        float total = 0;

        foreach (SchematicData treeData in SchematicDatas)
        {
            total += treeData.SpawnChance;
            if (random01 <= total)
                return treeData.Schematic;
        }

        return SchematicDatas[0].Schematic;
    }

    [System.Serializable]
    public class SchematicData
    {
        [field: SerializeField] public Schematic Schematic { get; private set; }
        [Range(0f, 1f)] public float SpawnChance;
    }
}