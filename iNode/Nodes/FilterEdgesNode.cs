// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// FilterEdgesNode.cs - Filter edges by geometric properties
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Filters edges from a list by geometric criteria.
    ///
    /// Modes:
    ///   - All:       pass through all edges
    ///   - Linear:    only straight/line edges
    ///   - Circular:  only arc/circle edges
    ///   - ByLength:  edges whose length falls within Min–Max range
    ///   - AlongX:    edges aligned with the X axis (within tolerance)
    ///   - AlongY:    edges aligned with the Y axis
    ///   - AlongZ:    edges aligned with the Z axis
    ///   - Shortest:  the N shortest edges
    ///   - Longest:   the N longest edges
    ///
    /// Workflow: Deconstruct Body → Filter Edges → Fillet / Chamfer
    /// </summary>
    public class FilterEdgesNode : Node
    {
        public override string TypeName => "FilterEdges";
        public override string Title => "Filter Edges";
        public override string Category => "Topology";

        public string Mode { get; set; } = "All";

        /// <summary>Angle tolerance in degrees for direction filtering.</summary>
        private const double ANGLE_TOL_DEG = 15.0;

        public FilterEdgesNode()
        {
            AddInput("Edges", "Edges", PortDataType.Edge, null);
            AddInput("Min", "Min", PortDataType.Number, 0.0);
            AddInput("Max", "Max", PortDataType.Number, 1000.0);
            AddInput("Count", "N", PortDataType.Number, 1.0);
            AddOutput("Result", "Result", PortDataType.Edge);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var edgesVal = GetInput("Edges")!.GetEffectiveValue();
            double min = GetInput("Min")!.GetDouble(0);
            double max = GetInput("Max")!.GetDouble(1000);
            int n = (int)GetInput("Count")!.GetDouble(1);

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
                    case "Linear":
                        foreach (var edgeObj in edgeList.Edges)
                        {
                            if (IsLinear(edgeObj)) result.Edges.Add(edgeObj);
                        }
                        break;

                    case "Circular":
                        foreach (var edgeObj in edgeList.Edges)
                        {
                            if (IsCircular(edgeObj)) result.Edges.Add(edgeObj);
                        }
                        break;

                    case "ByLength":
                        // Convert mm inputs to cm for Inventor internal units
                        double minCm = min * InventorContext.MM_TO_CM;
                        double maxCm = max * InventorContext.MM_TO_CM;
                        foreach (var edgeObj in edgeList.Edges)
                        {
                            double len = GetEdgeLength(edgeObj);
                            if (len >= minCm && len <= maxCm)
                                result.Edges.Add(edgeObj);
                        }
                        break;

                    case "AlongX":
                        FilterByDirection(edgeList.Edges, result.Edges, 1, 0, 0);
                        break;

                    case "AlongY":
                        FilterByDirection(edgeList.Edges, result.Edges, 0, 1, 0);
                        break;

                    case "AlongZ":
                        FilterByDirection(edgeList.Edges, result.Edges, 0, 0, 1);
                        break;

                    case "Shortest":
                        SortAndTake(edgeList.Edges, result.Edges, n, ascending: true);
                        break;

                    case "Longest":
                        SortAndTake(edgeList.Edges, result.Edges, n, ascending: false);
                        break;

                    case "All":
                    default:
                        result.Edges.AddRange(edgeList.Edges);
                        break;
                }

                GetOutput("Result")!.Value = result;
                GetOutput("Count")!.Value = (double)result.Edges.Count;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Filter failed: {ex.Message}";
            }
        }

        #region Edge Analysis Helpers

        private bool IsLinear(object edgeObj)
        {
            try
            {
                dynamic edge = edgeObj;
                var gt = edge.GeometryType.ToString();
                return gt.Contains("Line");
            }
            catch { return false; }
        }

        private bool IsCircular(object edgeObj)
        {
            try
            {
                dynamic edge = edgeObj;
                var gt = edge.GeometryType.ToString();
                return gt.Contains("Circle") || gt.Contains("Arc");
            }
            catch { return false; }
        }

        private double GetEdgeLength(object edgeObj)
        {
            try
            {
                dynamic edge = edgeObj;
                double minP = 0, maxP = 0;
                edge.Evaluator.GetParamExtents(out minP, out maxP);

                double length = 0;
                edge.Evaluator.GetLengthAtParam(minP, maxP, out length);
                return length;
            }
            catch
            {
                // Fallback: approximate with start/end distance
                try
                {
                    dynamic edge = edgeObj;
                    var sp = edge.StartVertex.Point;
                    var ep = edge.StopVertex.Point;
                    double dx = (double)ep.X - (double)sp.X;
                    double dy = (double)ep.Y - (double)sp.Y;
                    double dz = (double)ep.Z - (double)sp.Z;
                    return Math.Sqrt(dx * dx + dy * dy + dz * dz);
                }
                catch { return 0; }
            }
        }

        private void FilterByDirection(List<object> edges, List<object> result,
             double dx, double dy, double dz)
        {
            double cosTol = Math.Cos(ANGLE_TOL_DEG * Math.PI / 180.0);

            foreach (var edgeObj in edges)
            {
                try
                {
                    dynamic edge = edgeObj;
                    double minP = 0, maxP = 0;
                    edge.Evaluator.GetParamExtents(out minP, out maxP);
                    double midP = (minP + maxP) / 2.0;
                    double[] midParamArray = new double[] { midP };
                    double[] tangent = new double[3];
                    edge.Evaluator.GetTangent(ref midParamArray, ref tangent);

                    // Normalize
                    double mag = Math.Sqrt(tangent[0] * tangent[0] +
                                           tangent[1] * tangent[1] +
                                           tangent[2] * tangent[2]);
                    if (mag < 1e-10) continue;
                    tangent[0] /= mag; tangent[1] /= mag; tangent[2] /= mag;

                    // Check alignment (absolute dot product — direction doesn't matter)
                    double dot = Math.Abs(tangent[0] * dx + tangent[1] * dy + tangent[2] * dz);
                    if (dot >= cosTol)
                        result.Add(edgeObj);
                }
                catch { /* skip edges we can't analyze */ }
            }
        }

        private void SortAndTake(List<object> edges, List<object> result, int n, bool ascending)
        {
            var measured = new List<(object edge, double length)>();
            foreach (var edgeObj in edges)
            {
                measured.Add((edgeObj, GetEdgeLength(edgeObj)));
            }

            if (ascending)
                measured.Sort((a, b) => a.length.CompareTo(b.length));
            else
                measured.Sort((a, b) => b.length.CompareTo(a.length));

            for (int i = 0; i < Math.Min(n, measured.Count); i++)
                result.Add(measured[i].edge);
        }

        #endregion

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?> { ["Mode"] = Mode };
        }

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Filter:",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { "All", "Linear", "Circular", "ByLength",
                                     "AlongX", "AlongY", "AlongZ", "Shortest", "Longest" },
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
                Mode = new HashSet<string>
                {
                    "All", "Linear", "Circular", "ByLength",
                    "AlongX", "AlongY", "AlongZ", "Shortest", "Longest"
                }.Contains(s) ? s : "All";
            }
        }
    }
}
