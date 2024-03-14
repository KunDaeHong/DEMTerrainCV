using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using Newtonsoft.Json;

public class PlaneMap : MonoBehaviour
{
    [SerializeField]
    private Camera mainCam;
    private Texture2D mapMainTexture;
    private Texture2D mapResizeTexture;
    private bool isLoadingMap = false;
    private int mapTileCnt = 0;

    private void Start()
    {
        GameObject.Find("LoadingTitleBar").GetComponent<Image>().enabled = false;
        GameObject.Find("LoadingTitle").GetComponent<Text>().enabled = false;
    }

    private async void Update()
    {
        if (Input.GetKey(KeyCode.Y))
        {
            if (!isLoadingMap)
            {
                //왼쪽 위, 오른쪽 위, 왼쪽 하단, 오른쪽 하단
                Texture2D map = await getMapTexture2D(
                    new Wgs84Info(11.945088338330308, 108.41956416737673, 0),
                    new Wgs84Info(11.945088338330308, 108.42674728647341, 0),
                    new Wgs84Info(11.938431073223398, 108.41956416737673, 0),
                    new Wgs84Info(11.938431073223398, 108.42674728647341, 0)
                );

                //타일 한변의 길이 (m단위)
                //float tileDist = MapUtils.MapLoadUtils.tileDist(new TileInfo(0, 0, 0)); //lat lon zoom 순
                //Wgs84거리 측정 (m단위)
                //float wgs84Dist = MapUtils.MapLoadUtils.wgs84Dist(new Wgs84Info(0, 0, 0), new Wgs84Info(0, 0, 0)); //Wgs84Info lat lon zoom 순
            }
        }
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

    //저화질 맵을 로딩하는 함수
    IEnumerator loadMap(MapDemVO[] mapDemVOs)
    {
        List<Wgs84Info> wgs84Coords = new List<Wgs84Info> {
                mapDemVOs.First().topL,
                mapDemVOs.First().topR,
                mapDemVOs.First().bottomL,
                mapDemVOs.First().bottomR
            };

        TileInfo mapTile = MapUtils.MapLoadUtils.getTileListFromDEM(wgs84Coords[0], wgs84Coords[1], wgs84Coords[2], wgs84Coords[3]);
        yield return getGoogleMapSatellite(mapTile);
    }

    //고화질 맵을 로딩하는 함수
    IEnumerator loadMapHighQuality(MapDemVO[] mapDemVOs)
    {
        List<Wgs84Info> wgs84Coords = new List<Wgs84Info> {
                mapDemVOs.First().topL,
                mapDemVOs.First().topR,
                mapDemVOs.First().bottomL,
                mapDemVOs.First().bottomR
            };

        TileInfo mapTile = MapUtils.MapLoadUtils.getTileListFromDEM(wgs84Coords[0], wgs84Coords[1], wgs84Coords[2], wgs84Coords[3]);
        List<TileInfo> tileList = MapUtils.MapLoadUtils.getTilesInTile(mapTile, mapTile.zoom + 4);
        mapTileCnt = 1 << (mapTile.zoom + 4 - mapTile.zoom);

        yield return getGoogleMapSatellite15(tileList, mapTileCnt);
        yield return "";
    }

    async Task<Texture2D> getMapTexture2D(Wgs84Info topL, Wgs84Info topR, Wgs84Info bottomL, Wgs84Info bottomR)
    {
        StartCoroutine(getMapHighQuality(topL, topR, bottomL, bottomR));

        await Task.Run(async () =>
        {
            while (mapResizeTexture == null)
            {
                await Task.Delay(500);
            }
        });

        return mapResizeTexture;
    }

    //고화질 맵을 Texture2D로 반환
    IEnumerator getMapHighQuality(Wgs84Info topL, Wgs84Info topR, Wgs84Info bottomL, Wgs84Info bottomR)
    {
        isLoadingMap = true;
        try
        {
            //Vietnam VDC
            MapDemVO roadDem = new MapDemVO(
                demPath: "",
                elevationMinMax: new Vector2(0f, 0f),
                terrainDimension: new Vector2(0f, 0f),
                topL: topL,
                topR: topR,
                bottomL: bottomL,
                bottomR: bottomR
            );

            yield return loadMapHighQuality(new MapDemVO[] { roadDem });

            // string directoryPath = @Application.streamingAssetsPath + "/tileImage/";
            // if (Directory.Exists(directoryPath) == false)
            // {
            //     Directory.CreateDirectory(directoryPath);
            // }

            // var pngData = mapMainTexture.EncodeToJPG();
            // var path = @Application.streamingAssetsPath + "/tileImage/" + "background" + ".jpg";
            // File.WriteAllBytes(path, pngData);

            int mapSize = 256 * mapTileCnt;
            mapResizeTexture = CVUtils.resizeTexture2D(mapMainTexture, mapSize, mapSize);

            Material planeMapMaterial = new Material(Shader.Find("Standard"));
            planeMapMaterial.mainTexture = mapResizeTexture;
            Renderer planeRenderer = GetComponent<Renderer>();
            planeRenderer.material = planeMapMaterial;
        }
        finally
        {
            isLoadingMap = false;
        }

        yield return mapResizeTexture;
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

    // getGoogleMapSatellite15
    // 
    // 구글맵 15레벨 위성지도를 한개의 Sprite로 저장하는 함수입니다. 
    // List<TileInfo> tileList : 구글맵지도 api는 타일을 사용하여 호출합니다.
    // int tileXWay : x축의 타일 최대 갯수입니다.

    private IEnumerator getGoogleMapSatellite15(List<TileInfo> tileList, int tileXWay)
    {
        List<Texture2D> textures = new List<Texture2D>();

        foreach (var tile in tileList)
        {
            string api_url = $"{APIConst.map4d_tms_map_api}/{tile.zoom}/{tile.lon}/{tile.lat}.png";
            byte[] receivedByteArr = new byte[0];

            Task<byte[]> task = NetworkVO.reqAPI<byte[]>(api_url, NetworkEnum.GET);
            yield return new WaitUntil(() => task.IsCompleted);

            Debug.Log($"{api_url}");

            receivedByteArr = task.Result;
            Texture2D bmp = new Texture2D(8, 8);
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            bmp.LoadImage(receivedByteArr);
            Rect tRect = new Rect(0, 0, bmp.width, bmp.height);

            if (bmp.width > 256 && bmp.height > 256)
            {
                bmp = CVUtils.resizeTexture2D(bmp, 256, 256);
            }

            textures.Add(bmp);
        }

        mapMainTexture = mergeTexture(textures, tileXWay);

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
        Vector2 textureSize = new Vector2(0, tileXWay * 256);

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
