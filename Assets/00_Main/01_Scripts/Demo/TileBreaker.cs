using System;
using System.Collections.Generic;
using _00_Main._01_Scripts.SO;
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

    public class TileBreaker : MonoBehaviour
    {
        [Serializable]
        public struct TierTilemap
        {
            public Tilemap tilemap;
            public float posZ; // 이 티어 타일맵이 실제로 놓인 월드 Z값
        }

        [field:SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        
        [SerializeField] private Camera targetCamera;

        [InfoBox("낮은 티어부터 순서대로 등록 (검사는 역순으로 함)")]
        [SerializeField] private TierTilemap[] tilemaps;

        [Header("Break Shape")]
        [SerializeField] private BreakShape breakShape = BreakShape.Circle;

        [InfoBox("Circle: 반지름(셀 단위) / Rectangle: 가로/세로 절반 폭(셀 단위)")]
        [SerializeField, Range(0, 10)] private int radius = 1;

        [Header("Break Rate (마우스 홀드 시 연속 파괴 간격)")]
        [SerializeField] private float breakInterval = 0.15f;
      
        [Header("에디터 전용")]
#if UNITY_EDITOR
        [SerializeField] private bool enableDebugClickedTile;
#endif

        private Vector3Int[] _shapeOffsets;
        private List<Vector3Int>[] _breakPositionsByTier;
        private List<TileBase>[] _breakTilesByTier;
        private BreakShape _cachedShape;
        private int _cachedRadiusX = -1;
        private int _cachedRadiusY = -1;

        private float _nextBreakTime;

        // 인디케이터 등 외부에서 현재 파괴 범위 오프셋을 그대로 재사용할 수 있게 공개
        public Vector3Int[] ShapeOffsets => _shapeOffsets;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            RebuildShapeOffsetsIfNeeded();
            EnsureBreakBuffers();
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
            ChangeBreakAreaSize();
            
            if (!PlayerInput.IsClicking) return;
            if (Time.time < _nextBreakTime) return;

            if (!TryGetPointerCell(out Tilemap referenceTilemap, out Vector3Int cellPosition))
            {
                return;
            }

            BreakTopTilesInArea(referenceTilemap, cellPosition);

            _nextBreakTime = Time.time + breakInterval;

            if (!enableDebugClickedTile) return;
#if UNITY_EDITOR
            Debug.Log($"Cell: {(Vector2Int)cellPosition} / Cells checked: {_shapeOffsets.Length}");
#endif
        }

        private void ChangeBreakAreaSize()
        {
            if (!PlayerInput.IsShift || PlayerInput.ScrollY == 0) return;
            
            radius = Mathf.Clamp(radius + (int)PlayerInput.ScrollY, 0, 10);
        }

        /// <summary>
        /// 현재 마우스 커서가 가리키는 그리드 셀을 반환함.
        /// 커서 바로 아래에 타일이 없어도, 등록된 첫 타일맵의 그리드 평면을 기준으로 셀을 계산함.
        /// </summary>
        public bool TryGetPointerCell(out Tilemap referenceTilemap, out Vector3Int cellPosition)
        {
            referenceTilemap = null;
            cellPosition = default;

            if (targetCamera == null || Mouse.current == null || tilemaps == null)
            {
                return false;
            }

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);

            for (int i = 0; i < tilemaps.Length; i++)
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
                referenceTilemap = candidate;
                cellPosition = candidate.WorldToCell(worldPosition);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 범위의 각 셀마다 등록 순서상 가장 위(배열의 마지막)에 있는 타일 하나만 파괴함.
        /// 따라서 커서 중심 셀이 비어 있어도 범위 안의 타일은 파괴되고, 여러 티어가 겹친 셀은
        /// 화면에 가장 위로 표시되는 타일부터 파괴됨.
        /// </summary>
        private void BreakTopTilesInArea(Tilemap referenceTilemap, Vector3Int centerCell)
        {
            EnsureBreakBuffers();

            for (int tierIndex = 0; tierIndex < tilemaps.Length; tierIndex++)
            {
                _breakPositionsByTier[tierIndex].Clear();
                _breakTilesByTier[tierIndex].Clear();
            }

            for (int offsetIndex = 0; offsetIndex < _shapeOffsets.Length; offsetIndex++)
            {
                Vector3Int referenceCell = centerCell + _shapeOffsets[offsetIndex];
                Vector3 cellCenterWorld = referenceTilemap.GetCellCenterWorld(referenceCell);

                for (int tierIndex = tilemaps.Length - 1; tierIndex >= 0; tierIndex--)
                {
                    Tilemap candidate = tilemaps[tierIndex].tilemap;
                    if (candidate == null) continue;

                    Vector3Int candidateCell = candidate.WorldToCell(cellCenterWorld);
                    if (!candidate.HasTile(candidateCell)) continue;

                    _breakPositionsByTier[tierIndex].Add(candidateCell);
                    _breakTilesByTier[tierIndex].Add(null);
                    break;
                }
            }

            // Tilemap 갱신은 셀마다 하지 않고, 티어별 한 번의 SetTiles 호출로 묶음.
            // 범위가 커져도 타일맵 렌더/콜라이더 갱신 횟수는 최대 티어 수로 제한됨.
            for (int tierIndex = 0; tierIndex < tilemaps.Length; tierIndex++)
            {
                List<Vector3Int> positions = _breakPositionsByTier[tierIndex];
                if (positions.Count == 0) continue;

                tilemaps[tierIndex].tilemap.SetTiles(
                    positions.ToArray(),
                    _breakTilesByTier[tierIndex].ToArray());
            }
        }

        private void EnsureBreakBuffers()
        {
            if (tilemaps == null) return;

            if (_breakPositionsByTier != null && _breakPositionsByTier.Length == tilemaps.Length)
            {
                return;
            }

            _breakPositionsByTier = new List<Vector3Int>[tilemaps.Length];
            _breakTilesByTier = new List<TileBase>[tilemaps.Length];

            int capacity = _shapeOffsets?.Length ?? 0;
            for (int tierIndex = 0; tierIndex < tilemaps.Length; tierIndex++)
            {
                _breakPositionsByTier[tierIndex] = new List<Vector3Int>(capacity);
                _breakTilesByTier[tierIndex] = new List<TileBase>(capacity);
            }
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

            EnsureBreakBuffers();

        }

        private static Vector3Int[] BuildCircleOffsets(int radius)
        {
            if (radius <= 0)
            {
                return new[] { Vector3Int.zero };
            }

            var list = new List<Vector3Int>((radius * 2 + 1) * (radius * 2 + 1));
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
