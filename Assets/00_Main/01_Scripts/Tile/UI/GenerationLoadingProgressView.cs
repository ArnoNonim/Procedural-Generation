using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Main._01_Scripts.Tile.UI
{
    /// <summary>로딩 진행률을 UGUI Fill Image와 텍스트에 표시한다.</summary>
    public sealed class GenerationLoadingProgressView : MonoBehaviour
    {
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text progressText;

        public void SetProgress(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);

            if (progressFill != null) progressFill.fillAmount = clampedProgress;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(clampedProgress * 100f)}%";
        }
    }
}
