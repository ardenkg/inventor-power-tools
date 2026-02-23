// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SelectFaceNode.cs - Filter/select faces from a face list
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Selects faces from a face list by index or by type.
    /// Connect this to the Faces output of a Deconstruct Body node.
    ///
    /// Selection modes:
    ///   - Index: pick face by zero-based index
    ///   - All: pass through all faces
    ///   - Planar: only flat faces
    ///   - Cylindrical: only cylindrical faces
    /// </summary>
    public class SelectFaceNode : Node
    {
        public override string TypeName => "SelectFace";
        public override string Title => "Select Face";
        public override string Category => "Topology";

        /// <summary>
        /// Selection mode: "Index", "All", "Planar", "Cylindrical".
        /// </summary>
        public string Mode { get; set; } = "All";

        public SelectFaceNode()
        {
            AddInput("Faces", "Faces", PortDataType.Face, null);
            AddInput("Index", "Index", PortDataType.Number, 0.0);
            AddOutput("Selected", "Selected", PortDataType.Face);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var facesVal = GetInput("Faces")!.GetEffectiveValue();
            var index = (int)GetInput("Index")!.GetDouble(0);

            if (facesVal is not FaceListData faceList)
            {
                HasError = true;
                ErrorMessage = "No faces connected";
                return;
            }

            try
            {
                var result = new FaceListData { ParentBody = faceList.ParentBody };

                switch (Mode)
                {
                    case "Index":
                        if (index >= 0 && index < faceList.Faces.Count)
                        {
                            result.Faces.Add(faceList.Faces[index]);
                        }
                        else if (faceList.Faces.Count > 0)
                        {
                            HasError = true;
                            ErrorMessage = $"Index {index} out of range (0-{faceList.Faces.Count - 1})";
                            return;
                        }
                        break;

                    case "Planar":
                        foreach (var faceObj in faceList.Faces)
                        {
                            try
                            {
                                dynamic face = faceObj;
                                var st = face.SurfaceType.ToString();
                                if (st.Contains("Plane"))
                                    result.Faces.Add(faceObj);
                            }
                            catch { result.Faces.Add(faceObj); }
                        }
                        break;

                    case "Cylindrical":
                        foreach (var faceObj in faceList.Faces)
                        {
                            try
                            {
                                dynamic face = faceObj;
                                var st = face.SurfaceType.ToString();
                                if (st.Contains("Cylinder"))
                                    result.Faces.Add(faceObj);
                            }
                            catch { result.Faces.Add(faceObj); }
                        }
                        break;

                    case "All":
                    default:
                        result.Faces.AddRange(faceList.Faces);
                        break;
                }

                GetOutput("Selected")!.Value = result;
                GetOutput("Count")!.Value = (double)result.Faces.Count;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Face selection failed: {ex.Message}";
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
                    Choices = new[] { "All", "Index", "Planar", "Cylindrical" },
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
                    "Planar" => "Planar",
                    "Cylindrical" => "Cylindrical",
                    _ => "All"
                };
            }
        }
    }
}
