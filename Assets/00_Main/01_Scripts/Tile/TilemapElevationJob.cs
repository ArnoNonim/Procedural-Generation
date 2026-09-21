using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace _00_Main._01_Scripts.Tile
{
    /// <summary>셀별 고도 값을 병렬 계산하는 Burst 작업이다.</summary>
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct TilemapElevationJob : IJobParallelFor
    {
        public int Width;
        public int Height;
        public float NoiseScale;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;
        public float2 Seed;
        public float EdgeFalloff;

        [WriteOnly] public NativeArray<float> Results;

        public void Execute(int index)
        {
            int x = index % Width;
            int y = index / Width;
            float noiseValue = CalculateFbm(new float2(x, y) * NoiseScale);
            float normalizedX = Width > 1 ? x / (float)(Width - 1) * 2f - 1f : 0f;
            float normalizedY = Height > 1 ? y / (float)(Height - 1) * 2f - 1f : 0f;
            float centerMask = math.saturate(1f - math.length(new float2(normalizedX, normalizedY)));
            Results[index] = noiseValue * math.lerp(1f, centerMask, EdgeFalloff);
        }

        private float CalculateFbm(float2 position)
        {
            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < Octaves; i++)
            {
                float value = noise.snoise((Seed + position) * frequency) * 0.5f + 0.5f;
                total += value * amplitude;
                maxValue += amplitude;
                amplitude *= Persistence;
                frequency *= Lacunarity;
            }

            return total / maxValue;
        }
    }
}
