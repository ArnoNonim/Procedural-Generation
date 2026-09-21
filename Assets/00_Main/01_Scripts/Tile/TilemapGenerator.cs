using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00_Main._01_Scripts.Tile
{
    [Serializable]
    public struct TierLayer
    {
        public string tierName;
        public Tilemap tilemap;
        public TileBase tile;
        [Range(0f, 1f)] public float threshold;
    }

    /// <summary>생성 요청·작업 수명주기·진행 이벤트를 조정한다.</summary>
    public class TilemapGenerator : MonoBehaviour
    {
        private const float ApplyTilesStartProgress = 0.15f;

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

        private readonly TilemapBandApplier _bandApplier = new();
        private NativeArray<float> _elevation;
        private JobHandle _jobHandle;
        private bool _isGenerating;
        private Coroutine _applyRoutine;

        private void Update()
        {
            if (TilemapGenerationInput.IsRequestedThisFrame)
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

        private void GenerateNewMap()
        {
            if (_isGenerating)
            {
                return;
            }

            StopActiveGeneration();
            if (randomSeed)
            {
                seed = UnityEngine.Random.Range(1, 100000);
            }

            _elevation = new NativeArray<float>(width * height, Allocator.Persistent);
            var job = new TilemapElevationJob
            {
                Width = width,
                Height = height,
                NoiseScale = noiseScale,
                Octaves = octaves,
                Persistence = persistence,
                Lacunarity = lacunarity,
                EdgeFalloff = edgeFalloff,
                Seed = new float2(seed, seed),
                Results = _elevation
            };

            _jobHandle = job.Schedule(_elevation.Length, 64);
            _isGenerating = true;
            SetGenerationProgress(0f);
            OnGenerationStarted?.Invoke();
        }

        private System.Collections.IEnumerator ApplyTilesRoutine()
        {
            yield return _bandApplier.Apply(
                tiers,
                _elevation,
                width,
                height,
                rowsPerBand,
                frameBudgetMs,
                ApplyTilesStartProgress,
                SetGenerationProgress);

            DisposeJobData();
            _applyRoutine = null;
            SetGenerationProgress(1f);
            OnGenerationCompleted?.Invoke();
        }

        private void SetGenerationProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (Mathf.Approximately(GenerationProgress, progress))
            {
                return;
            }

            GenerationProgress = progress;
            OnGenerationProgressChanged?.Invoke(progress);
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

            DisposeJobData();
        }

        private void DisposeJobData()
        {
            if (_elevation.IsCreated)
            {
                _elevation.Dispose();
            }
        }

        private void OnDestroy() => StopActiveGeneration();

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            rowsPerBand = Mathf.Max(1, rowsPerBand);
            frameBudgetMs = Math.Max(0.1, frameBudgetMs);
        }
    }
}
