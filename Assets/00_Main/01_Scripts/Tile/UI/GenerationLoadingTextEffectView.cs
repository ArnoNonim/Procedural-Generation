using EasyTextEffects;
using UnityEngine;

namespace _00_Main._01_Scripts.Tile.UI
{
    /// <summary>로딩 제목의 EasyTextEffects 재생을 캡슐화한다.</summary>
    public sealed class GenerationLoadingTextEffectView : MonoBehaviour
    {
        [SerializeField] private TextEffect textEffect;

        public void Play()
        {
            if (textEffect == null) return;
            
            textEffect.StartManualEffects();
        }

        public void StopEffects()
        {
            if (textEffect == null) return;

            textEffect.StopAllEffects();
        }
    }
}
