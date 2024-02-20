using UnityEngine;

public class MapDemVO
{
    public MapDemVO(
        string demPath,
        Vector2 elevationMinMax,
        Vector2 terrainDimension,
        Wgs84Info topL,
        Wgs84Info topR,
        Wgs84Info bottomL,
        Wgs84Info bottomR)
    {
        this.demPath = demPath;
        this.elevationMinMax = elevationMinMax;
        this.terrainDimension = terrainDimension;
        this.topL = topL;
        this.topR = topR;
        this.bottomL = bottomL;
        this.bottomR = bottomR;
    }

    public string demPath;
    public Vector2 elevationMinMax;
    public Vector2 terrainDimension;
    public Wgs84Info topL;
    public Wgs84Info topR;
    public Wgs84Info bottomL;
    public Wgs84Info bottomR;
}

public class DEMCoord
{
    public DEMCoord(
        float contrast,
        int x,
        int y)
    {
        this.contrast = contrast;
        this.x = x;
        this.y = y;
    }
    public float contrast;
    public int x;
    public int y;
}
