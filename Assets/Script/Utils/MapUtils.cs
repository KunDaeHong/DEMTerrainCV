using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GISTech.GISTerrainLoader;
using BitMiracle.LibTiff.Classic;

using UnityEngine;

public class MapUtils
{

    private static RuntimeTerrainGenerator runtimeGenerator;
    private static GISTerrainLoaderRuntimePrefs runtimePrefs;
    public static bool isLoadingDEM = false;

    //기본 17배율 줌 값 셋팅
    public static TileInfo getTileListFromDEM(
        Wgs84Info topL,
        Wgs84Info topR,
        Wgs84Info bottomL,
        Wgs84Info bottomR)
    {

        int tileZoom = 17;
        TileInfo tileTopL = wgs84ToTile(topL.lon, topL.lat, tileZoom);
        TileInfo tileTopR = wgs84ToTile(topR.lon, topR.lat, tileZoom);
        TileInfo tileBottomL = wgs84ToTile(bottomL.lon, bottomL.lat, tileZoom);
        TileInfo tileBottomR = wgs84ToTile(bottomR.lon, bottomR.lat, tileZoom);

        while (true)
        {
            if (tileTopL.Equals(tileTopR) && tileTopL.Equals(tileBottomL) && tileTopL.Equals(tileBottomR))
            {
                break;
            }

            tileZoom--;

            tileTopL = wgs84ToTile(topL.lon, topL.lat, tileZoom);
            tileTopR = wgs84ToTile(topR.lon, topR.lat, tileZoom);
            tileBottomL = wgs84ToTile(bottomL.lon, bottomL.lat, tileZoom);
            tileBottomR = wgs84ToTile(bottomR.lon, bottomR.lat, tileZoom);
        }
        Debug.Log("Got tile numbers for 4 wgs84 coords.");

        return tileTopL;
    }

    //This function returns every 19 level tiles image in tile number
    public static void getTilesInTile()
    {

    }

