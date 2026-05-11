using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using System;

[CreateAssetMenu]
public class BlockBreakTimeTable : ScriptableObject
{
    [SerializeField] SerializedDictionary<ToolCategory, float> table = new SerializedDictionary<ToolCategory, float>();

    public SerializedDictionary<ToolCategory, float> Table => table;

    private void OnValidate()
    {
        foreach (ToolCategory toolCategory in Enum.GetValues(typeof(ToolCategory)))
        {
            if (!table.ContainsKey(toolCategory))
            {
                table.Add(toolCategory, 1);
            }
        }
    }
}
