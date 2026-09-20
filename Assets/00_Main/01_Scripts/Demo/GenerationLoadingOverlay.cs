using System.Collections;
using UnityEngine;

namespace _00_Main._01_Scripts.Demo
{
    [RequireComponent(typeof(CanvasGroup))]
    public class GenerationLoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        private Coroutine _fadeRoutine;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            EnsureCanvasGroup();
        }

        public void InitializeHidden()
        {
            EnsureCanvasGroup();

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void Show(float duration)
        {
            StartFade(1f, duration);
        }

        public void Hide(float duration)
        {
            StartFade(0f, duration);
        }

        private void StartFade(float targetAlpha, float duration)
        {
            EnsureCanvasGroup();

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeCanvasGroup(targetAlpha, duration));
        }

        private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            bool showing = targetAlpha > 0f;
            canvasGroup.blocksRaycasts = showing;
            canvasGroup.interactable = showing;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

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

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}
