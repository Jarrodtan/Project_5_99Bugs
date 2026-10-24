using UnityEngine;

/// <summary>
/// Reads ESA WorldCover classes from a PNG clip. Unity puts the image's top row at v = 1,
/// so the north-up PNG from the Python script lines up with terrain v (north = +Z) with no flipping.
/// </summary>
public class HimalayaLandCover
{
    public const int Tree = 10, Shrub = 20, Grass = 30, Crop = 40, Built = 50,
                     Bare = 60, Snow = 70, Water = 80, Wetland = 90, Moss = 100;

    readonly Color32[] pixels;
    readonly int width, height;

    public HimalayaLandCover(Texture2D texture)
    {
        width = texture.width;
        height = texture.height;
        pixels = texture.GetPixels32();   // needs Read/Write enabled and no compression
    }

    public int ClassAt(float u, float v)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(u * width), 0, width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(v * height), 0, height - 1);
        return pixels[y * width + x].r;
    }
}
