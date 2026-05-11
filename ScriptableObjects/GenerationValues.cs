using UnityEngine;
using NaughtyAttributes;
using BinGa.Threading;

[CreateAssetMenu()]
public class GenerationValues : ScriptableObject
{
    public int seed;

    [BoxGroup("Noises")]
    public OctavedNoise continentalnessNoise, erosionNoise, pvNoise, temperatureNoise, humidityNoise;
    [BoxGroup("Noises curves")]
    public ThreadSafeAnimationCurve continentalnessCurve, erosionCurve, pvCurve;
    [BoxGroup("Biomes")]
    public Biome[] biomes;
    [BoxGroup("Structures")]
    public Structure[] forests;

    public int[,,] biomesIndexes = new int[3, 6, 2] // continentalness, temperature, humidity
    {
        {
            // continentalness 0

            {0,0}, // big_icebergs
            {1,1}, // small_icebergs
            {2,2}, // cold_ocean
            {3,3}, // warm_ocean
            {4,4}, // tropical_ocean
            {5,5} // hot_ocean
        },
        {
            // continentalness 1

            {6,6}, // big_floe
            {7,7}, // small_floe
            {2,2}, // cold ocean
            {3,3}, // warm_ocean
            {4,4}, // tropical_ocean
            {5,5} // hot_ocean
        },
        {
            // continentalness 2

            {8,9}, // snow_desert, ice_desert
            {10,11}, // tundra, taiga
            {12,13}, // plains, spruce_forest
            {12,14}, // plains, oak_forest
            {15,16}, // savana_plains, savana_forest
            {17,18} // rocky_desert, sand_desert
        }
    };

    private void OnValidate()
    {
        Initialize();
    }
    public void Initialize()
    {
        // Initialize noises
        var prng = new System.Random(seed);

        continentalnessNoise.Initialize(prng);
        erosionNoise.Initialize(prng);
        pvNoise.Initialize(prng);
        temperatureNoise.Initialize(prng);
        humidityNoise.Initialize(prng);
        

        // Initialize curves
        continentalnessCurve.Initialize(4096);
        erosionCurve.Initialize(4096);
        pvCurve.Initialize(4096);

        // Initialize structures
        foreach (Structure forest in forests)
        {
            forest.Seed = prng.Next(1000);
        }
    }

    [System.Serializable]
    public class OctavedNoise
    {
        [Min(1)] public float globalScale = 1;
        public float globalAmplitude = 1;
        [Range(0, 1)] public float amplitudeFactorPerOctave = .5f;
        [Min(1)] public float frequencyFactorPerOctave = 2f;
        public Octave[] octaves;
        [ReadOnly, AllowNesting] public float finalFactor;

        public void Initialize(System.Random prng)
        {
            float octaveFrequency = 1f / globalScale;
            float octaveAmplitude = 1;
            float maxAmplitude = Mathf.Epsilon;
            for (int i = 0; i < octaves.Length; i++)
            {
                octaves[i].offset = new Vector2(prng.Next(10000, 20000), prng.Next(10000, 20000));
                octaves[i].octaveFrequency = octaveFrequency;
                octaves[i].octaveAmplitude = octaveAmplitude;
                octaveFrequency *= frequencyFactorPerOctave;
                octaveAmplitude *= amplitudeFactorPerOctave;
                maxAmplitude += octaves[i].octaveAmplitude;
            }
            finalFactor = globalAmplitude / maxAmplitude;
        }

        public float GetValue(Vector2Int position) // ~ 0..1
        {
            float value = 0f;
            for (int i = 0; i < octaves.Length; i++)
            {
                Octave octave = octaves[i];
                float x = position.x * octave.octaveFrequency * 0.95f + octave.offset.x;
                float y = position.y * octave.octaveFrequency * 0.95f + octave.offset.y; 

                value += octave.octaveAmplitude *
                    Mathf.PerlinNoise(x, y); // this can be optimized!
            }
            return finalFactor * value;
        }
    }

    [System.Serializable]
    public class Octave
    {
        [AllowNesting, ReadOnly] public Vector2 offset;
        [AllowNesting, ReadOnly, Range(0.00001f, 1)] public float octaveAmplitude;
        [AllowNesting, ReadOnly, Range(0.00001f, 1)] public float octaveFrequency;
    }

    [System.Serializable]
    public class Lode
    {
        public string blockTechnicalName;
        public int minHeight;
        public int maxHeight;
        public int transitionLenght;
        [Range(0.00001f, 1)] public float noiseScale;
        [Range(0.00001f, 1)] public float noiseThreshold;
        [AllowNesting, ReadOnly] public Vector3 offset;
    }
}
