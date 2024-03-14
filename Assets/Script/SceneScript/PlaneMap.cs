using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using System.IO;

using UnityEngine;
using UnityEngine.UI;

using Newtonsoft.Json;

public class PlaneMap : MonoBehaviour
{
    [SerializeField]
    private Camera mainCam;
    [SerializeField]
    private Texture2D mapMainTexture;
    private bool isLoadingMap = false;
    private int mapTileCnt = 0;

    private void Start()
    {
        GameObject.Find("LoadingTitleBar").GetComponent<Image>().enabled = false;
        GameObject.Find("LoadingTitle").GetComponent<Text>().enabled = false;

        /** 
        ExtensionFilter[] extensions = new ExtensionFilter[] {
        //     new ExtensionFilter("tif files", "tif"),
        // };

        // var fileUtils = new FileUtilsMacOS();
        // var filePathList = fileUtils.OpenFilePanel("Select File", "", extensions, false);

        Debug.Log($"file path {filePathList[0]}");
        **/
    }

    private void Update()
    {
        //Y키가 눌리기 전까진 실행하지 않습니다.
        if (Input.GetKey(KeyCode.Y))
        {
            //도로
            MapDemVO roadDem = new MapDemVO(
                demPath: Application.streamingAssetsPath + "/2-3_DEM_20240105.tif",
                elevationMinMax: new Vector2(-7.52798f, 7.82004f),
                terrainDimension: new Vector2(1.5589640f, 1.7998629f),
                topL: new Wgs84Info(35.14610622734318, 128.90448959913755, 0),
                topR: new Wgs84Info(35.146118706315704, 128.922692, 0),
                bottomL: new Wgs84Info(35.1298830354115, 128.9045088292273, 0),
                bottomR: new Wgs84Info(35.12989550683726, 128.922692, 0)

            /**
            topL: new Wgs84Info(35.14610622734318, 128.90448959913755, 0),
            topR: new Wgs84Info(35.146118706315704, 128.9215973525445, 0),
            bottomL: new Wgs84Info(35.1298830354115, 128.9045088292273, 0),
            bottomR: new Wgs84Info(35.12989550683726, 128.92161318930567, 0)
            // **/
            );

            //지면 (지면의 경우 좌표 데이터가 도로와 차이가 심하여 도로 사이즈와 같도록 임시 수정)
            MapDemVO terrainDem = new MapDemVO(
                demPath: Application.streamingAssetsPath + "/2-3_DEM_ALL.tif",
                //elevationMinMax: new Vector2(-2.65896f, 6.90098f),
                elevationMinMax: new Vector2(-7.52798f, 7.82004f),
                // terrainDimension: new Vector2(3.0436480f, 2.5983520f),
                terrainDimension: new Vector2(1.5589640f, 1.7998629f),
                topL: new Wgs84Info(35.14610622734318, 128.90448959913755, 0),
                topR: new Wgs84Info(35.146118706315704, 128.9215973525445, 0),
                bottomL: new Wgs84Info(35.1298830354115, 128.9045088292273, 0),
                bottomR: new Wgs84Info(35.12989550683726, 128.92161318930567, 0)
            // topL: new Wgs84Info(35.146106228231645, 128.90448959913755, 0),
            // topR: new Wgs84Info(35.14611870720418, 128.9215973525445, 0),
            // bottomL: new Wgs84Info(35.129883036299795, 128.9045088292273, 0),
            // bottomR: new Wgs84Info(35.12989550772555, 128.92161318930567, 0)
            );

            MapDemVO[] demVOs = new MapDemVO[] { roadDem };

            if (!isLoadingMap)
            {
                //StartCoroutine(loadMap(demVOs));
                StartCoroutine(loadMapHighQuality(demVOs));
            }
        }
    }

