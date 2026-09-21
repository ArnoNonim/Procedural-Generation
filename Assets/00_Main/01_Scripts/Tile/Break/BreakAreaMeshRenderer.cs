using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _00_Main._01_Scripts.Tile.Break
{
    /// <summary>파괴 범위 메시의 생성·재질·드로우 상태만 담당한다.</summary>
    public sealed class BreakAreaMeshRenderer
    {
        private static readonly Vector3Int CellDiagonal = new(1, 1, 0);

        private readonly Transform _transform;
        private readonly MeshRenderer _renderer;
        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new();
        private readonly List<Color> _colors = new();
        private readonly List<Vector2> _uvs = new();

        private Sprite _sprite;
        private Rect _uvRect;

        public BreakAreaMeshRenderer(
            Transform transform,
            MeshFilter meshFilter,
            MeshRenderer meshRenderer,
            string sortingLayerName,
            int sortingOrder)
        {
            _transform = transform;
            _renderer = meshRenderer;
            _mesh = new Mesh { name = "BreakAreaIndicatorMesh" };
            meshFilter.mesh = _mesh;
            _material = new Material(Shader.Find("Sprites/Default"));
            _renderer.sharedMaterial = _material;
            _renderer.sortingLayerName = sortingLayerName;
            _renderer.sortingOrder = sortingOrder;
        }

        public void SetVisible(bool isVisible)
        {
            _renderer.enabled = isVisible;
            if (!isVisible)
            {
                _mesh.Clear();
            }
        }

        public void SetAppearance(Sprite sprite)
        {
            if (_sprite == sprite)
            {
                return;
            }

            _sprite = sprite;
            if (sprite == null || sprite.texture == null)
            {
                _material.mainTexture = Texture2D.whiteTexture;
                _uvRect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            _material.mainTexture = sprite.texture;
            Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            _uvRect = new Rect(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y);
        }

        public void Build(Tilemap tilemap, Vector3Int centerCell, Vector3Int[] offsets, Color tintColor)
        {
            if (tilemap == null || offsets == null)
            {
                _mesh.Clear();
                return;
            }

            _vertices.Clear();
            _triangles.Clear();
            _colors.Clear();
            _uvs.Clear();

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3Int cell = centerCell + offsets[i];
                Vector3 localMin = _transform.InverseTransformPoint(tilemap.CellToWorld(cell));
                Vector3 localMax = _transform.InverseTransformPoint(tilemap.CellToWorld(cell + CellDiagonal));
                int baseIndex = _vertices.Count;

                _vertices.Add(new Vector3(localMin.x, localMin.y));
                _vertices.Add(new Vector3(localMax.x, localMin.y));
                _vertices.Add(new Vector3(localMax.x, localMax.y));
                _vertices.Add(new Vector3(localMin.x, localMax.y));
                _uvs.Add(new Vector2(_uvRect.xMin, _uvRect.yMin));
                _uvs.Add(new Vector2(_uvRect.xMax, _uvRect.yMin));
                _uvs.Add(new Vector2(_uvRect.xMax, _uvRect.yMax));
                _uvs.Add(new Vector2(_uvRect.xMin, _uvRect.yMax));
                _colors.Add(tintColor);
                _colors.Add(tintColor);
                _colors.Add(tintColor);
                _colors.Add(tintColor);
                _triangles.Add(baseIndex);
                _triangles.Add(baseIndex + 2);
                _triangles.Add(baseIndex + 1);
                _triangles.Add(baseIndex);
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

        public void Dispose()
        {
            Object.Destroy(_mesh);
            Object.Destroy(_material);
        }
    }
}
