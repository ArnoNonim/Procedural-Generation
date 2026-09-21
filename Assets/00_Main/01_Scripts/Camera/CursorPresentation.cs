using UnityEngine;

namespace _00_Main._01_Scripts.Camera
{
    /// <summary>Unity 커서 API 호출만 담당한다.</summary>
    public sealed class CursorPresentation
    {
        private readonly CursorTextures _textures;
        private readonly Vector2 _hotspot;

        public CursorPresentation(CursorTextures textures, Vector2 hotspot)
        {
            _textures = textures;
            _hotspot = hotspot;
        }

        public void SetPressed(bool isPressed)
        {
            Cursor.SetCursor(
                isPressed ? _textures.cursorDown : _textures.cursorUp,
                _hotspot,
                CursorMode.ForceSoftware);
        }

        public void SetVisible(bool isVisible) => Cursor.visible = isVisible;
    }
}
