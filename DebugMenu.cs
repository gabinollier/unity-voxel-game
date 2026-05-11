using UnityEngine;
using Lean.Gui;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.InputSystem;

public class DebugMenu : MonoBehaviour
{
    [SerializeField] Key openKey = Key.F3;
    [Space]

    public bool IsMenuOpened;
    public int ChunksToUpdateCount;
    public int ChunksToBuildMeshCount;
    public int ChunksCount;
    public int ChunksToGenerateInQueue;
    public int AdjustedChunkInstanciationPerFrame;
    public void SetChunksGeneratedLastFrame(float chunksGeneratedLastFrame)
    {
        _cpss.Add(chunksGeneratedLastFrame / Time.unscaledDeltaTime);
    }

    [SerializeField] TMPro.TextMeshProUGUI text;
    [SerializeField] Transform cam;
    List<float> _fpss = new List<float>();
    LeanToggle _leanToggle;
    int _fps = 0;
    float _fpsTimer = 0;
    int _fpsMin;
    private float _cpsTimer;
    private List<float> _cpss = new List<float>();
    private float _cps ;
    private float _cpsMin;


    void Awake()
    {
        _leanToggle = GetComponent<LeanToggle>();
    }

    void Update()
    {
        if (Keyboard.current[openKey].wasPressedThisFrame)
        {
            _leanToggle.Toggle();
            IsMenuOpened = _leanToggle.On;
        }


        if (!IsMenuOpened) return;

        // values
        Vector2Int chunkPosition = (cam.position.xz() / Chunk.CHUNK_SIZE).FloorToInt();

        // string builder
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine(" -    PGR DEBUG MENU    - ");

        sb.AppendLine("\nPlayer :");
        sb.AppendLine(cam.position.FloorToInt().ToString());
        sb.Append("<color=#BBBBBB>");
        sb.AppendLine(cam.position.ToString());
        sb.Append("<color=white>");

        sb.AppendLine("\nPlayer chunk :");
        sb.AppendLine(chunkPosition.ToString());

        sb.AppendLine("\nPlayer position in chunk :");
        sb.AppendLine((cam.position.xz().FloorToInt() - new Vector2Int(chunkPosition.x * Chunk.CHUNK_SIZE, chunkPosition.y * Chunk.CHUNK_SIZE)).ToString());

        sb.AppendLine("\nFPS :");
        _fpss.Add(1 / Time.unscaledDeltaTime);
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer > 1f)
        {
            (_fps, _fpsMin) = GetMean(_fpss);
            _fpsTimer = 0f;
        }
        sb.Append(_fps + "<color=#BBBBBB> (min : "+  _fpsMin + ")<color=white>");

        Vector2Int position2D = new Vector2Int((int)cam.position.x, (int)cam.position.z);

        sb.AppendLine("\nTerrain Generation :");
        float continentalness = GenerationAlgorithm.GetContinentalness(position2D);
        float erosion = GenerationAlgorithm.GetErosion(position2D); ;
        float humidity = GenerationAlgorithm.GetHumidity(position2D);
        float temperature = GenerationAlgorithm.GetTemperature(position2D);
        float continentalnessIndex = GenerationAlgorithm.GetContinentalnessIndex(continentalness);
        float humidityIndex = GenerationAlgorithm.GetHumidityIndex(humidity);
        float temperatureIndex = GenerationAlgorithm.GetTemperatureIndex(temperature);
        sb.AppendLine($"C : {continentalness} | E : {erosion} | H : {humidity} | T : {temperature}");
        sb.AppendLine($"Ci : {continentalnessIndex} |  Hi : {humidityIndex} | Ti : {temperatureIndex}");
        sb.AppendLine($"Biome : {GenerationAlgorithm.GetBiome(continentalness, temperature, humidity)}");

        ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
        sb.AppendLine("\nThreading :");
        sb.AppendLine($"Available threads in the pool : W {workerThreads} | CP {completionPortThreads}");
        sb.AppendLine($"Chunks to generate in queue : {ChunksToGenerateInQueue}");
        sb.AppendLine($"Count of Chunks To Update : {ChunksToUpdateCount}");
        sb.AppendLine($"Count of Chunks To Build Mesh : {ChunksToBuildMeshCount}");
        sb.AppendLine($"Count of Chunks : {ChunksCount})");
        _cpsTimer += Time.unscaledDeltaTime;
        if (_cpsTimer > 1f)
        {
            (_cps, _cpsMin) = GetMean(_cpss);
            _cpsTimer = 0f;
        }
        sb.AppendLine($"Chunks generated per second : {_cps} | min : {_cpsMin}");
        sb.AppendLine($"AdjustedChunkInstanciationPerFrame : {AdjustedChunkInstanciationPerFrame}");


        text.text = sb.ToString();
    }

    (int mean, int min) GetMean(List<float> array)
    {
        float min = float.MaxValue;
        float mean = 0;
        for (int i = 0; i < array.Count; i++)
        {
            mean += array[i];
            if (array[i] < min) 
                min = array[i];
        }
        mean /= array.Count;

        array.Clear();
        return ((int)mean, (int)min);
    }
}
