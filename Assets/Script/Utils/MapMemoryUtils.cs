using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

using Newtonsoft.Json;

namespace MapUtils
{
    public class MapMemoryUtils
    {
        public static Vector3 position = new Vector3();
        private static int mapSize = 0;

        IEnumerator loadNewTileMap()
        {
            GameObject newTileImg = GameObject.CreatePrimitive(PrimitiveType.Plane);
            yield return "";
        }

        IEnumerator loadMapHighQuality(MapDemVO[] mapDemVOs, int imageScale, Texture2D mainTexture)
        {
            List<Wgs84Info> wgs84Coords = new List<Wgs84Info> {
                mapDemVOs.First().topL,
                mapDemVOs.First().topR,
                mapDemVOs.First().bottomL,
                mapDemVOs.First().bottomR
            };

            TileInfo mapTile = MapUtils.MapLoadUtils.getTileListFromDEM(wgs84Coords[0], wgs84Coords[1], wgs84Coords[2], wgs84Coords[3]);
            List<TileInfo> tileList = MapUtils.MapLoadUtils.getTilesInTile(mapTile, mapTile.zoom + imageScale);
            int mapTileCnt = 1 << imageScale;

            yield return getGoogleMapSatelliteHQ(tileList, mapTileCnt);
            yield return "";

            mapSize = 256 * mapTileCnt;
            CVUtils.resizeTexture2D(mainTexture, mapSize, mapSize);
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
            //{"mapType", "roadmap"},
            {"language", "en-US"},
            {"region", "US"}
        };
            string res = await NetworkVO.reqAPI<string>(api_url, NetworkEnum.POST, JsonConvert.SerializeObject(data));
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(res);
            Const.Shared.Google_Session_key = dict["session"];
            Const.Shared.g_sessionExpired = Convert.ToInt64(dict["expiry"]);
        }

        // getGoogleMapSatelliteHQ
        // 
        // 구글 위성맵을 고화질로 가져옵니다. 
        // List<TileInfo> tileList : 구글맵지도 api는 타일을 사용하여 호출합니다.
        // int tileXWay : x축의 타일 최대 갯수입니다.
        private IEnumerator getGoogleMapSatelliteHQ(List<TileInfo> tileList, int tileXWay)
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

            CVUtils.mergeTexture(textures, tileXWay);

            // string directoryPath = @Application.streamingAssetsPath + "/tileImage/";
            // if (Directory.Exists(directoryPath) == false)
            // {
            //     Directory.CreateDirectory(directoryPath);
            // }

            // var pngData = mapMainTexture.EncodeToJPG();
            // var path = @Application.streamingAssetsPath + "/tileImage/" + "background" + ".jpg";
            // File.WriteAllBytes(path, pngData);
            yield return "";
        }

        private IEnumerator getMap4DSatelliteHQ(List<TileInfo> tileList, int tileXWay)
        {

            var tileQueryDict = new Dictionary<string, string> {
                {"session", Const.Shared.Google_Session_key},
                {"key", Const.Google_API}
        };
            string query = NetworkVO.queryParameterMaker(tileQueryDict);
            List<Texture2D> textures = new List<Texture2D>();

            foreach (var tile in tileList)
            {
                string api_url = $"{APIConst.google_map_api}/{tile.zoom}/{tile.lon}/{tile.lat}?{query}";
                //string api_url = $"{APIConst.map4d_tms_map_api}/{tile.zoom}/{tile.lon}/{tile.lat}.png";
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

            CVUtils.mergeTexture(textures, tileXWay);

            // string directoryPath = @Application.streamingAssetsPath + "/tileImage/";
            // if (Directory.Exists(directoryPath) == false)
            // {
            //     Directory.CreateDirectory(directoryPath);
            // }

            // var pngData = mapMainTexture.EncodeToJPG();
            // var path = @Application.streamingAssetsPath + "/tileImage/" + "background" + ".jpg";
            // File.WriteAllBytes(path, pngData);
            yield return "";
        }
    }
}