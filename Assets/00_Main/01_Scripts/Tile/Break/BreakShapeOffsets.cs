using System.Collections.Generic;
using UnityEngine;

namespace _00_Main._01_Scripts.Tile.Break
{
    /// <summary>파괴 범위에 필요한 셀 오프셋만 생성한다.</summary>
    public static class BreakShapeOffsets
    {
        public static Vector3Int[] Create(BreakShape shape, int radius)
        {
            return shape == BreakShape.Circle ? CreateCircle(radius) : CreateRectangle(radius, radius);
        }

        private static Vector3Int[] CreateCircle(int radius)
        {
            if (radius <= 0)
            {
                return new[] { Vector3Int.zero };
            }

            var offsets = new List<Vector3Int>((radius * 2 + 1) * (radius * 2 + 1));
            float radiusSquared = (radius + 0.5f) * (radius + 0.5f);

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radiusSquared)
                    {
                        offsets.Add(new Vector3Int(x, y));
                    }
                }
            }

            return offsets.ToArray();
        }

        private static Vector3Int[] CreateRectangle(int halfWidth, int halfHeight)
        {
            int width = halfWidth * 2 + 1;
            int height = halfHeight * 2 + 1;
            var offsets = new Vector3Int[width * height];
            int index = 0;

            for (int y = -halfHeight; y <= halfHeight; y++)
            {
                for (int x = -halfWidth; x <= halfWidth; x++)
                {
                    offsets[index++] = new Vector3Int(x, y);
                }
            }

            return offsets;
        }
    }
}
