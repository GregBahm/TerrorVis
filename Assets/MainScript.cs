using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class MainScript : MonoBehaviour 
{
    public Material Mat;
    public ComputeShader HeatmapCompute;

    private const int TextureResolution = 512;
    public RenderTexture _heatmapTexture;
    private const int GroupSize = 128;
    private int _groupsToDispatch;
    private int _computeKernel;
    private ComputeBuffer _dataBuffer;
    private const int DataBufferStride = sizeof(float) * 2 + // Lat and Long
                                        sizeof(int); // Deaths

    struct BufferPoint
    {
        public Vector2 Pos;
        public int Deaths;
    }

	void Start () 
    {
        string preprocessedDataPath = Application.dataPath + "\\PreprocessedDataPath.xml";
        List<TerrorismDataPoint> data = DataLoader.LoadDataPointsFromPreprocess(preprocessedDataPath);

        _heatmapTexture = new RenderTexture(TextureResolution, TextureResolution, 0, RenderTextureFormat.RFloat);
        _heatmapTexture.enableRandomWrite = true;
        _heatmapTexture.Create();

        _computeKernel = HeatmapCompute.FindKernel("HeatmapCompute");
        _dataBuffer = GetDataBuffer(data);
        _groupsToDispatch = Mathf.CeilToInt(data.Count / GroupSize);

        ProcessHeatmap();
	}

    private void ProcessHeatmap()
    {
        HeatmapCompute.SetTexture(_computeKernel, "_HeatmapTexture", _heatmapTexture);
        HeatmapCompute.SetBuffer(_computeKernel, "_DataBuffer", _dataBuffer);
        HeatmapCompute.Dispatch(_computeKernel, _groupsToDispatch, 1, 1);
    }

    private void Update()
    {

        Mat.SetTexture("_MainTex", _heatmapTexture);
    }

    private ComputeBuffer GetDataBuffer(List<TerrorismDataPoint> sourceData)
    {
        ComputeBuffer ret = new ComputeBuffer(sourceData.Count, DataBufferStride);
        BufferPoint[] bufferData = sourceData.Select(ToBufferPoint).ToArray();
        ret.SetData(bufferData);
        return ret;
    }

    private static BufferPoint ToBufferPoint(TerrorismDataPoint dataPoint)
    {
        float normalizedLat = (dataPoint.Lat + 90f) / 180f;
        float normalizedLong = (dataPoint.Long + 180f) / 360f;
        return new BufferPoint() { Pos = new Vector2(normalizedLat, normalizedLong), Deaths = dataPoint.Deaths };
    }

    private void RefreshSource(string preprocessedDataPath)
    {
        string dataSourcePath = Application.dataPath + "\\SourceData.csv";
        if(!File.Exists(dataSourcePath))
        {
            Debug.LogError(dataSourcePath + " does not exist. You may need to unzip Assests\\SourceData.zip first.");
            return;
        }
        List<TerrorismDataPoint> data = DataLoader.LoadDataPointsFromSource(dataSourcePath);
        DataLoader.SaveDataPoints(data, preprocessedDataPath);
    }

    private void OnDestroy()
    {
        _dataBuffer.Release();
        _heatmapTexture.Release();
    }
}
