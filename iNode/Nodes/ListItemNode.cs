// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ListItemNode.cs - Pick item(s) from a face or edge list by index/range
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Picks one or more items from a list (faces or edges) by index or range.
    /// Replaces the separate SelectEdge/SelectFace nodes with a single unified node.
    ///
    /// Modes:
    ///   - Index:  pick a single item by zero-based index
    ///   - Range:  pick items from Start to End (inclusive)
    ///   - First:  first N items
    ///   - Last:   last N items
    ///   - All:    pass through entire list
    ///
    /// Accepts both FaceListData and EdgeListData on the List input.
    /// Outputs the same type as the input.
    /// </summary>
    public class ListItemNode : Node
    {
        public override string TypeName => "ListItem";
        public override string Title => "List Item";
        public override string Category => "Topology";

        public string Mode { get; set; } = "Index";

        public ListItemNode()
        {
            AddInput("List", "List", PortDataType.Any, null);
            AddInput("Index", "Index", PortDataType.Number, 0.0);
            AddInput("End", "End", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Any);
            AddOutput("Body", "Body", PortDataType.Geometry);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var listVal = GetInput("List")!.GetEffectiveValue();
            int index = (int)GetInput("Index")!.GetDouble(0);
            int end = (int)GetInput("End")!.GetDouble(0);

            if (listVal is EdgeListData edgeList)
            {
                ComputeEdges(edgeList, index, end);
            }
            else if (listVal is FaceListData faceList)
            {
                ComputeFaces(faceList, index, end);
            }
            else
            {
                HasError = true;
                ErrorMessage = "Connect a face or edge list";
                return;
            }
        }

        private void ComputeEdges(EdgeListData edgeList, int index, int end)
        {
            var result = new EdgeListData { ParentBody = edgeList.ParentBody };
            int count = edgeList.Edges.Count;

            switch (Mode)
            {
                case "Index":
                    if (index >= 0 && index < count)
                        result.Edges.Add(edgeList.Edges[index]);
                    else if (count > 0)
                    { HasError = true; ErrorMessage = $"Index {index} out of range (0–{count - 1})"; return; }
                    break;

                case "Range":
                    int rStart = Math.Max(0, Math.Min(index, count - 1));
                    int rEnd = Math.Max(0, Math.Min(end, count - 1));
                    if (rStart > rEnd) (rStart, rEnd) = (rEnd, rStart);
                    for (int i = rStart; i <= rEnd; i++)
                        result.Edges.Add(edgeList.Edges[i]);
                    break;

                case "First":
                    int takeFirst = Math.Max(1, index);
                    for (int i = 0; i < Math.Min(takeFirst, count); i++)
                        result.Edges.Add(edgeList.Edges[i]);
                    break;

                case "Last":
                    int takeLast = Math.Max(1, index);
                    for (int i = Math.Max(0, count - takeLast); i < count; i++)
                        result.Edges.Add(edgeList.Edges[i]);
                    break;

                case "All":
                default:
                    result.Edges.AddRange(edgeList.Edges);
                    break;
            }

            GetOutput("Result")!.Value = result;
            GetOutput("Body")!.Value = edgeList.ParentBody;
            GetOutput("Count")!.Value = (double)result.Edges.Count;
        }

        private void ComputeFaces(FaceListData faceList, int index, int end)
        {
            var result = new FaceListData { ParentBody = faceList.ParentBody };
            int count = faceList.Faces.Count;

            switch (Mode)
            {
                case "Index":
                    if (index >= 0 && index < count)
                        result.Faces.Add(faceList.Faces[index]);
                    else if (count > 0)
                    { HasError = true; ErrorMessage = $"Index {index} out of range (0–{count - 1})"; return; }
                    break;

                case "Range":
                    int rStart = Math.Max(0, Math.Min(index, count - 1));
                    int rEnd = Math.Max(0, Math.Min(end, count - 1));
                    if (rStart > rEnd) (rStart, rEnd) = (rEnd, rStart);
                    for (int i = rStart; i <= rEnd; i++)
                        result.Faces.Add(faceList.Faces[i]);
                    break;

                case "First":
                    int takeFirst = Math.Max(1, index);
                    for (int i = 0; i < Math.Min(takeFirst, count); i++)
                        result.Faces.Add(faceList.Faces[i]);
                    break;

                case "Last":
                    int takeLast = Math.Max(1, index);
                    for (int i = Math.Max(0, count - takeLast); i < count; i++)
                        result.Faces.Add(faceList.Faces[i]);
                    break;

                case "All":
                default:
                    result.Faces.AddRange(faceList.Faces);
                    break;
            }

            GetOutput("Result")!.Value = result;
            GetOutput("Body")!.Value = faceList.ParentBody;
            GetOutput("Count")!.Value = (double)result.Faces.Count;
        }

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
                    Label = "Mode:",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { "Index", "Range", "First", "Last", "All" },
                    DisplayOnNode = Mode
                }
            };
        }

        public override string? GetDisplaySummary() => Mode;

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Mode", out var mode))
            {
                var s = mode?.ToString() ?? "Index";
                Mode = s switch
                {
                    "Range" => "Range",
                    "First" => "First",
                    "Last" => "Last",
                    "All" => "All",
                    _ => "Index"
                };
            }
        }
    }
}
