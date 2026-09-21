using System.Collections;
using UnityEngine;

namespace _00_Main._01_Scripts.Tile.UI
{
    public class FramerateInspector : MonoBehaviour
    {
        private float _deltaTime;

        [SerializeField] private TMPro.TMP_Text textCompo;   
        [SerializeField] private int size = 25;
        [SerializeField] private Color color = Color.red;
        [SerializeField, Range(0, 10f)] private float delay = 1f;

        private float _fps;
        private float _ms;

        private void Awake()
        {
            StartCoroutine(nameof(UpdateUI));
        }
        
        private void OnValidate()
        {
            Initialize();
        }

        private void Initialize()
        {
            textCompo.color = color;
            textCompo.fontSize = size;
        }

        private IEnumerator UpdateUI()
        {
            Calculate();
            Draw();
            yield return new WaitForSeconds(delay);
            StartCoroutine(nameof(UpdateUI));
        }

        private void Calculate()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            _ms = _deltaTime * 1000f;
            _fps = 1.0f / _deltaTime;
        }

        private void Draw()
        {
            string text = string.Format("{0:0.} FPS ({1:0.0} ms)", _fps, _ms);
            textCompo.text = text;
        }
    }
}