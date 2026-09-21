using _00_Main._01_Scripts.SO;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00_Main._01_Scripts.Tile.Break
{
    /// <summary>입력과 타일 파괴 정보를 받아 범위 메시 뷰를 조정한다.</summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class BreakAreaIndicatorMesh : MonoBehaviour
    {
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [SerializeField] private TileBreaker tileBreaker;

        [Header("Appearance")]
        [SerializeField] private Sprite highlightSprite;
        [SerializeField] private Color tintColor = new(1f, 1f, 1f, 0.6f);

        [Header("Sorting")]
        [SerializeField] private string sortingLayerName;
        [SerializeField] private int sortingOrder = 100;

        private BreakAreaMeshRenderer _meshRenderer;
        private Vector3Int _lastCellPosition;
        private int _lastOffsetsLength = -1;
        private bool _isVisible;
        private bool _isInitialized;

        private void Awake()
        {
            _meshRenderer = new BreakAreaMeshRenderer(
                transform,
                GetComponent<MeshFilter>(),
                GetComponent<MeshRenderer>(),
                sortingLayerName,
                sortingOrder);
            _meshRenderer.SetAppearance(highlightSprite);
            SetVisibility(false);
        }

        private void OnEnable()
        {
            if (PlayerInput != null)
            {
                PlayerInput.OnCursorToggle += SetVisibility;
            }
        }

        private void OnValidate()
        {
            _meshRenderer?.SetAppearance(highlightSprite);
            _isInitialized = false;
        }

        private void LateUpdate()
        {
            if (!_isVisible || tileBreaker == null)
            {
                return;
            }

            _meshRenderer.SetAppearance(highlightSprite);
            bool hasPointerCell = tileBreaker.TryGetPointerCell(out Tilemap referenceTilemap, out Vector3Int cellPosition);
            Vector3Int[] offsets = tileBreaker.ShapeOffsets;
            int offsetsLength = offsets?.Length ?? 0;

            if (_isInitialized && hasPointerCell && cellPosition == _lastCellPosition && offsetsLength == _lastOffsetsLength)
            {
                return;
            }

            _isInitialized = true;
            _lastCellPosition = cellPosition;
            _lastOffsetsLength = offsetsLength;
            _meshRenderer.Build(hasPointerCell ? referenceTilemap : null, cellPosition, offsets, tintColor);
        }

        private void SetVisibility(bool isCursorVisible)
        {
            _isVisible = !isCursorVisible;
            _isInitialized = false;
            _meshRenderer?.SetVisible(_isVisible);
        }

        private void OnDisable()
        {
            if (PlayerInput != null)
            {
                PlayerInput.OnCursorToggle -= SetVisibility;
            }
        }

        private void OnDestroy()
        {
            _meshRenderer?.Dispose();
        }
    }
}
