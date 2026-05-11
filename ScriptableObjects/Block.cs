using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using System;
using BinGa.Serialization;
using AYellowpaper.SerializedCollections;

[CreateAssetMenu()]
public class Block : ScriptableObject, ISerializationCallbackReceiver
{
    public enum TerrainMaterial
    {
        Opaque = 0,
        Water = 1,
        Flowers = 2,
        Leaves = 3
    }

    public static int TerrainMaterialCount = Enum.GetNames(typeof(TerrainMaterial)).Length;

    [field: Header("Data"), HorizontalLine]
    [field: SerializeField] public string TechnicalName;
    [field: SerializeField] public float BaseBreakTime { get; private set; } = 2;
    [field: SerializeField] public BlockBreakTimeTable BreakTimeTable { get; private set; }
    [field: SerializeField] public BlockItemData Item { get; private set; }

    // les 3 premier bits sont pour la rotation, les 13 suivants sont l'id du bloc
    // les rotations sont dans cet ordre :
    // Direction4 : sud, ouest, nord, est
    // Direction6 : sud, ouest, nord, est, bottom, top
    // Flipable : sud, ouest, nord, est, sud renversé, ouest renversé, nord renversé, est renversé.
    [field: SerializeField, ReadOnly, Label("IDs (3 bits for the rotation, 13 bits for the ID)")] public ushort[] IDs;


    [field: Header("Hitboxes"), HorizontalLine]

    [SerializeField] Hitbox[] hitboxesWithDefaultRotation;
    public Hitbox[,] Hitboxes { get; private set; } // [rotation, i] avec i l'index de la hitbox
    [SerializeField, ReadOnly, AllowNesting] Hitbox[] _serializedHitboxes;

    [field: SerializeField, ReadOnly] public bool HasHitbox { get; private set; }

    [field: Header("Material"), HorizontalLine]
    [field: SerializeField] public TerrainMaterial Material { get; private set; } = TerrainMaterial.Opaque;

    [field: Header("Meshes"), HorizontalLine]
    [field: SerializeField] public GameObject ItemModel { get; private set; }
    [field: SerializeField] public Mesh DisplayMesh { get; private set; }
    [SerializeField, ReadOnly] bool hasMesh;

    [SerializeField] Mesh selectionMesh;
    public ProcessedMesh[,] ProcessedMeshes { get; private set; } // [rotation,face] représente les triangles collés à la paroie au sud (face = 0), nord (face = 1), top (2), bottom (3), ouest (4), est (5), aucune (6)
    [SerializeField] ProcessedMesh[] _serializedProcessMeshes;

    [field: Header("Adjacent-faces drawing"), HorizontalLine]
    [field: SerializeField] bool forceAdjacentFacesDrawing = false;
    public bool[,] ShouldDrawAdjacentFaces { get; private set; } // [rotation, face] réprésente si, quand une face est collée à ce bloc, avec ce bloc au sud (face = 0), nord (face = 1), top (2), bottom (3), ouest (4), est (5), est ce que la face devrait être draw
    [SerializeField] bool[] _serializedShouldDrawAdjacentFaces;

    [field: Header("Textures"), HorizontalLine]
    [InfoBox("You need to recreate the texture2DArray after any change."), ValidateInput("ValidateTextures", "You need to asign all the textures"), Header("Textures")]
    [SerializeField] public Texture2D[] textures;
    [SerializeField, ReadOnly] 
    int[] textureIndexes;

    [field: Header("Rotation"), HorizontalLine]
    [field: SerializeField] public BlockRotationType RotationType { get; private set; }
    [SerializeField, ReadOnly] int numberOfRotations;

    Mesh _selectionMeshInOneSubmesh;

    public enum BlockRotationType
    {
        None,
        Direction4, // furnaces
        Direction6, // pistons
        Flipable // stairs
    }

    [Button("Apply first texture to all")]
    private void ApplyFirstTextureToAll()
    {
        for (int i = 1; i < textures.Length; i++)
        {
            textures[i] = textures[0];
        }
    }

