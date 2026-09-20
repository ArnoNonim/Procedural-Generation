using System;
using TriInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace _00_Main._01_Scripts.Demo
{
    public enum BreakShape
    {
        Circle,
        Rectangle
    }

    public class TileBreakDemo : MonoBehaviour
    {
        [Serializable]
        public struct TierTilemap
        {
            public Tilemap tilemap;
            public float posZ; // 이 티어 타일맵이 실제로 놓인 월드 Z값
        }

        [SerializeField] private Camera targetCamera;

        [InfoBox(TilemapsTooltip)]
        [SerializeField] private TierTilemap[] tilemaps;

        [Header("Break Shape")]
        [SerializeField] private BreakShape breakShape = BreakShape.Circle;

        [InfoBox(RadiusTooltip)]
        [SerializeField, Range(0, 10)] private int radius = 1;

        [Header("Break Rate (마우스 홀드 시 연속 파괴 간격)")]
        [SerializeField] private float breakInterval = 0.15f;

        private Vector3Int[] _shapeOffsets;
        private Vector3Int[] _positionsBuffer;
        private TileBase[] _nullTilesBuffer;

        private BreakShape _cachedShape;
        private int _cachedRadiusX = -1;
        private int _cachedRadiusY = -1;

        private float _nextBreakTime;

        private const string RadiusTooltip = "Circle: 반지름(셀 단위) / Rectangle: 가로/세로 절반 폭(셀 단위)";
        private const string TilemapsTooltip = "낮은 티어부터 순서대로 등록 (검사는 역순으로 함)";

        // 인디케이터 등 외부에서 현재 파괴 범위 오프셋을 그대로 재사용할 수 있게 공개
        public Vector3Int[] ShapeOffsets => _shapeOffsets;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            RebuildShapeOffsetsIfNeeded();
        }

        private void OnValidate()
        {
            RebuildShapeOffsetsIfNeeded();

            for (int i = 0; i < tilemaps.Length; i++)
            {
                tilemaps[i].posZ = tilemaps[i].tilemap.transform.position.z;
            }
        }

        private void Update()
        {
            RebuildShapeOffsetsIfNeeded();

            if (Mouse.current == null) return;
            if (!Mouse.current.leftButton.isPressed) return;
            if (Time.time < _nextBreakTime) return;

            if (!TryGetHoverCell(out Tilemap tilemap, out Vector3Int cellPosition, out TileBase tile))
            {
                return;
            }

            for (int n = 0; n < _shapeOffsets.Length; n++)
            {
                _positionsBuffer[n] = cellPosition + _shapeOffsets[n];
            }

            tilemap.SetTiles(_positionsBuffer, _nullTilesBuffer);

            _nextBreakTime = Time.time + breakInterval;

#if UNITY_EDITOR
            Debug.Log($"Tilemap: {tilemap.name} / Cell: {(Vector2Int)cellPosition} / Tile: {tile.name} / Cells broken: {_shapeOffsets.Length}");
#endif
        }

        /// <summary>
        /// 현재 마우스 커서 아래에 있는 셀을 찾음. 가장 위(마지막 등록된) 티어부터 검사해서
        /// 실제로 타일이 존재하는 첫 번째 티어를 반환함.
        /// 파괴 로직과 인디케이터 표시 로직이 이 메서드 하나를 공유해서 서로 결과가 어긋나지 않음.
        /// </summary>
        public bool TryGetHoverCell(out Tilemap hitTilemap, out Vector3Int cellPosition, out TileBase tile)
        {
            hitTilemap = null;
            cellPosition = default;
            tile = null;

            if (targetCamera == null || Mouse.current == null || tilemaps == null)
            {
                return false;
            }

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);

            for (int i = tilemaps.Length - 1; i >= 0; i--)
            {
                Tilemap candidate = tilemaps[i].tilemap;
                if (candidate == null) continue;

                float planeZ = tilemaps[i].posZ;
                var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));

                if (!plane.Raycast(ray, out float enter))
                {
                    continue;
                }

                Vector3 worldPosition = ray.GetPoint(enter);
                Vector3Int candidateCell = candidate.WorldToCell(worldPosition);
                TileBase candidateTile = candidate.GetTile(candidateCell);

                if (candidateTile == null) continue;

                hitTilemap = candidate;
                cellPosition = candidateCell;
                tile = candidateTile;
                return true;
            }

            return false;
        }

        private void RebuildShapeOffsetsIfNeeded()
        {
            if (_shapeOffsets != null
                && _cachedShape == breakShape
                && _cachedRadiusX == radius
                && _cachedRadiusY == radius)
            {
                return;
            }

            _cachedShape = breakShape;
            _cachedRadiusX = radius;
            _cachedRadiusY = radius;

            _shapeOffsets = breakShape == BreakShape.Circle
                ? BuildCircleOffsets(radius)
                : BuildRectangleOffsets(radius, radius);

            _positionsBuffer = new Vector3Int[_shapeOffsets.Length];
            _nullTilesBuffer = new TileBase[_shapeOffsets.Length];
        }

        private static Vector3Int[] BuildCircleOffsets(int radius)
        {
            if (radius <= 0)
            {
                return new[] { Vector3Int.zero };
            }

            var list = new System.Collections.Generic.List<Vector3Int>((radius * 2 + 1) * (radius * 2 + 1));
            float radiusSq = (radius + 0.5f) * (radius + 0.5f);

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radiusSq)
                    {
                        list.Add(new Vector3Int(x, y, 0));
                    }
                }
            }

            return list.ToArray();
        }

        private static Vector3Int[] BuildRectangleOffsets(int halfWidth, int halfHeight)
        {
            int width = halfWidth * 2 + 1;
            int height = halfHeight * 2 + 1;
            var array = new Vector3Int[width * height];

            int index = 0;
            for (int y = -halfHeight; y <= halfHeight; y++)
            {
                for (int x = -halfWidth; x <= halfWidth; x++)
                {
                    array[index] = new Vector3Int(x, y, 0);
                    index++;
                }
            }

            return array;
        }
    }
}