    [ContextMenu("ReadDEMFile")]
    public void ReadDEMFile()
    {
        //도로
        MapDemVO roadDem = new MapDemVO(
            demPath: Application.streamingAssetsPath + "/2-3_DEM_20240105.tif",
            elevationMinMax: new Vector2(-7.52798f, 7.82004f),
            terrainDimension: new Vector2(1.5589640f, 1.7998629f),
            topL: new Wgs84Info(35.14610622734318, 128.90448959913755, 0),
            topR: new Wgs84Info(35.146118706315704, 128.922692, 0),
            bottomL: new Wgs84Info(35.1298830354115, 128.9045088292273, 0),
            bottomR: new Wgs84Info(35.12989550683726, 128.922692, 0)
        // topL: new Wgs84Info(35.14610622734318, 128.90448959913755, 0),
        // topR: new Wgs84Info(35.146118706315704, 128.9215973525445, 0),
        // bottomL: new Wgs84Info(35.1298830354115, 128.9045088292273, 0),
        // bottomR: new Wgs84Info(35.12989550683726, 128.92161318930567, 0)
        );

        //지면 (지면의 경우 좌표 데이터가 도로와 차이가 심하여 도로 사이즈와 같도록 임시 수정)
        MapDemVO terrainDem = new MapDemVO(
            demPath: Application.streamingAssetsPath + "/2-3_DEM_ALL.tif",
            //elevationMinMax: new Vector2(-2.65896f, 6.90098f),
            elevationMinMax: new Vector2(-7.52798f, 7.82004f),
            // terrainDimension: new Vector2(3.0436480f, 2.5983520f),
            terrainDimension: new Vector2(1.5589640f, 1.7998629f),
            topL: new Wgs84Info(35.129883036299795, 128.9045088292273, 0),
            topR: new Wgs84Info(35.14611870720418, 128.9215973525445, 0),
            bottomL: new Wgs84Info(35.146106228231645, 128.90448959913755, 0),
            bottomR: new Wgs84Info(35.12989550772555, 128.92161318930567, 0)
        // topL: new Wgs84Info(35.146106228231645, 128.90448959913755, 0),
        // topR: new Wgs84Info(35.14611870720418, 128.9215973525445, 0),
        // bottomL: new Wgs84Info(35.129883036299795, 128.9045088292273, 0),
        // bottomR: new Wgs84Info(35.12989550772555, 128.92161318930567, 0)
        );

        MapDemVO[] demVOs = new MapDemVO[] { roadDem };
        StartCoroutine(loadMapHighQuality(demVOs));
    }

    [ContextMenu("Test")]
    void Test()
    {
        //TODO: Test code (sewer)
        StartCoroutine(makeSewer(new TileInfo(1620, 3514, 12), 2048));
    }

    // StartAsync 
    // 
    // 구글맵을 사용하기 위해 세션키를 미리 받을 수 있도록 만든 비동기 함수입니다.
    // float waitTime :) 시작 시 기다릴 시간
    IEnumerator startAsync()
    {
        Task task = getGoogleMapSession();
        yield return new WaitUntil(() => task.IsCompleted);
    }

    //맵을 로딩하는 함수
    IEnumerator loadMap(MapDemVO[] mapDemVOs)
    {
        isLoadingMap = true;
        GameObject.Find("LoadingTitleBar").GetComponent<Image>().enabled = true;
        GameObject.Find("LoadingTitle").GetComponent<Text>().enabled = true;
        try
        {
            List<Wgs84Info> wgs84Coords = new List<Wgs84Info> {
                mapDemVOs.First().topL,
                mapDemVOs.First().topR,
                mapDemVOs.First().bottomL,
                mapDemVOs.First().bottomR
            };

            TileInfo mapTile = MapUtils.MapLoadUtils.getTileListFromDEM(wgs84Coords[0], wgs84Coords[1], wgs84Coords[2], wgs84Coords[3]);
            yield return getGoogleMapSatellite(mapTile);

            // plane을 지도 이미지로 변경
            Material planeMapMaterial = new Material(Shader.Find("Standard"));
            planeMapMaterial.mainTexture = mapMainTexture;
            Renderer planeRenderer = GetComponent<Renderer>();
            planeRenderer.material = planeMapMaterial;
            StartCoroutine(makeDEMTerrain(mapDemVOs));
        }
        finally
        {
            isLoadingMap = false;
        }
    }

