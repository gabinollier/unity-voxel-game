using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu()]
public class ItemDataList : ScriptableObject
{
    [field: SerializeField] public List<ItemData> ItemDatas { get; private set; }

#if UNITY_EDITOR

    [NaughtyAttributes.Button("Refresh", NaughtyAttributes.EButtonEnableMode.Editor)]
    private void OnValidate()
    {
        ItemDatas = new List<ItemData>();

        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        ;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDatas.Add(AssetDatabase.LoadAssetAtPath<ItemData>(path));
        }
    }
#endif
} 
