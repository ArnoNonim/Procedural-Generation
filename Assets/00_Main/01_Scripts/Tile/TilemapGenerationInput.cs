using UnityEngine.InputSystem;

namespace _00_Main._01_Scripts.Tile
{
    /// <summary>데모용 맵 생성 단축키를 한곳에서 제공한다.</summary>
    public static class TilemapGenerationInput
    {
        public static bool IsRequestedThisFrame => Keyboard.current?.spaceKey.wasPressedThisFrame == true;
    }
}