    //맵 로딩
    IEnumerator loadMapHighQuality(MapDemVO[] mapDemVOs)
    {
        isLoadingMap = true;
        GameObject.Find("LoadingTitleBar").GetComponent<Image>().enabled = true;
        GameObject.Find("LoadingTitle").GetComponent<Text>().enabled = true;
        try
        {
            List<Wgs84Info> wgs84Coords = new List<Wgs84Info> {
                mapDemVOs.First().topL,
                mapDemVOs.First().topR,
                mapDemVOs.First().bottomL,
                mapDemVOs.First().bottomR
            };

            TileInfo mapTile = MapUtils.MapLoadUtils.getTileListFromDEM(wgs84Coords[0], wgs84Coords[1], wgs84Coords[2], wgs84Coords[3]);
            List<TileInfo> tileList = MapUtils.MapLoadUtils.getTilesInTile(mapTile);
            int zoomDiff = 15 - mapTile.zoom;
            mapTileCnt = 1 << zoomDiff;

            yield return getGoogleMapSatellite15(tileList, mapTileCnt);
            yield return "";

            // plane을 지도 이미지로 변경
            int mapSize = 256 * mapTileCnt;
            mapMainTexture = CVUtils.resizeTexture2D(mapMainTexture, mapSize, mapSize);
            Material planeMapMaterial = new Material(Shader.Find("Standard"));
            planeMapMaterial.mainTexture = mapMainTexture;
            Renderer planeRenderer = GetComponent<Renderer>();
            planeRenderer.material = planeMapMaterial;
            StartCoroutine(makeDEMTerrain(mapDemVOs));
        }
        finally
        {
            isLoadingMap = false;
        }
    }

