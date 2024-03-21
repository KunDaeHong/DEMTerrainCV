using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace FacilityUtils
{
    public class BuildingUtils
    {

        //Test OBJ
        static string buildingPrefabPath = "Assets/assets/Terrains/OBJ/Facility" + "/buildings" + "/BuildingSample.prefab";

        //buildings 자동이동
        public static void Add(List<FacilityVO.FacilityInfoVO> objs, TileInfo currentTileLoc, int tilePixelSize)
        {
            int index = 0;
            GameObject obj = (GameObject)AssetDatabase.LoadAssetAtPath(buildingPrefabPath, typeof(GameObject));
            float tileDist = MapUtils.MapLoadUtils.tileDist(currentTileLoc);
            float sampleObjMeter = 11.704977f;

            foreach (var buildings in objs)
            {
                while (GameObject.Find("Building " + index.ToString()))
                {
                    index++;
                }

                GameObject building = GameObject.Instantiate(obj);
                building.name = "Building " + index.ToString();
                float realSize = (float)(sampleObjMeter * tilePixelSize / tileDist);

                Wgs84Info objCenterCoord = MapUtils.MapLoadUtils.centerWithWgs84(buildings.coords);
                Vector2 movePos = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, objCenterCoord, tilePixelSize);
                Vector3 moveVector = new Vector3(movePos.x, 0, tilePixelSize - movePos.y);
                Vector3 nBuildingRot = building.transform.rotation.eulerAngles;
                Vector3 scale = new Vector3(realSize, realSize, realSize);

                building.transform.rotation = Quaternion.Euler(new Vector3(nBuildingRot.x, buildings.azimuth, nBuildingRot.z));
                building.transform.position = moveVector;
                building.transform.localScale = scale;
                index++;
            }
        }
    }
}