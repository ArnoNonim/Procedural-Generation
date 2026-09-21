using _00_Main._01_Scripts.SO;
using TriInspector;
using UnityEngine;

namespace _00_Main._01_Scripts.Camera
{
    [System.Serializable]
    public struct CursorTextures
    {
        public Texture2D cursorUp;
        public Texture2D cursorDown;
    }
    
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager Instance { get; private set; }
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }

        [InfoBox("평상시와 눌렸을 때의 텍스쳐 크기가 같아야 합니다")]
        [InlineProperty, HideLabel, SerializeField] private CursorTextures cursorSettings;

        [InfoBox("$" + nameof(HotspotTooltip))]
        [SerializeField] private Vector2 hotspot;

        private CursorPresentation _presentation;

        private string HotspotTooltip
        {
            get
            {
                if (cursorSettings.cursorUp == null) return "커서 이미지를 등록해 주세요";
                return $"현재 사용하고 있는 커서 텍스쳐의 크기는 {cursorSettings.cursorUp.width}x{cursorSettings.cursorUp.height} 입니다";
            }
        }
        
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);
            
            _presentation = new CursorPresentation(cursorSettings, hotspot);
            _presentation.SetPressed(false);
            _presentation.SetVisible(false);
        }
        
        private void OnEnable()
        {
            PlayerInput.OnCursorClicked += CursorClick;
            PlayerInput.OnCursorToggle += CursorToggle;
        }

        private void OnValidate()
        {
            _presentation = new CursorPresentation(cursorSettings, hotspot);
            _presentation.SetPressed(false);
        }
        
        private void CursorClick(bool toggle)
        {
            _presentation.SetPressed(toggle);
        }
        
        private void CursorToggle(bool toggle)
        {
            _presentation.SetVisible(toggle);
        }

        private void OnDisable()
        {
            PlayerInput.OnCursorClicked -= CursorClick;
            PlayerInput.OnCursorToggle -= CursorToggle;
        }
    }
}
