using UnityEngine;

public class MapVisualizer : MonoBehaviour
{
	enum Visualization
    {
		FullTerrain,
		Continentalness,
		Erosion,
		pv,
		Humidity,
		Temperature,
		ContinentalnessIndex,
		ErosionIndex,
		HumidityIndex,
		TemperatureIndex,
		Biome
    }

	[SerializeField] MeshRenderer meshRenderer;
    [Space]
	[SerializeField] GenerationValues values;
	[SerializeField] bool refreshInUpdate;
	[SerializeField] bool showWater;
	[SerializeField] int size;
	[SerializeField] int blockPerPixel;
	[SerializeField] Visualization visualization;


    [NaughtyAttributes.Button("SetSizeToEntireWorld (be carefull)")]
	void SetSizeToEntireWorld()
    {
		size = GenerationAlgorithm.HALF_WORLD_MERIDIAN * 2;
    }

	void Update()
    {
		if (refreshInUpdate) Refresh();
	}

    [NaughtyAttributes.Button("Refresh", NaughtyAttributes.EButtonEnableMode.Playmode)]
	void Refresh()
    {
		int textureSize = size / blockPerPixel;

		Texture2D texture = new Texture2D(textureSize, textureSize);

		Color[] colourMap = new Color[(textureSize) * (textureSize)];
		for (int y = 0; y < textureSize; y ++)
		{
			for (int x = 0; x < textureSize; x ++)
			{
				Vector2Int position = new Vector2Int(size / 2 - x * blockPerPixel, size / 2 - y * blockPerPixel);

				float value = 1f;
				float continentalness = GenerationAlgorithm.GetContinentalness(position);
				float erosion = GenerationAlgorithm.GetErosion(position);
				float pv = GenerationAlgorithm.GetPV(position);
				float humidity = GenerationAlgorithm.GetHumidity(position);
				float temperature = GenerationAlgorithm.GetTemperature(position);

				switch (visualization)
                {
                    case Visualization.Continentalness:
						value = continentalness;
						break;
                    case Visualization.Erosion:
						value = erosion;
						break;
					case Visualization.Humidity:
						value = humidity;
						break;
					case Visualization.Temperature:
						value = temperature;
						break;
					case Visualization.pv:
						value = pv;
						break;
					case Visualization.FullTerrain:
						value = GenerationAlgorithm.GetTerrainHeightValue(continentalness, erosion, pv);
						break;
					case Visualization.TemperatureIndex:
						value = GenerationAlgorithm.GetTemperatureIndex(temperature) / 5f;
						break;
					case Visualization.ContinentalnessIndex:
						value = GenerationAlgorithm.GetContinentalnessIndex(continentalness) / 2f;
						break;
					case Visualization.HumidityIndex:
						value = GenerationAlgorithm.GetHumidityIndex(humidity) / 1f;
						break;
				}

				Color color = Color.Lerp(Color.black, Color.white, value);
				if (showWater && value <= 0.25f) color *= Color.blue;
				if (visualization == Visualization.Biome) color = GenerationAlgorithm.GetBiome(continentalness, temperature, humidity).debugColor;
				if (value > 1f) color *= Color.red;

				colourMap[y * textureSize + x] = color;
            }
        }

        texture.SetPixels(colourMap);
		texture.Apply();
		texture.filterMode = FilterMode.Point;

		meshRenderer.material.mainTexture = texture;
		meshRenderer.transform.localScale = new Vector3(size / 10, 1, size / 10);
        //meshRenderer.transform.position = new Vector3(0.5f * size, 0, 0.5f * size);
    }
}
