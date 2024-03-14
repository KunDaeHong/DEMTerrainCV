using System;

using UnityEngine;

using Newtonsoft.Json;

public struct TileInfo : IEquatable<TileInfo>
{
    public int lat;
    public int lon;
    public int zoom;

    public TileInfo(int lat, int lon, int zoom)
    {
        this.lat = lat;
        this.lon = lon;
        this.zoom = zoom;
    }

    public bool Equals(TileInfo other)
    {
        return lat == other.lat && lon == other.lon && zoom == other.zoom;
    }
}

public struct TileSprite
{
    public TileInfo tileInfo;
    public Sprite tileImage;

    public TileSprite(TileInfo tileInfo, Sprite tileImage)
    {
        this.tileInfo = tileInfo;
        this.tileImage = tileImage;
    }
}

//구조체에서 생성자 사용 시 C# 10.0 버전 이상이 필요.
public class Wgs84Info : IEquatable<Wgs84Info>
{
    public Wgs84Info(double lat, double lon, int zoom)
    {
        this.lat = lat;
        this.lon = lon;
        this.zoom = zoom;
    }

    [JsonProperty("latitude")]
    public double lat;
    [JsonProperty("longitude")]
    public double lon;
    [JsonProperty("zoomLv")]
    public int zoom;

    public bool Equals(Wgs84Info other)
    {
        return this.lat == other.lat && this.lon == other.lon;
    }
}
