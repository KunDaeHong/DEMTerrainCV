using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GISTech.GISTerrainLoader;
using BitMiracle.LibTiff.Classic;
using ImageMagick;

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

    //MARK: FILE MANAGER
    private static Texture2D loadTiffToTexture2D(string filePath)
    {
        Tiff tiff = Tiff.Open(filePath, "r"); //tiff 읽기
        int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
        int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
        int BITSPERSAMPLE = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
        int samplesPerPixel = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt(); //픽셀 색상레이어 수
        List<float[,]> heightData = new List<float[,]>();

        for (int band = 0; band < samplesPerPixel; band++)
        {
            var list = new float[width, height];
            heightData.Add(list);
        }

        var scanline = new byte[tiff.ScanlineSize()];
        var scanlineFloat = new float[tiff.ScanlineSize()];

        int BandCounter = 0; //현재 픽셀 색상 레이어
        int C_BandCounter = 0;
        int R_BandCounter = 0;

        for (int i = 0; i < height; i++)
        {
            tiff.ReadScanline(scanline, i); //scanline은 byte데이터로 저장됨.

            for (int j = 0; j < width * samplesPerPixel; j++)
            {
                Buffer.BlockCopy(scanline, 0, scanlineFloat, 0, scanline.Length); // Buffer.BlockCopy로 byte -> 부동소숫점 형태로 변환하여 저장
                float el = Convert.ToSingle(scanlineFloat[j]); // 부동소숫점으로 변환

                if (BandCounter >= samplesPerPixel)
                {
                    BandCounter = 0;
                    C_BandCounter++;

                    if (C_BandCounter > width - 1)
                    {
                        C_BandCounter = 0;
                        R_BandCounter++;
                    }
                }

                heightData[BandCounter][C_BandCounter, R_BandCounter] = el;
                BandCounter++;
            }
        }

        Color[] colors = new Color[width * height];

        for (int r = 0; r < width; r++)
        {
            for (int c = 0; c < height; c++)
            {
                float value = heightData[0][r, c];
                Color color = new Color(value, value, value);
                colors[c * width + r] = color;
            }
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels(colors);
        texture.Apply(true);

        /**
        Unity Editor에서 tif을 사용 시 자동으로 정사각형사이즈로 변환 후 
        알파색채널은 무시하고 RGB로 BC6H(HDR) Compression을 하여 최대한 평평하게 나오도록 변경.
        **/

        return texture;
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

        yield return makeTerrain(loadTiffToTexture2D(mapDemVOs.demPath), tData);
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
                heightValues[cX, cY] = terrainSmoothAvg(heightValues, cX, cY, 0.9f);
            }
        }

        //바이레터럴 필터
        Task bilateralFilterTask = bilateralFilterCoroutine(heightValues);
        yield return new WaitUntil(() => bilateralFilterTask.IsCompleted);

        //median 필터링으로 필요없는 부분은 날리고 필요한 부분은 채움
        //yield return medianFilteringCoroutine(gameClass, heightValues, 3);
        Task filterTask = testMedianFilteringCoroutine(heightValues, 3);
        yield return new WaitUntil(() => filterTask.IsCompleted);

        //상관 필터링 부드럽게 경계처리
        for (int cX = 0; cX < heightValues.GetLength(0); cX++)
        {
            for (int cY = 0; cY < heightValues.GetLength(1); cY++)
            {
                heightValues[cX, cY] = terrainSmoothAvg(heightValues, cX, cY, 0.9f);
            }
        }

        tData.SetHeights(0, 0, heightValues);
        isLoadingDEM = false;
        Debug.Log("Finish Filtering DEM");
    }

    //싱글스레드
    private static IEnumerator medianFilteringCoroutine(MonoBehaviour gameClass, float[,] convolutionList, int smoothFac)
    {
        YieldCollection manager = new YieldCollection();

        for (int i = 1; i <= smoothFac; i++)
        {
            //gameClass.StartCoroutine(manager.CountCoroutine(terrainSmoothHistogram(convolutionList)));
        }

        yield return manager;
    }

    //bilateral filter Task
    private static async Task<bool> bilateralFilterCoroutine(float[,] convolutionList)
    {
        List<Task> tasks = new()
        {
            Task.Run(() =>
            {
                bilateralFilter(convolutionList, 5, 3, 7);
            })
        };

        await Task.WhenAll(tasks);

        return true;
    }

    //medianFilter MultiTask
    private static async Task<bool> testMedianFilteringCoroutine(float[,] convolutionList, int smoothFac)
    {
        List<Task> tasks = new();

        for (int i = 1; i <= smoothFac; i++)
        {
            await Task.Delay(3000);
            tasks.Add(Task.Run(() =>
            {
                terrainSmoothHistogram(convolutionList);
            }));
        }

        await Task.WhenAll(tasks);

        return true;
    }

    //This function will returns average smooth value.
    //상관 필터링 작업
    private static float terrainSmoothAvg(float[,] convolutionList, int x, int y, float smoothFac)
    {
        Vector2 maxRowCol = new Vector2(convolutionList.GetLength(0) - 1, convolutionList.GetLength(1) - 1);
        float avg = 0f;
        float total = 0f;

        for (int cX = x - 1; cX < x + 1; cX++)
        {
            for (int cY = y - 1; cY < y + 1; cY++)
            {
                if (isBoundary(cX, cY, maxRowCol))
                {
                    avg += convolutionList[cX, cY] * smoothFac;
                    total += 1.0f;
                }
            }
        }

        return avg / total;
    }

    // median 필터링 작업 (3by3 kernel)
    //while에서 for문으로 변경 (이유: 속도가 매우 느려서...)
    private static void terrainSmoothHistogram(float[,] convolutionList)
    {
        Vector2 maxRowCol = new Vector2(convolutionList.GetLength(0) - 1, convolutionList.GetLength(1) - 1);
        Dictionary<string, int> usedCoordsDic = new Dictionary<string, int>();

        for (int cX = 0; cX < maxRowCol.x; cX++)
        {
            for (int cY = 0; cY < maxRowCol.y; cY++)
            {
                string coordString = $"{cX}/{cY}";

                if (usedCoordsDic.ContainsKey(coordString))
                {
                    cY++;
                    continue;
                }

                Dictionary<int, int> frequencyDic = new Dictionary<int, int>();
                List<float> contrastList = new List<float>();
                List<DEMCoord> coords = new List<DEMCoord>(); // Maximum 9 elements
                DEMCoord min;
                DEMCoord max;

                for (int x = cX - 1; x <= cX + 1; x++)
                {
                    for (int y = cY - 1; y <= cY + 1; y++)
                    {
                        if (isBoundary(x, y, maxRowCol))
                        {
                            DEMCoord coord = new DEMCoord(convolutionList[x, y], x, y);
                            coords.Add(coord);
                            contrastList.Add(convolutionList[x, y]);
                            usedCoordsDic[$"{x}/{y}"] = 1;
                        }
                    }
                }

                min = coords.OrderBy(i => i.contrast).FirstOrDefault();
                max = coords.OrderByDescending(i => i.contrast).FirstOrDefault();

                if (min != null)
                {
                    float mostFrequentNumber = coords.Sum(i => i.contrast) / coords.Count;
                    convolutionList[min.x, min.y] = mostFrequentNumber;
                }

                if (min != null && max != null && coords.Count == 9 && max.contrast > 0)
                {
                    DEMCoord[,] window = new DEMCoord[3, 3];

                    int idx = 0;

                    for (int x = 0; x < window.GetLength(0); x++)
                    {
                        for (int y = 0; y < window.GetLength(1); y++)
                        {
                            window[x, y] = coords[idx];
                            idx++;
                        }
                    }

                    if (window[1, 1].contrast < max.contrast && window[1, 1].contrast > min.contrast)
                    {
                        convolutionList[window[1, 1].x, window[1, 1].y] = max.contrast;
                    }
                }
            }
        }
    }

    //bilateral filter (specific kernel)
    private static void bilateralFilter(float[,] grayScaleFloat, int kernelSize, int spaceWeight, int colorWeight)
    {
        Vector2 maxRowCol = new Vector2(grayScaleFloat.GetLength(0) - 1, grayScaleFloat.GetLength(1) - 1);

        for (int y = 0; y < maxRowCol.y; y++)
        {
            for (int x = 0; x < maxRowCol.x; x++)
            {
                float sumWeight = 0;
                float sumIntensity = 0;
                float currentColorFloat = grayScaleFloat[x, y]; //현재 색상

                for (int c = -kernelSize; c < kernelSize; c++)
                {
                    for (int r = -kernelSize; r < kernelSize; r++)
                    {
                        if (isBoundary(x + c, y + r, maxRowCol)) // 현재 픽셀이 이미지의 픽셀안에 존재 하는지 체크
                        {
                            //선택된 주변 픽셀 색상
                            float currentNeighborColor = grayScaleFloat[x + c, y + r];
                            //공간 가중치 (타겟 픽셀과 가까울 수록 값이 커짐)
                            double nSpaceWeight = Math.Exp(-Math.Sqrt(Math.Pow(x + c - x, 2.0) + Math.Pow(y + r - y, 2.0)) / 2 * Math.Pow(spaceWeight, 2.0));
                            //색상 가중치 (타겟 픽셀과 색상이 비슷할 수록 값이 커짐)
                            double nColorIntensity = Math.Exp(-Math.Abs(currentColorFloat - currentNeighborColor) / 2 * Math.Pow(colorWeight, 2.0));

                            //kernel에 있는 가중을 모두 더함
                            sumWeight += (float)(nSpaceWeight * nColorIntensity);
                            //전체 가중치
                            sumIntensity += (float)(nSpaceWeight * nColorIntensity * currentNeighborColor);
                        }
                    }
                }
                //가중평균 구함
                grayScaleFloat[x, y] = (float)(sumIntensity / sumWeight);
            }
        }
    }


    // This function will returns bool if pixel is in the dem boundary size.
    private static bool isBoundary(int x, int y, Vector2 maxRowCol)
    {
        return ((x >= 0 && x < maxRowCol.x) && (y >= 0 && y < maxRowCol.y));
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
