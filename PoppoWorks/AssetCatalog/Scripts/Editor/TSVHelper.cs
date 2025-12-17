// SPDX-License-Identifier: CC0-1.0

using System.Collections.Generic;
using System.IO;

namespace AssetCatalog.Editor
{
    public static class TSVHelper
    {
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        public static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\r", "\r");
        }

        public static void SaveCatalog(string path, EntryGroup[] groups)
        {
            var lines = new List<string>();
            lines.Add("category\ttitle\tcomment\turl");

            if (groups != null)
            {
                foreach (var group in groups)
                {
                    if (group.entries != null)
                    {
                        foreach (var entry in group.entries)
                        {
                            lines.Add($"{Escape(group.groupName)}\t{Escape(entry.entryName)}\t{Escape(entry.entryNote)}\t{Escape(entry.entryLink)}");
                        }
                    }
                }
            }

            File.WriteAllLines(path, lines);
        }

        public const string SPACER_MARKER = "__SPACER__";

        public static EntryGroup[] LoadCatalog(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return new EntryGroup[0];

            var result = new List<EntryGroup>();
            string currentGroup = null;
            List<CatalogEntry> currentEntries = null;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];

                // 空行 = スペーサー
                if (string.IsNullOrWhiteSpace(line))
                {
                    // 現在のグループを保存
                    if (currentGroup != null && currentEntries != null)
                    {
                        result.Add(new EntryGroup { groupName = currentGroup, entries = currentEntries.ToArray() });
                        currentGroup = null;
                        currentEntries = null;
                    }
                    // スペーサーグループを追加
                    result.Add(new EntryGroup { groupName = SPACER_MARKER, entries = new CatalogEntry[0] });
                    continue;
                }

                var values = line.Split('\t');
                if (values.Length >= 4)
                {
                    string groupName = Unescape(values[0]);
                    var entry = new CatalogEntry
                    {
                        entryName = Unescape(values[1]),
                        entryNote = Unescape(values[2]),
                        entryLink = Unescape(values[3])
                    };

                    // グループが変わったら保存
                    if (currentGroup != groupName)
                    {
                        if (currentGroup != null && currentEntries != null)
                        {
                            result.Add(new EntryGroup { groupName = currentGroup, entries = currentEntries.ToArray() });
                        }
                        currentGroup = groupName;
                        currentEntries = new List<CatalogEntry>();
                    }

                    currentEntries.Add(entry);
                }
            }

            // 最後のグループを保存
            if (currentGroup != null && currentEntries != null)
            {
                result.Add(new EntryGroup { groupName = currentGroup, entries = currentEntries.ToArray() });
            }

            return result.ToArray();
        }

        public static void SaveQRSettings(string path, string category, string title, string comment, string link)
        {
            var lines = new string[]
            {
                "category\ttitle\tcomment\turl",
                $"{Escape(category)}\t{Escape(title)}\t{Escape(comment)}\t{Escape(link)}"
            };
            File.WriteAllLines(path, lines);
        }

        public static (string category, string title, string comment, string link) LoadQRSettings(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return ("", "", "", "");

            var values = lines[1].Split('\t');
            if (values.Length >= 4)
            {
                return (Unescape(values[0]), Unescape(values[1]), Unescape(values[2]), Unescape(values[3]));
            }

            return ("", "", "", "");
        }
    }
}
