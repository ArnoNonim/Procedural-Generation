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

    public class TilemapDemo : MonoBehaviour
    {
        // 로딩 UI 등 외부에서 구독할 이벤트
        public event Action OnGenerationStarted;
        public event Action OnGenerationCompleted;

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

        private NativeArray<float> _elevation;  // width*height
        private NativeArray<float> _thresholds; // tiers.Length
        private JobHandle _jobHandle;
        private bool _isGenerating;

        private Coroutine _applyRoutine;
        private TileBase[][] _managedTilesPerTier; // 밴드 크기(width*rowsPerBand)로 재사용되는 버퍼

        private BoundsInt _bounds;

        private void Start()
        {
            GenerateNewMap();
        }

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

                _applyRoutine = StartCoroutine(ApplyTilesRoutine());
            }
        }

        private void GenerateNewMap()
        {
            // 이전 Job이 실행 중이면 안전하게 완료 후 메모리 해제
            if (_isGenerating)
            {
                _jobHandle.Complete();
                DisposeJobData();
            }

            // 이전 타일 적용 코루틴이 돌고 있으면 중단 (중복 적용 방지)
            if (_applyRoutine != null)
            {
                StopCoroutine(_applyRoutine);
                _applyRoutine = null;
            }

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
            _thresholds = new NativeArray<float>(tiers.Length, Allocator.Persistent);

            for (int i = 0; i < tiers.Length; i++)
            {
                _thresholds[i] = tiers[i].threshold;
            }

            int startX = -width / 2;
            int startY = -height / 2;
            _bounds = new BoundsInt(startX, startY, 0, width, height, 1);

            var job = new ComputeElevationJob
            {
                width = width,
                height = height,
                noiseScale = noiseScale,
                octaves = octaves,
                persistence = persistence,
                lacunarity = lacunarity,
                edgeFalloff = edgeFalloff,
                seed = seed,
                results = _elevation
            };

            _jobHandle = job.Schedule(cellCount, 64);
            _isGenerating = true;

            OnGenerationStarted?.Invoke();
        }

        private IEnumerator ApplyTilesRoutine()
        {
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

            OnGenerationCompleted?.Invoke();
        }

        private void DisposeJobData()
        {
            if (_elevation.IsCreated)
            {
                _elevation.Dispose();
            }

            if (_thresholds.IsCreated)
            {
                _thresholds.Dispose();
            }
        }

        private void OnDestroy()
        {
            if (_isGenerating)
            {
                _jobHandle.Complete();
            }

            if (_applyRoutine != null)
            {
                StopCoroutine(_applyRoutine);
            }

            DisposeJobData();
        }

        [BurstCompile(
            FloatMode = FloatMode.Fast,
            FloatPrecision = FloatPrecision.Low
        )]
        private struct ComputeElevationJob : IJobParallelFor
        {
            public int width;
            public int height;
            public float noiseScale;
            public int octaves;
            public float persistence;
            public float lacunarity;
            public float2 seed;

            [Range(0f, 1f)]
            public float edgeFalloff;

            [WriteOnly]
            public NativeArray<float> results;

            public void Execute(int index)
            {
                int x = index % width;
                int y = index / width;

                float noiseValue = CalculateFbm(new float2(x, y) * noiseScale);

                // 좌표를 -1 ~ 1 범위로 정규화
                float normalizedX = ((x / (float)(width - 1)) * 2f) - 1f;
                float normalizedY = ((y / (float)(height - 1)) * 2f) - 1f;

                // 중심: 0 / 모서리: 1
                float distanceFromCenter = math.length(new float2(normalizedX, normalizedY));

                // 원형 범위로 보정: 중심 1, 바깥쪽 0
                float centerMask = math.saturate(1f - distanceFromCenter);

                // 가장자리에서 노이즈를 감쇠
                noiseValue *= math.lerp(1f, centerMask, edgeFalloff);

                results[index] = noiseValue; // 0~1 연속값 저장, 티어마다 따로 threshold 비교
            }

            private float CalculateFbm(float2 position)
            {
                float total = 0f;
                float frequency = 1f;
                float amplitude = 1f;
                float maxValue = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    // snoise 범위: -1 ~ 1 → 0 ~ 1로 변환
                    float value = noise.snoise((seed + position) * frequency);
                    value = value * 0.5f + 0.5f;

                    total += value * amplitude;
                    maxValue += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                return total / maxValue;
            }
        }
    }
}