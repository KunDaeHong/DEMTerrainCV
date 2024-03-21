using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using System.IO;

using BitMiracle.LibTiff.Classic;

using UnityEngine;

public class CVUtils
{
    public static Texture2D loadTiffToTexture2D(string filePath)
    {
        Tiff tiff = Tiff.Open(filePath, "r"); //tiff 읽기
        int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
        int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
        int BITSPERSAMPLE = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
        int samplesPerPixel = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt(); //픽셀 색상레이어 수
        List<float[,]> heightData = new List<float[,]>();

        for (int band = 0; band < samplesPerPixel; band++)
        {
            var list = new float[width, height];
            heightData.Add(list);
        }

        var scanline = new byte[tiff.ScanlineSize()];
        var scanlineFloat = new float[tiff.ScanlineSize()];

        int BandCounter = 0; //현재 픽셀 색상 레이어
        int C_BandCounter = 0;
        int R_BandCounter = 0;

        for (int i = 0; i < height; i++)
        {
            tiff.ReadScanline(scanline, i); //scanline은 byte데이터로 저장됨.

            for (int j = 0; j < width * samplesPerPixel; j++)
            {
                Buffer.BlockCopy(scanline, 0, scanlineFloat, 0, scanline.Length); // Buffer.BlockCopy로 byte -> 부동소숫점 형태로 변환하여 저장
                float el = Convert.ToSingle(scanlineFloat[j]); // 부동소숫점으로 변환

                if (BandCounter >= samplesPerPixel)
                {
                    BandCounter = 0;
                    C_BandCounter++;

                    if (C_BandCounter > width - 1)
                    {
                        C_BandCounter = 0;
                        R_BandCounter++;
                    }
                }

                heightData[BandCounter][C_BandCounter, R_BandCounter] = el;
                BandCounter++;
            }
        }

        Color[] colors = new Color[width * height];

        for (int r = 0; r < width; r++)
        {
            for (int c = 0; c < height; c++)
            {
                float value = heightData[0][r, c];
                Color color = new Color(value, value, value);
                colors[c * width + r] = color;
            }
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture renderTexture = new RenderTexture(texture.width, texture.height, 0);
        texture.Apply(true, false);

        Vector2 textureSize = new Vector2(0, 0);

        foreach (var color in colors)
        {
            texture.SetPixel(
                (int)textureSize.x,
                height - (int)textureSize.y,
                color);

            textureSize.x += 1;

            if (textureSize.x >= width)
            {
                textureSize.x = 0;
                textureSize.y += 1;
            }
        }

        //texture.SetPixels(colors);
        /**
        Unity Editor에서 tif을 사용 시 자동으로 정사각형사이즈로 변환 후 
        알파색채널은 무시하고 RGB로 BC6H(HDR) Compression을 하여 최대한 평평하게 나오도록 변경.
        **/

        // string directoryPath = @Application.streamingAssetsPath + "/tileImage/";
        // if (Directory.Exists(directoryPath) == false)
        // {
        //     Directory.CreateDirectory(directoryPath);
        // }

        // var pngData = texture.EncodeToJPG();
        // var path = @Application.streamingAssetsPath + "/tileImage/" + "dem" + ".jpg";
        // File.WriteAllBytes(path, pngData);

        return texture;
    }

    public static Texture2D hightValue2Texture2D(float[,] heightMap)
    {

        Texture2D texture = new Texture2D(heightMap.GetLength(0), heightMap.GetLength(1), TextureFormat.RGBA32, false);
        Vector2 textureSize = new Vector2(0, 0);
        Color[] colors = new Color[texture.width * texture.height];
        texture.Apply(true, false);

        for (int r = 0; r < texture.width; r++)
        {
            for (int c = 0; c < texture.height; c++)
            {
                float value = heightMap[r, c];
                Color color = new Color(value, value, value);
                colors[c * texture.width + r] = color;
            }
        }

        foreach (var color in colors)
        {
            texture.SetPixel(
                (int)textureSize.x,
                texture.height - (int)textureSize.y,
                color);

            textureSize.x += 1;

            if (textureSize.x >= texture.width)
            {
                textureSize.x = 0;
                textureSize.y += 1;
            }
        }

        return texture;
    }

    //싱글스레드
    public static IEnumerator medianFilteringCoroutine(MonoBehaviour gameClass, float[,] convolutionList, int smoothFac)
    {
        YieldCollection manager = new YieldCollection();

        for (int i = 1; i <= smoothFac; i++)
        {
            //gameClass.StartCoroutine(manager.CountCoroutine(terrainSmoothHistogram(convolutionList)));
        }

        yield return manager;
    }

    //bilateral filter Task
    public static async Task<bool> bilateralFilterCoroutine(float[,] convolutionList)
    {
        List<Task> tasks = new()
        {
            Task.Run(() =>
            {
                bilateralFilter(convolutionList, 5, 4, 8);
            })
        };

        await Task.WhenAll(tasks);

        return true;
    }

    //medianFilter MultiTask
    public static async Task<bool> testMedianFilteringCoroutine(float[,] convolutionList, int smoothFac)
    {
        List<Task> tasks = new();

        for (int i = 1; i <= smoothFac; i++)
        {
            await Task.Delay(3000);
            tasks.Add(Task.Run(() =>
            {
                terrainSmoothHistogram(convolutionList);
            }));
        }

        await Task.WhenAll(tasks);

        return true;
    }

    //This function will returns average smooth value.
    //상관 필터링 작업
    public static float terrainSmoothAvg(float[,] convolutionList, int x, int y, float smoothFac)
    {
        Vector2 maxRowCol = new Vector2(convolutionList.GetLength(0) - 1, convolutionList.GetLength(1) - 1);
        float avg = 0f;
        float total = 0f;

        for (int cX = x - 1; cX < x + 1; cX++)
        {
            for (int cY = y - 1; cY < y + 1; cY++)
            {
                if (isBoundary(cX, cY, maxRowCol))
                {
                    avg += convolutionList[cX, cY] * smoothFac;
                    total += 1.0f;
                }
            }
        }

        return avg / total;
    }

    // median 필터링 작업 (3by3 kernel)
    //while에서 for문으로 변경 (이유: 속도가 매우 느려서...)
    private static void terrainSmoothHistogram(float[,] convolutionList)
    {
        Vector2 maxRowCol = new Vector2(convolutionList.GetLength(0) - 1, convolutionList.GetLength(1) - 1);
        Dictionary<string, int> usedCoordsDic = new Dictionary<string, int>();

        for (int cX = 0; cX < maxRowCol.x; cX++)
        {
            for (int cY = 0; cY < maxRowCol.y; cY++)
            {
                string coordString = $"{cX}/{cY}";

                if (usedCoordsDic.ContainsKey(coordString))
                {
                    cY++;
                    continue;
                }

                Dictionary<int, int> frequencyDic = new Dictionary<int, int>();
                List<float> contrastList = new List<float>();
                List<DEMCoord> coords = new List<DEMCoord>(); // Maximum 9 elements
                DEMCoord min;
                DEMCoord max;

                for (int x = cX - 1; x <= cX + 1; x++)
                {
                    for (int y = cY - 1; y <= cY + 1; y++)
                    {
                        if (isBoundary(x, y, maxRowCol))
                        {
                            DEMCoord coord = new DEMCoord(convolutionList[x, y], x, y);
                            coords.Add(coord);
                            contrastList.Add(convolutionList[x, y]);
                            usedCoordsDic[$"{x}/{y}"] = 1;
                        }
                    }
                }

                min = coords.OrderBy(i => i.contrast).FirstOrDefault();
                max = coords.OrderByDescending(i => i.contrast).FirstOrDefault();

                if (min != null)
                {
                    float mostFrequentNumber = coords.Sum(i => i.contrast) / coords.Count;
                    convolutionList[min.x, min.y] = mostFrequentNumber;
                }

                if (min != null && max != null && coords.Count == 9 && max.contrast > 0)
                {
                    DEMCoord[,] window = new DEMCoord[3, 3];

                    int idx = 0;

                    for (int x = 0; x < window.GetLength(0); x++)
                    {
                        for (int y = 0; y < window.GetLength(1); y++)
                        {
                            window[x, y] = coords[idx];
                            idx++;
                        }
                    }

                    if (window[1, 1].contrast < max.contrast && window[1, 1].contrast > min.contrast)
                    {
                        convolutionList[window[1, 1].x, window[1, 1].y] = max.contrast;
                    }
                }
            }
        }
    }

    //bilateral filter (specific kernel)
    private static void bilateralFilter(float[,] grayScaleFloat, int kernelSize, int spaceWeight, int colorWeight)
    {
        Vector2 maxRowCol = new Vector2(grayScaleFloat.GetLength(0) - 1, grayScaleFloat.GetLength(1) - 1);

        for (int y = 0; y < maxRowCol.y; y++)
        {
            for (int x = 0; x < maxRowCol.x; x++)
            {
                float sumWeight = 0;
                float sumIntensity = 0;
                float currentColorFloat = grayScaleFloat[x, y]; //현재 색상

                for (int c = -kernelSize; c < kernelSize; c++)
                {
                    for (int r = -kernelSize; r < kernelSize; r++)
                    {
                        if (isBoundary(x + c, y + r, maxRowCol)) // 현재 픽셀이 이미지의 픽셀안에 존재 하는지 체크
                        {
                            //선택된 주변 픽셀 색상
                            float currentNeighborColor = grayScaleFloat[x + c, y + r];
                            //공간 가중치 (타겟 픽셀과 가까울 수록 값이 커짐)
                            double nSpaceWeight = Math.Exp(-Math.Sqrt(Math.Pow(x + c - x, 2.0) + Math.Pow(y + r - y, 2.0)) / 2 * Math.Pow(spaceWeight, 2.0));
                            //색상 가중치 (타겟 픽셀과 색상이 비슷할 수록 값이 커짐)
                            double nColorIntensity = Math.Exp(-Math.Abs(currentColorFloat - currentNeighborColor) / 2 * Math.Pow(colorWeight, 2.0));

                            //kernel에 있는 가중을 모두 더함
                            sumWeight += (float)(nSpaceWeight * nColorIntensity);
                            //전체 가중치
                            sumIntensity += (float)(nSpaceWeight * nColorIntensity * currentNeighborColor);
                        }
                    }
                }
                //가중평균 구함
                grayScaleFloat[x, y] = (float)(sumIntensity / sumWeight);
            }
        }
    }


    // This function will returns bool if pixel is in the dem boundary size.
    private static bool isBoundary(int x, int y, Vector2 maxRowCol)
    {
        return ((x >= 0 && x < maxRowCol.x) && (y >= 0 && y < maxRowCol.y));
    }

    //양선형 보간법
    public static Texture2D resizeTexture2D(Texture2D target, int width, int height)
    {
        Texture2D resizedTexture = new Texture2D(width, height);
        Color[] pixels = resizedTexture.GetPixels();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / (float)width;
                float v = (float)y / (float)height;
                pixels[y * width + x] = target.GetPixelBilinear(u, v);
            }
        }

        resizedTexture.SetPixels(pixels);
        resizedTexture.Apply();

        return resizedTexture;
    }

    //이미지 기하학적 변형
    public static Texture2D geoRemapLensTexture2D(Texture2D target, float radius)
    {
        int scale = 1;
        int tWidth = target.width;
        int tHeight = target.height;
        float[,] heightMap = new float[target.width, target.height];

        for (int terrainY = 0; terrainY < tHeight; terrainY++)
        {
            for (int terrainX = 0; terrainX < tWidth; terrainX++)
            {
                Color heightColor = target.GetPixel(terrainX, terrainY - tHeight);
                heightMap[terrainY, terrainX] = heightColor.r;
            }
        }

        return hightValue2Texture2D(heightMap);
    }

    // Sprite를 사각형의 틀에 맞게 크롭하는 함수
    public static Texture2D cropTexture(Texture2D texture, Rect rect)
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
    public static Texture2D mergeTexture(List<Texture2D> textures, int tileXWay)
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
