using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Linq;

public class Entity : MonoBehaviour
{
    [SerializeField] Hitbox[] hitboxes;
    [SerializeField] float maxStepOffset;
    [SerializeField] bool applyGravity;
    [Space, Header("Debug")]
    [SerializeField] bool drawGizmos;

    [field: ShowNonSerializedField] public Vector3 Velocity { get; private set; } 
    [ReadOnly] public bool IsGrounded;
    ChunkLoader _chunkLoader;
    HashSet<Vector3Int> _projectedBlockPositions;

    private void OnValidate()
    {
        for (int i = 0; i < hitboxes.Length; i++)
        {
            hitboxes[i].CalculateVertices();
        }
    }

    private void Awake()
    {
        _chunkLoader = FindObjectOfType<ChunkLoader>();
        _projectedBlockPositions = new HashSet<Vector3Int>();
    }

    private void FixedUpdate()
    {
        // Modify velocity
        if (applyGravity)
            Velocity += Physics.gravity * Time.fixedDeltaTime;

        // Check for collisions
        Velocity = Velocity.Mul(CheckForCollisionOnAxes(Velocity * Time.fixedDeltaTime));

        // Apply movement
        transform.position += Velocity * Time.fixedDeltaTime;
    }

    /// <summary>
    /// Returns a Vector3 with, for each axis, 0 if the object can't move and 1 if it can.
    /// </summary>
    Vector3 CheckForCollisionOnAxes(Vector3 movement)
    {
        Vector3 axes = Vector3.one;

        // X
        if (movement.x == 0 || CheckForCollision(movement.Mul(Vector3.right), shouldStepOffset: true))
            axes.x = 0;

        // Y
        if (movement.y == 0 || CheckForCollision(movement.Mul(Vector3.up), shouldStepOffset: true))
            axes.y = 0;

        // Z
        if (movement.z == 0 || CheckForCollision(movement.Mul(Vector3.forward), shouldStepOffset: true))
            axes.z = 0;

        // Anti-blocking :
        // (explication : si plusieurs axes du mouvement n'ont pas de collision individuellement, on check si ils ont des collisions ensemble. Si oui, on ne garde que le plus grand)
        if (CheckForCollision(movement.Mul(axes), shouldStepOffset: false))
            axes = movement.x > movement.y ? (movement.x > movement.z ? Vector3.right : Vector3.forward) : (movement.y > movement.z ? Vector3.up : Vector3.forward); 

        // Is Grounded
        IsGrounded = movement.y < 0 && axes.y == 0;

        return axes;

        bool CheckForCollision(Vector3 _movement, bool shouldStepOffset)
        {
            float stepOffset = 0;
            bool collided = false;

            foreach (Hitbox hitbox in hitboxes)
            {
                _projectedBlockPositions.Clear();

                foreach (Vector3 vertex in hitbox.Vertices) 
                {
                    Vector3Int projectedBlockPosition = (transform.position + vertex + _movement).FloorToInt();
                    _projectedBlockPositions.Add(projectedBlockPosition);
                }

                foreach (Vector3Int projectedBlockPosition in _projectedBlockPositions)
                {
                    (Block block, int rotation) = _chunkLoader.GetBlock(projectedBlockPosition);

                    if (!block.HasHitbox)
                        continue;

                    for (int i = 0; i < block.Hitboxes.GetLength(1); i++)
                    {
                        // if collided
                        if (transform.position.x + _movement.x + hitbox.Origin.x <= projectedBlockPosition.x + block.Hitboxes[rotation, i].MaxCorner.x// - security
                            && hitbox.MaxCorner.x + transform.position.x + _movement.x >= block.Hitboxes[rotation, i].Origin.x + projectedBlockPosition.x// + security
                            && transform.position.y + _movement.y + hitbox.Origin.y <= projectedBlockPosition.y + block.Hitboxes[rotation, i].MaxCorner.y// ...
                            && hitbox.MaxCorner.y + transform.position.y + _movement.y >= block.Hitboxes[rotation, i].Origin.y + projectedBlockPosition.y
                            && transform.position.z + _movement.z + hitbox.Origin.z <= projectedBlockPosition.z + block.Hitboxes[rotation, i].MaxCorner.z
                            && hitbox.MaxCorner.z + transform.position.z + _movement.z >= block.Hitboxes[rotation, i].Origin.z + projectedBlockPosition.z)
                        {
                            if (!shouldStepOffset)
                                return true;

                            float desiredStepOffset = projectedBlockPosition.y + block.Hitboxes[rotation, i].MaxCorner.y
                                - (transform.position.y + hitbox.Origin.y);

                            if (desiredStepOffset > maxStepOffset)
                                return true;

                            stepOffset = Mathf.Max(stepOffset, desiredStepOffset);
                            collided = true;
                        }
                    }

                }
            }

            if (shouldStepOffset && IsGrounded && stepOffset != 0 && !CheckForCollision(_movement + Vector3.up * stepOffset * 1.1f, shouldStepOffset:false))
            {
                transform.position += Vector3.up * stepOffset * 1.1f;
                return false;
            }

            return collided;
        }

    }

