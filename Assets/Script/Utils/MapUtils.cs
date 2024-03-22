using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

using GISTech.GISTerrainLoader;

using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapUtils
{
    public class MapLoadUtils
    {

        private static RuntimeTerrainGenerator runtimeGenerator;
        private static GISTerrainLoaderRuntimePrefs runtimePrefs;
        public static bool isLoadingDEM = false;
        public static TileInfo getTileListFromDEM(
            Wgs84Info topL,
            Wgs84Info topR,
            Wgs84Info bottomL,
            Wgs84Info bottomR)
        {

            int tileZoom = 20;
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
        //
        public static List<TileInfo> getTilesInTile(TileInfo tileInfo, int maxZoom)
        {
            int lon = tileInfo.lon;
            int lat = tileInfo.lat;
            int zoom = tileInfo.zoom;

            if (zoom >= maxZoom)
            {
                throw new Exception("The maximum size for Google Maps stellite images is 19 level.");
            }

            int zoomDiff = maxZoom - zoom;
            int zoomMultiples = 1 << zoomDiff;

            int nLon = lon * zoomMultiples;
            int nLat = lat * zoomMultiples;

            List<TileInfo> tileList = new List<TileInfo>();

            for (int y = 0; y < zoomMultiples; y++)
            {
                for (int x = 0; x < zoomMultiples; x++)
                {
                    TileInfo newTile = new TileInfo(nLat + y, nLon + x, maxZoom);
                    tileList.Add(newTile);
                }
            }

            return tileList;
        }

        public static List<TileInfo> getTilesWithNum(TileInfo startTile, TileInfo endTile)
        {
            List<TileInfo> tiles = new List<TileInfo>();

            for (int y = startTile.lat; y < endTile.lat; y++)
            {
                for (int x = startTile.lon; x < endTile.lon; x++)
                {
                    TileInfo newTile = new TileInfo(y, x, startTile.zoom);
                    tiles.Add(newTile);
                }
            }

            return tiles;
        }

        // This function returns the NW-Corner of the tile.
        public static Wgs84Info tile2Wgs84(int lon, int lat, int zoom)
        {
            double merX = lon / Math.Pow(2, zoom) * 360 - 180;
            double merY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * lat / Math.Pow(2, zoom)))) * 180 / Math.PI;

            return new Wgs84Info(merY, merX, 0);
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

        public static Vector2 tileToPixel(TileInfo tileCoord, Wgs84Info wgs84Coord, double tilePixelSize)
        {
            // Earth Radius
            double earthR = 6371e3;

            //Tile WGS84 Coordinate
            //It will returns the coordinate of the upper left point.
            double merX = tileCoord.lon / Math.Pow(2, tileCoord.zoom) * 360 - 180;
            double merY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileCoord.lat / Math.Pow(2, tileCoord.zoom)))) * 180 / Math.PI;
            //It will returns the coordinate of the bottom left point.
            double merYB = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileCoord.lat + 1) / Math.Pow(2, tileCoord.zoom)))) * 180 / Math.PI;

            // float pX = (float)((wgs84Coord.lon - merX) * Math.Pow(2, tileCoord.zoom) * tilePixelSize / 360);
            // float pY = (float)((merY - wgs84Coord.lat) * Math.Pow(2, tileCoord.zoom) * tilePixelSize / 360);

            //Radian
            float lat1 = (float)((wgs84Coord.lat * Math.PI) / 180); // specific lat
            float lat2 = (float)((merY * Math.PI) / 180); // merY lat
            float lat3 = (float)((merYB * Math.PI) / 180); // merYB lat
            float lonCos = (float)((merX - wgs84Coord.lon) * Math.PI / 180); // specific lon Radian
            float lonCosSame = (float)((merX - merX) * Math.PI / 180); // merX lon cos Radian

            float distX = (float)(Math.Acos(Math.Sin(lat1) * Math.Sin(lat1) + Math.Cos(lat1) * Math.Cos(lat1) * Math.Cos(lonCos)) * earthR);
            float distY = (float)(Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lonCosSame)) * earthR);
            float merYBDist = (float)(Math.Acos(Math.Sin(lat3) * Math.Sin(lat2) + Math.Cos(lat3) * Math.Cos(lat2) * Math.Cos(lonCosSame)) * earthR);

            float pYN = (float)((distY * tilePixelSize) / merYBDist);
            float pXN = (float)((distX * tilePixelSize) / merYBDist);

            Debug.Log("finish with pixel coordinate");
            return new Vector2(pXN, pYN);
        }

        public static Vector2 tileToMeterCoord(TileInfo tileCoord, Wgs84Info wgs84Coord) // tile2Wgs84 함수 사용 필수
        {
            // Earth Radius
            double earthR = 6371e3;

            //Tile WGS84 Coordinate
            //It will returns the coordinate of the upper left point.
            double merX = tileCoord.lon / Math.Pow(2, tileCoord.zoom) * 360 - 180;
            double merY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileCoord.lat / Math.Pow(2, tileCoord.zoom)))) * 180 / Math.PI;
            //It will returns the coordinate of the bottom left point.
            double merYB = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileCoord.lat + 1) / Math.Pow(2, tileCoord.zoom)))) * 180 / Math.PI;

            // float pX = (float)((wgs84Coord.lon - merX) * Math.Pow(2, tileCoord.zoom) * tilePixelSize / 360);
            // float pY = (float)((merY - wgs84Coord.lat) * Math.Pow(2, tileCoord.zoom) * tilePixelSize / 360);

            //Radian
            float lat1 = (float)((wgs84Coord.lat * Math.PI) / 180); // specific lat
            float lat2 = (float)((merY * Math.PI) / 180); // merY lat
            float lat3 = (float)((merYB * Math.PI) / 180); // merYB lat
            float lonCos = (float)((merX - wgs84Coord.lon) * Math.PI / 180); // specific lon Radian
            float lonCosSame = (float)((merX - merX) * Math.PI / 180); // merX lon cos Radian

            float distX = (float)(Math.Acos(Math.Sin(lat1) * Math.Sin(lat1) + Math.Cos(lat1) * Math.Cos(lat1) * Math.Cos(lonCos)) * earthR);
            float distY = (float)(Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lonCosSame)) * earthR);

            Debug.Log("finish with pixel coordinate");
            return new Vector2(distX, distY);
        }

        public static float tileDist(TileInfo tileCoord)
        {
            // Earth Radius
            double earthR = 6371e3;

            //Tile WGS84 Coordinate
            //It will returns the coordinate of the upper left point.
            double merX = tileCoord.lon / Math.Pow(2, tileCoord.zoom) * 360 - 180;
            double merY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileCoord.lat / Math.Pow(2, tileCoord.zoom)))) * 180 / Math.PI;
            //It will returns the coordinate of the bottom left point.
            double merYB = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileCoord.lat + 1) / Math.Pow(2, tileCoord.zoom)))) * 180 / Math.PI;

            //Radian
            float lat2 = (float)((merY * Math.PI) / 180); // merY lat
            float lat3 = (float)((merYB * Math.PI) / 180); // merYB lat
            float lonCosSame = (float)((merX - merX) * Math.PI / 180); // merX lon cos Radian
            float merYBDist = (float)(Math.Acos(Math.Sin(lat3) * Math.Sin(lat2) + Math.Cos(lat3) * Math.Cos(lat2) * Math.Cos(lonCosSame)) * earthR);

            return merYBDist;
        }

        public static float wgs84Dist(Wgs84Info fP, Wgs84Info lP)
        {
            // Earth Radius
            double earthR = 6371e3;

            float lat1 = (float)((lP.lat * Math.PI) / 180);
            float lat2 = (float)((fP.lat * Math.PI) / 180);
            float lonCos = (float)((fP.lon - lP.lon) * Math.PI / 180);

            float dist = (float)(Math.Acos(Math.Sin(lat2) * Math.Sin(lat1) + Math.Cos(lat2) * Math.Cos(lat1) * Math.Cos(lonCos)) * earthR);

            return dist;
        }

        public static TileInfo wgs84ToTile(double lon, double lat, int zoom, bool google = false)
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

            if (google)
            {
                int zoomG = (1 << zoom) - 1;
                tileInfo.lat = zoomG - tileInfo.lat;
            }

            return tileInfo;
        }

        public static TileInfo googleTileToTMS(TileInfo googleF)
        {
            TileInfo tileInfo = googleF;
            int zoomG = (1 << tileInfo.zoom) + 1;
            tileInfo.lat = zoomG - tileInfo.lat;

            return tileInfo;
        }


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
            Vector2 topLP = tileToPixel(mapTile, mapDemVOs.topL, 256);
            Vector2 bottomLP = tileToPixel(mapTile, mapDemVOs.bottomL, 256);
            Vector2 bottomRP = tileToPixel(mapTile, mapDemVOs.bottomR, 256);
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
            container.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
            container.transform.localScale = new Vector3(1, 1, 1);
            container.GetComponent<Terrain>().heightmapPixelError = 1;
            container.GetComponent<Terrain>().basemapDistance = 120;
            container.GetComponent<Terrain>().terrainData = tData;

            Texture2D heightMap = CVUtils.loadTiffToTexture2D(mapDemVOs.demPath);
            Texture2D heightMapResize = CVUtils.resizeTexture2D(heightMap, 2048, 2048);

            yield return makeTerrain(heightMapResize, tData);
        }

        public static IEnumerator makeTerrain(Texture2D heightMap, TerrainData tData)
        {
            int tWidth = heightMap.width;
            int tHeight = heightMap.height;
            float[,] heightValues = new float[tHeight, tWidth];

            for (int terrainY = 0; terrainY < tHeight; terrainY++)
            {
                for (int terrainX = 0; terrainX < tWidth; terrainX++)
                {
                    Color heightColor = heightMap.GetPixel(terrainX, terrainY - tHeight);
                    heightValues[terrainY, terrainX] = heightColor.r;
                }
            }

            //tData.SetHeights(0, 0, heightValues);

            ///** 주석을 해제 시 자동으로 전체 주석처리 됩니다.

            //상관 필터링 부드럽게 경계처리
            for (int cX = 0; cX < heightValues.GetLength(0); cX++)
            {
                for (int cY = 0; cY < heightValues.GetLength(1); cY++)
                {
                    heightValues[cX, cY] = CVUtils.terrainSmoothAvg(heightValues, cX, cY, 0.9f);
                }
            }

            //바이레터럴 필터로 필요없는 부분은 날리고 필요한 부분 미리 채움
            Task bilateralFilterTask = CVUtils.bilateralFilterCoroutine(heightValues);
            yield return new WaitUntil(() => bilateralFilterTask.IsCompleted);

            //median필터로 필요한 부분은 채움
            Task filterTask = CVUtils.testMedianFilteringCoroutine(heightValues, 5);
            yield return new WaitUntil(() => filterTask.IsCompleted);

            //상관 필터링 부드럽게 경계처리
            for (int cX = 0; cX < heightValues.GetLength(0); cX++)
            {
                for (int cY = 0; cY < heightValues.GetLength(1); cY++)
                {
                    heightValues[cX, cY] = CVUtils.terrainSmoothAvg(heightValues, cX, cY, 0.9f);
                }
            }

            // **/
            tData.SetHeights(0, 0, heightValues);
            isLoadingDEM = false;
            Debug.Log("Finish Filtering DEM");

            /**
            Texture2D demConverted = CVUtils.hightValue2Texture2D(heightValues);
            string directoryPath = @Application.streamingAssetsPath + "/tileImage/";
            if (Directory.Exists(directoryPath) == false)
            {
                Directory.CreateDirectory(directoryPath);
            }

            var pngData = demConverted.EncodeToJPG();
            var path = @Application.streamingAssetsPath + "/tileImage/" + "dem" + ".jpg";
            File.WriteAllBytes(path, pngData);
            //**/
        }


        public static IEnumerator makePipe()
        {

            yield return "";
        }
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
