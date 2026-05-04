using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Worley3DGenerator : MonoBehaviour
{
    struct WorleyData
    {
        public float dist;
        public float secondDist;
    }

    public static Texture3D Generate(int size, int seed)
    {
        // Frequencies (tweak these)
        int lowCells = 4;
        int midCells = 8;
        int highCells = 16;

        System.Random rng = new System.Random(seed);

        // Precompute feature points for each layer
        var bigCloudPoints = GenerateFeaturePoints(lowCells, rng);
        var mediumCloudPoints = GenerateFeaturePoints(midCells, rng);
        var smallCloudPoints = GenerateFeaturePoints(highCells, rng);

        Color[] data = new Color[size * size * size];

        for (int z = 0; z < size; z++)
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int index = x + y * size + z * size * size;

                    Vector3 p = new Vector3(x, y, z) / size;

                    // Scale positions per layer
                    var large = SampleWorley(p, lowCells, bigCloudPoints);
                    var medium = SampleWorley(p, midCells, mediumCloudPoints);
                    var small = SampleWorley(p, highCells, smallCloudPoints);

                    // Normalize distances
                    float largeDist = large.dist;
                    float mediumDist = medium.dist;
                    float smallDist = small.dist;

                    float largeSecondDist = large.secondDist;

                    // --- Build channels ---

                    // Base shape
                    float baseShape = 1.0f - (largeDist * 0.7f + mediumDist * 0.3f);

                    // Edge mask
                    float edge = Mathf.Clamp01(largeSecondDist - largeDist);

                    // High-frequency detail
                    float detail = 1.0f - smallDist;

                    // Optional warp
                    float warp = (float)rng.NextDouble();

                    data[index] = new Color(baseShape, edge, detail, warp);
                }

        Texture3D tex = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
        tex.SetPixels(data);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.Apply();

        return tex;
    }

    // Generate feature points
    private static Vector3[,,] GenerateFeaturePoints(int cells, System.Random rng)
    {
        Vector3[,,] points = new Vector3[cells, cells, cells];

        for (int z = 0; z < cells; z++)
            for (int y = 0; y < cells; y++)
                for (int x = 0; x < cells; x++)
                {
                    points[x, y, z] = new Vector3(
                        (float)rng.NextDouble(),
                        (float)rng.NextDouble(),
                        (float)rng.NextDouble()
                    );
                }

        return points;
    }

    // Sample Worley with tiling
    private static WorleyData SampleWorley(Vector3 p, int cells, Vector3[,,] points)
    {
        int cellX = Mathf.FloorToInt(p.x * cells);
        int cellY = Mathf.FloorToInt(p.y * cells);
        int cellZ = Mathf.FloorToInt(p.z * cells);

        float minDist = float.MaxValue;
        float secondMinDist = float.MaxValue;

        for (int dz = -1; dz <= 1; dz++)
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = (cellX + dx + cells) % cells;
                    int ny = (cellY + dy + cells) % cells;
                    int nz = (cellZ + dz + cells) % cells;

                    Vector3 feature = points[nx, ny, nz];
                    Vector3 featurePos = new Vector3(nx, ny, nz) + feature;

                    Vector3 diff = p * cells - featurePos;

                    // Tile wrapping
                    diff.x -= Mathf.Round(diff.x / cells) * cells;
                    diff.y -= Mathf.Round(diff.y / cells) * cells;
                    diff.z -= Mathf.Round(diff.z / cells) * cells;

                    float dist = diff.sqrMagnitude;

                    if (dist < minDist)
                    {
                        secondMinDist = minDist;
                        minDist = dist;
                    }
                    else if (dist < secondMinDist)
                    {
                        secondMinDist = dist;
                    }
                }

        // Convert back to linear distance
        minDist = Mathf.Sqrt(minDist);
        secondMinDist = Mathf.Sqrt(secondMinDist);

        // Normalize
        float maxDist = Mathf.Sqrt(3); // max possible in cell

        return new WorleyData
        {
            dist = minDist / maxDist,
            secondDist = secondMinDist / maxDist
        };
    }
}
