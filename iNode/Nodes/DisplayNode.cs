// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// DisplayNode.cs - Shows a value as text (debugging utility)
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Displays an input value as text. Non-functional pass-through node
    /// useful for debugging workflows by showing intermediate values.
    /// </summary>
    public class DisplayNode : Node
    {
        public override string TypeName => "Display";
        public override string Title => "Display";
        public override string Category => "Utility";

        /// <summary>The last displayed text (shown on the node face).</summary>
        public string DisplayText { get; set; } = "";

        public DisplayNode()
        {
            AddInput("Value", "Value", PortDataType.Any, null);
            AddOutput("Value", "Pass-through", PortDataType.Any);
        }

        protected override void Compute()
        {
            var val = GetInput("Value")!.GetEffectiveValue();

            if (val == null)
            {
                DisplayText = "(null)";
            }
            else if (val is double d)
            {
                DisplayText = d.ToString("G6");
            }
            else if (val is ValueTuple<double, double, double> pt)
            {
                DisplayText = $"({pt.Item1:F2}, {pt.Item2:F2}, {pt.Item3:F2})";
            }
            else if (val is BodyData bd)
            {
                DisplayText = bd.Description;
            }
            else if (val is FaceListData fl)
            {
                DisplayText = fl.Description;
            }
            else if (val is EdgeListData el)
            {
                DisplayText = el.Description;
            }
            else if (val is DataList dl)
            {
                DisplayText = FormatDataList(dl, 0);
            }
            else
            {
                DisplayText = val.ToString() ?? "(unknown)";
            }

            // Pass through
            GetOutput("Value")!.Value = val;
        }

        public override string? GetDisplaySummary() => DisplayText;

        /// <summary>
        /// Recursively formats a DataList into a readable tree structure.
        /// </summary>
        private static string FormatDataList(DataList list, int depth)
        {
            if (depth > 5) return "[...]"; // prevent infinite recursion
            var lines = new System.Collections.Generic.List<string>();
            string indent = new string(' ', depth * 2);
            lines.Add($"{indent}List [{list.Count}]:");
            for (int i = 0; i < Math.Min(list.Count, 20); i++)
            {
                var item = list.Items[i];
                if (item is DataList sub)
                    lines.Add(FormatDataList(sub, depth + 1));
                else if (item is double d)
                    lines.Add($"{indent}  [{i}] {d:G6}");
                else if (item is ValueTuple<double, double, double> pt)
                    lines.Add($"{indent}  [{i}] ({pt.Item1:F2}, {pt.Item2:F2}, {pt.Item3:F2})");
                else if (item is BodyData bd)
                    lines.Add($"{indent}  [{i}] {bd.Description}");
                else if (item == null)
                    lines.Add($"{indent}  [{i}] (null)");
                else
                    lines.Add($"{indent}  [{i}] {item}");
            }
            if (list.Count > 20)
                lines.Add($"{indent}  ... +{list.Count - 20} more");
            return string.Join("\n", lines);
        }
    }
}
