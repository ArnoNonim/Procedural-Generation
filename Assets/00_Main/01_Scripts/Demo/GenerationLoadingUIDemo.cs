using System.Collections;
using UnityEngine;

namespace _00_Main._01_Scripts.Demo
{
    [RequireComponent(typeof(CanvasGroup))]
    public class GenerationLoadingUIDemo : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TilemapDemo mapGenerator;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Fade")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.4f;

        private Coroutine _fadeRoutine;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            // 시작 시엔 완전히 숨김 상태로 고정
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            if (mapGenerator == null)
            {
                Debug.LogWarning($"{nameof(GenerationLoadingUIDemo)}: mapGenerator 참조 없음.", this);
                return;
            }

            mapGenerator.OnGenerationStarted += HandleGenerationStarted;
            mapGenerator.OnGenerationCompleted += HandleGenerationCompleted;
        }

        private void OnDisable()
        {
            if (mapGenerator == null) return;

            mapGenerator.OnGenerationStarted -= HandleGenerationStarted;
            mapGenerator.OnGenerationCompleted -= HandleGenerationCompleted;
        }

        private void HandleGenerationStarted()
        {
            StartFade(1f, fadeInDuration);
        }

        private void HandleGenerationCompleted()
        {
            StartFade(0f, fadeOutDuration);
        }

        private void StartFade(float targetAlpha, float duration)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeCanvasGroup(targetAlpha, duration));
        }

        private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            // 켜지는 순간부터는 입력 막아서 로딩 중 클릭 방지
            bool showing = targetAlpha > 0f;
            canvasGroup.blocksRaycasts = showing;
            canvasGroup.interactable = showing;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            // 타임스케일 영향 안 받게 unscaledDeltaTime 사용 (로딩 중 timeScale 건드릴 수도 있으니)
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            _fadeRoutine = null;
        }
    }
}