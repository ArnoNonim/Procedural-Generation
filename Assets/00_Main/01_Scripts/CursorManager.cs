using _00_Main._01_Scripts.SO;
using TriInspector;
using UnityEngine;

namespace _00_Main._01_Scripts
{
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager Instance { get; private set; }
        [field:SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        
        [SerializeField] private Texture2D cursorUp;
        [SerializeField] private Texture2D cursorDown;

        [InfoBox("$" + nameof(HotspotTooltip))]
        [SerializeField] private Vector2 hotspot;

        private string HotspotTooltip
        {
            get
            {
                if (cursorUp == null) return "커서 이미지를 등록해 주세요";
                return $"현재 사용하고 있는 커서 텍스쳐의 크기는 {cursorUp.width}x{cursorUp.height} 입니다";
            }
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
                Destroy(this);
            
            ChangeCursorTexture();
            Cursor.visible = false;
        }
        
        private void OnEnable()
        {
            PlayerInput.OnCursorClicked += CursorClick;
            PlayerInput.OnCursorToggle += CursorToggle;
        }

        private void OnValidate()
        {
            ChangeCursorTexture();
        }
        
        private void CursorClick(bool toggle)
        {
            ChangeCursorTexture(toggle);
        }

        private void ChangeCursorTexture(bool toggle = false)
        {
            Cursor.SetCursor(toggle ? cursorDown : cursorUp, hotspot, CursorMode.ForceSoftware);
        }
        
        private void CursorToggle(bool toggle)
        {
            Cursor.visible = toggle;
        }

        private void OnDisable()
        {
            PlayerInput.OnCursorClicked -= CursorClick;
            PlayerInput.OnCursorToggle -= CursorToggle;
        }
    }
}
