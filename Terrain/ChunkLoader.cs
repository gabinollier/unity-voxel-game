using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Threading;
using NaughtyAttributes;
using System;
using System.Linq;

// "generate" stands for "populate blockIDs array + update mesh datas"
// These are handled by the ThreadPool

// "update" stands for "update mesh datas"
// These are handled by the _chunksUpdateThread

// Since Meshes are not thread safe, mesh building is done by the main thread

public class ChunkLoader : MonoBehaviour
{
    [field: SerializeField, ValidateInput("ValidateMaterials", "Les matériaux doivent être ceux de Block.TerrainMaterial")] public Material[] Materials { get; private set; }

    [Min(2), SerializeField] int defaultViewDistanceInChunks;
    [SerializeField] float unloadDistanceFactor;
    [SerializeField] int maxChunkInstanciationPerFrame;
    [SerializeField] string terrainLayer;
    int terrainLayerID;

    [ShowNonSerializedField] int _viewDistanceInChunksSq;
    [ShowNonSerializedField] int _unloadDistanceInChunksSq;
    [ShowNonSerializedField] float _maxChunksEstimation;
    [ShowNonSerializedField] int _adjustedChunkInstanciationPerFrame;
    [ShowNonSerializedField] int _adjustedMeshBuildingPerFrame;
    Camera _cam;
    Dictionary<Vector2Int, Chunk> _chunks = new Dictionary<Vector2Int, Chunk>();
    List<Chunk> _chunksToBuildMesh = new List<Chunk>();
    Queue<Action> _chunksToUpdate = new Queue<Action>();
    Thread _chunkUpdateThread;
    int _chunksToGenerateInQueue = 0;
    int _chunksGeneratedLastFrame = 0;
    int _unloadTickMax = 31; // must be (2^n)-1
    int _unloadTick = 0;
    int _viewDistanceInChunks;
    Stack<Vector2Int> _chunkUnloadStack = new Stack<Vector2Int>();
    DebugMenu _debugMenu;
    WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

    bool ValidateMaterials()
    {
        if (Materials.Length == Block.TerrainMaterialCount &&
            Materials.All((mat) => mat != null))
            return true;

        Debug.LogWarning("Les matériaux du chunkLoader doivent être ceux de Block.TerrainMaterial");
        return false;
    }


    void Awake()
    {
        _cam = Camera.main;
        _debugMenu = FindObjectOfType<DebugMenu>();
        terrainLayerID = LayerMask.NameToLayer(terrainLayer);

        SetViewDistance(defaultViewDistanceInChunks);
    }

    private void Start()
    {
        _chunkUpdateThread = new Thread(new ThreadStart(ChunkUpdateThread));
        _chunkUpdateThread.Start();

    }

    private void OnDisable() => _chunkUpdateThread.Abort();

    public void SetViewDistance(int viewDistanceInChunks)
    {
        _viewDistanceInChunks = viewDistanceInChunks;
        _viewDistanceInChunksSq = _viewDistanceInChunks * _viewDistanceInChunks;
        _unloadDistanceInChunksSq = (int)(_viewDistanceInChunksSq * unloadDistanceFactor * unloadDistanceFactor);
        _maxChunksEstimation = (Mathf.PI * _viewDistanceInChunksSq) * 1.15f;
    }

    // Main thread
    void LateUpdate()
    {
        Vector2Int playerChunkPosition = GetChunkPosition(_cam.transform.position.FloorToInt());

        CheckRenderDistance(playerChunkPosition);
        UnloadChunks(playerChunkPosition);
        BuildMeshesFromQueue();
        UpdateDebugInfos();

        _chunksGeneratedLastFrame = 0;

        _adjustedChunkInstanciationPerFrame = (int)Mathf.Lerp(maxChunkInstanciationPerFrame, 1, _chunks.Count / _maxChunksEstimation);
        _adjustedMeshBuildingPerFrame = _adjustedChunkInstanciationPerFrame * 2;
    }

    // In main thread
    void CheckRenderDistance(Vector2Int playerChunkPosition)
    {
        int instanciationCounter = 0;

        CheckChunk(0, 0);

        for (int a = 1; a < _viewDistanceInChunks; a++)
        {

            CheckChunk(a, 0);
            CheckChunk(-a, 0);
            CheckChunk(0, a);
            CheckChunk(0, -a);

            for (int b = 1; b < a; b++)
            {
                if (CheckChunk(a, b) ||
                    CheckChunk(a, -b) ||
                    CheckChunk(-a, b) ||
                    CheckChunk(-a, -b) ||
                    CheckChunk(b, a) ||
                    CheckChunk(b, -a) ||
                    CheckChunk(-b, a) ||
                    CheckChunk(-b, -a))
                    return;
            }

            if (CheckChunk(a, a) ||
                CheckChunk(a, -a) ||
                CheckChunk(-a, a) ||
                CheckChunk(-a, -a))
                return;
            
        }

        // return false by default / return true if instanciationCounter is greater than the maximum
        bool CheckChunk(int chunkRelativePositionX, int chunkRelativePositionZ) 
        {

            Vector2Int chunkPosition = new Vector2Int(
                playerChunkPosition.x + chunkRelativePositionX,
                playerChunkPosition.y + chunkRelativePositionZ
                );

            if (_chunks.ContainsKey(chunkPosition) || ChunkDistanceSq(playerChunkPosition, chunkPosition) > _viewDistanceInChunksSq)
            {
                return false;
            }

            // RESPONSABLE DES LIKE SPIKES
            Chunk chunk = new Chunk(chunkPosition, terrainLayerID, this);

            instanciationCounter++;

            _chunks.Add(chunkPosition, chunk);
            StartCoroutine(GenerateChunk(chunk));


            if (instanciationCounter >= _adjustedChunkInstanciationPerFrame)
            {
                return true;
            }

            return false;
        }
    }

