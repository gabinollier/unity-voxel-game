using UnityEngine;
using System.Collections.Generic;
using System;

public class Chunk
{
    public const int CHUNK_SIZE_IN_BITS = 4;
    public const int CHUNK_SIZE = 1 << CHUNK_SIZE_IN_BITS;

    public Vector3Int ChunkPositionInBlocks { get; private set; }
    public Vector2Int ChunkPositionInChunks;

    public static readonly Vector2Int[] relativeAdjacentPositions = new Vector2Int[4]
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

    public bool IsBlockIDsPopulated = false;

    ChunkLoader _chunkLoader;
    GameObject _gameObject;
    MeshFilter _meshFilter;
    public ushort[,,] _blockIDs;
    List<Vector3> _vertices = new List<Vector3>();
    List<int>[] _triangles = new List<int>[Block.TerrainMaterialCount];
    List<Vector3> _uvs = new List<Vector3>(); //x and y are the uvs, z is the texture index.
    object _meshDataThreadLock = new object();

    public Chunk(Vector2Int chunkPositionInChunks, int layer, ChunkLoader chunkLoader)
    {
        _chunkLoader = chunkLoader;

        _gameObject = new GameObject();
        _meshFilter = _gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = _gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = chunkLoader.Materials;
        _gameObject.transform.parent = chunkLoader.transform;
        _gameObject.layer = layer;

        ChunkPositionInChunks = chunkPositionInChunks;
        ChunkPositionInBlocks = new Vector3Int(
            chunkPositionInChunks.x << CHUNK_SIZE_IN_BITS,
            0,
            chunkPositionInChunks.y << CHUNK_SIZE_IN_BITS
            );

        _gameObject.transform.position = ChunkPositionInBlocks;

#if UNITY_EDITOR
        _gameObject.name = "Chunk " + chunkPositionInChunks;
#endif
    }

    public void Destroy()
    {
        GameObject.Destroy(_gameObject);
    }