    // getGoogleMapSession
    // 
    // 구글맵을 사용하기 위해 미리 세션키를 발급받는 함수입니다.
    // 
    private async Task getGoogleMapSession()
    {
        string query = NetworkVO.queryParameterMaker(new Dictionary<string, string> { { "key", Const.Google_API } });
        string api_url = $"{APIConst.google_session_api}?{query}";
        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            {"mapType", "satellite"},
            {"language", "en-US"},
            {"region", "US"}
        };
        string res = await NetworkVO.reqAPI<string>(api_url, NetworkEnum.POST, JsonConvert.SerializeObject(data));
        var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(res);
        Const.Shared.Google_Session_key = dict["session"];
        Const.Shared.g_sessionExpired = Convert.ToInt64(dict["expiry"]);
    }

    // getGoogleMapSatellite
    // 
    // 구글맵 위성지도를 Sprite으로 저장하는 함수입니다. 
    // TileInfo tileInfo :) 구글맵지도 api는 타일을 사용하여 호출합니다.
    private IEnumerator getGoogleMapSatellite(TileInfo tileInfo)
    {
        if (Convert.ToInt64(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) > Const.Shared.g_sessionExpired + 30)
        {
            Task getSessionKeyTask = getGoogleMapSession();
            yield return new WaitUntil(() => getSessionKeyTask.IsCompleted);
        }

        var tileQueryDict = new Dictionary<string, string> {
                {"session", Const.Shared.Google_Session_key},
                {"key", Const.Google_API}
        };

        string query = NetworkVO.queryParameterMaker(tileQueryDict);
        string api_url = $"{APIConst.google_map_api}/{tileInfo.zoom}/{tileInfo.lon}/{tileInfo.lat}?{query}";
        byte[] receivedByteArr = new byte[0];

        Task<byte[]> task = NetworkVO.reqAPI<byte[]>(api_url, NetworkEnum.GET);
        yield return new WaitUntil(() => task.IsCompleted);
        receivedByteArr = task.Result;
        Texture2D bmp = new Texture2D(8, 8);
        Vector2 pivot = new Vector2(0.5f, 0.5f);

        bmp.LoadImage(receivedByteArr);
        //Rect tRect = new Rect(0, 0, bmp.width, bmp.height);

        mapMainTexture = bmp;
        yield return "";
    }

    // getGoogleMapSatellite19
    // 
    // 구글맵 15레벨 위성지도를 한개의 Sprite로 저장하는 함수입니다. 
    // List<TileInfo> tileList : 구글맵지도 api는 타일을 사용하여 호출합니다.
    // int tileXWay : x축의 타일 최대 갯수입니다.

    private IEnumerator getGoogleMapSatellite15(List<TileInfo> tileList, int tileXWay)
    {
        if (Convert.ToInt64(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) > Const.Shared.g_sessionExpired + 30)
        {
            Task getSessionKeyTask = getGoogleMapSession();
            yield return new WaitUntil(() => getSessionKeyTask.IsCompleted);
        }

        var tileQueryDict = new Dictionary<string, string> {
                {"session", Const.Shared.Google_Session_key},
                {"key", Const.Google_API}
        };
        string query = NetworkVO.queryParameterMaker(tileQueryDict);
        List<Texture2D> textures = new List<Texture2D>();

        foreach (var tile in tileList)
        {
            string api_url = $"{APIConst.google_map_api}/{tile.zoom}/{tile.lon}/{tile.lat}?{query}";
            byte[] receivedByteArr = new byte[0];

            Task<byte[]> task = NetworkVO.reqAPI<byte[]>(api_url, NetworkEnum.GET);
            yield return new WaitUntil(() => task.IsCompleted);

            Debug.Log($"{APIConst.google_map_api}/{tile.zoom}/{tile.lon}/{tile.lat}?{query}");

            receivedByteArr = task.Result;
            Texture2D bmp = new Texture2D(8, 8);
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            bmp.LoadImage(receivedByteArr);
            Rect tRect = new Rect(0, 0, bmp.width, bmp.height);
            textures.Add(bmp);
        }

        mapMainTexture = mergeTexture(textures, tileXWay);

        // string directoryPath = @Application.streamingAssetsPath + "/tileImage/";
        // if (Directory.Exists(directoryPath) == false)
        // {
        //     Directory.CreateDirectory(directoryPath);
        // }

        // var pngData = mapSprite.texture.EncodeToJPG();
        // var path = @Application.streamingAssetsPath + "/tileImage/" + "background" + ".jpg";
        // File.WriteAllBytes(path, pngData);
        yield return "";
    }

    IEnumerator makeDEMTerrain(MapDemVO[] mapDemVOs)
    {

        foreach (var mapDem in mapDemVOs)
        {

            //yield return new WaitUntil(() => MapUtils.MapLoadUtils.isLoadingDEM == false);
            //yield return MapUtils.MapLoadUtils.makeDEM(mapDem, this);
            // MapUtils.makeTerrain(
            //     mapDem.demPath,
            //     mapDem.elevationMinMax,
            //     mapDem.terrainDimension,
            //     this
            // );
        }

        // while (true)
        // {
        //     if (GameObject.Find($"Terrains {mapDemVOs.Length}") == null)
        //     {
        //         yield return null;
        //     }
        //     else
        //     {
        //         break;
        //     }
        // }


        for (int i = 1; i <= mapDemVOs.Length; i++)
        {
            var mapDem = mapDemVOs[i - 1];
            var terrainObj = GameObject.Find($"Terrains {i}");
            TileInfo mapTile = MapUtils.MapLoadUtils.getTileListFromDEM(mapDem.topL, mapDem.topR, mapDem.bottomL, mapDem.bottomR);
            int mapSize = 256 * mapTileCnt;
            //TODO: Test code (sewer)
            yield return makeSewer(mapTile, mapSize);
            Vector2 topLP = MapUtils.MapLoadUtils.tileToPixel(mapTile, mapDem.topL, mapSize);
            Vector2 bottomLP = MapUtils.MapLoadUtils.tileToPixel(mapTile, mapDem.bottomL, mapSize);
            Vector2 bottomRP = MapUtils.MapLoadUtils.tileToPixel(mapTile, mapDem.bottomR, mapSize);
            Rect tileImgPRect = new Rect(topLP.x, topLP.y, bottomRP.x - bottomLP.x, bottomLP.y - topLP.y);
            //Wgs84Info centerWgs84 = MapUtils.centerWithWgs84(wgs84Coords);
            //Vector2 centerP = MapUtils.tileToPixel(mapTile, centerWgs84);

            //타일좌표상에서 유니티 좌표로 변환 후 오브젝트가 이동할 좌표 구하기
            Vector3 mapPose = new Vector3(bottomLP.x / mapTileCnt, 0, bottomLP.y / mapTileCnt - (tileImgPRect.height / mapTileCnt));
            // Terrain currentTerrain = terrainObj.transform.GetChild(0).gameObject.GetComponent<Terrain>(); //GIS TECH 라이브러리 사용 시
            Terrain currentTerrain = terrainObj.GetComponent<Terrain>();
            //terrainObj.transform.position = mapPose;

            //받은 dem에 지도 material로 생성
            Texture2D newTerrainImg = cropTexture(mapMainTexture, tileImgPRect);
            Material newTerrainMaterial = new Material(Shader.Find("Standard"));
            // newTerrainMaterial.mainTexture = newTerrainImg;
            // currentTerrain.materialTemplate = newTerrainMaterial;
            // currentTerrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            //currentTerrain.terrainData.size = new Vector3(50, (mapDem.elevationMinMax.y - mapDem.elevationMinMax.x) / 10, 50); //현재 받은 dem의 위치 정보가 이상하여 정상 데이터로 받으면 삭제(보여주기 용)

            // DEM 높이를 plane map 기준으로 설정 (GIS TECH 라이브러리 사용 시)
            /**
            int terrainWidth = currentTerrain.terrainData.heightmapResolution;
            int terrainHeight = currentTerrain.terrainData.heightmapResolution;
            float[,] heightValues = currentTerrain.terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);
            currentTerrain.terrainData.SetHeights(0, 0, heightValues);
            //**/


            if (i == 1) // DEM맵이 위치한 부분에 카메라 이동
            {
                Vector3 moveVector = new Vector3(tileImgPRect.x / mapTileCnt, 20, tileImgPRect.y / mapTileCnt);
                mainCam.transform.position = moveVector;
            }
        }

        GameObject.Find("LoadingTitleBar").GetComponent<Image>().enabled = false;
        GameObject.Find("LoadingTitle").GetComponent<Text>().enabled = false;
    }


    IEnumerator makeSewer(TileInfo currentTile, int tileSize)
    {
        FacilityUtils.SewerUtils.Add(Const.facList, currentTile, tileSize);
        yield return "";
    }
    // Sprite를 사각형의 틀에 맞게 크롭하는 함수
    Texture2D cropTexture(Texture2D texture, Rect rect)
    {
        Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        Debug.Log($"cropSprite pixels x: {(int)rect.x} y: {(int)rect.y} width: {(int)rect.width} height: {(int)rect.height}");

        Texture2D croppedTexture = new Texture2D((int)rect.width, (int)rect.height);
        croppedTexture.SetPixels(pixels);
        croppedTexture.Apply();

        //Sprite croppedSprite = Sprite.Create(croppedTexture, new Rect(0, 0, (int)rect.width, (int)rect.height), new Vector2(0.5f, 0.5f));

        return croppedTexture;
    }

    //Makes one sprite from multiple sprite.
    Texture2D mergeTexture(List<Texture2D> textures, int tileXWay)
    {
        int xSize = tileXWay * 256;
        Texture2D mapTexture = new Texture2D(xSize, xSize);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        Vector2 textureSize = new Vector2(0, 2048f);

        mapTexture.Apply(true, false);

        foreach (var texture in textures)
        {
            Texture2D mapSpriteTexture = texture;
            mapSpriteTexture.Apply(true, false);

            mapTexture.SetPixels(
                (int)textureSize.x,
                (int)textureSize.y - 256,
                256,
                256,
                mapSpriteTexture.GetPixels());

            textureSize.x += 256;

            if (textureSize.x >= xSize)
            {
                textureSize.x = 0;
                textureSize.y -= 256;
            }
        }

        // Rect tRect = new Rect(0, 0, mapTexture.width, mapTexture.height);
        return mapTexture;
    }

}