    [Button("Set Hitboxes to a simple cube")]
    private void SetHitboxsToCube()
    {
        hitboxesWithDefaultRotation = new Hitbox[1];
        hitboxesWithDefaultRotation[0].Origin = Vector3.zero;
        hitboxesWithDefaultRotation[0].Size = Vector3.one;
        ProcessHitboxes();
    }
    bool ValidateTextures()
    {
        return textures != null && !textures.Contains(null);
    }

    [Button("[DEBUG] Call OnValidate()")]
    void OnValidate()
    {
        TechnicalName = this.name;

        numberOfRotations = RotationType == BlockRotationType.None ? 1 :
            RotationType == BlockRotationType.Direction4 ? 4 :
            RotationType == BlockRotationType.Direction6 ? 6 :
            RotationType == BlockRotationType.Flipable ? 8 : 1;

        IDs = new ushort[numberOfRotations];

        for (int i = 0; i < numberOfRotations; i++)
        {
            IDs[i] = (ushort)((i << 13) | (TechnicalName.GetHashCode() & 0b_0001111111111111));
        }

        ProcessMesh();
        ProcessShouldDrawAdjacentFaces();
        ProcessHitboxes(); 
    }

    public bool DoesRayCollide(Vector3 rayStart, Vector3 rayDirectionNormalized, float rayLenghtSquared, Vector3Int blockPosition, ref Vector3 relativeHitPosition, ref Vector3 face)
    {
        if (!HasHitbox)
            return false;

        rayStart -= blockPosition;

        float minDistanceSq = float.MaxValue;

        foreach (Hitbox hitbox in Hitboxes)
        {
            Vector3 intersection;

            // WEST FACE
            if (TryFindIntersection(rayStart, rayDirectionNormalized, hitbox.Origin, Vector3.left, out intersection)
                && DoesPointBelingsToFace(new Vector2(hitbox.Origin.z, hitbox.Origin.y), new Vector2(hitbox.MaxCorner.z, hitbox.MaxCorner.y), new Vector2(intersection.z, intersection.y))
                )
            {
                CheckDistance(intersection, rayStart, Vector3.left, ref relativeHitPosition, ref face);
            }

            // SOUTH FACE
            if (TryFindIntersection(rayStart, rayDirectionNormalized, hitbox.Origin, Vector3.back, out intersection)
                && DoesPointBelingsToFace(new Vector2(hitbox.Origin.x, hitbox.Origin.y), new Vector2(hitbox.MaxCorner.x, hitbox.MaxCorner.y), new Vector2(intersection.x, intersection.y))
                )
            {
                CheckDistance(intersection, rayStart, Vector3.back, ref relativeHitPosition, ref face);
            }

            // BOTTOM FACE
            if (TryFindIntersection(rayStart, rayDirectionNormalized, hitbox.Origin, Vector3.down, out intersection)
                && DoesPointBelingsToFace(new Vector2(hitbox.Origin.x, hitbox.Origin.z), new Vector2(hitbox.MaxCorner.x, hitbox.MaxCorner.z), new Vector2(intersection.x, intersection.z))
                )
            {
                CheckDistance(intersection, rayStart, Vector3.down, ref relativeHitPosition, ref face);
            }
            // EAST FACE
            if (TryFindIntersection(rayStart, rayDirectionNormalized, hitbox.MaxCorner, Vector3.right, out intersection)
                && DoesPointBelingsToFace(new Vector2(hitbox.Origin.z, hitbox.Origin.y), new Vector2(hitbox.MaxCorner.z, hitbox.MaxCorner.y), new Vector2(intersection.z, intersection.y))
                )
            {
                CheckDistance(intersection, rayStart, Vector3.right, ref relativeHitPosition, ref face);
            }

            // NORTH FACE
            if (TryFindIntersection(rayStart, rayDirectionNormalized, hitbox.MaxCorner, Vector3.forward, out intersection)
                && DoesPointBelingsToFace(new Vector2(hitbox.Origin.x, hitbox.Origin.y), new Vector2(hitbox.MaxCorner.x, hitbox.MaxCorner.y), new Vector2(intersection.x, intersection.y))
                )
            {
                CheckDistance(intersection, rayStart, Vector3.forward, ref relativeHitPosition, ref face);
            }

            // TOP FACE
            if (TryFindIntersection(rayStart, rayDirectionNormalized, hitbox.MaxCorner, Vector3.up, out intersection)
                && DoesPointBelingsToFace(new Vector2(hitbox.Origin.x, hitbox.Origin.z), new Vector2(hitbox.MaxCorner.x, hitbox.MaxCorner.z), new Vector2(intersection.x, intersection.z))
                )
            {
                CheckDistance(intersection, rayStart, Vector3.up, ref relativeHitPosition, ref face);
            }
        }

        return minDistanceSq <= rayLenghtSquared;


        bool TryFindIntersection(Vector3 rayStart, Vector3 rayDirection, Vector3 planPoint, Vector3 planNormal, out Vector3 intersection)
        {
            float denominator = rayDirection.x * planNormal.x + rayDirection.y * planNormal.y + rayDirection.z * planNormal.z;
            if (denominator != 0f)
            {
                float t = ((planPoint.x - rayStart.x) * planNormal.x + (planPoint.y - rayStart.y) * planNormal.y + (planPoint.z - rayStart.z) * planNormal.z) / (denominator);
                intersection = new Vector3(rayStart.x + rayDirection.x * t, rayStart.y + rayDirection.y * t, rayStart.z + rayDirection.z * t);
                return true;
            }
            intersection = Vector3.zero;
            return false;
        }
        
        bool DoesPointBelingsToFace(Vector2 faceMinCorner, Vector2 faceMaxCorner, Vector2 point)
        {
            return faceMinCorner.x <= point.x && point.x <= faceMaxCorner.x
                && faceMinCorner.y <= point.y && point.y <= faceMaxCorner.y;
        }

        void CheckDistance(Vector3 intersection, Vector3 rayStart, Vector3 normal, ref Vector3 relativeHitPosition, ref Vector3 face)
        {
            float a = intersection.x - rayStart.x;
            float b = intersection.y - rayStart.y;
            float c = intersection.z - rayStart.z;
            float distanceSq = a * a + b * b + c * c;

            if (distanceSq < minDistanceSq)
            {
                minDistanceSq = distanceSq;
                face = normal;
                relativeHitPosition = intersection;
            }
        }
    }

