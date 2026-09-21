using UnityEngine;

namespace _00_Main._01_Scripts.Tile.UI
{
    /// <summary>맵 생성 이벤트를 로딩 UI 뷰에 전달한다.</summary>
    public sealed class GenerationLoadingManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TilemapGenerator mapGenerator;
        [SerializeField] private GenerationLoadingOverlay overlay;
        [SerializeField] private GenerationLoadingProgressView progressView;
        [SerializeField] private GenerationLoadingTextEffectView textEffectView;

        private void Awake()
        {
            progressView?.SetProgress(0f);
        }

        private void OnEnable()
        {
            if (mapGenerator == null)
            {
                Debug.LogWarning($"{nameof(GenerationLoadingManager)}: TilemapGenerator 참조 없음.", this);
                return;
            }

            mapGenerator.OnGenerationStarted += HandleGenerationStarted;
            mapGenerator.OnGenerationProgressChanged += HandleGenerationProgressChanged;
            mapGenerator.OnGenerationCompleted += HandleGenerationCompleted;
        }

        private void OnDisable()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnGenerationStarted -= HandleGenerationStarted;
                mapGenerator.OnGenerationProgressChanged -= HandleGenerationProgressChanged;
                mapGenerator.OnGenerationCompleted -= HandleGenerationCompleted;
            }

        }

        private void HandleGenerationStarted()
        {
            progressView?.SetProgress(0f);
            overlay?.Show();
            textEffectView?.Play();
        }

        private void HandleGenerationProgressChanged(float progress) => progressView?.SetProgress(progress);

        private void HandleGenerationCompleted()
        {
            progressView?.SetProgress(1f);
            overlay?.Hide();

            textEffectView?.StopEffects();
        }
    }
}
