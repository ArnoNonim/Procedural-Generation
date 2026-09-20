using UnityEngine;
using UnityEngine.UI;

namespace _00_Main._01_Scripts.Demo
{
    [RequireComponent(typeof(Slider))]
    public class GenerationProgressSlider : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        private void Reset()
        {
            slider = GetComponent<Slider>();
        }

        private void Awake()
        {
            EnsureSlider();

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        public void SetProgress(float value)
        {
            EnsureSlider();
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        private void EnsureSlider()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }
        }
    }
}