    public void PopulateBlockIDs()
    {
        _blockIDs = new ushort[CHUNK_SIZE, GenerationAlgorithm.WORLD_HEIGHT, CHUNK_SIZE];
        int[,] terrainHeightCache = new int[CHUNK_SIZE, CHUNK_SIZE];
        Biome[,] biomeCache = new Biome[CHUNK_SIZE, CHUNK_SIZE];


        // Terrain Generation
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                Vector2Int position2D = new Vector2Int(ChunkPositionInBlocks.x + x, ChunkPositionInBlocks.z + z);
                float continentalness = GenerationAlgorithm.GetContinentalness(position2D);
                float erosion = GenerationAlgorithm.GetErosion(position2D);
                float pv = GenerationAlgorithm.GetPV(position2D);
                float temperature = GenerationAlgorithm.GetTemperature(position2D);
                float humidity = GenerationAlgorithm.GetHumidity(position2D);
                int terrainHeight = GenerationAlgorithm.GetTerrainHeight(continentalness, erosion, pv);
                Biome biome = GenerationAlgorithm.GetBiome(continentalness, temperature, humidity);
                float detailsNoise = Noise.Hash12(position2D);

                terrainHeightCache[x, z] = terrainHeight;
                biomeCache[x, z] = biome;

                for (int y = 0; y < GenerationAlgorithm.WORLD_HEIGHT; y++)
                {
                    Vector3Int position = ChunkPositionInBlocks + new Vector3Int(x, y, z);
                    _blockIDs[x, y, z] = GenerationAlgorithm.GetBlockID(position, terrainHeight, biome, detailsNoise);
                }
            }
        }

        // Structure Generation

        Action<Vector3Int, ushort> editBlockCallback = (Vector3Int localPosition, ushort id) => 
        {
            _blockIDs[localPosition.x, localPosition.y, localPosition.z] = id;
        };
        Func<Vector2Int, (int, Biome)> getTerrainHeightAndBiome = (Vector2Int localPosition) => 
        {
            return (
            terrainHeightCache[localPosition.x, localPosition.y], 
            biomeCache[localPosition.x, localPosition.y]
            );
        };
        GenerationAlgorithm.GenerateStructures(ChunkPositionInChunks, getTerrainHeightAndBiome, editBlockCallback);


    IsBlockIDsPopulated = true;
    }

    public void UpdateMeshData()
    {
        lock(_meshDataThreadLock)
        {
            _vertices.Clear();
            _uvs.Clear();
            _triangles = new List<int>[Block.TerrainMaterialCount];
            for (int i = 0; i < Block.TerrainMaterialCount; i++) _triangles[i] = new List<int>();
            int vertexIndex = 0;

            for (int y = 0; y < GenerationAlgorithm.WORLD_HEIGHT; y++)
            {
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        Vector3Int localPosition = new Vector3Int(x, y, z);
                        ushort blockID = _blockIDs[x, y, z];
                        if (blockID == Block.AirID) 
                            continue;
                        (Block block, int rotation) = Block.GetBlock(blockID);

                        // la septième face représente tous les triangles qui ne sont pas sur une des 6 faces (ex : la marche d'un escalier)
                        for (int face = 0; face < 7; face++)
                        {
                            if (!ShouldDrawFace(localPosition, block.IDs[0], face))
                                continue;

                            var processedMesh = block.ProcessedMeshes[rotation,face];

                            for (int i = 0; i < processedMesh.vertices.Length; i++)
                            {
                                _vertices.Add(localPosition + processedMesh.vertices[i]);
                                _uvs.Add(processedMesh.uvs[i]);
                            }

                            for (int materialIndex = 0; materialIndex < Block.TerrainMaterialCount; materialIndex++)
                            {
                                if (((int)block.Material) == materialIndex)
                                {
                                    for (int i = 0; i < processedMesh.triangles.Length; i++)
                                    {
                                        _triangles[materialIndex].Add(vertexIndex + processedMesh.triangles[i]);
                                    }
                                }
                            }

                            vertexIndex += processedMesh.vertices.Length;
                        }
                    }
                }
            }

            bool ShouldDrawFace(Vector3Int localPosition, ushort blockID, int face)
            {
                if (face == 6)
                {
                    if (blockID == Block.WaterID)
                        face = 2;
                    else
                        return true;
                }

                Vector3Int adjacentBlockPositionInChunk = localPosition + CubeData.relativeAdjacentPositions[face];

                Block adjacentBlock;
                int rotation;
                if (IsBlockInChunk(adjacentBlockPositionInChunk))
                {
                    (adjacentBlock, rotation) = Block.GetBlock(_blockIDs[adjacentBlockPositionInChunk.x, adjacentBlockPositionInChunk.y, adjacentBlockPositionInChunk.z]);
                }
                else
                {
                    (adjacentBlock, rotation) = _chunkLoader.GetBlock(ChunkPositionInBlocks + adjacentBlockPositionInChunk); 
                }

                if (blockID == Block.WaterID && adjacentBlock.IDs[0] == Block.WaterID)
                    return false;

                return adjacentBlock.ShouldDrawAdjacentFaces[rotation, face];
            }

        }
    }

    public void BuildMesh()
    {
        Mesh mesh = new Mesh();
        mesh.subMeshCount = 4;

        lock (_meshDataThreadLock)
        {
            mesh.vertices = _vertices.ToArray();
            mesh.SetUVs(0, _uvs.ToArray());
            for (int materialIndex = 0; materialIndex < Block.TerrainMaterialCount; materialIndex++)
            {
                mesh.SetTriangles(_triangles[materialIndex], materialIndex);
            }
        }

        mesh.RecalculateNormals();
        mesh.Optimize();

#if UNITY_EDITOR
        mesh.name = "Generated Mesh";
#endif
        _meshFilter.mesh = mesh;
    }


    #region NonGenerationRelated


    public bool TryGetBlockID(Vector3Int globalBlockPosition, out ushort id)
    {
        if (!IsBlockIDsPopulated)
        {
            id = 0;
            return false;
        }

        Vector3Int p = GetLocalPosition(globalBlockPosition);
        id = _blockIDs[p.x, p.y, p.z];
        return true;
    }

    Vector3Int GetLocalPosition(Vector3Int globalBlockPosition)
    {
        return globalBlockPosition - ChunkPositionInBlocks;
    }

    public bool TryEditBlock(Vector3Int globalBlockPosition, ushort newBlockID, bool updateChunks = true)
    {
        if (!IsBlockIDsPopulated)
            return false;

        Vector3Int localPosition = GetLocalPosition(globalBlockPosition);
        _blockIDs[localPosition.x, localPosition.y, localPosition.z] = newBlockID;

        if (!updateChunks)
            return true;

        if (newBlockID != Block.AirID) _chunkLoader.StartUpdatingChunk(ChunkPositionInChunks);

        //update adjacent chunks
        for (int f = 0; f < 6; f++)
        {
            if (!IsBlockInChunk(localPosition + CubeData.relativeAdjacentPositions[f]))
            {
                Vector2Int chunkPosition = _chunkLoader.GetChunkPosition(ChunkPositionInBlocks + localPosition + CubeData.relativeAdjacentPositions[f]);
                _chunkLoader.StartUpdatingChunk(chunkPosition);
            }
        }

        if (newBlockID == Block.AirID) _chunkLoader.StartUpdatingChunk(ChunkPositionInChunks);

        return true;
    }

    public static Vector2Int GetLocalPosition(Vector2Int chunkPosition, Vector2Int blockGlobalPosition)
    {
        return blockGlobalPosition - GetChunkPositionInBlocks(chunkPosition);
    }

    public static Vector3Int GetLocalPosition(Vector2Int chunkPosition, Vector3Int blockGlobalPosition)
    {
        return blockGlobalPosition - GetChunkPositionInBlocks(chunkPosition).x0y();
    }

    public static Vector2Int GetChunkPositionInBlocks(Vector2Int chunkPosition)
    {
        return new Vector2Int(
            chunkPosition.x << CHUNK_SIZE_IN_BITS,
            chunkPosition.y << CHUNK_SIZE_IN_BITS
            );
    }
    public static bool IsBlockInChunk(Vector3Int localPosition)
    {
        return !(localPosition.x < 0 || localPosition.x > CHUNK_SIZE - 1
            || localPosition.y < 0 || localPosition.y > GenerationAlgorithm.WORLD_HEIGHT - 1
            || localPosition.z < 0 || localPosition.z > CHUNK_SIZE - 1);
    }
    public static bool IsBlockInChunk(Vector2Int localPosition)
    {
        return !(localPosition.x < 0 || localPosition.x > CHUNK_SIZE - 1
            || localPosition.y < 0 || localPosition.y > CHUNK_SIZE - 1);
    }

    #endregion
}