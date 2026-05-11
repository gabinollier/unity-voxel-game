using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu()]
public class LootTable : ScriptableObject
{
    [SerializeField] private LootTableItem[] lootTableItems;

    [System.Serializable]
    private class LootTableItem
    {
        public ItemData itemData;
        public Probability[] probabilities;
    }

    [System.Serializable]
    private class Probability
    {
        [Range(0, 1)]
        public float CumulativeChance;
        public int amount;
    }

    private void OnValidate()
    {
        foreach (LootTableItem item in lootTableItems)
        {
            for (int i = 1; i < item.probabilities.Length; i++)
            {
                if (item.probabilities[i].CumulativeChance < item.probabilities[i - 1].CumulativeChance)
                {
                    item.probabilities[i].CumulativeChance = item.probabilities[i - 1].CumulativeChance;
                }
            }
        }
    }
    
    public ItemData[] GetRandomItems()
    {
        List<ItemData> itemStacks = new List<ItemData>();

        foreach (LootTableItem item in lootTableItems)
        {
            float value = Random.value;
            foreach (Probability probability in item.probabilities)
            {
                if (value < probability.CumulativeChance)
                {
                    for (int i = 0; i < probability.amount; i++)
                        itemStacks.Add(item.itemData);

                    break;
                }
                
            }
        }

        return itemStacks.ToArray();
    }
}
