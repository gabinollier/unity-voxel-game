using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class Texture2DArrayCreator : EditorWindow
{
    [SerializeField] string _loadPath = "BlockTextures";
    [SerializeField] string _savePath = "Textures";
    [SerializeField] string _saveName = "blocksTexture2DArray";

    [MenuItem("BinGa/Texture2DArray Creator")]
    static void Init()
    {
        Texture2DArrayCreator window = (Texture2DArrayCreator)GetWindow(typeof(Texture2DArrayCreator));
        window.Show();
    }

    private void OnGUI()
    {
        _loadPath = EditorGUILayout.TextField("Load Path in Resources", _loadPath);
        _savePath = EditorGUILayout.TextField("Save Path in Assets", _savePath);
        _saveName = EditorGUILayout.TextField("Save name", _saveName);

        if (GUILayout.Button("Save Texture2DArray"))
        {
            Texture2D[] textures = Resources.LoadAll<Texture2D>(_loadPath);

            if (textures.Length == 0)
            {
                Debug.LogWarning("Load path does not contains textures.");
                return;
            }

            SaveTexture2DArray(textures);

            Debug.Log("Saved Texture2DArray");
        }
    }

    void SaveTexture2DArray(Texture2D[] textures)
    {
        Texture2DArray array = new Texture2DArray(textures[0].width, textures[0].height, textures.Length, textures[0].format, true);
        array.filterMode = textures[0].filterMode;
        Dictionary<string, int> textureIndexes = new Dictionary<string, int>();

        for (int i = 0; i < textures.Length; i++)
        {
            Graphics.CopyTexture(textures[i], 0, array, i);
            textureIndexes[textures[i].name] = i;
        }

        foreach (Block block in Resources.LoadAll<Block>("Blocks"))
        {
            EditorUtility.SetDirty(block);
            block.PopulateTextureIndexes(textureIndexes);
        }

        string assetPath = "Assets/" + _savePath + '/' + Path.GetFileNameWithoutExtension(_saveName) + ".asset";
        Object existingAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (existingAsset != null)
        {
            if (!EditorUtility.DisplayDialog("Save Texture2DArray",
                "Warning : Asset with that name already exists, override it?",
                "Override!", "Cancel!"))
            {
                return;
            }
        }


        if (existingAsset == null)
        {
            AssetDatabase.CreateAsset(array, assetPath);
        }
        else
        {
            array.name = _saveName;
            EditorUtility.CopySerialized(array, existingAsset);
        }
        AssetDatabase.SaveAssets();

    }
}