    void UnloadChunks(Vector2Int playerChunkPosition)
    {
        foreach (Vector2Int chunkPosition in _chunks.Keys.Where(chunkPosition =>
            (chunkPosition.y & _unloadTickMax) != _unloadTick &&
            ChunkDistanceSq(chunkPosition, playerChunkPosition) > _unloadDistanceInChunksSq))
        {
            _chunkUnloadStack.Push(chunkPosition);
        }

        if (++_unloadTick > _unloadTickMax)
            _unloadTick = 0;

        while (_chunkUnloadStack.Count != 0)
        {
            Vector2Int pos = _chunkUnloadStack.Pop();
            Chunk chunk = _chunks[pos];

            chunk.Destroy();

            _chunks.Remove(pos);
        }
    }

    // Make a WaitCallback that will generates the chunk
    // Enqueue the WaitCallback in the threadpool so the chunk is generates by thread pool.
    // Then, enqueue the chunk in _chunksToBuildMesh so its mesh is build by the main thread
    // Called in the main thread
    IEnumerator GenerateChunk(Chunk chunk)
    {
        bool isDone = false;
        WaitCallback work = (state) => 
        {
            chunk.PopulateBlockIDs();
            chunk.UpdateMeshData();

            isDone = true;
        };

        ThreadPool.QueueUserWorkItem(work);
        _chunksToGenerateInQueue++;
         
        while (!isDone) yield return _waitForEndOfFrame;

        _chunksGeneratedLastFrame++;
        _chunksToGenerateInQueue--;

        _chunksToBuildMesh.Add(chunk); 
    }


    // Make a coroutine that will updates the chunk
    // Enqueue the Action in _chunksToUpdate so the chunk is updated by the _chunkUpdateThread.
    // Then, enqueue the chunk in _chunksToBuildMesh so its mesh is build by the main thread
    // Called by the chunks in the main thread to update themselves.
    public IEnumerator UpdateChunk(Chunk chunk)
    {
        bool isDone = false;

        Action work = delegate
        {
            chunk.UpdateMeshData();
            isDone = true;
        };

        _chunksToUpdate.Enqueue(work);

        while (!isDone) yield return null;

        _chunksToBuildMesh.Insert(0, chunk);
    }

    // Called by chunks in main thread
    public void StartUpdatingChunk(Vector2Int chunkPosition)
    {
        if (_chunks.TryGetValue(chunkPosition, out var chunk))
        {
            StartCoroutine(UpdateChunk(chunk));
        }
    }

    // Chunk Update Thread
    void ChunkUpdateThread()
    {
        while (true)
        {
            if (_chunksToUpdate.Count > 0)
            {
                _chunksToUpdate.Dequeue()(); 
            }
        }
    }

    // Main thread
    private void BuildMeshesFromQueue()
    {
        int meshBuilt = 0;
        while (_chunksToBuildMesh.Count > 0 && meshBuilt < _adjustedMeshBuildingPerFrame)
        {
            _chunksToBuildMesh[0].BuildMesh();
            _chunksToBuildMesh.RemoveAt(0);
            meshBuilt++;
        }
    }

    int ChunkDistanceSq(Vector2Int a, Vector2Int b)
    {
        return (b - a).sqrMagnitude;
    }

    public Vector2Int GetChunkPosition(Vector3Int blockPosition)
    {
        return GetChunkPosition(blockPosition.xz());
    }

    public Vector2Int GetChunkPosition(Vector2Int blockPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(blockPosition.x >> Chunk.CHUNK_SIZE_IN_BITS),
            Mathf.RoundToInt(blockPosition.y >> Chunk.CHUNK_SIZE_IN_BITS));
    }

    public ushort GetBlockID(Vector3Int blockPosition)
    {
        if (!GenerationAlgorithm.IsBlockInWorld(blockPosition)) return Block.AirID;

        Vector2Int chunkPosition = GetChunkPosition(blockPosition);

        if (_chunks.TryGetValue(chunkPosition, out Chunk chunk) &&
            chunk.TryGetBlockID(blockPosition, out ushort id))
        {
            return id;
        }

        return GenerationAlgorithm.GetBlockID(blockPosition);
    }

    public (Block block, int rotation) GetBlock(Vector3Int blockPosition)
    {
        return Block.GetBlock(GetBlockID(blockPosition));
    }

    public void EditBlock(Vector3Int blockPosition, ushort blockID, bool updateChunks = true)
    {
        if (!GenerationAlgorithm.IsBlockInWorld(blockPosition)) return;

        Vector2Int chunkPosition = GetChunkPosition(blockPosition); 

        if (_chunks.TryGetValue(chunkPosition, out Chunk chunk))
        {
            chunk.TryEditBlock(blockPosition, blockID, updateChunks);
        }
    }

    bool IsChunkEditable(Vector2Int chunkPosition)
    {
        return 
            _chunks.TryGetValue(chunkPosition, out Chunk chunk)
            && chunk.IsBlockIDsPopulated;
    }

    void UpdateDebugInfos()
    {
        if (_debugMenu.IsMenuOpened)
        {
            _debugMenu.ChunksToBuildMeshCount = _chunksToBuildMesh.Count;
            _debugMenu.ChunksToUpdateCount = _chunksToUpdate.Count;
            _debugMenu.ChunksCount = _chunks.Count;
            _debugMenu.ChunksToGenerateInQueue = _chunksToGenerateInQueue;
            _debugMenu.SetChunksGeneratedLastFrame(_chunksGeneratedLastFrame);
            _debugMenu.AdjustedChunkInstanciationPerFrame = _adjustedChunkInstanciationPerFrame;
        }
    }
}
