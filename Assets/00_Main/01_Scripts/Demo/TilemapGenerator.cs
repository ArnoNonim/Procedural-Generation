using System;
using System.Collections;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace _00_Main._01_Scripts.Demo
{
    [Serializable]
    public struct TierLayer
    {
        public string tierName;   // 인스펙터 구분용
        public Tilemap tilemap;   // 이 티어 전용 Tilemap
        public TileBase tile;     // 이 티어에서 쓸 단일 타일
        [Range(0f, 1f)] public float threshold; // 이 값 이상이면 이 티어로 채워짐
    }

    public class TilemapGenerator : MonoBehaviour
    {
        public event Action OnGenerationStarted;
        public event Action OnGenerationCompleted;
        
        public event Action<float> OnGenerationProgressChanged;
        public float GenerationProgress { get; private set; }


        [Header("Tiers (threshold)")]
        [SerializeField] private TierLayer[] tiers;

        [SerializeField] private int width = 20;
        [SerializeField] private int height = 20;

        [Header("Edge Falloff")]
        [SerializeField, Range(0f, 1f)] private float edgeFalloff = 0.65f;

        [Header("FBM Noise Settings")]
        [SerializeField] private float noiseScale = 0.1f;
        [SerializeField] private int octaves = 4;
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2f;
        [SerializeField] private int seed;
        [SerializeField] private bool randomSeed;

        [Header("Loading Performance")]
        [SerializeField] private int rowsPerBand = 16;
        [SerializeField] private double frameBudgetMs = 8.0;

        private const float ApplyTilesStartProgress = 0.15f;

        private NativeArray<float> _elevation;  // width*height
        private JobHandle _jobHandle;
        private bool _isGenerating;

        private Coroutine _applyRoutine;
        private TileBase[][] _managedTilesPerTier; // 밴드 크기(width*rowsPerBand)로 재사용되는 버퍼

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                GenerateNewMap();
            }

            if (_isGenerating && _jobHandle.IsCompleted)
            {
                _jobHandle.Complete();
                _isGenerating = false;

                SetGenerationProgress(ApplyTilesStartProgress);
                _applyRoutine = StartCoroutine(ApplyTilesRoutine());
            }
        }
        
        private void SetGenerationProgress(float value)
        {
            value = Mathf.Clamp01(value);

            if (Mathf.Approximately(GenerationProgress, value))
                return;

            GenerationProgress = value;
            OnGenerationProgressChanged?.Invoke(value);
        }

        private void GenerateNewMap()
        {
            StopActiveGeneration();

            if (randomSeed)
            {
                seed = UnityEngine.Random.Range(1, 100000);
            }

            int cellCount = width * height;
            int bandCellCount = width * rowsPerBand;

            // 밴드 크기 버퍼만 재사용 (전체 크기 배열 안 만듦 → GC/메모리 절약)
            if (_managedTilesPerTier == null || _managedTilesPerTier.Length != tiers.Length
                || (_managedTilesPerTier.Length > 0 && _managedTilesPerTier[0].Length != bandCellCount))
            {
                _managedTilesPerTier = new TileBase[tiers.Length][];
                for (int i = 0; i < tiers.Length; i++)
                {
                    _managedTilesPerTier[i] = new TileBase[bandCellCount];
                }
            }

            _elevation = new NativeArray<float>(cellCount, Allocator.Persistent);

            var job = new ComputeElevationJob
            {
                Width = width,
                Height = height,
                NoiseScale = noiseScale,
                Octaves = octaves,
                Persistence = persistence,
                Lacunarity = lacunarity,
                EdgeFalloff = edgeFalloff,
                Seed = seed,
                Results = _elevation
            };

            _jobHandle = job.Schedule(cellCount, 64);
            _isGenerating = true;

            SetGenerationProgress(0f);
            OnGenerationStarted?.Invoke();
        }

        private IEnumerator ApplyTilesRoutine()
        {
            int bandsPerTier = Mathf.CeilToInt(height / (float)rowsPerBand);
            int totalBands = tiers.Length * bandsPerTier;
            int completedBands = 0;
            
            int startX = -width / 2;
            int startY = -height / 2;

            var stopwatch = Stopwatch.StartNew();

            for (int t = 0; t < tiers.Length; t++)
            {
                float threshold = tiers[t].threshold;
                TileBase tile = tiers[t].tile;

                tiers[t].tilemap.ClearAllTiles();

                for (int bandStartY = 0; bandStartY < height; bandStartY += rowsPerBand)
                {
                    int bandHeight = Mathf.Min(rowsPerBand, height - bandStartY);
                    int bandCellCount = width * bandHeight;
                    TileBase[] bandTiles = _managedTilesPerTier[t];

                    for (int i = 0; i < bandCellCount; i++)
                    {
                        int localX = i % width;
                        int localY = i / width;
                        int globalIndex = (bandStartY + localY) * width + localX;
                        bandTiles[i] = _elevation[globalIndex] >= threshold ? tile : null;
                    }

                    var bandBounds = new BoundsInt(startX, startY + bandStartY, 0, width, bandHeight, 1);

                    if (bandCellCount == bandTiles.Length)
                    {
                        tiers[t].tilemap.SetTilesBlock(bandBounds, bandTiles);
                    }
                    else
                    {
                        var trimmed = new TileBase[bandCellCount];
                        Array.Copy(bandTiles, trimmed, bandCellCount);
                        tiers[t].tilemap.SetTilesBlock(bandBounds, trimmed);
                    }
                    
                    completedBands++;

                    float applyProgress = completedBands / (float)totalBands;
                    SetGenerationProgress(Mathf.Lerp(ApplyTilesStartProgress, 1f, applyProgress));

                    // 고정 프레임 양보 대신, 예산 초과했을 때만 양보
                    if (stopwatch.Elapsed.TotalMilliseconds >= frameBudgetMs)
                    {
                        yield return null;
                        stopwatch.Restart();
                    }
                }
            }

            DisposeJobData();
            _applyRoutine = null;

            SetGenerationProgress(1f);
            OnGenerationCompleted?.Invoke();
        }

        private void StopActiveGeneration()
        {
            if (_isGenerating)
            {
                _jobHandle.Complete();
                _isGenerating = false;
            }

            if (_applyRoutine != null)
            {
                StopCoroutine(_applyRoutine);
                _applyRoutine = null;
            }

            // 코루틴을 중단하면 마지막의 DisposeJobData가 실행되지 않으므로 여기서 항상 정리함.
            DisposeJobData();
        }

        private void DisposeJobData()
        {
            if (_elevation.IsCreated)
            {
                _elevation.Dispose();
            }

        }

        private void OnDestroy()
        {
            StopActiveGeneration();
        }

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            rowsPerBand = Mathf.Max(1, rowsPerBand);
            frameBudgetMs = Math.Max(0.1, frameBudgetMs);
        }

        [BurstCompile(
            FloatMode = FloatMode.Fast,
            FloatPrecision = FloatPrecision.Low
        )]
        private struct ComputeElevationJob : IJobParallelFor
        {
            public int Width;
            public int Height;
            public float NoiseScale;
            public int Octaves;
            public float Persistence;
            public float Lacunarity;
            public float2 Seed;

            [Range(0f, 1f)]
            public float EdgeFalloff;

            [WriteOnly]
            public NativeArray<float> Results;

            public void Execute(int index)
            {
                int x = index % Width;
                int y = index / Width;

                float noiseValue = CalculateFbm(new float2(x, y) * NoiseScale);

                // 좌표를 -1 ~ 1 범위로 정규화
                float normalizedX = Width > 1 ? ((x / (float)(Width - 1)) * 2f) - 1f : 0f;
                float normalizedY = Height > 1 ? ((y / (float)(Height - 1)) * 2f) - 1f : 0f;

                // 중심: 0 / 모서리: 1
                float distanceFromCenter = math.length(new float2(normalizedX, normalizedY));

                // 원형 범위로 보정: 중심 1, 바깥쪽 0
                float centerMask = math.saturate(1f - distanceFromCenter);

                // 가장자리에서 노이즈를 감쇠
                noiseValue *= math.lerp(1f, centerMask, EdgeFalloff);

                Results[index] = noiseValue; // 0~1 연속값 저장, 티어마다 따로 threshold 비교
            }

            private float CalculateFbm(float2 position)
            {
                float total = 0f;
                float frequency = 1f;
                float amplitude = 1f;
                float maxValue = 0f;

                for (int i = 0; i < Octaves; i++)
                {
                    // noise 범위: -1 ~ 1 → 0 ~ 1로 변환
                    float value = noise.snoise((Seed + position) * frequency);
                    value = value * 0.5f + 0.5f;

                    total += value * amplitude;
                    maxValue += amplitude;

                    amplitude *= Persistence;
                    frequency *= Lacunarity;
                }

                return total / maxValue;
            }
        }
    }
}