    public Mesh GetSelectionMeshInOneSubmesh()
    {
        if (selectionMesh == null)
            return new Mesh();

        if (_selectionMeshInOneSubmesh == null)
        {
            _selectionMeshInOneSubmesh = new Mesh();
            _selectionMeshInOneSubmesh.vertices = selectionMesh.vertices;
            _selectionMeshInOneSubmesh.triangles = selectionMesh.triangles;
            _selectionMeshInOneSubmesh.normals = selectionMesh.normals;
            _selectionMeshInOneSubmesh.uv = selectionMesh.uv;
        }

        return _selectionMeshInOneSubmesh;
    }

    // called by the editor window Texture2DArrayCreator
    public void PopulateTextureIndexes(Dictionary<string, int> textureIndexes)
    {
        this.textureIndexes = new int[textures.Length];

        for (int i = 0; i < textures.Length; i++)
        {
            this.textureIndexes[i] = textureIndexes[textures[i].name];
        }

        ProcessMesh();
        ProcessShouldDrawAdjacentFaces();
    }

    void ProcessMesh()
    {
        hasMesh = DisplayMesh != null && DisplayMesh.vertices.Length != 0;

        if (DisplayMesh == null) return;

        if (textures.Length != DisplayMesh.subMeshCount)
        {
            textures = new Texture2D[DisplayMesh.subMeshCount];
        }

        if (textures.Contains(null))
            return;

        ProcessedMeshes = new ProcessedMesh[numberOfRotations, 7];

        for (int rotation = 0; rotation < numberOfRotations; rotation++)
        {
            AddProcessedMeshes(rotation);
        }

        void AddProcessedMeshes(int rotation)
        {
            List<Vector3>[] vertices = new List<Vector3>[7];
            List<Vector3>[] normals = new List<Vector3>[7];
            List<Vector3>[] uvs = new List<Vector3>[7];
            List<int>[] triangles = new List<int>[7];

            for (int i = 0; i < 7; i++)
            {
                vertices[i] = new List<Vector3>();
                normals[i] = new List<Vector3>();
                uvs[i] = new List<Vector3>();
                triangles[i] = new List<int>();
            }

            for (int t = 0; t < DisplayMesh.triangles.Length; t += 3)
            {
                int vertexIndex0 = DisplayMesh.triangles[t];
                int vertexIndex1 = DisplayMesh.triangles[t + 1];
                int vertexIndex2 = DisplayMesh.triangles[t + 2];

                Vector3 vertex0 = DisplayMesh.vertices[vertexIndex0];
                Vector3 vertex1 = DisplayMesh.vertices[vertexIndex1];
                Vector3 vertex2 = DisplayMesh.vertices[vertexIndex2];

                Vector3 normal0 = DisplayMesh.normals[vertexIndex0];
                Vector3 normal1 = DisplayMesh.normals[vertexIndex1];
                Vector3 normal2 = DisplayMesh.normals[vertexIndex2];

                Vector3 uv0 = new Vector3(DisplayMesh.uv[vertexIndex0].x, DisplayMesh.uv[vertexIndex0].y, textureIndexes[GetSubmeshIndex(vertexIndex0)]);
                Vector3 uv1 = new Vector3(DisplayMesh.uv[vertexIndex1].x, DisplayMesh.uv[vertexIndex1].y, textureIndexes[GetSubmeshIndex(vertexIndex1)]);
                Vector3 uv2 = new Vector3(DisplayMesh.uv[vertexIndex2].x, DisplayMesh.uv[vertexIndex2].y, textureIndexes[GetSubmeshIndex(vertexIndex2)]);

                // rotate vertices and normals.
                vertex0 = RotateVertex(vertex0, rotation);
                vertex1 = RotateVertex(vertex1, rotation);
                vertex2 = RotateVertex(vertex2, rotation);
                normal0 = RotateNormal(normal0, rotation);
                normal1 = RotateNormal(normal1, rotation);
                normal2 = RotateNormal(normal2, rotation);

                //if the block is flipped the ordered need to be reversed :
                if (RotationType == BlockRotationType.Flipable && rotation >=4)
                {
                    (vertex0, vertex2) = (vertex2, vertex0);
                    (normal0, normal2) = (normal2, normal0);
                    (uv0, uv2) = (uv2, uv0);
                }

                int GetSubmeshIndex(int vertexIndex)
                {
                    int submeshCount = DisplayMesh.subMeshCount;
                    for (int i = 0; i < submeshCount; i++)
                    {
                        var indices = DisplayMesh.GetIndices(i);
                        if (Array.Exists(indices, element => element == vertexIndex))
                        {
                            return i;
                        }
                    }
                    return 0;
                }

                if (vertex0.z == vertex1.z && vertex0.z == vertex2.z)
                {
                    if (vertex0.z == 0)
                    {
                        // le triangle est collé au sud
                        AddTriangle(0);
                        continue;
                    }
                    if (vertex0.z == 1)
                    {
                        // au nord
                        AddTriangle(1);
                        continue;
                    }
                }

                if (vertex0.y == vertex1.y && vertex0.y == vertex2.y)
                {

                    if (vertex0.y == 1)
                    {
                        // au top
                        AddTriangle(2);
                        continue;
                    }

                    if (vertex0.y == 0)
                    {
                        // le triangle est collé au bottom
                        AddTriangle(3);
                        continue;
                    }
                }

                if (vertex0.x == vertex1.x && vertex0.x == vertex2.x)
                {
                    if (vertex0.x == 0)
                    {
                        // le triangle est collé à l'ouest
                        AddTriangle(4);
                        continue;
                    }
                    if (vertex0.x == 1)
                    {
                        // à l'est
                        AddTriangle(5);
                        continue;
                    }
                }

                // le triangle n'est collé à aucune paroie
                AddTriangle(6);

                void AddTriangle(int processedMeshIndex)
                {
                    AddVertex(vertex0, normal0, uv0);
                    AddVertex(vertex1, normal1, uv1);
                    AddVertex(vertex2, normal2, uv2);

                    void AddVertex(Vector3 vertex, Vector3 normal, Vector3 uv)
                    {
                        int vertexIndex;
                        // if vertex is already in the list and its normal is the same
                        if (vertices[processedMeshIndex].Contains(vertex) && normals[processedMeshIndex][vertices[processedMeshIndex].IndexOf(vertex)] == normal)
                        {
                            // then it's the same vertex, we use it instead of a new one
                            vertexIndex = vertices[processedMeshIndex].IndexOf(vertex);
                        }
                        else
                        {
                            // else we add it
                            vertices[processedMeshIndex].Add(vertex);
                            normals[processedMeshIndex].Add(normal);
                            uvs[processedMeshIndex].Add(uv);
                            vertexIndex = vertices[processedMeshIndex].Count - 1;
                        }

                        triangles[processedMeshIndex].Add(vertexIndex);
                    }
                }
            }

            for (int face = 0; face < 7; face++)
            {
                ProcessedMeshes[rotation, face] = new ProcessedMesh();
                ProcessedMeshes[rotation, face].triangles = triangles[face].ToArray();
                ProcessedMeshes[rotation, face].normals = normals[face].ToArray();
                ProcessedMeshes[rotation, face].uvs = uvs[face].ToArray();
                ProcessedMeshes[rotation, face].vertices = vertices[face].ToArray();
            }
        }

    }

