using System.Collections;
using UnityEngine;

namespace _00_Main._01_Scripts.Tile.UI
{
    /// <summary>로딩 오버레이의 표시 상태와 페이드를 담당한다.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class GenerationLoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.1f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.25f;

        private Coroutine _fadeRoutine;

        private void Awake()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            HideImmediately();
        }

        private void OnDisable()
        {
            StopFade();
        }

        public void Show() => StartFade(1f, fadeInDuration, false);

        public void Hide() => StartFade(0f, fadeOutDuration, true);

        private void HideImmediately()
        {
            if (canvasGroup == null) return;

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void StartFade(float targetAlpha, float duration, bool disableRaycastsWhenHidden)
        {
            if (canvasGroup == null) return;

            StopFade();
            if (targetAlpha > 0f) canvasGroup.blocksRaycasts = true;
            _fadeRoutine = StartCoroutine(FadeTo(targetAlpha, duration, disableRaycastsWhenHidden));
        }

        private IEnumerator FadeTo(float targetAlpha, float duration, bool disableRaycastsWhenHidden)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            if (disableRaycastsWhenHidden) canvasGroup.blocksRaycasts = false;
            _fadeRoutine = null;
        }

        private void StopFade()
        {
            if (_fadeRoutine == null) return;

            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }
}
