using System;
using System.Collections;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00_Main._01_Scripts.Tile
{
    /// <summary>계산 완료된 고도를 프레임 예산 안에서 타일맵에 적용한다.</summary>
    public sealed class TilemapBandApplier
    {
        private TileBase[][] _tilesPerTier;

        public IEnumerator Apply(
            TierLayer[] tiers,
            NativeArray<float> elevation,
            int width,
            int height,
            int rowsPerBand,
            double frameBudgetMs,
            float startProgress,
            Action<float> reportProgress)
        {
            EnsureBuffers(tiers.Length, width * rowsPerBand);

            int bandsPerTier = Mathf.CeilToInt(height / (float)rowsPerBand);
            int totalBands = tiers.Length * bandsPerTier;
            int completedBands = 0;
            int startX = -width / 2;
            int startY = -height / 2;
            var stopwatch = Stopwatch.StartNew();

            for (int tierIndex = 0; tierIndex < tiers.Length; tierIndex++)
            {
                TierLayer tier = tiers[tierIndex];
                if (tier.tilemap == null)
                {
                    continue;
                }

                tier.tilemap.ClearAllTiles();

                for (int bandStartY = 0; bandStartY < height; bandStartY += rowsPerBand)
                {
                    int bandHeight = Mathf.Min(rowsPerBand, height - bandStartY);
                    int bandCellCount = width * bandHeight;
                    TileBase[] tiles = _tilesPerTier[tierIndex];

                    for (int index = 0; index < bandCellCount; index++)
                    {
                        int localX = index % width;
                        int localY = index / width;
                        int elevationIndex = (bandStartY + localY) * width + localX;
                        tiles[index] = elevation[elevationIndex] >= tier.threshold ? tier.tile : null;
                    }

                    var bounds = new BoundsInt(startX, startY + bandStartY, 0, width, bandHeight, 1);
                    if (bandCellCount == tiles.Length)
                    {
                        tier.tilemap.SetTilesBlock(bounds, tiles);
                    }
                    else
                    {
                        var finalBand = new TileBase[bandCellCount];
                        Array.Copy(tiles, finalBand, bandCellCount);
                        tier.tilemap.SetTilesBlock(bounds, finalBand);
                    }

                    completedBands++;
                    reportProgress?.Invoke(Mathf.Lerp(startProgress, 1f, completedBands / (float)totalBands));

                    if (stopwatch.Elapsed.TotalMilliseconds >= frameBudgetMs)
                    {
                        yield return null;
                        stopwatch.Restart();
                    }
                }
            }
        }

        private void EnsureBuffers(int tierCount, int bandCellCount)
        {
            if (_tilesPerTier != null && _tilesPerTier.Length == tierCount &&
                (tierCount == 0 || _tilesPerTier[0].Length == bandCellCount))
            {
                return;
            }

            _tilesPerTier = new TileBase[tierCount][];
            for (int tierIndex = 0; tierIndex < tierCount; tierIndex++)
            {
                _tilesPerTier[tierIndex] = new TileBase[bandCellCount];
            }
        }
    }
}