    Vector3 RotateVertex(Vector3 vertex, int rotation)
    {
        if (rotation == 0)
            return vertex;

        if (rotation == 1)
            return new Vector3(vertex.z, vertex.y, 1 - vertex.x);

        if (rotation == 2)
            return new Vector3(1 - vertex.x, vertex.y, 1 - vertex.z);

        if (rotation == 3)
            return new Vector3(1 - vertex.z, vertex.y, vertex.x);

        if (RotationType == BlockRotationType.Direction6)
        {
            if (rotation == 4)
                return new Vector3(vertex.x, vertex.z, 1 - vertex.y);
            if (rotation == 5)
                return new Vector3(vertex.x, 1 - vertex.z, vertex.y);
        }

        if (RotationType == BlockRotationType.Flipable)
        {
            if (rotation == 4)
                return new Vector3(vertex.x, 1 - vertex.y, vertex.z);

            if (rotation == 5)
                return new Vector3(vertex.z, 1 - vertex.y, 1 - vertex.x);

            if (rotation == 6)
                return new Vector3(1 - vertex.x, 1 - vertex.y, 1 - vertex.z);

            if (rotation == 7)
                return new Vector3(1 - vertex.z, 1 - vertex.y, vertex.x);
        }

        return vertex;

    }
    Vector3 RotateNormal(Vector3 normal, int rotation)
    {
        if (rotation == 0)
            return normal;

        if (rotation <= 3)
            return Quaternion.AngleAxis(90 * rotation, Vector3.up) * normal;

        if (RotationType == BlockRotationType.Direction6)
        {
            if (rotation == 5)
                return Quaternion.AngleAxis(90, Vector3.right) * normal;
            if (rotation == 6)
                return Quaternion.AngleAxis(-90, Vector3.right) * normal;
        }

        if (RotationType == BlockRotationType.Flipable)
        {
            return Quaternion.AngleAxis(90 * rotation, Vector3.up) * new Vector3(normal.x, -normal.y, normal.z);
        }

        return normal;
    }

