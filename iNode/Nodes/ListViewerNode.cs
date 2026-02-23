// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ListViewerNode.cs - Displays list/tree structure on the node face
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Displays the full structure and contents of a DataList on the node body.
    /// Shows items with indices, nested structure with indentation, and types.
    /// Useful for debugging list/tree data flow.
    /// </summary>
    public class ListViewerNode : Node
    {
        public override string TypeName => "ListViewer";
        public override string Title => "List Viewer";
        public override string Category => "Utility";

        /// <summary>The formatted structure text (displayed on node face).</summary>
        public string StructureText { get; set; } = "(empty)";

        public ListViewerNode()
        {
            AddInput("List", "List", PortDataType.Any, null);
            AddOutput("List", "Pass-through", PortDataType.Any);
        }

        protected override void Compute()
        {
            var val = GetInput("List")!.GetEffectiveValue();

            if (val is DataList dl)
            {
                StructureText = FormatTree(dl, "", true);
            }
            else if (val is FaceListData fl)
            {
                StructureText = $"Faces [{fl.Faces.Count}]:\n";
                for (int i = 0; i < Math.Min(fl.Faces.Count, 15); i++)
                    StructureText += $"  [{i}] Face\n";
                if (fl.Faces.Count > 15) StructureText += $"  ... +{fl.Faces.Count - 15} more\n";
            }
            else if (val is EdgeListData el)
            {
                StructureText = $"Edges [{el.Edges.Count}]:\n";
                for (int i = 0; i < Math.Min(el.Edges.Count, 15); i++)
                    StructureText += $"  [{i}] Edge\n";
                if (el.Edges.Count > 15) StructureText += $"  ... +{el.Edges.Count - 15} more\n";
            }
            else if (val != null)
            {
                StructureText = $"Single value: {FormatValue(val)}";
            }
            else
            {
                StructureText = "(null)";
            }

            // Pass through
            GetOutput("List")!.Value = val;
        }

        public override string? GetDisplaySummary() => StructureText;

        private static string FormatTree(DataList list, string prefix, bool isRoot)
        {
            var lines = new List<string>();
            string header = isRoot ? $"List [{list.Count}]" : $"List [{list.Count}]";
            lines.Add($"{prefix}{header}");

            for (int i = 0; i < list.Count && i < 30; i++)
            {
                var item = list.Items[i];
                bool isLast = (i == list.Count - 1) || (i == 29 && list.Count > 30);
                string connector = isLast ? "└─" : "├─";
                string childPrefix = prefix + (isLast ? "  " : "│ ");

                if (item is DataList sub)
                {
                    lines.Add($"{prefix}{connector} [{i}] {FormatTree(sub, childPrefix, false).TrimStart()}");
                }
                else
                {
                    lines.Add($"{prefix}{connector} [{i}] {FormatValue(item)}");
                }
            }

            if (list.Count > 30)
                lines.Add($"{prefix}   ... +{list.Count - 30} more");

            return string.Join("\n", lines);
        }

        private static string FormatValue(object? val)
        {
            if (val == null) return "(null)";
            if (val is double d) return d.ToString("G6");
            if (val is bool b) return b ? "True" : "False";
            if (val is ValueTuple<double, double, double> pt)
                return $"({pt.Item1:F2}, {pt.Item2:F2}, {pt.Item3:F2})";
            if (val is BodyData bd) return bd.Description;
            if (val is string s) return $"\"{s}\"";
            return val.ToString() ?? "???";
        }
    }
}
