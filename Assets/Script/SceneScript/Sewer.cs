using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Sewer : MonoBehaviour
{

    [SerializeField]
    private GameObject pipeObj;

    [SerializeField]
    private Material stdMtl;

    void Start()
    {

    }
    void Update()
    {

    }

    [ContextMenu("SetSewerTest")]
    void SetSewerTest()
    {
        //시작위치
        /**

        *O*
        OOO
        *O*

        자르는 시작점은 항상 가장 자리 꼭짓점 이여야 함.
        **/
        setSewerObj(SewerPoint.topLeft, 90f, 20f);
        //qsetSewerObj(SewerPoint.topRight, 90f, 45f);
        // setSewerObj(SewerPoint.topRight, 90f, 30f);
    }

    void setSewerObj(SewerPoint startPoint, float degree, float pipeLength)
    {
        if (degree > 180)
        {
            throw new Exception("The pipe degree can't be 180.");
        }

        GameObject pipe = Instantiate(pipeObj);
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
                dirVec = new Vector3(degree / 100 / pipeLengthUnity, 0, -0.9f);
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
