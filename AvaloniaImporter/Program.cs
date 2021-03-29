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
            var copyNotice = $@"
<!-- 
    Copyright (c) Microsoft Corporation. 
    Licensed under the MIT license.
    This file is auto generated, Do not make edits or they will be removed later.
-->
";

            stringBuilder.Insert(0, copyNotice);

            return stringBuilder.ToString();
        }

        static void Main(string[] args)
        {
            var curDir = Directory.GetCurrentDirectory();
            var absSvgPath = Path.GetFullPath(Path.Combine(curDir, "../assets"));

            File.WriteAllText("FluentUiIcons_Regular.xaml", GenerateAvaloniaFluentIcons(absSvgPath, "regular"));

            File.WriteAllText("FluentUiIcons_Filled.xaml", GenerateAvaloniaFluentIcons(absSvgPath, "filled"));
            
            File.WriteAllText("icons_avalonia.md", GenerateAvaloniaFluentIcons(absSvgPath, "regular", true));

        }

        static string GenerateAvaloniaFluentIcons(string absSvgPath, string designator, bool markdownMode = false)
        {
            var svgPaths = Directory.EnumerateFiles(absSvgPath, "*.svg", new EnumerationOptions() { RecurseSubdirectories = true })
            .Where(x => x.Contains("_regular"))
            .ToList();

            var entries = new Dictionary<string, List<string>>();
            var highestResVersion = new Dictionary<string, string>();
            var iconPaths = new Dictionary<string, (string path, string streamgeoxaml, string streamgeoraw)>();

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


                var key = $"{entry.Key.Replace(' ', '_').ToLowerInvariant()}_{designator.ToLowerInvariant()}";

                var pathNodes = xmlDoc.SelectNodes("//path");

                var pathAccumulator = "";

                foreach (var pathNode in pathNodes.Cast<XmlNode>())
                {
                    var pathData = pathNode.Attributes["d"].Value + " ";
                    pathAccumulator += pathData;
                }
                
                var finalDG = $@"<StreamGeometry x:Key=""{key}"">{pathAccumulator.Trim()}</StreamGeometry>";

                iconPaths.Add(entry.Key, (Path.GetRelativePath(absSvgPath + "/../", entry.Value), finalDG, pathAccumulator.Trim()));
            }


            if (markdownMode)
            {
                var outMarkdown = new StringBuilder("");

                // outMarkdown.AppendLine(@" This file is generated using AvaloniaImporter -->");
                
                foreach (var v in iconPaths.Select(x => x))
                {
                    outMarkdown.Append("##### ");
                    outMarkdown.Append(v.Key);
                    outMarkdown.Append(" ![img](");
                    outMarkdown.Append("https://raw.githubusercontent.com/jmacato/fluentui-system-icons/master/");
                    outMarkdown.Append(v.Value.path);
                    outMarkdown.AppendLine(")");
                    outMarkdown.AppendLine();
                    outMarkdown.AppendLine("<details>");
                    outMarkdown.AppendLine("<summary>Code</summary>");
                    outMarkdown.AppendLine();
                    outMarkdown.AppendLine("```");
                    outMarkdown.AppendLine(v.Value.streamgeoxaml);
                    outMarkdown.AppendLine("```");
                    outMarkdown.AppendLine();
                    outMarkdown.AppendLine("</details>");        
                    outMarkdown.AppendLine();
                }

                return outMarkdown.ToString();
            }
            
            var outXml = new StringBuilder("");

            
            outXml.AppendLine(@"<Styles xmlns=""https://github.com/avaloniaui""");
            outXml.AppendLine(@"xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">");
            outXml.AppendLine(@"<Design.PreviewWith>");
            
            

            
            foreach (var keyName in iconPaths.Select(x => x.Key))
            {
                outXml.AppendLine(@"<StackPanel Orientation=""Horizontal"">");
                outXml.AppendLine($@"<PathIcon Data=""{{DynamicResource {keyName}}}"" />");
                outXml.AppendLine($@"<TextBlock Margin=""10 0""  Text=""{keyName}"" />");
                outXml.AppendLine(@"</StackPanel>");
            }            
            
            outXml.AppendLine(@"</Design.PreviewWith>");
            outXml.AppendLine(@"<Styles.Resources>");

            
            foreach (var iconPath in iconPaths.Select(x => x.Value))
                outXml.AppendLine(iconPath.streamgeoxaml);

            outXml.AppendLine(@"</Styles.Resources>");
            outXml.AppendLine(@"</Styles>");

            return PrettyXml(outXml.ToString());
        }
    }
}
