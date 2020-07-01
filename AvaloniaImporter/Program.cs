using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AvaloniaImporter
{
    class Program
    {
        static string PrettyXml(string xml)
        {
            var stringBuilder = new StringBuilder();

            var element = XElement.Parse(xml);

            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;

            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                element.Save(xmlWriter);
            }

            return stringBuilder.ToString();
        }

        static void Main(string[] args)
        {
            var curDir = Directory.GetCurrentDirectory();
            var absSvgPath = Path.GetFullPath(Path.Combine(curDir, "../assets"));

            File.WriteAllText("FluentUiIcons_Regular.xaml", GenerateAvaloniaFluentIcons(absSvgPath, "regular"));

            File.WriteAllText("FluentUiIcons_Filled.xaml", GenerateAvaloniaFluentIcons(absSvgPath, "filled"));
        }

        static string GenerateAvaloniaFluentIcons(string absSvgPath, string designator)
        {
            var svgPaths = Directory.EnumerateFiles(absSvgPath, "*.svg", new EnumerationOptions() { RecurseSubdirectories = true })
            .Where(x => x.Contains("_regular"))
            .ToList();

            var entries = new Dictionary<string, List<string>>();
            var highestResVersion = new Dictionary<string, string>();
            var iconPaths = new Dictionary<string, string>();

            foreach (var entry in svgPaths.Select(x => (x.Replace(absSvgPath, string.Empty).Split('/')[1], x)))
            {
                if (!entries.ContainsKey(entry.Item1))
                    entries.Add(entry.Item1, new List<string>());

                entries[entry.Item1].Add(entry.x);
            }


            foreach (var entry in entries.Select(x => (x.Key, x.Value.OrderByDescending(x => x).First())))
            {
                if (!highestResVersion.ContainsKey(entry.Item1))
                    highestResVersion.Add(entry.Item1, string.Empty);

                highestResVersion[entry.Item1] = entry.Item2;
            }


            foreach (var entry in highestResVersion)
            {
                var path = entry.Value;

                var xmlDoc = new XmlDocument(); // Create an XML document object    
                var xmlStr = File.ReadAllText(path);

                xmlStr = Regex.Replace(xmlStr, @"xmlns(:\w+)?=""([^""]+)""|xsi(:\w+)?=""([^""]+)""", "");

                xmlDoc.LoadXml(xmlStr); // Load the XML document from the specified file

                // Get elements
                var svgNode = xmlDoc.SelectSingleNode("/svg");
                var width = svgNode.Attributes["width"].Value.Replace("px", string.Empty);
                var height = svgNode.Attributes["height"].Value.Replace("px", string.Empty);

                var ghostRect = $"M0,0 {width},0 {width},{height} 0,{height}";

                var finalDG = $@"<DrawingGroup x:Key=""{entry.Key.Replace(' ', '_').ToLowerInvariant()}_{designator.ToLowerInvariant()}""><GeometryDrawing Brush=""#00000000"" Geometry=""{ghostRect}"" />";

                var pathNodes = xmlDoc.SelectNodes("//path");

                foreach (var pathNode in pathNodes.Cast<XmlNode>())
                {
                    var pathData = pathNode.Attributes["d"].Value;
                    // var brush = pathNode.ParentNode.Attributes["fill"].Value.ToUpperInvariant();

                    finalDG += $@"<GeometryDrawing Brush=""#FF000000"" Geometry=""{pathData}"" />";

                }

                finalDG += $@"</DrawingGroup>";

                iconPaths.Add(entry.Key, finalDG);
            }

            var outXml = new StringBuilder($@"
            <!-- 
            Copyright (c) Microsoft Corporation. 
            Licensed under the MIT license.
            This file is auto generated, Do not make edits or they will be removed later.
            -->
            <Styles xmlns=""https://github.com/avaloniaui"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""><Style><Style.Resources>");

            foreach (var iconPath in iconPaths.Select(x => x.Value))
                outXml.Append(iconPath);

            outXml.Append(@"</Style.Resources></Style></Styles>");

            return PrettyXml(outXml.ToString());
        }
    }
}
