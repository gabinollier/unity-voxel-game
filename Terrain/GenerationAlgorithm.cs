using System;
using System.Linq;
using UnityEngine;

public static class GenerationAlgorithm
{
    public const int HALF_WORLD_MERIDIAN = 8192;
    public const int WORLD_HEIGHT = 256;
    public const float SEA_LEVEL_PERCENTAGE = 0.25f;
    public const float SEA_LEVEL_BLOCKS = WORLD_HEIGHT * SEA_LEVEL_PERCENTAGE;

    static GenerationValues _values;

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        _values = Resources.Load<GenerationValues>("Generation/GenerationValues");
        _values.Initialize();
    }

    public static ushort GetBlockID(Vector3Int position)
    {
        Vector2Int position2D = new Vector2Int(position.x, position.z);
        float continentalness = GetContinentalness(position2D);
        float erosion = GetErosion(position2D);
        float pv = GetPV(position2D);
        float temperature = GetTemperature(position2D);
        float humidity = GetHumidity(position2D);
        int terrainHeight = GetTerrainHeight(continentalness, erosion, pv);
        Biome biome = GetBiome(continentalness, temperature, humidity);
        float detailsNoise = Noise.Hash12(position2D);

        return GetBlockID(position, terrainHeight, biome, detailsNoise);
    }

    public static ushort GetBlockID(Vector3Int position, int terrainHeight, Biome biome, float detailsNoise)
    {
        if (!IsBlockInWorld(position)) return Block.AirID;
        if (position.y == 0) return Block.AirID;

        // Generation Code

        ///if (Mathf.FloorToInt(position.x / (float)chunkWidth) is -1 or 0 or 1) return "air"; // pour créer une tranche

        if (position.y > terrainHeight) return Air();
        if (position.y < terrainHeight - 8) return Underground();
        return Ground();

        ushort Air()
        {
            if (position.y <= SEA_LEVEL_BLOCKS)
                return Block.GetBlockID("water");

            if (position.y == terrainHeight + 1 && detailsNoise > 0.5f) // TODO : changer ça
                return Block.GetBlockID("tall_grass");

            return Block.AirID;
        }

        ushort Ground()
        {
            if (position.y == terrainHeight) 
                return biome.grassSubstitute.IDs[0];

            return biome.dirtSubstitute.IDs[0];
        }

        ushort Underground()
        {
            return biome.stoneSubstitute.IDs[0];
        }
    }

    public static float GetTerrainHeightValue(float continentalness, float erosion, float pv) // return value between ~0 and ~1
    {
        if (continentalness < SEA_LEVEL_PERCENTAGE)
            pv = 1;

        return (continentalness - SEA_LEVEL_PERCENTAGE) * erosion * pv + SEA_LEVEL_PERCENTAGE;
    }

    public static int GetTerrainHeight(float continentalness, float erosion, float pv)
    {
        float value = GetTerrainHeightValue(continentalness, erosion, pv);
        int terrainHeight = (int)(value * WORLD_HEIGHT);

        // clamp
        if (terrainHeight < 0) terrainHeight = 0;
        if (terrainHeight >= WORLD_HEIGHT) terrainHeight = WORLD_HEIGHT - 1;

        return terrainHeight;
    }



    public static Biome GetBiome(float continentalness, float temperature, float humidity)
    {
        int continentalnessIndex = GetContinentalnessIndex(continentalness);
        int temperatureIndex = GetTemperatureIndex(temperature);
        int humidityIndex = GetHumidityIndex(humidity);

        int biomeIndex = _values.biomesIndexes[continentalnessIndex, temperatureIndex, humidityIndex];
        return _values.biomes[biomeIndex];
    }

    public static int GetHumidityIndex(float humidity)
    {
        return 
            (humidity <= 0.50f) ? 0 :
            1;
    }

    public static int GetTemperatureIndex(float temperature)
    {
        return 
            (temperature <= 0.16f) ? 0 :
            (temperature <= 0.33f) ? 1 :
            (temperature <= 0.5f) ? 2 :
            (temperature <= 0.66f) ? 3 :
            (temperature <= 0.83f) ? 4 :
            5;
    }

    public static int GetContinentalnessIndex(float continentalness)
    {
        return 
            (continentalness < 0.10f) ? 0 :
            (continentalness < 0.25f) ? 1 :
            2;
    }

    public static float GetHumidity(Vector2Int position)
    {
        return _values.humidityNoise.GetValue(position);
    }

    public static float GetTemperature(Vector2Int position)
    {
        float noise = _values.temperatureNoise.GetValue(position);
        return Mathf.InverseLerp(HALF_WORLD_MERIDIAN, 0, Mathf.Abs(position.y)) + noise - _values.temperatureNoise.globalAmplitude / 2f;
    }

    public static float GetErosion(Vector2Int position)
    {
        float noise = _values.erosionNoise.GetValue(position);
        return _values.erosionCurve.Evaluate(noise);
    }

    public static float GetPV(Vector2Int position)
    {
        float noise = _values.pvNoise.GetValue(position);
        return _values.pvCurve.Evaluate(noise);
    }

    public static float GetContinentalness(Vector2Int position)
    {
        float noise = _values.continentalnessNoise.GetValue(position);
        return _values.continentalnessCurve.Evaluate(noise);
    }

    // called by thread-pool threads in Chunk.cs
    public static void GenerateStructures(Vector2Int chunkPosition, Func<Vector2Int, (int, Biome)> getTerrainHeightAndBiome, Action<Vector3Int, ushort> editBlockCallback)
    {
        Vector2Int position = Chunk.GetChunkPositionInBlocks(chunkPosition);

        foreach (Structure forest in _values.forests)
        {
            Vector2Int cell0 = ((Vector2)position / forest.CellSize).FloorToInt();
            int cellAmountInAChunk = Mathf.CeilToInt(Chunk.CHUNK_SIZE / (float)forest.CellSize);

            // Pour les structures plus grandes que leur cellule : changer le "-1" : il doit être égal à - le nombre minimum par lequel on doit multiplier la CellSize pour dépasser la taille 
            // de la structure. Par exemple si une structure fait 20 blocks et que la cellSize fait 15 il faut mettre -2 car 2*15>20
            for (int i = -1; i <= cellAmountInAChunk; i++)
            {
                for (int j = -1; j <= cellAmountInAChunk; j++)
                {
                    Vector2Int cell = cell0 + new Vector2Int(i, j);
                    Vector3 hash = Noise.Hash32(cell, _values.seed, forest.Seed); //x and y are for the root, z is for which schematic we choose in the structure
                    Vector2Int localRoot2D = (hash.xy() * forest.CellSize).FloorToInt();
                    Vector2Int globalRoot2D = cell * forest.CellSize + localRoot2D;
                    Vector2Int globalGroundedBlock2D = globalRoot2D + forest.GroundedBlock.xz();

                    int terrainHeight;
                    Biome biome;

                    Vector2Int chunkRelativeGroundedBlock2D = Chunk.GetLocalPosition(chunkPosition, globalGroundedBlock2D);
                    if (Chunk.IsBlockInChunk(chunkRelativeGroundedBlock2D))
                    {
                        (terrainHeight, biome) = getTerrainHeightAndBiome(chunkRelativeGroundedBlock2D);
                    }
                    else
                    {
                        float continentalness = GetContinentalness(globalGroundedBlock2D);
                        float erosion = GetErosion(globalGroundedBlock2D);
                        float pv = GetPV(globalGroundedBlock2D);
                        float temperature = GetTemperature(globalGroundedBlock2D);
                        float humidity = GetHumidity(globalGroundedBlock2D);

                        terrainHeight = GetTerrainHeight(continentalness, erosion, pv);
                        biome = GetBiome(continentalness, temperature, humidity);
                    }

                    if (!forest.Biomes.Contains(biome))
                        continue;

                    Vector3Int globalRoot = new Vector3Int(
                        globalRoot2D.x, 
                        terrainHeight + 1 - forest.GroundedBlock.y, 
                        globalRoot2D.y);

                    Vector3Int chunkRelativeRoot = Chunk.GetLocalPosition(chunkPosition, globalRoot);

                    Schematic schematic = forest.GetSchematic(hash.z);

                    for (int x = 0; x < schematic.Size.x; x++)
                    {
                        for (int y = 0; y < schematic.Size.y; y++)
                        {
                            for (int z = 0; z < schematic.Size.x; z++)
                            {
                                Vector3Int schematicRelativeBlockPosition = new Vector3Int(x, y, z);
                                Vector3Int chunkRelativeBlockPosition = chunkRelativeRoot + schematicRelativeBlockPosition;
                                if (Chunk.IsBlockInChunk(chunkRelativeBlockPosition))
                                {
                                    ushort id = schematic.GetBlockID(schematicRelativeBlockPosition);
                                    if (id == Block.SchematicVoidID)
                                        continue;
                                    editBlockCallback(chunkRelativeBlockPosition, id);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static bool IsBlockInWorld(Vector3Int blockPosition)
    {
        return (0 <= blockPosition.y && blockPosition.y < WORLD_HEIGHT);
    }
}
