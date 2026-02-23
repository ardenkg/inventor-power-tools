// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SelectEdgeNode.cs - Filter/select edges from an edge list
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Selects edges from an edge list by index or by type.
    /// Connect this to the Edges output of a Deconstruct Body node,
    /// then feed the selected edges into a Fillet or Chamfer node.
    ///
    /// Selection modes:
    ///   - Index: pick edge(s) by zero-based index
    ///   - All: pass through all edges
    ///   - Linear: only straight/line edges
    ///   - Circular: only arc/circle edges
    /// </summary>
    public class SelectEdgeNode : Node
    {
        public override string TypeName => "SelectEdge";
        public override string Title => "Select Edge";
        public override string Category => "Topology";

        /// <summary>
        /// Selection mode: "Index", "All", "Linear", "Circular".
        /// </summary>
        public string Mode { get; set; } = "All";

        public SelectEdgeNode()
        {
            AddInput("Edges", "Edges", PortDataType.Edge, null);
            AddInput("Index", "Index", PortDataType.Number, 0.0);
            AddOutput("Selected", "Selected", PortDataType.Edge);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var edgesVal = GetInput("Edges")!.GetEffectiveValue();
            var index = (int)GetInput("Index")!.GetDouble(0);

            if (edgesVal is not EdgeListData edgeList)
            {
                HasError = true;
                ErrorMessage = "No edges connected";
                return;
            }

            try
            {
                var result = new EdgeListData { ParentBody = edgeList.ParentBody };

                switch (Mode)
                {
                    case "Index":
                        if (index >= 0 && index < edgeList.Edges.Count)
                        {
                            result.Edges.Add(edgeList.Edges[index]);
                        }
                        else if (edgeList.Edges.Count > 0)
                        {
                            HasError = true;
                            ErrorMessage = $"Index {index} out of range (0-{edgeList.Edges.Count - 1})";
                            return;
                        }
                        break;

                    case "Linear":
                        foreach (var edgeObj in edgeList.Edges)
                        {
                            try
                            {
                                dynamic edge = edgeObj;
                                // CurveTypeEnum.kLineSegmentCurve == 12
                                if ((int)edge.GeometryType == 12 ||
                                    edge.GeometryType.ToString().Contains("Line"))
                                {
                                    result.Edges.Add(edgeObj);
                                }
                            }
                            catch { result.Edges.Add(edgeObj); }
                        }
                        break;

                    case "Circular":
                        foreach (var edgeObj in edgeList.Edges)
                        {
                            try
                            {
                                dynamic edge = edgeObj;
                                // CurveTypeEnum.kCircleCurve == 4, kEllipseFullCurve, kCircularArcCurve
                                var gt = edge.GeometryType.ToString();
                                if (gt.Contains("Circle") || gt.Contains("Arc"))
                                {
                                    result.Edges.Add(edgeObj);
                                }
                            }
                            catch { result.Edges.Add(edgeObj); }
                        }
                        break;

                    case "All":
                    default:
                        result.Edges.AddRange(edgeList.Edges);
                        break;
                }

                GetOutput("Selected")!.Value = result;
                GetOutput("Count")!.Value = (double)result.Edges.Count;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Edge selection failed: {ex.Message}";
            }
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["Mode"] = Mode
            };
        }

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Mode:",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { "All", "Index", "Linear", "Circular" },
                    DisplayOnNode = Mode
                }
            };
        }

        public override string? GetDisplaySummary() => Mode;

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Mode", out var mode))
            {
                var s = mode?.ToString() ?? "All";
                Mode = s switch
                {
                    "Index" => "Index",
                    "Linear" => "Linear",
                    "Circular" => "Circular",
                    _ => "All"
                };
            }
        }
    }
}
