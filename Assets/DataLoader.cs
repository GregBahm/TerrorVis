using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

public static class DataLoader
{
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

public class TerrorismDataPoint
{
    public readonly float Lat;
    public readonly float Long;
    public readonly int Deaths;
    public readonly DateTime Time;

    public TerrorismDataPoint(string latString, string longString, string deathsString, DateTime time)
    { 
        if (latString == "" || longString == "")
        {
            return;
        }
        Lat = Convert.ToSingle(latString);
        Long = Convert.ToSingle(longString);
        if (deathsString != "")
        {
            Deaths = Convert.ToInt32(deathsString);
        }
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
        return new TerrorismDataPoint(latString, longString, deathsString, time);
    }

    public const string XmlNodeName = "DataPoint";
    public const string XmlLatNodeName = "Lat";
    public const string XmlLongNodeName = "Long";
    public const string XmlDeathsNodeName = "Deaths";
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

        XmlElement dateNode = doc.CreateElement(XmlDateNodeName);
        dateNode.InnerText = Time.Ticks.ToString();

        ret.AppendChild(latNode);
        ret.AppendChild(longNode);
        ret.AppendChild(deathsNode);
        ret.AppendChild(dateNode);

        return ret;
    }

    public static TerrorismDataPoint FromXml(XmlElement node)
    {
        string latString = node.SelectSingleNode(XmlLatNodeName).InnerText;
        string longString = node.SelectSingleNode(XmlLongNodeName).InnerText;
        string deathsString = node.SelectSingleNode(XmlDeathsNodeName).InnerText;
        string timeTicks = node.SelectSingleNode(XmlDateNodeName).InnerText;
        long ticks = Convert.ToInt64(timeTicks);
        DateTime time = new DateTime(ticks);
        return new TerrorismDataPoint(latString, longString, deathsString, time);
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