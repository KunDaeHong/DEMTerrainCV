using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;

public class TerrainSet : MonoBehaviour
{
    public GameObject PlaneMap;
    public GameObject PlaneMapObj;
    public Texture2D HeightMap;
    private Sprite mapSprite;
    private bool isLoadingMap = false;

    // makeTerrain과 같은 함수.(이 함수로 Unity에서 선로딩 후 사용중)

    [ContextMenu("SetHeight")]
    public void SetHeight()
    {
        TerrainData terrainData = GetComponent<Terrain>().terrainData;

        // int terrainWidth = terrainData.heightmapResolution;
        // int terrainHeight = terrainData.heightmapResolution;
        // float[,] heightValues = terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);

        // for (int terrainY = 0; terrainY < terrainHeight; terrainY++)
        // {
        //     if (terrainY >= HeightMap.height)
        //     {
        //         break;
        //     }

        //     for (int terrainX = 0; terrainX < terrainWidth; terrainX++)
        //     {
        //         if (terrainX >= HeightMap.width)
        //         {
        //             break;
        //         }

        //         Color heightColor = HeightMap.GetPixel(terrainY, terrainX);
        //         heightValues[terrainX, terrainY] = heightColor.r;
        //     }
        // }

        // terrainData.SetHeights(0, 0, heightValues);
        StartCoroutine(MapUtils.MapLoadUtils.makeTerrain(HeightMap, terrainData));
    }

    private void Start()
    {
        StartCoroutine(startAsync(1f));
    }

    private void Update()
    {
        //Y키가 눌리기 전까진 실행하지 않습니다.
        if (Input.GetKey(KeyCode.Y))
        {
            Wgs84Info topL = new Wgs84Info(35.14610622734318, 128.90448959913755, 0);
            Wgs84Info topR = new Wgs84Info(35.146118706315704, 128.9215973525445, 0);
            Wgs84Info bottomL = new Wgs84Info(35.1298830354115, 128.9045088292273, 0);
            Wgs84Info bottomR = new Wgs84Info(35.12989550683726, 128.92161318930567, 0);
            if (!isLoadingMap) StartCoroutine(loadMap(topL, topR, bottomL, bottomR));
        }
    }


    IEnumerator startAsync(float waitTime)
    {
        Task task = getGoogleMapSession();
        yield return new WaitForSeconds(waitTime);
    }

    //구글맵을 사용하기 위해 미리 세센키를 발급받는 함수
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

