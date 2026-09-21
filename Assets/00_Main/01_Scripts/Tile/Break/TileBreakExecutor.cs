using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00_Main._01_Scripts.Tile.Break
{
    /// <summary>선택된 범위에서 화면상 최상단 타일을 찾아 티어별로 일괄 제거한다.</summary>
    public sealed class TileBreakExecutor
    {
        private List<Vector3Int>[] _positionsByTier;
        private List<TileBase>[] _tilesByTier;

        public void BreakTopTiles(
            TileBreaker.TierTilemap[] tiers,
            Tilemap referenceTilemap,
            Vector3Int centerCell,
            Vector3Int[] offsets)
        {
            if (tiers == null || referenceTilemap == null || offsets == null)
            {
                return;
            }

            EnsureBuffers(tiers.Length, offsets.Length);
            ClearBuffers();

            for (int offsetIndex = 0; offsetIndex < offsets.Length; offsetIndex++)
            {
                Vector3Int referenceCell = centerCell + offsets[offsetIndex];
                Vector3 cellCenterWorld = referenceTilemap.GetCellCenterWorld(referenceCell);

                for (int tierIndex = tiers.Length - 1; tierIndex >= 0; tierIndex--)
                {
                    Tilemap candidate = tiers[tierIndex].tilemap;
                    if (candidate == null)
                    {
                        continue;
                    }

                    Vector3Int candidateCell = candidate.WorldToCell(cellCenterWorld);
                    if (!candidate.HasTile(candidateCell))
                    {
                        continue;
                    }

                    _positionsByTier[tierIndex].Add(candidateCell);
                    _tilesByTier[tierIndex].Add(null);
                    break;
                }
            }

            for (int tierIndex = 0; tierIndex < tiers.Length; tierIndex++)
            {
                if (_positionsByTier[tierIndex].Count == 0 || tiers[tierIndex].tilemap == null)
                {
                    continue;
                }

                tiers[tierIndex].tilemap.SetTiles(
                    _positionsByTier[tierIndex].ToArray(),
                    _tilesByTier[tierIndex].ToArray());
            }
        }

        private void EnsureBuffers(int tierCount, int capacity)
        {
            if (_positionsByTier != null && _positionsByTier.Length == tierCount)
            {
                return;
            }

            _positionsByTier = new List<Vector3Int>[tierCount];
            _tilesByTier = new List<TileBase>[tierCount];

            for (int tierIndex = 0; tierIndex < tierCount; tierIndex++)
            {
                _positionsByTier[tierIndex] = new List<Vector3Int>(capacity);
                _tilesByTier[tierIndex] = new List<TileBase>(capacity);
            }
        }

        private void ClearBuffers()
        {
            for (int tierIndex = 0; tierIndex < _positionsByTier.Length; tierIndex++)
            {
                _positionsByTier[tierIndex].Clear();
                _tilesByTier[tierIndex].Clear();
            }
        }
    }
}
