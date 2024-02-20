using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

public class NetworkVO
{

    //TODO: Network Functions
    public static String queryParameterMaker(Dictionary<String, String> data)
    {
        var queryStringBuilder = new StringBuilder();

        foreach (var parameter in data)
        {
            if (queryStringBuilder.Length > 0) queryStringBuilder.Append("&");
            queryStringBuilder.Append($"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}");
        }

        return queryStringBuilder.ToString();
    }

    public static async Task<T> reqAPI<T>(string url, NetworkEnum reqType, string data = null)
    {
        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            Encoding encoding = Encoding.UTF8;

            switch (reqType)
            {
                case NetworkEnum.POST:
                    request.Method = "POST";
                    break;
                case NetworkEnum.GET:
                    request.Method = "GET";
                    break;
                default:
                    request.Method = "GET";
                    break;
            }

            if (data != null)
            {
                request.Timeout = 30 * 1000;
                // request.Headers.Add("Authorization", "BASIC SGVsbG8=");
                request.Accept = "Application/json";
                byte[] jsonByteData = encoding.GetBytes(data);
                request.ContentLength = jsonByteData.Length;
                Stream stream = request.GetRequestStream();
                stream.Write(jsonByteData, 0, jsonByteData.Length);
                stream.Close();
            }

            HttpWebResponse httpRes = (HttpWebResponse)await request.GetResponseAsync();
            Stream resultStream = httpRes.GetResponseStream();
            StreamReader reader = new StreamReader(resultStream, Encoding.Default);

            if (httpRes.StatusCode != HttpStatusCode.OK)
            {
                var resData = reader.ReadToEnd();
                //throw new Exception($"Network Status Code {httpRes.StatusCode}, Message: {resData}");
                Debug.Log($"Res Error Data from server: {resData}");
                return (T)Convert.ChangeType(resData, typeof(T));
            }

            if (httpRes.Headers["Content-Type"] != "Application/json" &&
            httpRes.Headers["Content-Type"] != "application/json; charset=UTF-8" &&
            httpRes.Headers["Content-Type"] != "application/json"
            )
            {
                MemoryStream memStream = new MemoryStream();
                int bytesRead;
                byte[] byteArr = new byte[1024];

                while ((bytesRead = resultStream.Read(byteArr, 0, byteArr.Length)) > 0)
                {
                    memStream.Write(byteArr, 0, bytesRead);
                }

                return (T)Convert.ChangeType(memStream.ToArray(), typeof(T));
            }
            else
            {
                var resData = reader.ReadToEnd();
                Debug.Log($"Res Data from server: {resData}");
                return (T)Convert.ChangeType(resData, typeof(T));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"error {e}");
            throw e;
        }
    }
}

public enum NetworkEnum
{
    POST,
    GET
}
