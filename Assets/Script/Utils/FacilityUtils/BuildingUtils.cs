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

            foreach (var buildings in objs)
            {
                while (GameObject.Find("Building " + index.ToString()))
                {
                    index++;
                }

                GameObject building = GameObject.Instantiate(obj);
                building.name = "Building " + index.ToString();

                Wgs84Info objCenterCoord = MapUtils.MapLoadUtils.centerWithWgs84(buildings.coords);
                Vector2 movePos = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, objCenterCoord, tilePixelSize);
                Vector3 moveVector = new Vector3(movePos.x, 0, tilePixelSize - movePos.y);
                building.transform.position = moveVector;
                index++;
            }
        }
    }
}