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

            Console.WriteLine($"Using assets from: {absSvgPath}");
            File.WriteAllText("icons_avalonia.html", GenerateAvaloniaFluentIcons(absSvgPath));

        }

        static string GenerateAvaloniaFluentIcons(string absSvgPath)
        {
            var svgPaths = Directory.EnumerateFiles(absSvgPath, "*.svg", new EnumerationOptions() { RecurseSubdirectories = true })
                .ToList();

            var entries = new Dictionary<string, List<string>>();
            var highestResVersion = new Dictionary<string, string>();
            var iconPaths = new Dictionary<string, (string path, string streamgeoxaml, string streamgeoraw)>();

            var entryNames = svgPaths.Select(x => (x.Replace(absSvgPath, string.Empty).Split(Path.DirectorySeparatorChar.ToString())[1], x));

            foreach (var entry in entryNames) {
                var designator = entry.Item2.Split('_').Last().Replace(".svg", string.Empty);
                var designatedKey = entry.Item1 + $" {designator}";

                if (!entries.ContainsKey(designatedKey)) {
                    entries.Add(designatedKey, new List<string>());
                }

                entries[designatedKey].Add(entry.x);
            }

            Console.WriteLine($"Found {entries.Count} unique icons");

            foreach (var entry in entries.Select(x => (x.Key, x.Value.OrderByDescending(x => x).First()))) {
                if (!highestResVersion.ContainsKey(entry.Item1)) {
                    highestResVersion.Add(entry.Item1, string.Empty);
                }

                highestResVersion[entry.Item1] = entry.Item2;
            }


            foreach (var entry in highestResVersion)
            {
                var path = entry.Value;
                var fileName = Path.GetFileNameWithoutExtension(path);
                var designator = fileName.Split('_').Last();

                var xmlDoc = new XmlDocument(); // Create an XML document object    
                var xmlStr = File.ReadAllText(path);

                xmlStr = Regex.Replace(xmlStr, @"xmlns(:\w+)?=""([^""]+)""|xsi(:\w+)?=""([^""]+)""", "");

                xmlDoc.LoadXml(xmlStr); // Load the XML document from the specified file

                // Get elements
                var svgNode = xmlDoc.SelectSingleNode("/svg");
                var key = $"{entry.Key.Replace(' ', '_').ToLowerInvariant()}";

                var pathNodes = xmlDoc.SelectNodes("//path");
                var pathAccumulator = "";

                foreach (var pathNode in pathNodes.Cast<XmlNode>()) {
                    var pathData = pathNode.Attributes["d"].Value + " ";
                    pathAccumulator += pathData;
                }
                
                var finalDG = $@"<StreamGeometry x:Key=""{key}"">{pathAccumulator.Trim()}</StreamGeometry>";
                iconPaths.Add(key, (Path.GetRelativePath(absSvgPath + "/../", entry.Value), finalDG, pathAccumulator.Trim()));
            }


            var outHtml = new StringBuilder("");
            foreach (var v in iconPaths.Select(x => x)) {
               var h =
                   $@"<p style=""display: inline-flex;""><img src=""https://raw.githubusercontent.com/AvaloniaUI/fluentui-system-icons/master/{v.Value.path}"" width=""24"" height=""24"">{v.Key}<div class=""language-xml highlighter-rouge"" style=""max-width: 800px;""> <div class=""highlight""> <pre class=""highlight""><code><span class=""nt"">&lt;StreamGeometry</span> <span class=""na"">x:Key=</span><span class=""s"">&quot;{v.Key}&quot;</span><span class=""nt"">&gt;</span>{v.Value.streamgeoraw}<span class=""nt"">&lt;/StreamGeometry&gt;</span></code></pre> </div> </div> </p>";
                outHtml.AppendLine(h);
            }

            return outHtml.ToString();
        }
    }
}