    void ProcessShouldDrawAdjacentFaces()
    {
        ShouldDrawAdjacentFaces = new bool[numberOfRotations, 6];

        if (forceAdjacentFacesDrawing)
        {
            for (int rotation = 0; rotation < numberOfRotations; rotation++)
            {
                for (int face = 0; face < 6; face++)
                {
                    ShouldDrawAdjacentFaces[rotation, face] = true;
                }
            }

            return;
        }

        if (DisplayMesh == null)
            return;

        for (int rotation = 0; rotation < numberOfRotations; rotation++)
        {
            for (int face = 0; face < 6; face++)
            {
                ProcessedMesh mesh = ProcessedMeshes[rotation, face];
                Vector3 normal = CubeData.relativeAdjacentPositions[face];
                Vector3 vertex0, vertex1, vertex2, vertex3;

                switch (face)
                {
                    case 0:
                        vertex0 = new Vector3(1, 0, 0);
                        vertex1 = new Vector3(0, 0, 0);
                        vertex2 = new Vector3(0, 1, 0);
                        vertex3 = new Vector3(1, 1, 0);
                        break;
                    case 1:
                        vertex0 = new Vector3(1, 0, 1);
                        vertex1 = new Vector3(0, 0, 1);
                        vertex2 = new Vector3(0, 1, 1);
                        vertex3 = new Vector3(1, 1, 1);
                        break;
                    case 2:
                        vertex0 = new Vector3(1, 1, 1);
                        vertex1 = new Vector3(0, 1, 1);
                        vertex2 = new Vector3(1, 1, 0);
                        vertex3 = new Vector3(0, 1, 0);
                        break;
                    case 3:
                        vertex0 = new Vector3(1, 0, 1);
                        vertex1 = new Vector3(0, 0, 1);
                        vertex2 = new Vector3(1, 0, 0);
                        vertex3 = new Vector3(0, 0, 0);
                        break;
                    case 4:
                        vertex0 = new Vector3(0, 0, 1);
                        vertex1 = new Vector3(0, 1, 0);
                        vertex2 = new Vector3(0, 0, 0);
                        vertex3 = new Vector3(0, 1, 1);
                        break;
                    case 5:
                        vertex0 = new Vector3(1, 0, 1);
                        vertex1 = new Vector3(1, 1, 0);
                        vertex2 = new Vector3(1, 0, 0);
                        vertex3 = new Vector3(1, 1, 1);
                        break;
                    default:
                        vertex0 = Vector3.zero;
                        vertex1 = Vector3.zero;
                        vertex2 = Vector3.zero;
                        vertex3 = Vector3.zero;
                        break;
                }

                ShouldDrawAdjacentFaces[rotation, face] = !(mesh.normals.Length == 4 &&
                    new[] { mesh.normals[0], mesh.normals[1], mesh.normals[2], mesh.normals[3] }.All(x => x == normal) &&
                    mesh.vertices.Length == 4 &&
                    mesh.vertices.Count((vertex) => (vertex == vertex0)) == 1 &&
                    mesh.vertices.Count((vertex) => (vertex == vertex1)) == 1 &&
                    mesh.vertices.Count((vertex) => (vertex == vertex2)) == 1 &&
                    mesh.vertices.Count((vertex) => (vertex == vertex3)) == 1
                    );
            }

            // on inverse les valeurs parce que l'array sera lue par des bloc au SUD de ce blocs, donc pour lesquels ce bloc est au NORD, par exemple
            (ShouldDrawAdjacentFaces[rotation, 0], ShouldDrawAdjacentFaces[rotation, 1]) = (ShouldDrawAdjacentFaces[rotation, 1], ShouldDrawAdjacentFaces[rotation, 0]);
            (ShouldDrawAdjacentFaces[rotation, 2], ShouldDrawAdjacentFaces[rotation, 3]) = (ShouldDrawAdjacentFaces[rotation, 3], ShouldDrawAdjacentFaces[rotation, 2]);
            (ShouldDrawAdjacentFaces[rotation, 4], ShouldDrawAdjacentFaces[rotation, 5]) = (ShouldDrawAdjacentFaces[rotation, 5], ShouldDrawAdjacentFaces[rotation, 4]);
        }
    }

