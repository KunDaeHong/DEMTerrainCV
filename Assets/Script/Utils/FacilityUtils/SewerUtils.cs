using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FacilityUtils
{
    public class SewerUtils
    {
        static string sewerPrefabPath = "Assets/assets/Terrains/OBJ/Facility" + "/Pipe" + "/Pipe.prefab";
        static string sewerStandardMtlPath = "Asset/assets/Terrains/Material" + "BackRender.mat";

        static void setAlign(List<GameObject> pipeObjs, List<Wgs84Info> pipeLocationList)
        {
            //모든 파이프 오브젝트의 pivot은 중심점임.
            foreach (var pipe in pipeObjs)
            {


            }
        }

        static void add(List<FacilityVO.FacilityInfoVO> pipes, TileInfo currentTileLoc, int tilePixelSize)
        {
            int index = 1;
            int previousDeg = 0; //직전 각도
            GameObject obj = (GameObject)AssetDatabase.LoadAssetAtPath(sewerPrefabPath, typeof(GameObject));
            Material stdMtl = (Material)AssetDatabase.LoadAssetAtPath(sewerStandardMtlPath, typeof(Material));

            while (GameObject.Find("Sewer " + index.ToString()))
            {
                index++;
            }

            //linestring의 경우 교차지점은 1개의 위치좌표로 관리
            //다음 파이프가 존재 시 해당 gameobj에서 잘라야함.
            for (int i = 0; i < pipes.Count - 1; i++)
            {
                var pipe = pipes[i];
                GameObject nPipe = GameObject.Instantiate(obj);

                Vector2 startP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, pipe.coords.First(), tilePixelSize);
                Vector2 endP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, pipe.coords.Last(), tilePixelSize);

                float width = Math.Abs(startP.x - endP.x);
                float height = Math.Abs(startP.y - endP.y);

                if (width < height)
                {

                }

                // tData.size = new Vector3(bottomRP.x - bottomLP.x, 0.3f, bottomLP.y - topLP.y);
                // setSewerObj()


            }
        }

        /// <summary>
        /// 각도가 있는 파이프로 수정 시 사용합니다.
        /// </summary>
        /// <param name="startPoint">자르는 시작위치</param>
        /// <param name="degree">각도</param>
        /// <param name="pipeLength">파이프 길이</param>
        /// <param name="stdMtl">사용 머터리얼</param>
        /// <param name="pipe">파이프 오브젝트</param>
        /// <exception cref="Exception"></exception>
        static void setSewerObj(SewerPoint startPoint, float degree, float pipeLength, Material stdMtl, GameObject pipe)
        {
            if (degree > 180)
            {
                throw new Exception("The pipe degree can't be 180.");
            }

            float pipeLengthUnity = pipeLength / 10;
            pipe.transform.localScale = new Vector3(1, pipeLengthUnity, 1);
            Vector3 dirVec = new Vector3();
            Vector3 cutPoint = new Vector3();

            switch (startPoint)
            {
                case SewerPoint.topLeft:
                    cutPoint.x = pipe.transform.position.x - (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z + pipe.transform.localScale.y;
                    dirVec = new Vector3(-(degree / 100) / pipeLengthUnity, 0, -0.9f);
                    MeshCut.cut(pipe, cutPoint, dirVec, stdMtl, false, true);
                    break;
                case SewerPoint.topRight:
                    cutPoint.x = pipe.transform.position.x + (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z + pipe.transform.localScale.y;
                    dirVec = new Vector3(degree / 100 / pipeLengthUnity, 0, -0.9f);
                    MeshCut.cut(pipe, cutPoint, dirVec, stdMtl, false, true);
                    break;
                case SewerPoint.bottomLeft:
                    cutPoint.x = pipe.transform.position.x + (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z - pipe.transform.localScale.y;
                    dirVec = new Vector3(-(degree / 100) / pipeLengthUnity, 0, -0.9f);
                    MeshCut.cut(pipe, cutPoint, dirVec, stdMtl, true, false);
                    break;
                case SewerPoint.bottomRight:
                    cutPoint.x = pipe.transform.position.x - (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z - pipe.transform.localScale.y;
                    dirVec = new Vector3(degree / 100 / pipeLengthUnity, 0, -0.9f);
                    MeshCut.cut(pipe, cutPoint, dirVec, stdMtl, true, false);
                    break;
                default:
                    throw new Exception("Couldn't find startPoint.");
            }
        }
    }

    public enum SewerPoint
    {
        topLeft,
        topRight,
        bottomLeft,
        bottomRight
    }
}