    // 구글맵 위성지도를 Sprite으로 저장하는 함수
    private IEnumerator getGoogleMapSatellite(TileInfo tileInfo)
    {
        if (Convert.ToInt64(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) > Const.Shared.g_sessionExpired + 30)
        {
            yield return new WaitUntil(() => getGoogleMapSession().IsCompleted);
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
        Rect tRect = new Rect(0, 0, bmp.width, bmp.height);
        mapSprite = Sprite.Create(bmp, tRect, pivot);
        yield return "";
    }

    //맵을 로딩하는 함수
    IEnumerator loadMap(Wgs84Info topL, Wgs84Info topR, Wgs84Info bottomL, Wgs84Info bottomR)
    {
        isLoadingMap = true;
        try
        {
            List<Wgs84Info> wgs84Coords = new List<Wgs84Info> {
                topL,
                topR,
                bottomL,
                bottomR
            };
            TileInfo mapTile = MapUtils.MapLoadUtils.getTileListFromDEM(topL, topR, bottomL, bottomR);
            yield return StartCoroutine(getGoogleMapSatellite(mapTile));

            // plane을 지도 이미지로 변경
            Material planeMapMaterial = new Material(Shader.Find("Standard"));
            planeMapMaterial.mainTexture = mapSprite.texture;
            Renderer planeRenderer = PlaneMap.GetComponent<Renderer>();
            planeRenderer.material = planeMapMaterial;

            //지도 타일 좌표를 유니티 좌표로 변환
            Vector2 topLP = MapUtils.MapLoadUtils.tileToPixel(mapTile, topL, 256);
            Vector2 bottomLP = MapUtils.MapLoadUtils.tileToPixel(mapTile, bottomL, 256);
            Vector2 bottomRP = MapUtils.MapLoadUtils.tileToPixel(mapTile, bottomR, 256);
            Rect tileImgPRect = new Rect(bottomLP.x, bottomLP.y, bottomRP.x - bottomLP.x, bottomLP.y - topLP.y);
            //Wgs84Info centerWgs84 = MapUtils.centerWithWgs84(wgs84Coords);
            //Vector2 centerP = MapUtils.tileToPixel(mapTile, centerWgs84);

            //타일좌표상에서 유니티 좌표로 변환 후 오브젝트가 이동할 좌표 구하기
            Vector3 mapPose = new Vector3((int)tileImgPRect.x, 0, (int)tileImgPRect.y);
            Terrain currentTerrain = GetComponent<Terrain>();
            transform.position = mapPose;
            PlaneMapObj.transform.position = mapPose;
            currentTerrain.terrainData.size = new Vector3(
                (int)tileImgPRect.width,
                currentTerrain.terrainData.size.y,
                (int)tileImgPRect.height);

            //받은 dem obj에 지도 material로 생성
            Sprite newTerrainImg = cropSprite(mapSprite, tileImgPRect);
            Material newTerrainMaterial = new Material(Shader.Find("Standard"));
            newTerrainMaterial.mainTexture = newTerrainImg.texture;

            // 유니티 Standard Terrain Material
            currentTerrain.materialTemplate = newTerrainMaterial;

            // OBJ Standard Material
            // var demObj = PlaneMapObj.transform.GetChild(PlaneMapObj.transform.childCount - 1);
            // var demObjChild = demObj.transform.GetChild(demObj.transform.childCount - 1);
            // demObjChild.GetComponent<MeshRenderer>().material = newTerrainMaterial;
        }
        finally
        {
            isLoadingMap = false;
        }
    }

    //This makes terrains with 2D Texture DEM TIFF Image.
    public void makeTerrain(Texture2D heightMap)
    {
        TerrainData terrainData = GetComponent<Terrain>().terrainData;

        int terrainWidth = terrainData.heightmapResolution;
        int terrainHeight = terrainData.heightmapResolution;
        float[,] heightValues = terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);

        for (int terrainY = 0; terrainY < terrainHeight; terrainY++)
        {
            if (terrainY >= heightMap.height)
            {
                break;
            }

            for (int terrainX = 0; terrainX < terrainWidth; terrainX++)
            {
                if (terrainX >= heightMap.width)
                {
                    break;
                }

                Color heightColor = heightMap.GetPixel(terrainY, terrainX);
                heightValues[terrainX, terrainY] = heightColor.r;
            }
        }

        terrainData.SetHeights(0, 0, heightValues);
    }

    public void Terrain2DToMesh(Texture2D heightMap, Wgs84Info topL, Wgs84Info topR, Wgs84Info bottomL, Wgs84Info bottomR)
    {
        //wgs84좌표에 의해 사이즈가 결정됨.
        TileInfo mapTile = MapUtils.MapLoadUtils.getTileListFromDEM(topL, topR, bottomL, bottomR);
        Vector2 topLP = MapUtils.MapLoadUtils.tileToPixel(mapTile, topL, 256);
        Vector2 bottomLP = MapUtils.MapLoadUtils.tileToPixel(mapTile, bottomL, 256);
        Vector2 bottomRP = MapUtils.MapLoadUtils.tileToPixel(mapTile, bottomR, 256);
        Rect tileImgPRect = new Rect(bottomLP.x, bottomLP.y, bottomRP.x - bottomLP.x, bottomLP.y - topLP.y);
        int terrainWidth = (int)tileImgPRect.width;
        int terrainHeight = (int)tileImgPRect.height;

        for (int terrainY = 0; terrainY < terrainHeight; terrainY++)
        {
            if (terrainY >= heightMap.height)
            {
                break;
            }

            for (int terrainX = 0; terrainX < terrainWidth; terrainX++)
            {

            }
        }

    }

    // Sprite를 사각형의 틀에 맞게 크롭하는 함수
    Sprite cropSprite(Sprite sprite, Rect rect)
    {
        Texture2D texture = sprite.texture;
        Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        Debug.Log($"cropSprite pixels x: {(int)rect.x} y: {(int)rect.y} width: {(int)rect.width} height: {(int)rect.height}");

        Texture2D croppedTexture = new Texture2D((int)rect.width, (int)rect.height);
        croppedTexture.SetPixels(pixels);
        croppedTexture.Apply();

        Sprite croppedSprite = Sprite.Create(croppedTexture, new Rect(0, 0, (int)rect.width, (int)rect.height), new Vector2(0.5f, 0.5f));

        return croppedSprite;
    }

}
