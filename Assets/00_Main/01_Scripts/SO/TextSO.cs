using UnityEngine;
using UnityEngine.Serialization;

namespace _00_Main._01_Scripts.SO
{
    [CreateAssetMenu(fileName = "Text", menuName = "Text/Text", order = 0)]
    public class TextSO : ScriptableObject
    {
        [TextArea(3, 100)] public string textString;
        [field:SerializeField] public int TextLength { get; private set; }

        private void OnValidate()
        {
            TextLength = textString.Length;
        }
    }
}