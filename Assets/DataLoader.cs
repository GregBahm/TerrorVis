using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;

public static class DataLoader
{
    public static List<TerrorismDataPoint> LoadDataPointsFromSource()
    {
        string dataSourcePath = Application.dataPath + "\\SourceData.csv";
        if (!File.Exists(dataSourcePath))
        {
            Debug.LogError(dataSourcePath + " does not exist. You may need to unzip Assests\\SourceData.zip first.");
            throw new Exception();
        }
        return LoadDataPointsFromSource(dataSourcePath);
    }

    public static List<TerrorismDataPoint> LoadDataPointsFromSource(string dataSourcePath)
    {
        System.IO.StreamReader file = new System.IO.StreamReader(dataSourcePath);
        string line;
        List<TerrorismDataPoint> ret = new List<TerrorismDataPoint>();
        file.ReadLine(); // Skipping the header line
        
        while ((line = file.ReadLine()) != null)
        {
            TerrorismDataPoint dataPoint = TerrorismDataPoint.LoadFromSource(line);
            if(dataPoint.Deaths > 0)
            {
                ret.Add(dataPoint);
            }
        }

        file.Close();
        return ret;
    }

    public static void SaveDataPoints(List<TerrorismDataPoint> data, string preprocessedDataPath)
    {
        XmlDocument doc = new XmlDocument();
        doc.AppendChild(doc.CreateElement("Root"));
        foreach (TerrorismDataPoint datum in data)
        {
            doc.DocumentElement.AppendChild(datum.ToXml(doc));
        }
        doc.Save(preprocessedDataPath);
    }

    public static List<TerrorismDataPoint> LoadDataPointsFromPreprocess(string preprocessedDataPath)
    {
        List<TerrorismDataPoint> ret = new List<TerrorismDataPoint>();
        XmlDocument doc = new XmlDocument();
        doc.Load(preprocessedDataPath);
        foreach (XmlElement node in doc.DocumentElement.ChildNodes)
        {
            TerrorismDataPoint point = TerrorismDataPoint.FromXml(node);
            ret.Add(point);
        }
        return ret;
    }
}

public enum AttackSource
{
    International,
    Domestic,
    Unknown
}

public class TerrorismDataPoint
{
    public readonly float Lat;
    public readonly float Long;
    public readonly int Deaths;
    public readonly AttackSource AttackSource;
    public readonly DateTime Time;

    public TerrorismDataPoint(string latString, string longString, string deathsString, AttackSource attackSource, DateTime time)
    { 
        if (latString == "" || longString == "")
        {
            return;
        }
        Lat = Convert.ToSingle(latString);
        Long = Convert.ToSingle(longString);
        AttackSource = attackSource;
        if (deathsString != "")
        {
            Deaths = Convert.ToInt32(deathsString);
        }
        Time = time;
    }

    public static TerrorismDataPoint LoadFromSource(string rawLine)
    {
        List<string> splitLine = RegexParser.SplitCSV(rawLine);
        string latString = splitLine[13];
        string longString = splitLine[14];
        string yearString = splitLine[1];
        string monthString = splitLine[2];
        string dayString = splitLine[3];
        string deathsString = splitLine[98];
        string logisticalString = splitLine[133];
        
        AttackSource attackSource = GetAttackSourceFromString(logisticalString);

        int year = Convert.ToInt32(yearString);
        int month = Convert.ToInt32(monthString);
        if (month == 0)
        {
            month = 1;
        }
        int day = Convert.ToInt32(dayString);
        if (day == 0)
        {
            day = 1;
        }
        DateTime time = new DateTime(year, month, day);
        return new TerrorismDataPoint(latString, longString, deathsString, attackSource, time);
    }

    private static AttackSource GetAttackSourceFromString(string logisticalString)
    {
        if(logisticalString == "0")
        {
            return AttackSource.Domestic;
        }
        if(logisticalString == "1")
        {
            return AttackSource.International;
        }
        return AttackSource.Unknown;
    }

    public const string XmlNodeName = "DataPoint";
    public const string XmlLatNodeName = "Lat";
    public const string XmlLongNodeName = "Long";
    public const string XmlDeathsNodeName = "Deaths";
    public const string XmlAttackSourceNodeName = "Domestic";
    public const string XmlDateNodeName = "Time";

    public XmlElement ToXml(XmlDocument doc)
    {
        XmlElement ret = doc.CreateElement(XmlNodeName);
        
        XmlElement latNode = doc.CreateElement(XmlLatNodeName);
        latNode.InnerText = Lat.ToString();

        XmlElement longNode = doc.CreateElement(XmlLongNodeName);
        longNode.InnerText = Long.ToString();

        XmlElement deathsNode = doc.CreateElement(XmlDeathsNodeName);
        deathsNode.InnerText = Deaths.ToString();

        XmlElement domesticNode = doc.CreateElement(XmlAttackSourceNodeName);
        domesticNode.InnerText = ((int)AttackSource).ToString();

        XmlElement dateNode = doc.CreateElement(XmlDateNodeName);
        dateNode.InnerText = Time.Ticks.ToString();

        ret.AppendChild(latNode);
        ret.AppendChild(longNode);
        ret.AppendChild(deathsNode);
        ret.AppendChild(domesticNode);
        ret.AppendChild(dateNode);

        return ret;
    }

    public static TerrorismDataPoint FromXml(XmlElement node)
    {
        string latString = node.SelectSingleNode(XmlLatNodeName).InnerText;
        string longString = node.SelectSingleNode(XmlLongNodeName).InnerText;
        string deathsString = node.SelectSingleNode(XmlDeathsNodeName).InnerText;
        string domesticString = node.SelectSingleNode(XmlDeathsNodeName).InnerText;
        string timeTicks = node.SelectSingleNode(XmlDateNodeName).InnerText;
        string attackSourceString = node.SelectSingleNode(XmlAttackSourceNodeName).InnerText;
        long ticks = Convert.ToInt64(timeTicks);
        DateTime time = new DateTime(ticks);
        AttackSource attackSource = (AttackSource)Convert.ToInt32(attackSourceString);
        return new TerrorismDataPoint(latString, longString, deathsString, attackSource, time);
    }
}

public static class RegexParser
{
    private static Regex csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)");

    public static List<string> SplitCSV(string input)
    {
        List<string> list = new List<string>();
        string curr = null;
        foreach (Match match in csvSplit.Matches(input))
        {
            curr = match.Value;
            if (0 == curr.Length)
            {
                list.Add("");
            }

            list.Add(curr.TrimStart(','));
        }

        return list;
    }
}