    public void AddVelocity(Vector3 velocityToAdd)
    {
        Velocity += velocityToAdd;
    }
    public void SetVelocity(Vector3 velocity)
    {
        Velocity = velocity;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.green;

        foreach (Hitbox box in hitboxes)
        {
            Gizmos.DrawWireCube(transform.position + box.Origin + box.Size / 2, box.Size);
        }
    }

    public bool DoesCollideWithBlock(Block block, int blockRotation, Vector3Int blockPosition)
    {
        if (!block.HasHitbox)
            return false;

        for (int i = 0; i < block.Hitboxes.GetLength(1); i++)
        {
            Hitbox blockHitbox = block.Hitboxes[blockRotation, i];

            foreach (Hitbox entityHitbox in hitboxes)
            {
                // if (collided)
                if (transform.position.x + entityHitbox.Origin.x <= blockPosition.x + blockHitbox.MaxCorner.x
                    && transform.position.x + entityHitbox.MaxCorner.x >= blockPosition.x + blockHitbox.Origin.x
                    && transform.position.y + entityHitbox.Origin.y <= blockPosition.y + blockHitbox.MaxCorner.y
                    && transform.position.y + entityHitbox.MaxCorner.y >= blockPosition.y + blockHitbox.Origin.y
                    && transform.position.z + entityHitbox.Origin.z <= blockPosition.z + blockHitbox.MaxCorner.z
                    && transform.position.z + entityHitbox.MaxCorner.z >= blockPosition.z + blockHitbox.Origin.z)
                    return true;
            }
        }

        return false;
    }
}

[System.Serializable]
public struct Hitbox
{
    public Vector3 Origin;
    public Vector3 Size;

    [field: SerializeField, AllowNesting, ReadOnly] public Vector3[] Vertices { get; private set; }
    public Vector3 MaxCorner
    {
        get
        {
            return Origin + Size;
        }
        set
        {
            Size = value - Origin;
        }
    }

    public void CalculateVertices()
    {
        HashSet<Vector3> vertices = new HashSet<Vector3>();

        for (float iX = Origin.x; iX <= MaxCorner.x + 1; iX++)
        {
            float x = iX > MaxCorner.x ? MaxCorner.x : iX;
            for (float iY = Origin.y; iY <= MaxCorner.y + 1; iY++)
            {
                float y = iY > MaxCorner.y ? MaxCorner.y : iY;
                for (float iZ = Origin.z; iZ <= MaxCorner.z + 1; iZ++)
                {
                    float z = iZ > MaxCorner.z ? MaxCorner.z : iZ;
                    vertices.Add(new Vector3(x, y, z));
                }
            }
        }

        Vertices = vertices.ToArray();
    }
}