    void ProcessHitboxes()
    {
        if (hitboxesWithDefaultRotation == null)
        {
            HasHitbox = false;
            return;
        }

        Hitboxes = new Hitbox[numberOfRotations, hitboxesWithDefaultRotation.Length];

        for (int rotation = 0; rotation < numberOfRotations; rotation++)
        {
            for (int hitboxIndex = 0; hitboxIndex < hitboxesWithDefaultRotation.Length; hitboxIndex++)
            {
                Hitboxes[rotation, hitboxIndex] = new Hitbox();

                Vector3 origin = hitboxesWithDefaultRotation[hitboxIndex].Origin;
                Vector3 size = hitboxesWithDefaultRotation[hitboxIndex].Size;

                Vector3 newOrigin = new Vector3();
                Vector3 newMaxCorner = new Vector3();

                if (rotation == 0)
                {
                    newOrigin = origin;
                    newMaxCorner = origin + size;
                }
                if (rotation == 1)
                {
                    newOrigin = new Vector3(origin.x + size.x, origin.y, origin.z);
                    newMaxCorner = new Vector3(origin.x, origin.y + size.y, origin.z + size.z);
                }
                if (rotation == 2)
                {
                    newOrigin = new Vector3(origin.x + size.x, origin.y, origin.z + size.z);
                    newMaxCorner = new Vector3(origin.x, origin.y + size.y, origin.z);
                }
                if (rotation == 3)
                {
                    newOrigin = new Vector3(origin.x, origin.y, origin.z + size.z);
                    newMaxCorner = new Vector3(origin.x + size.x, origin.y + size.y, origin.z);
                }

                if (RotationType == BlockRotationType.Direction6)
                {
                    if (rotation == 4)
                    {
                        newOrigin = new Vector3(origin.x, origin.y + size.y, origin.z);
                        newMaxCorner = new Vector3(origin.x + size.x, origin.y, origin.z + size.z);
                    }
                    if (rotation == 5)
                    {
                        newOrigin = new Vector3(origin.x, origin.y, origin.z + size.z);
                        newMaxCorner = new Vector3(origin.x + size.x, origin.y + size.y, origin.z);
                    }
                }

                if(RotationType == BlockRotationType.Flipable)
                {
                    if (rotation == 4)
                    {
                        newOrigin = new Vector3(origin.x, origin.y + size.y, origin.z);
                        newMaxCorner = new Vector3(origin.x + size.x, origin.y, origin.z + size.z);
                    }
                    if (rotation == 5)
                    {
                        newOrigin = new Vector3(origin.x + size.x, origin.y + size.y, origin.z);
                        newMaxCorner = new Vector3(origin.x, origin.y, origin.z + size.z);
                    }
                    if (rotation == 6)
                    {
                        newOrigin = new Vector3(origin.x + size.x, origin.y + size.y, origin.z + size.z);
                        newMaxCorner = new Vector3(origin.x, origin.y, origin.z);
                    }
                    if (rotation == 7)
                    {
                        newOrigin = new Vector3(origin.x, origin.y + size.y, origin.z + size.z);
                        newMaxCorner = new Vector3(origin.x + size.x, origin.y, origin.z);
                    }
                }

                Hitboxes[rotation, hitboxIndex].Origin = RotateVertex(newOrigin, rotation);
                Hitboxes[rotation, hitboxIndex].MaxCorner = RotateVertex(newMaxCorner, rotation);
                Hitboxes[rotation, hitboxIndex].CalculateVertices();
            }
        }

        HasHitbox = (Hitboxes != null && Hitboxes.Length > 0);
    }

