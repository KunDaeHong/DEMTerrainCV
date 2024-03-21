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
        static string sewerStandardMtlPath = "Assets/assets/Terrains/Material" + "/BackRender.mat";

        public static void setAlign(List<GameObject> pipeObjs, List<FacilityVO.FacilityInfoVO> pipes, TileInfo mapTile, int mapSize)
        {
            int index = 0;

            foreach (var pipe in pipeObjs)
            {
                Wgs84Info objCenterCoord = MapUtils.MapLoadUtils.centerWithWgs84(pipes[index].coords);
                Vector2 movePos = MapUtils.MapLoadUtils.tileToPixel(mapTile, objCenterCoord, mapSize);
                Vector3 moveVector = new Vector3(movePos.x, 0, mapSize - movePos.y);
                pipe.transform.position = moveVector;
                index++;
            }
        }

        //TODO: 수직 관 기능 생성
        public static List<GameObject> Add(List<FacilityVO.FacilityInfoVO> pipes, TileInfo currentTileLoc, int tilePixelSize)
        {
            List<GameObject> pipeObjs = new List<GameObject>();
            int index = 1;
            float previousDeg = 0;
            SewerPoint previousStartP = SewerPoint.topRight;
            SewerPoint downCutPoint = SewerPoint.bottomRight;

            GameObject obj = (GameObject)AssetDatabase.LoadAssetAtPath(sewerPrefabPath, typeof(GameObject));
            Material stdMtl = (Material)AssetDatabase.LoadAssetAtPath(sewerStandardMtlPath, typeof(Material));

            for (int i = 0; i < pipes.Count; i++)
            {
                while (GameObject.Find("Sewer " + index.ToString()))
                {
                    index++;
                }

                var pipe = pipes[i];
                GameObject nPipe = GameObject.Instantiate(obj);
                nPipe.name = "Sewer " + index.ToString();
                float pipeDiameter = 5f;

                Vector2 startP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, pipe.coords.First(), tilePixelSize);
                Vector2 endP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, pipe.coords.Last(), tilePixelSize);
                Rect currentPipeRect = new Rect(startP.x, startP.y, startP.x - endP.x, startP.y - endP.y);
                Vector3 nPipeRot = nPipe.transform.rotation.eulerAngles;
                float pipeLength = Math.Abs(currentPipeRect.height) < Math.Abs(currentPipeRect.width) ? Math.Abs(currentPipeRect.width / 2) : Math.Abs(currentPipeRect.height / 2);
                var currentDeg = (float)Math.Abs(Math.Atan2((double)(endP.y - startP.y), (double)(endP.x - startP.x)) * 180 / Math.PI);
                float nPipeDeg = 90 - currentDeg;

                Wgs84Info objCenterCoord = MapUtils.MapLoadUtils.centerWithWgs84(pipe.coords);
                Vector2 movePos = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, objCenterCoord, tilePixelSize);
                Vector3 moveVector = new Vector3(movePos.x, 0, tilePixelSize - movePos.y);
                pipeLength += pipeDiameter / 2;
                nPipe.transform.localScale = new Vector3(pipeDiameter, pipeLength, pipeDiameter);

                // if (Math.Abs(currentPipeRect.width) > Math.Abs(currentPipeRect.height))
                // {
                //     nPipeDeg = -1 * nPipeDeg;
                // }

                if (previousStartP == SewerPoint.topLeft && Math.Abs(currentPipeRect.width) > Math.Abs(currentPipeRect.height))
                {
                    downCutPoint = SewerPoint.bottomLeft;
                    nPipeDeg = 90 + currentDeg;
                }

                if (i > 0)
                {
                    cutSewerObj(downCutPoint, Math.Abs(90 + previousDeg), pipeLength, stdMtl, nPipe, pipeDiameter);
                }

                if (pipes.Count - 1 > i)
                {
                    var nextPipe = pipes[i + 1];
                    Vector2 nextStartP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, nextPipe.coords.First(), tilePixelSize);
                    Vector2 nextEndP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, nextPipe.coords.Last(), tilePixelSize);
                    previousDeg = (float)(Math.Atan2((double)(nextStartP.y - nextEndP.y), (double)(nextEndP.x - nextStartP.x)) * 180 / Math.PI - 90);
                    previousStartP = SewerPoint.topLeft;

                    if (nextStartP.y > nextEndP.y && Math.Abs(currentPipeRect.width) > Math.Abs(currentPipeRect.height))
                    {
                        previousStartP = SewerPoint.topRight;
                    }

                    cutSewerObj(previousStartP, Math.Abs(90 + previousDeg), pipeLength, stdMtl, nPipe, pipeDiameter);
                }

                if (Math.Abs(currentPipeRect.width) > Math.Abs(currentPipeRect.height))
                {
                    moveVector.x += pipeDiameter / 4;
                }
                else
                {
                    moveVector.z += pipeDiameter / 4;
                }

                nPipe.transform.rotation = Quaternion.Euler(new Vector3(nPipeRot.x, nPipeRot.y, nPipeDeg));
                nPipe.transform.position = moveVector;
                pipeObjs.Add(nPipe);
            }

            return pipeObjs;
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
        public static void cutSewerObj(SewerPoint startPoint, float degree, float pipeLength, Material stdMtl, GameObject pipe, float pipeDiameter)
        {
            if (degree > 180)
            {
                throw new Exception("The pipe degree can't be 180.");
            }
            Vector3 dirVec = new Vector3();
            Vector3 cutPoint = new Vector3();

            switch (startPoint)
            {
                case SewerPoint.topLeft:
                    cutPoint.x = pipe.transform.position.x - (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z + pipe.transform.localScale.y;
                    dirVec = new Vector3(-(degree / (10 * (10 / pipeDiameter))) / pipeLength, 0, -0.9f);
                    MeshCut.cutObject(pipe, cutPoint, dirVec, stdMtl, false, true); //right, left
                    break;
                case SewerPoint.topRight:
                    cutPoint.x = pipe.transform.position.x + (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z + pipe.transform.localScale.y;
                    dirVec = new Vector3(degree / (10 * (10 / pipeDiameter)) / pipeLength, 0, -0.9f);
                    MeshCut.cutObject(pipe, cutPoint, dirVec, stdMtl, false, true);
                    break;
                case SewerPoint.bottomLeft:
                    cutPoint.x = pipe.transform.position.x - (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z - pipe.transform.localScale.y;
                    dirVec = new Vector3(degree / (10 * (10 / pipeDiameter)) / pipeLength, 0, -0.9f);
                    MeshCut.cutObject(pipe, cutPoint, dirVec, stdMtl, true, false);
                    break;
                case SewerPoint.bottomRight:
                    cutPoint.x = pipe.transform.position.x + (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z - pipe.transform.localScale.y;
                    dirVec = new Vector3(-(degree / (10 * (10 / pipeDiameter))) / pipeLength, 0, -0.9f);
                    MeshCut.cutObject(pipe, cutPoint, dirVec, stdMtl, true, false);
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