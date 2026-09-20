using UnityEngine;

namespace _00_Main._01_Scripts.Demo
{
    /// <summary>
    /// 생성 이벤트와 표시 전용 UI 컴포넌트를 연결하는 컨트롤러.
    /// </summary>
    public class GenerationLoadingManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TilemapGenerator mapGenerator;
        [SerializeField] private GenerationLoadingOverlay overlay;
        [SerializeField] private GenerationProgressSlider progressSlider;

        [Header("Fade")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.4f;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            overlay?.InitializeHidden();
            progressSlider?.SetProgress(0f);
        }

        private void OnEnable()
        {
            if (mapGenerator == null)
            {
                Debug.LogWarning($"{nameof(GenerationLoadingManager)}: mapGenerator 참조 없음.", this);
                return;
            }

            mapGenerator.OnGenerationStarted += HandleGenerationStarted;
            mapGenerator.OnGenerationProgressChanged += HandleGenerationProgressChanged;
            mapGenerator.OnGenerationCompleted += HandleGenerationCompleted;
        }

        private void OnDisable()
        {
            if (mapGenerator == null) return;

            mapGenerator.OnGenerationStarted -= HandleGenerationStarted;
            mapGenerator.OnGenerationProgressChanged -= HandleGenerationProgressChanged;
            mapGenerator.OnGenerationCompleted -= HandleGenerationCompleted;
        }

        private void HandleGenerationStarted()
        {
            progressSlider?.SetProgress(0f);
            overlay?.Show(fadeInDuration);
        }

        private void HandleGenerationProgressChanged(float progress)
        {
            progressSlider?.SetProgress(progress);
        }

        private void HandleGenerationCompleted()
        {
            progressSlider?.SetProgress(1f);
            overlay?.Hide(fadeOutDuration);
        }

        private void ResolveReferences()
        {
            if (overlay == null)
            {
                overlay = GetComponent<GenerationLoadingOverlay>();
                if (overlay == null)
                {
                    overlay = gameObject.AddComponent<GenerationLoadingOverlay>();
                }
            }

            if (progressSlider != null) return;

            progressSlider = GetComponentInChildren<GenerationProgressSlider>(true);
            if (progressSlider != null) return;

            var slider = GetComponentInChildren<UnityEngine.UI.Slider>(true);
            if (slider == null)
            {
                Debug.LogWarning($"{nameof(GenerationLoadingManager)}: 하위 Slider를 찾지 못함.", this);
                return;
            }

            progressSlider = slider.GetComponent<GenerationProgressSlider>();
            if (progressSlider == null)
            {
                progressSlider = slider.gameObject.AddComponent<GenerationProgressSlider>();
            }
        }
    }
}
