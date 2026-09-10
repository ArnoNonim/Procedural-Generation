using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace _00_Main._01_Scripts.Demo
{
    public class TilemapDemo : MonoBehaviour
    {
        public Tilemap tilemap;
        public TileBase theTile;
        public int width = 20;     
        public int height = 20;
        public float genDelaySeconds = 0.01f; 

        [Header("FBM Noise Settings")]
        public float noiseScale = 0.1f;       
        public int octaves = 4;               
        public float persistence = 0.5f;       
        public float lacunarity = 2.0f;        
        [Range(0f, 1f)]
        public float threshold = 0.4f; 

        private float _seedX;
        private float _seedY;

        private CancellationTokenSource _cts;

        private void Start()
        {
            GenerateNewSeed();
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }

                _cts = new CancellationTokenSource();

                GenerateNewSeed();

                PlaceTilesCentred(_cts.Token).Forget();
            }
        }

        private void GenerateNewSeed()
        {
            _seedX = Random.Range(100f, 1000f) + 0.1234f;
            _seedY = Random.Range(100f, 1000f) + 0.5678f;
        }

        private float CalculateFbm(float x, float y, int oct, float per, float lac)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;
    
            for (int i = 0; i < oct; i++) {
                float sampleX = (_seedX + x) * frequency;
                float sampleY = (_seedY + y) * frequency;

                total += Mathf.PerlinNoise(sampleX, sampleY) * amplitude;
                maxValue += amplitude;
                
                amplitude *= per;
                frequency *= lac;
            }
    
            return total / maxValue;
        }

        private async UniTaskVoid PlaceTilesCentred(CancellationToken token)
        {
            Debug.Log("Placing tiles Started");
            tilemap.ClearAllTiles();

            int startX = -width / 2;
            int startY = -height / 2;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (token.IsCancellationRequested) return;

                    float noiseValue = CalculateFbm(x * noiseScale, y * noiseScale, octaves, persistence, lacunarity);

                    if (noiseValue >= threshold)
                    {
                        Vector3Int pos = new Vector3Int(startX + x, startY + y, 0);
                        tilemap.SetTile(pos, theTile);

                        bool isCancelled = await UniTask.WaitForSeconds(genDelaySeconds, cancellationToken: token)
                            .SuppressCancellationThrow();

                        if (isCancelled) return;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
        }
    }
}
