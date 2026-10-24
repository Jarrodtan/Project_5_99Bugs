using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>Lat/lon box plus conversions. u runs west to east, v runs south to north (v=1 is +Z, north).</summary>
public readonly struct GeoBounds
{
    public readonly double West, South, East, North;

    public GeoBounds(double west, double south, double east, double north)
    {
        West = west; South = south; East = east; North = north;
    }

    public double WidthMeters => (East - West) * 111320.0 * Math.Cos((South + North) * 0.5 * Math.PI / 180.0);
    public double HeightMeters => (North - South) * 110574.0;

    public Vector2 ToUV(double lat, double lon) =>
        new Vector2((float)((lon - West) / (East - West)), (float)((lat - South) / (North - South)));

    /// <summary>Overpass order: south,west,north,east. Always invariant culture (a Spanish locale would write 27,85).</summary>
    public string ToOverpass() =>
        string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", South, West, North, East);

    /// <summary>Short key for cache file names, so changing the box never reuses stale data.</summary>
    public string Key =>
        string.Format(CultureInfo.InvariantCulture, "{0:F3}_{1:F3}_{2:F3}_{3:F3}", West, South, East, North);
}

/// <summary>Web Mercator maths used by slippy-map tiles (Terrarium elevation tiles).</summary>
public static class HimalayaGeo
{
    public static double WorldPixelX(double lon, int zoom) =>
        (lon + 180.0) / 360.0 * 256.0 * Math.Pow(2, zoom);

    public static double WorldPixelY(double lat, int zoom)
    {
        double r = lat * Math.PI / 180.0;
        return (1.0 - Math.Log(Math.Tan(r) + 1.0 / Math.Cos(r)) / Math.PI) / 2.0 * 256.0 * Math.Pow(2, zoom);
    }
}

/// <summary>Disk cache in persistentDataPath so each download happens once.</summary>
public static class GeoCache
{
    static string Dir
    {
        get
        {
            var d = Path.Combine(Application.persistentDataPath, "HimalayaCache");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    public static bool TryReadBytes(string file, out byte[] data)
    {
        var p = Path.Combine(Dir, file);
        if (File.Exists(p)) { data = File.ReadAllBytes(p); return true; }
        data = null;
        return false;
    }

    public static void WriteBytes(string file, byte[] data) => File.WriteAllBytes(Path.Combine(Dir, file), data);

    public static bool TryReadText(string file, out string text)
    {
        var p = Path.Combine(Dir, file);
        if (File.Exists(p)) { text = File.ReadAllText(p); return true; }
        text = null;
        return false;
    }

    public static void WriteText(string file, string text) => File.WriteAllText(Path.Combine(Dir, file), text);

    public static void ClearAll()
    {
        var d = Path.Combine(Application.persistentDataPath, "HimalayaCache");
        if (Directory.Exists(d)) Directory.Delete(d, true);
    }
}
