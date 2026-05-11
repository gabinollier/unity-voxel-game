using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBlockInteractor : MonoBehaviour
{
    public bool EnableSelectionOutline = false;
    public Block SelectedBlock { get; private set; }

    [SerializeField] PlayerValues playerValues;
    [SerializeField] GameObject selectionOutline, selectionOutlineChild;
    [SerializeField] Texture2D clearTexture;
    [SerializeField] List<Texture2D> breakingTextures;
    Entity _entity;
    Material _breakingMaterial;

    MeshFilter _selectionMeshFilter;
    Vector3Int? _selectedPosition, _placePosition;
    Transform _cam;
    ChunkLoader _chunkLoader;
    [NaughtyAttributes.ShowNonSerializedField] bool _shouldPlacedBlockBeReversed;
    bool _isDestroyingBlockThisFrame = false;
    float _destroyingTime = 0;
    BlockBreakTimeTable breakTimeTable = null;

    public static PlayerBlockInteractor Instance;

    private void Awake()
    {
        if (Instance == null) 
            Instance = this;
        else
            Destroy(this);

        _cam = Camera.main.transform;
        _chunkLoader = FindObjectOfType<ChunkLoader>();
        _selectionMeshFilter = selectionOutline.GetComponentInChildren<MeshFilter>();
        _entity = GetComponent<Entity>();
        _breakingMaterial = selectionOutline.GetComponentInChildren<MeshRenderer>().sharedMaterial;
    }

    private void Start()
    {
        ResetDestroyingTime();
    }

    public void TryDestroyBlock(float speedMultiplicator, ToolCategory toolCategory)
    {
        if (_selectedPosition.HasValue && breakTimeTable != null)
        {
            _destroyingTime += Time.deltaTime * speedMultiplicator;

            int textureIndex = Mathf.FloorToInt(breakingTextures.Count * (_destroyingTime / breakTimeTable.Table[toolCategory]));
            if (textureIndex >= breakingTextures.Count)
                textureIndex = breakingTextures.Count - 1;

            _breakingMaterial.SetTexture("_BreakingTexture", breakingTextures[textureIndex]);

            if (_destroyingTime >= breakTimeTable.Table[toolCategory])
            {
                ResetDestroyingTime();
                _chunkLoader.EditBlock(_selectedPosition.Value, Block.AirID);
            }

            _isDestroyingBlockThisFrame = true;
        }
    }

    public bool TryPlaceBlock(Block blockToPlace)
    {
        if (_placePosition == null)
            return false;

        Vector3Int position = (Vector3Int)_placePosition;

        if (_chunkLoader.GetBlockID(position) != Block.AirID)
            return false;

        int rotation = blockToPlace.GetRotation(_cam.forward, _shouldPlacedBlockBeReversed);

        if (_entity.DoesCollideWithBlock(blockToPlace, rotation, position))
        {
            return false;
        }

        _chunkLoader.EditBlock(position, blockToPlace.IDs[rotation]);
        return true;
    }

    private void Update()
    {
        Vector3 relativeHitPosition = Vector3.zero;
        Vector3 face = Vector3.zero;
        Func<Block, Vector3Int, bool> condition = (Block block, Vector3Int blockPosition) => block.DoesRayCollide(_cam.position, _cam.forward, playerValues.ReachSq, blockPosition, ref relativeHitPosition, ref face);
        var result = CustomRaycasts.BlockRaycast(_cam.position, _cam.forward, playerValues.Reach, condition);

        _shouldPlacedBlockBeReversed =
            face == Vector3.up ? false :
            face == Vector3.down ? true :
            relativeHitPosition.y < 0.5f ? false :
            true;

        if (!selectionOutline.activeSelf && EnableSelectionOutline && result.success) 
            selectionOutline.SetActive(true);
        if (selectionOutline.activeSelf && (!EnableSelectionOutline || !result.success)) 
            selectionOutline.SetActive(false);
        
        if (result.block != SelectedBlock)
        {
            SelectedBlock = result.block;
            if (result.success)
                _selectionMeshFilter.mesh = SelectedBlock.GetSelectionMeshInOneSubmesh();
        }

        // change selected position
        if (result.position != _selectedPosition)
        {
            _selectedPosition = result.position;
            ResetDestroyingTime();

            if (result.success)
            {
                // change destruction time
                breakTimeTable = result.block.BreakTimeTable;

                // move selection outline
                selectionOutline.transform.position = (Vector3Int)result.position;
                selectionOutlineChild.transform.rotation = Quaternion.identity;
                selectionOutlineChild.transform.localScale = new Vector3(1, 1, 1);
                selectionOutlineChild.transform.localPosition = Vector3.zero;

                // rotate selection outline
                if (result.block.RotationType == Block.BlockRotationType.Flipable && result.rotation >= 4)
                {
                    selectionOutlineChild.transform.localScale = new Vector3(1, -1, 1);
                    selectionOutlineChild.transform.localPosition = Vector3.up;
                }

                if (result.block.RotationType == Block.BlockRotationType.Direction6 && result.rotation == 4)
                {
                    selectionOutlineChild.transform.RotateAround((Vector3Int)result.position + Vector3.one * 0.5f, Vector3.right, -90f);
                }
                else if (result.block.RotationType == Block.BlockRotationType.Direction6 && result.rotation == 5)
                {
                    selectionOutlineChild.transform.RotateAround((Vector3Int)result.position + Vector3.one * 0.5f, Vector3.right, 90f);
                }
                else
                {
                    selectionOutlineChild.transform.RotateAround((Vector3Int)result.position + Vector3.one * 0.5f, Vector3.up, result.rotation * 90f);
                }
            }
            else
            {
                breakTimeTable = null;
            }
        }

        // change place position
        if (result.position + face != _placePosition)
        {
            _placePosition = result.position + face.FloorToInt();
        }

        // reset destroying time
        if (!_isDestroyingBlockThisFrame)
            ResetDestroyingTime();

        _isDestroyingBlockThisFrame = false;

    }

    void ResetDestroyingTime()
    {
        _destroyingTime = 0;
        _breakingMaterial.SetTexture("_BreakingTexture", clearTexture);
    }
}