    public int GetRotation(Vector3 cameraForward, bool shouldBlockBeReversed)
    {
        if (RotationType == BlockRotationType.None)
            return 0;


        if (RotationType == BlockRotationType.Direction4)
        {
            Vector2 direction = cameraForward.xz();

            if (Mathf.Abs(direction.x) < Mathf.Abs(direction.y))
            {
                if (direction.y < 0)
                    return 2;
                else
                    return 0;
            }
            else
            {
                if (direction.x < 0)
                    return 3;
                else
                    return 1;
            }
        }

        if (RotationType == BlockRotationType.Direction6)
        {
            // X is the largest
            if (Mathf.Abs(cameraForward.x) > Mathf.Abs(cameraForward.y) && Mathf.Abs(cameraForward.x) > Mathf.Abs(cameraForward.z))
            {
                if (cameraForward.x < 0)
                    return 3;
                else
                    return 1;
            }
            // Y is the largest
            if (Mathf.Abs(cameraForward.y) > Mathf.Abs(cameraForward.x) && Mathf.Abs(cameraForward.y) > Mathf.Abs(cameraForward.z))
            {
                if (cameraForward.y < 0)
                    return 5;
                else
                    return 4;
            }
            // Z is the largest
            if (Mathf.Abs(cameraForward.z) > Mathf.Abs(cameraForward.x) && Mathf.Abs(cameraForward.z) > Mathf.Abs(cameraForward.y))
            {
                if (cameraForward.z < 0)
                    return 2;
                else
                    return 0;
            }

        }

        if (RotationType == BlockRotationType.Flipable)
        {
            Vector2 direction = cameraForward.xz();

            if (Mathf.Abs(direction.x) < Mathf.Abs(direction.y))
            {
                if (direction.y < 0)
                    return shouldBlockBeReversed ? 6 : 2;
                else
                    return shouldBlockBeReversed? 4 : 0;
            }
            else
            {
                if (direction.x < 0)
                    return shouldBlockBeReversed ? 7 : 3;
                else
                    return shouldBlockBeReversed ? 5 : 1;
            }
        }

        return 0;
    }

