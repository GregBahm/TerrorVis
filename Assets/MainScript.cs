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

    [Range(0, 1)]
    public float MinTime;
    [Range(0, 1)]
    public float MaxTime;

    private const int TextureResolution = 256;
    private const int GroupSize = 128;
    private int _groupsToComputeKillmap;
    private int _groupsToClear;
    private int _computeKernel;
    private int _clearKernel;
    private int _dataCount;
    private ComputeBuffer _dataBuffer;
    private ComputeBuffer _foreignKillsBuffer;
    private ComputeBuffer _domesticKillsBuffer;
    private const int DataBufferStride = sizeof(float) * 2 + // Lat and Long
                                        sizeof(int) + // ForeignKills
                                        sizeof(int) + // DomesticKills
                                        sizeof(float); // Time

    struct BufferPoint
    {
        public Vector2 Pos;
        public int ForeignKills;
        public int DomesticKills;
        public float Time;
    }

    void Start()
    {
        string preprocessedDataPath = Application.dataPath + "\\PreprocessedDataPath.xml";
        //RefreshSource(preprocessedDataPath);
        List<TerrorismDataPoint> data = DataLoader.LoadDataPointsFromPreprocess(preprocessedDataPath);

        _computeKernel = HeatmapCompute.FindKernel("HeatmapCompute");
        _clearKernel = HeatmapCompute.FindKernel("ClearBuffers");
        _dataBuffer = GetDataBuffer(data);
        _dataCount = data.Count;
        _foreignKillsBuffer = new ComputeBuffer(TextureResolution * TextureResolution, sizeof(int));
        _domesticKillsBuffer = new ComputeBuffer(TextureResolution * TextureResolution, sizeof(int));
        _groupsToComputeKillmap = Mathf.CeilToInt((float)data.Count / GroupSize);
        _groupsToClear = (512 * 512) / GroupSize;
        
    }

    private void ProcessHeatmap()
    {
        HeatmapCompute.SetInt("_MaxThread", _dataCount);
        HeatmapCompute.SetBuffer(_clearKernel, "_ForeignKillsBuffer", _foreignKillsBuffer);
        HeatmapCompute.SetBuffer(_clearKernel, "_DomesticKilsBuffer", _domesticKillsBuffer);
        HeatmapCompute.Dispatch(_clearKernel, _groupsToClear, 1, 1);
        
        HeatmapCompute.SetFloat("_MinTime", MinTime);
        HeatmapCompute.SetFloat("_MaxTime", MaxTime);
        HeatmapCompute.SetBuffer(_computeKernel, "_DataBuffer", _dataBuffer);
        HeatmapCompute.SetBuffer(_computeKernel, "_ForeignKillsBuffer", _foreignKillsBuffer);
        HeatmapCompute.SetBuffer(_computeKernel, "_DomesticKilsBuffer", _domesticKillsBuffer);
        HeatmapCompute.Dispatch(_computeKernel, _groupsToComputeKillmap, 1, 1);
    }

    private void Update()
    {
        MinTime = Mathf.Min(MinTime, MaxTime);
        MaxTime = Mathf.Max(MinTime, MaxTime);
        ProcessHeatmap();

        Mat.SetBuffer("_ForeignKillsBuffer", _foreignKillsBuffer);
        Mat.SetBuffer("_DomesticKilsBuffer", _domesticKillsBuffer);
    }

    private ComputeBuffer GetDataBuffer(List<TerrorismDataPoint> sourceData)
    {
        long start = sourceData.Min(item => item.Time.Ticks);
        long end = sourceData.Max(item => item.Time.Ticks);
        ComputeBuffer ret = new ComputeBuffer(sourceData.Count, DataBufferStride);
        BufferPoint[] bufferData = new BufferPoint[sourceData.Count];
        for (int i = 0; i < sourceData.Count; i++)
        {
            BufferPoint newPoint = ToBufferPoint(sourceData[i], start, end);
            bufferData[i] = newPoint;
        }
        ret.SetData(bufferData);
        return ret;
    }

    private static BufferPoint ToBufferPoint(TerrorismDataPoint dataPoint, long start, long end)
    {
        float normalizedLat = (dataPoint.Lat + 90f) / 180f;
        float normalizedLong = (dataPoint.Long + 180f) / 360f;
        float attackSource = ToAttackSourceWeight(dataPoint.AttackSource);
        float normalizedTime = (float)((double)(dataPoint.Time.Ticks - start) / (end - start));
        Vector2 pos = new Vector2(normalizedLat, normalizedLong);
        int foreignKills = (int)((double)dataPoint.Deaths * attackSource);
        int domesticKills = (int)((double)dataPoint.Deaths * (1 - attackSource));
        return new BufferPoint()
        {
            Pos = pos,
            ForeignKills = foreignKills,
            DomesticKills = domesticKills,
            Time = normalizedTime
        };
    }

    private static float ToAttackSourceWeight(AttackSource attackSource)
    {
        switch (attackSource)
        {
            case AttackSource.International:
                return 1;
            case AttackSource.Domestic:
                return 0;
            case AttackSource.Unknown:
            default:
                return .5f;
        }
    }

    private void RefreshSource(string preprocessedDataPath)
    {
        List<TerrorismDataPoint> data = DataLoader.LoadDataPointsFromSource();
        DataLoader.SaveDataPoints(data, preprocessedDataPath);
    }

    private void OnDestroy()
    {
        _dataBuffer.Release();
        _foreignKillsBuffer.Release();
        _domesticKillsBuffer.Release();
    }
}