    // This function returns the NW-Corner of the tile.
    public static Wgs84Info tile2Wgs84(int lon, int lat, int zoom)
    {
        int zoomN = 1 << zoom;
        double lon_deg = lon / zoomN * 360.0 - 180;
        double lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * lat / zoomN)));
        double lat_deg = lat_rad * (180 / Math.PI);

        return new Wgs84Info(lat_deg, lon_deg, 0);
    }

    // This function returns the center of the tile.
    public static Wgs84Info tile2Wgs84Center(int lon, int lat, int zoom)
    {
        int zoomN = 1 << zoom;
        double lon_deg = (lon + 0.5) / zoomN * 360.0 - 180;
        double lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (lat + 0.5) / zoomN)));
        double lat_deg = lat_rad * (180 / Math.PI);

        return new Wgs84Info(lat_deg, lon_deg, 0);
    }

    public static Wgs84Info centerWithWgs84(List<Wgs84Info> coordinates)
    {
        if (coordinates.Count < 4)
        {
            throw new ArgumentException("List of Wgs84Info coordinates must be need 4 at least");
        }

        double sumLat = 0.0;
        double sumLon = 0.0;

        foreach (var coord in coordinates)
        {
            sumLat += coord.lat;
            sumLon += coord.lon;
        }

        double avgLat = sumLat / coordinates.Count;
        double avgLon = sumLon / coordinates.Count;

        return new Wgs84Info(avgLat, avgLon, 0);
    }

    public static Vector2 tileToPixel(TileInfo tileCoord, Wgs84Info wgs84Coord)
    {
        const double tilePixelSize = 256;

        double merX = tileCoord.lon / Math.Pow(2, tileCoord.zoom) * 360 - 180;
        double merY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileCoord.lat / Math.Pow(2, tileCoord.zoom)))) * 180 / Math.PI;

        double pX = (double)((wgs84Coord.lon - merX) * Math.Pow(2, tileCoord.zoom) * tilePixelSize / 360);
        double py = (double)((merY - wgs84Coord.lat) * Math.Pow(2, tileCoord.zoom) * tilePixelSize / 360);

        Debug.Log("finish with pixel coordinate");
        return new Vector2((float)pX, (float)py);
    }

    public static TileInfo wgs84ToTile(double lon, double lat, int zoom)
    {
        double latRad = lat * ((float)Math.PI / 180f);
        int zoomN = 1 << zoom;

        int lonTileNum = (int)Math.Floor((float)((lon + 180) / 360 * zoomN));
        int latTileNum = (int)Math.Floor((float)((1.0 - Math.Asinh(Math.Tan(latRad)) / Math.PI) / 2.0 * zoomN));

        if (lonTileNum < 0)
        {
            lonTileNum = 0;
        }

        if (lonTileNum >= zoomN)
        {
            lonTileNum = zoomN - 1;
        }

        if (latTileNum < 0)
        {
            latTileNum = 0;
        }

        if (latTileNum >= zoomN)
        {
            latTileNum = zoomN - 1;
        }

        TileInfo tileInfo = new TileInfo();
        tileInfo.lat = latTileNum;
        tileInfo.lon = lonTileNum;
        tileInfo.zoom = zoom;

        return tileInfo;
    }

    /**
    // TODO:대충 13 == x 레벨에서 전체로 가져온다라고 가정 할 시

    구글 지도는 19레벨이 최대 해상도이고 19 - 현재 타일의 지도 레벨(x)
    그럼 6이 나온다. 1부터 6까지 수열로 곱한다. 대략 64가 나오고 각 타일은 줌을 할 시 2제곱
    그럼 13레벨을 64개의 조각을 나누어서 각 사각형을 만든 후 해당 사각형의 센터 좌표를 다시 wgs84좌표로 변환
    
    wgs84좌표를 다시 타일 좌표로 변환 후 구글 satellite 타일 이미지로 64개에 대한 타일 요청 후 64개의 타일을 순서대로 접합

    **/

    //MARK: GIS TECH DEM Loader
    public static void makeTerrain(String terrainPath, Vector2 elevationMinMax, Vector2 terrainDimension, MonoBehaviour gameClass)
    {
        isLoadingDEM = true;
        //WebGL
        // InitializingRuntimePrefs(terrainPath);
        // gameClass.StartCoroutine(runtimeGenerator.StartGenerating());

        if (!string.IsNullOrEmpty(terrainPath) && System.IO.File.Exists(terrainPath))
        {
            runtimeGenerator = GISTech.GISTerrainLoader.RuntimeTerrainGenerator.Get;
            runtimePrefs = GISTech.GISTerrainLoader.GISTerrainLoaderRuntimePrefs.Get;

            RuntimeTerrainGenerator.OnFinish += OnTerrainGeneratingCompleted;
            InitializingRuntimePrefs(terrainPath, elevationMinMax, terrainDimension);
            gameClass.StartCoroutine(runtimeGenerator.StartGenerating());
        }
        else
        {
            Debug.LogError("Terrain file null or not supported.. Try againe");
            return;
        }
    }

    //DEM은 Bands Data별로 크기를 지정합니다.
    private static void InitializingRuntimePrefs(string TerrainPath, Vector2 elevationMinMax, Vector2 terrainDimensions)
    {
        runtimeGenerator.Error = false;
        runtimeGenerator.enabled = true;
        runtimeGenerator.TerrainFilePath = TerrainPath;
        runtimeGenerator.RemovePrevTerrain = false;

        runtimePrefs.readingMode = ReadingMode.Full;
        runtimePrefs.EPSGCode = 5187;
        runtimePrefs.tiffElevationSource = TiffElevationSource.DEM;
        runtimePrefs.TerrainElevation = TerrainElevation.RealWorldElevation;
        runtimePrefs.TerrainFixOption = FixOption.ManualFix;
        runtimePrefs.TerrainMaxMinElevation = elevationMinMax;
        runtimePrefs.terrainScale = new Vector3(0.025f, 0, 0.025f);
        runtimePrefs.terrainDimensionMode = TerrainDimensionsMode.Manual;
        runtimePrefs.TerrainHasDimensions = false;
        runtimePrefs.TerrainDimensions = terrainDimensions;
        runtimePrefs.heightmapResolution = 2049;
        runtimePrefs.detailResolution = 512;
        runtimePrefs.resolutionPerPatch = 16;
        runtimePrefs.baseMapResolution = 2048;
        runtimePrefs.PixelError = 1f;
        runtimePrefs.BaseMapDistance = 450;
        //runtimePrefs.terrainCount = Vector2Int.one;
        runtimePrefs.textureMode = TextureMode.WithoutTexture;
        runtimePrefs.UseTerrainHeightSmoother = true;
        runtimePrefs.TerrainHeightSmoothFactor = 0.6f;
        runtimePrefs.UseTerrainSurfaceSmoother = true;
        runtimePrefs.TerrainSurfaceSmoothFactor = 8;
        runtimePrefs.vectorType = VectorType.OpenStreetMap;
    }

    private static void OnTerrainGeneratingCompleted()
    {

        isLoadingDEM = false;
    }


    //MARK: Daehyeon Hong DEM Loader
    public static IEnumerator makeDEM(MapDemVO mapDemVOs, MonoBehaviour gc)
    {
        var tData = new TerrainData();
        var dimension = mapDemVOs.terrainDimension;
        isLoadingDEM = true;

        tData.SetDetailResolution(512, 16);
        tData.heightmapResolution = 2049;
        tData.baseMapResolution = 2048;

        TileInfo mapTile = getTileListFromDEM(mapDemVOs.topL, mapDemVOs.topR, mapDemVOs.bottomL, mapDemVOs.bottomR);
        Vector2 topLP = tileToPixel(mapTile, mapDemVOs.topL);
        Vector2 bottomLP = tileToPixel(mapTile, mapDemVOs.bottomL);
        Vector2 bottomRP = tileToPixel(mapTile, mapDemVOs.bottomR);
        tData.size = new Vector3(bottomRP.x - bottomLP.x, 0.3f, bottomLP.y - topLP.y);
        int index = 1;
        //yield return new WaitUntil(() => loadHeightMap.IsCompleted);

        while (GameObject.Find("Terrains " + index.ToString()))
        {
            index++;
        }

        var container = Terrain.CreateTerrainGameObject(tData);
        container.name = "Terrains " + index.ToString();
        container.gameObject.SetActive(true);
        container.transform.position = new Vector3(0, 0, 0);
        container.transform.localScale = new Vector3(1, 1, 1);
        container.GetComponent<Terrain>().heightmapPixelError = 1;
        container.GetComponent<Terrain>().basemapDistance = 120;
        container.GetComponent<Terrain>().terrainData = tData;

        yield return makeTerrain(CVUtils.loadTiffToTexture2D(mapDemVOs.demPath), tData);
    }

    public static IEnumerator makeTerrain(Texture2D heightMap, TerrainData tData)
    {
        int tWidth = tData.heightmapResolution;
        int tHeight = tData.heightmapResolution;
        float[,] heightValues = tData.GetHeights(0, 0, tWidth, tHeight);

        for (int terrainY = 0; terrainY < tHeight; terrainY++)
        {
            if (terrainY >= heightMap.height)
            {
                break;
            }

            for (int terrainX = 0; terrainX < tWidth; terrainX++)
            {
                if (terrainX >= heightMap.width)
                {
                    break;
                }

                Color heightColor = heightMap.GetPixel(terrainY, terrainX);
                heightValues[terrainX, terrainY] = heightColor.r;
            }
        }

        tData.SetHeights(0, 0, heightValues);

        //상관 필터링 부드럽게 경계처리
        for (int cX = 0; cX < heightValues.GetLength(0); cX++)
        {
            for (int cY = 0; cY < heightValues.GetLength(1); cY++)
            {
                heightValues[cX, cY] = CVUtils.terrainSmoothAvg(heightValues, cX, cY, 0.9f);
            }
        }

        //바이레터럴 필터
        Task bilateralFilterTask = CVUtils.bilateralFilterCoroutine(heightValues);
        yield return new WaitUntil(() => bilateralFilterTask.IsCompleted);

        //median 필터링으로 필요없는 부분은 날리고 필요한 부분은 채움
        //yield return medianFilteringCoroutine(gameClass, heightValues, 3);
        Task filterTask = CVUtils.testMedianFilteringCoroutine(heightValues, 3);
        yield return new WaitUntil(() => filterTask.IsCompleted);

        //상관 필터링 부드럽게 경계처리
        for (int cX = 0; cX < heightValues.GetLength(0); cX++)
        {
            for (int cY = 0; cY < heightValues.GetLength(1); cY++)
            {
                heightValues[cX, cY] = CVUtils.terrainSmoothAvg(heightValues, cX, cY, 0.9f);
            }
        }

        tData.SetHeights(0, 0, heightValues);
        isLoadingDEM = false;
        Debug.Log("Finish Filtering DEM");
    }
}

public class YieldCollection : CustomYieldInstruction
{
    int _count;

    //Each time you call this, you call the coroutine and count is increased until the end.
    public IEnumerator CountCoroutine(IEnumerator coroutine)
    {
        _count++;
        yield return coroutine;
        _count--;
    }

    //If count is 0,no one coroutine is running.
    public override bool keepWaiting => _count != 0;
}

public class TIFFBandsData
{
    public List<float[,]> BandsData = new List<float[,]>();

}
