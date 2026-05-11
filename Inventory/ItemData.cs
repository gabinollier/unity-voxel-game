using UnityEngine;
using System;
using NaughtyAttributes;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

public abstract class ItemData : ScriptableObject
{
    [field: SerializeField, ReadOnly] public string TechnicalName { get; protected set; }
    [field: Header("Item")]
    [field: SerializeField] public string DisplayName { get; protected set; }
    [field: SerializeField] public ItemRarity Rarity { get; protected set; } = ItemRarity.Common;
    [field: SerializeField] public string Description { get; protected set; }
    [field: SerializeField, Min(1)] public int MaxStackSize { get; protected set; }
    [field: Header("Icon")]
    [field: SerializeField, AllowNesting] GameObject model;
    [field: SerializeField, AllowNesting] protected Vector2 iconPosition = new Vector2(-0.4f, -0.6f);
    [field: SerializeField, AllowNesting] protected Quaternion iconRotation = Quaternion.Euler(-13, -13, 2.15f);
    [field: SerializeField, AllowNesting, Min(0.0001f)] float modelSize = 1f;
    [field: SerializeField, AllowNesting] string saveDirectory = "Textures/Items";
    [field: SerializeField, AllowNesting, ShowAssetPreview(256, 256)] public Sprite Icon { get; protected set; }


    public abstract void RightClickAction(int indexInInventory);
    public abstract void LeftClickAction(int indexInInventory);

#if UNITY_EDITOR
    [Button("Generate Icon from Model")]
    void GenerateIconFromModel()
    {
        if (model == null)
            return;

        GameObject obj = Instantiate(model, iconPosition, iconRotation);
        GenerateIcon();
        DestroyImmediate(obj);
    }

    protected void GenerateIcon()
    {
        RenderTexture renderTexture = new RenderTexture(256, 256, 24);
        Camera camera = new GameObject().AddComponent<Camera>();

        camera.backgroundColor = Color.clear;
        camera.clearFlags = CameraClearFlags.Nothing;
        camera.nearClipPlane = 0.01f;
        camera.orthographic = true;
        camera.targetTexture = renderTexture;
        camera.transform.position = -Vector3.forward * 50f;
        camera.orthographicSize = 0.88f / modelSize;
        camera.Render();

        RenderTexture.active = renderTexture;
        Texture2D texture2D = new Texture2D(256, 256);
        texture2D.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;

        DestroyImmediate(camera.gameObject);
        DestroyImmediate(renderTexture);

        string path = Application.dataPath + '/' + saveDirectory + '/';
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        byte[] bytes = texture2D.EncodeToPNG();
        File.WriteAllBytes(path + TechnicalName + ".png", bytes);

        AssetDatabase.Refresh();

        string filePath = "Assets/" + saveDirectory + '/' + TechnicalName + ".png";
        File.WriteAllText(filePath + ".meta",
        Regex.Replace(File.ReadAllText(filePath + ".meta"), "textureType: 0", "textureType: 8"));
        File.WriteAllText(filePath + ".meta",
        Regex.Replace(File.ReadAllText(filePath + ".meta"), "alphaIsTransparency: 0", "alphaIsTransparency: 1"));
        File.WriteAllText(filePath + ".meta",
        Regex.Replace(File.ReadAllText(filePath + ".meta"), "spriteMode: 0", "spriteMode: 1"));


    

        AssetDatabase.Refresh();

        EditorUtility.SetDirty(this);
        Icon = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);

        Debug.Log("Successfully created icon");

    }
#endif
    void OnValidate()
    {
        TechnicalName = name;
    }
}

public enum ItemRarity
{
    Common,
    Rare,
    Legendary
}


public enum Equipability
{
    None,
    Bag,
    Armor,
    Wings
}