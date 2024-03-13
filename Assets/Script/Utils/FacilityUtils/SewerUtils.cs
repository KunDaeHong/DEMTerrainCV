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

        public static void setAlign(List<GameObject> pipeObjs, List<Wgs84Info> pipeLocationList)
        {
            //모든 파이프 오브젝트의 pivot은 중심점임.
            foreach (var pipe in pipeObjs)
            {


            }
        }

        //TODO: 수직 관 기능 생성
        public static void Add(List<FacilityVO.FacilityInfoVO> pipes, TileInfo currentTileLoc, int tilePixelSize)
        {
            int index = 1;
            float previousDeg = 0; //직전 각도
            SewerPoint previousStartP = SewerPoint.topLeft;

            GameObject obj = (GameObject)AssetDatabase.LoadAssetAtPath(sewerPrefabPath, typeof(GameObject));
            Material stdMtl = (Material)AssetDatabase.LoadAssetAtPath(sewerStandardMtlPath, typeof(Material));

            //linestring의 경우 교차지점은 1개의 위치좌표로 관리
            //다음 파이프가 존재 시 해당 gameobj에서 잘라야함.
            for (int i = 0; i < pipes.Count; i++)
            {
                while (GameObject.Find("Sewer " + index.ToString()))
                {
                    index++;
                }

                var pipe = pipes[i];
                GameObject nPipe = GameObject.Instantiate(obj);
                nPipe.name = "Sewer " + index.ToString();
                SewerPoint downCutPoint = SewerPoint.bottomRight;

                if (previousStartP == SewerPoint.topLeft)
                {
                    downCutPoint = SewerPoint.bottomLeft;
                }

                //현재 파이프
                Vector2 startP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, pipe.coords.First(), tilePixelSize);
                Vector2 endP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, pipe.coords.Last(), tilePixelSize);
                Rect currentPipeRect = new Rect(startP.x, startP.y, startP.x - endP.x, startP.y - endP.y);
                float pipeLength = Math.Abs(currentPipeRect.width);

                if (pipeLength < Math.Abs(currentPipeRect.height))
                {
                    pipeLength = Math.Abs(currentPipeRect.height);
                }

                var currentDeg = (float)(Math.Atan2((double)(endP.y - startP.y), (double)(endP.x - startP.x)) * 180 / Math.PI - 90);
                nPipe.transform.localScale = new Vector3(1, pipeLength / 100, 1);

                if (i > 0)
                {
                    cutSewerObj(downCutPoint, Math.Abs(previousDeg - 90), pipeLength, stdMtl, nPipe);
                }

                if (pipes.Count - 1 <= i)
                {
                    break;
                }

                //다음 파이프
                var nextPipe = pipes[i + 1];
                Vector2 nextStartP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, nextPipe.coords.First(), tilePixelSize);
                Vector2 nextEndP = MapUtils.MapLoadUtils.tileToPixel(currentTileLoc, nextPipe.coords.Last(), tilePixelSize);
                previousDeg = (float)(Math.Atan2((double)(nextStartP.y - nextEndP.y), (double)(nextEndP.x - nextStartP.x)) * 180 / Math.PI - 90);
                previousStartP = SewerPoint.topLeft;

                if (startP.x < nextEndP.x)
                {
                    previousStartP = SewerPoint.topRight;
                }

                cutSewerObj(previousStartP, Math.Abs(previousDeg - 90), pipeLength / 100, stdMtl, nPipe);
                Vector3 nPipeRot = nPipe.transform.rotation.eulerAngles;
                Quaternion nPipeRotQ = Quaternion.Euler(new Vector3(nPipeRot.x, nPipeRot.y, currentDeg));
                nPipe.transform.rotation = nPipeRotQ;
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
        public static void cutSewerObj(SewerPoint startPoint, float degree, float pipeLength, Material stdMtl, GameObject pipe)
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
                    dirVec = new Vector3(-(degree / 100) / pipeLength, 0, -0.9f);
                    MeshCut.cutObject(pipe, cutPoint, dirVec, stdMtl, false, true);
                    break;
                case SewerPoint.topRight:
                    cutPoint.x = pipe.transform.position.x + (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z + pipe.transform.localScale.y;
                    dirVec = new Vector3(degree / 100 / pipeLength, 0, -0.9f);
                    MeshCut.cutObject(pipe, cutPoint, dirVec, stdMtl, false, true);
                    break;
                case SewerPoint.bottomLeft:
                    cutPoint.x = pipe.transform.position.x + (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z - pipe.transform.localScale.y;
                    dirVec = new Vector3(-(degree / 100) / pipeLength, 0, -0.9f);
                    MeshCut.cutObject(pipe, cutPoint, dirVec, stdMtl, true, false);
                    break;
                case SewerPoint.bottomRight:
                    cutPoint.x = pipe.transform.position.x - (pipe.transform.localScale.x / 2);
                    cutPoint.y = pipe.transform.position.y;
                    cutPoint.z = pipe.transform.position.z - pipe.transform.localScale.y;
                    dirVec = new Vector3(degree / 100 / pipeLength, 0, -0.9f);
                    MeshCut.cutObject(pipe, cutPoint, dirVec, stdMtl, false, true);
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