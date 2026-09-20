using System.Collections.Generic;
using _00_Main._01_Scripts.SO;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00_Main._01_Scripts.Demo
{
    /// <summary>
    /// 파괴 범위에 해당하는 셀들을 하나의 Mesh(한 번의 드로우콜)로 그림.
    /// 셀 경계는 Tilemap.CellToWorld로 직접 구해서 실제 렌더링 경계와 항상 정확히 일치하고,
    /// 각 쿼드에는 인스펙터에서 지정한 Sprite 텍스처를 UV 매핑해서 입힘.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class BreakAreaIndicatorMesh : MonoBehaviour
    {
        [field:SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        
        [SerializeField] private TileBreakDemo tileBreakDemo;

        [Header("Appearance")]
        [Tooltip("각 타일에 입힐 텍스처. 비워두면 흰색 단색으로 표시됨.")]
        [SerializeField] private Sprite highlightSprite;

        [Tooltip("Sprite 위에 곱해질 색상/알파. highlightSprite가 없으면 이 색이 곧 타일 색이 됨.")]
        [SerializeField] private Color tintColor = new Color(1f, 1f, 1f, 0.6f);

        [Header("Sorting")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 100;

        private Mesh _mesh;
        private Material _material;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<Vector2> _uvs = new List<Vector2>();

        private static readonly Vector3Int CellDiagonal = new Vector3Int(1, 1, 0);

        // 이전 프레임과 비교해서 실제로 모양/위치가 바뀌었을 때만 메쉬를 다시 만듦
        private Vector3Int _lastCellPosition;
        private int _lastOffsetsLength = -1;
        private bool _lastHasHover;
        private bool _initialized;

        // 스프라이트가 바뀌면 텍스처/UV 영역도 다시 계산해야 함
        private Sprite _cachedSprite;
        private Rect _cachedUvRect; // 0~1 정규화된 UV 영역
        
        private bool _isIndicatorActive;
        private MeshRenderer _meshRenderer;
        
        private void Awake()
        {
            _mesh = new Mesh { name = "BreakAreaIndicatorMesh" };
            GetComponent<MeshFilter>().mesh = _mesh;

            _material = new Material(Shader.Find("Sprites/Default"));

            _meshRenderer = GetComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = _material;
            _meshRenderer.sortingLayerName = sortingLayerName;
            _meshRenderer.sortingOrder = sortingOrder;

            ApplyTexture();
        }

        private void OnEnable()
        {
            PlayerInput.OnCursorToggle += ToggleIndicator;
        }

        private void Start()
        {
            ToggleIndicator(!true);
        }


        private void OnValidate()
        {
            ApplyTexture();
        }

        private void ToggleIndicator(bool toggle)
        {
            _isIndicatorActive = !toggle;
            _meshRenderer.enabled = !toggle;

            if (toggle)
                _mesh.Clear();
            else
                _initialized = false;
        }
        
        /// <summary>
        /// highlightSprite가 바뀌었을 때 머티리얼의 텍스처와 UV 영역을 갱신함.
        /// Sprite Atlas로 패킹된 스프라이트여도 UnityEngine.Sprites.DataUtility.GetOuterUV를 쓰면
        /// 패딩/좌표 변환을 직접 계산할 필요 없이 항상 정확한 UV가 나옴
        /// (SpriteRenderer가 내부적으로 쓰는 것과 동일한 API).
        /// </summary>
        private void ApplyTexture()
        {
            if (_material == null) return;

            if (highlightSprite == null)
            {
                _material.mainTexture = Texture2D.whiteTexture;
                _cachedUvRect = new Rect(0f, 0f, 1f, 1f);
                _cachedSprite = null;
                return;
            }

            Texture2D texture = highlightSprite.texture;

            if (texture == null)
            {
                // Sprite Atlas가 아직 패킹/로드되지 않은 예외적인 상황에 대한 방어 코드
                Debug.LogWarning($"{nameof(BreakAreaIndicatorMesh)}: highlightSprite의 texture가 null임. " +
                                  "Sprite Atlas 패킹 상태를 확인해야 함.", this);
                _material.mainTexture = Texture2D.whiteTexture;
                _cachedUvRect = new Rect(0f, 0f, 1f, 1f);
                _cachedSprite = null;
                return;
            }

            _material.mainTexture = texture;

            // 아틀라스 패킹 여부와 무관하게 항상 정확한 UV 영역을 돌려줌
            Vector4 outerUv = UnityEngine.Sprites.DataUtility.GetOuterUV(highlightSprite);
            _cachedUvRect = new Rect(
                outerUv.x,
                outerUv.y,
                outerUv.z - outerUv.x,
                outerUv.w - outerUv.y
            );

            _cachedSprite = highlightSprite;
        }

        private void LateUpdate()
        {
            if (!_isIndicatorActive || tileBreakDemo == null) return;

            // 인스펙터에서 런타임 중 스프라이트를 바꿨을 수도 있으니 확인
            if (_cachedSprite != highlightSprite)
            {
                ApplyTexture();
            }

            bool hasHover = tileBreakDemo.TryGetHoverCell(
                out Tilemap hitTilemap, out Vector3Int cellPosition, out _);

            Vector3Int[] offsets = tileBreakDemo.ShapeOffsets;
            int offsetsLength = offsets?.Length ?? 0;

            bool unchanged = _initialized
                              && hasHover == _lastHasHover
                              && cellPosition == _lastCellPosition
                              && offsetsLength == _lastOffsetsLength;

            if (unchanged) return;

            _initialized = true;
            _lastHasHover = hasHover;
            _lastCellPosition = cellPosition;
            _lastOffsetsLength = offsetsLength;

            if (!hasHover || offsetsLength == 0)
            {
                _mesh.Clear();
                return;
            }

            BuildMesh(hitTilemap, cellPosition, offsets);
        }

        private void BuildMesh(Tilemap hitTilemap, Vector3Int cellPosition, Vector3Int[] offsets)
        {
            _vertices.Clear();
            _triangles.Clear();
            _colors.Clear();
            _uvs.Clear();

            Rect uv = _cachedUvRect;

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3Int cell = cellPosition + offsets[i];

                // 실제 셀 경계를 타일맵에서 직접 물어봄 (Cell Gap 등과 무관하게 항상 정확)
                Vector3 worldMin = hitTilemap.CellToWorld(cell);
                Vector3 worldMax = hitTilemap.CellToWorld(cell + CellDiagonal);

                Vector3 localMin = transform.InverseTransformPoint(worldMin);
                Vector3 localMax = transform.InverseTransformPoint(worldMax);

                int baseIndex = _vertices.Count;

                _vertices.Add(new Vector3(localMin.x, localMin.y, 0f));
                _vertices.Add(new Vector3(localMax.x, localMin.y, 0f));
                _vertices.Add(new Vector3(localMax.x, localMax.y, 0f));
                _vertices.Add(new Vector3(localMin.x, localMax.y, 0f));

                // 타일 하나마다 스프라이트 전체(0~1)를 그대로 매핑 — 셀마다 같은 텍스처가 반복됨
                _uvs.Add(new Vector2(uv.xMin, uv.yMin));
                _uvs.Add(new Vector2(uv.xMax, uv.yMin));
                _uvs.Add(new Vector2(uv.xMax, uv.yMax));
                _uvs.Add(new Vector2(uv.xMin, uv.yMax));

                _colors.Add(tintColor);
                _colors.Add(tintColor);
                _colors.Add(tintColor);
                _colors.Add(tintColor);

                _triangles.Add(baseIndex + 0);
                _triangles.Add(baseIndex + 2);
                _triangles.Add(baseIndex + 1);
                _triangles.Add(baseIndex + 0);
                _triangles.Add(baseIndex + 3);
                _triangles.Add(baseIndex + 2);
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        private void OnDisable()
        {
            PlayerInput.OnCursorToggle -= ToggleIndicator;
        }
        
        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
        }
    }
}