    #region static methods & properties

    public static ushort AirID { get; private set; }
    public static ushort WaterID { get; private set; }
    public static ushort SchematicVoidID { get; private set; }
    public static float OneBlockSizeInAtlas { get; private set; }

    static Dictionary<ushort, Block> _blocks;
    static Dictionary<string, ushort> _blockIDs;

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        int atlasSizeInBlocks = 16;
        OneBlockSizeInAtlas = 1f / atlasSizeInBlocks;

        AirID = GetBlockIDSlow("air");
        WaterID = GetBlockIDSlow("water");
        SchematicVoidID = GetBlockIDSlow("schematic_void");

        _blocks = new Dictionary<ushort, Block>();
        _blockIDs = new Dictionary<string, ushort>();

        foreach (Block block in Resources.LoadAll<Block>("Blocks"))
        {
            _blocks.Add(block.IDs[0], block);
            _blockIDs.Add(block.TechnicalName, block.IDs[0]);
        }
    }


    [Button("[DEBUG] Call InitializeStaticProperties()")]
    void InitializeStaticProperties()
    {
        Initialize();
    }

    static public (Block block, int rotation) GetBlock(ushort blockID)
    {
        ushort pureID = (ushort)(blockID & 0b_0001111111111111);
        int rotation = GetRotation(blockID);

        if (_blocks.TryGetValue(pureID, out Block block)) return (block, rotation);
        else
        {
            Debug.LogError($"ID {pureID} was not present in the dictionarry (rotation is {rotation})");
            return (null, 0);
        }
    }

    static public ushort GetBlockID(string technicalName)
    {
        if (_blockIDs.TryGetValue(technicalName, out ushort id)) return id;
        else
        {
            Debug.LogError($"Block {technicalName} was not present in the dictionarry");
            return 0;
        }
    }

    static public int GetRotation(ushort id)
    {
        return id >> 13;
    }

    static public ushort GetBlockIDSlow(string technicalName)
    {
        return (ushort)(technicalName.GetHashCode() & 0b_0001111111111111);
    }

    #endregion

    public void OnBeforeSerialize()
    {
        if(hasMesh)
            _serializedProcessMeshes = Serializer.Convert(ProcessedMeshes);
        _serializedShouldDrawAdjacentFaces = Serializer.Convert(ShouldDrawAdjacentFaces);
        if (HasHitbox)
        {
            _serializedHitboxes = Serializer.Convert(Hitboxes);
        }
    }

    public void OnAfterDeserialize()
    {
        if(hasMesh)
            ProcessedMeshes = Serializer.Convert(_serializedProcessMeshes, new Vector2Int(numberOfRotations, 7));
        ShouldDrawAdjacentFaces = Serializer.Convert(_serializedShouldDrawAdjacentFaces, new Vector2Int(numberOfRotations, 6));
        if (HasHitbox)
        {
            Hitboxes = Serializer.Convert(_serializedHitboxes, new Vector2Int(numberOfRotations, (_serializedHitboxes.Length / numberOfRotations)));
        }
    }


    [System.Serializable]
    public class ProcessedMesh
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector3[] uvs;
        public int[] triangles;
    }
}