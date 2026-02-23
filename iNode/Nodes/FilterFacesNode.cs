// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// FilterFacesNode.cs - Filter faces by geometric/appearance properties
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Filters faces from a list by geometric or appearance criteria.
    /// Implements the same topology algorithms used in the CaseSelection add-in.
    ///
    /// Modes:
    ///   - All:         pass through all faces
    ///   - Planar:      only flat/plane faces
    ///   - Cylindrical: only cylindrical faces
    ///   - Spherical:   only spherical faces
    ///   - Conical:     only conical faces
    ///   - Blend:       fillet/chamfer faces (cylinder, cone, sphere, torus, b-spline)
    ///   - ByColor:     faces matching a specific RGB color (within tolerance)
    ///   - ByFeature:   faces created by the same feature as the seed (Index=0 → first)
    ///   - Tangent:     faces tangentially connected to the seed face
    ///   - ByArea:      faces with area within Min–Max range (mm²)
    ///
    /// For ByColor mode, connect R/G/B inputs (0-255).
    /// For ByFeature/Tangent, provide Index to pick the seed face.
    /// </summary>
    public class FilterFacesNode : Node
    {
        public override string TypeName => "FilterFaces";
        public override string Title => "Filter Faces";
        public override string Category => "Topology";

        public string Mode { get; set; } = "All";

        public FilterFacesNode()
        {
            AddInput("Faces", "Faces", PortDataType.Face, null);
            AddInput("Index", "Seed Index", PortDataType.Number, 0.0);
            AddInput("R", "R", PortDataType.Number, 255.0);
            AddInput("G", "G", PortDataType.Number, 0.0);
            AddInput("B", "B", PortDataType.Number, 0.0);
            AddInput("Min", "Min Area", PortDataType.Number, 0.0);
            AddInput("Max", "Max Area", PortDataType.Number, 999999.0);
            AddOutput("Result", "Result", PortDataType.Face);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var facesVal = GetInput("Faces")!.GetEffectiveValue();
            int seedIdx = (int)GetInput("Index")!.GetDouble(0);
            int r = (int)GetInput("R")!.GetDouble(255);
            int g = (int)GetInput("G")!.GetDouble(0);
            int b = (int)GetInput("B")!.GetDouble(0);
            double minArea = GetInput("Min")!.GetDouble(0);
            double maxArea = GetInput("Max")!.GetDouble(999999);

            if (facesVal is not FaceListData faceList || faceList.Faces.Count == 0)
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
                    case "Planar":
                        FilterBySurfaceType(faceList, result, "Plane");
                        break;
                    case "Cylindrical":
                        FilterBySurfaceType(faceList, result, "Cylinder");
                        break;
                    case "Spherical":
                        FilterBySurfaceType(faceList, result, "Sphere");
                        break;
                    case "Conical":
                        FilterBySurfaceType(faceList, result, "Cone");
                        break;
                    case "Blend":
                        FilterBlendFaces(faceList, result);
                        break;
                    case "ByColor":
                        FilterByColor(faceList, result, r, g, b);
                        break;
                    case "ByFeature":
                        FilterByFeature(faceList, result, seedIdx);
                        break;
                    case "Tangent":
                        FilterTangent(faceList, result, seedIdx);
                        break;
                    case "ByArea":
                        FilterByArea(faceList, result, minArea, maxArea);
                        break;
                    case "All":
                    default:
                        result.Faces.AddRange(faceList.Faces);
                        break;
                }

                GetOutput("Result")!.Value = result;
                GetOutput("Count")!.Value = (double)result.Faces.Count;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Filter failed: {ex.Message}";
            }
        }

        #region Filter Implementations

        private void FilterBySurfaceType(FaceListData src, FaceListData dst, string typeMatch)
        {
            foreach (var faceObj in src.Faces)
            {
                try
                {
                    dynamic face = faceObj;
                    var st = face.SurfaceType.ToString();
                    if (st.Contains(typeMatch))
                        dst.Faces.Add(faceObj);
                }
                catch { }
            }
        }

        /// <summary>
        /// Identifies blend (fillet/chamfer) faces: cylinder, cone, sphere,
        /// torus, and heuristic b-spline surfaces. Same algorithm as
        /// CaseSelection TopologyAnalyzer.IsBlendFace.
        /// </summary>
        private void FilterBlendFaces(FaceListData src, FaceListData dst)
        {
            foreach (var faceObj in src.Faces)
            {
                try
                {
                    dynamic face = faceObj;
                    var st = face.SurfaceType.ToString();
                    if (st.Contains("Cylinder") || st.Contains("Cone") ||
                        st.Contains("Sphere") || st.Contains("Torus"))
                    {
                        dst.Faces.Add(faceObj);
                    }
                    else if (st.Contains("BSpline"))
                    {
                        // Heuristic: b-spline with mostly curved edges adjacent
                        // to a known blend type is likely a variable-radius blend
                        if (IsLikelyBSplineBlend(face))
                            dst.Faces.Add(faceObj);
                    }
                }
                catch { }
            }
        }

        private bool IsLikelyBSplineBlend(dynamic face)
        {
            try
            {
                int curved = 0, straight = 0;
                foreach (var edge in face.Edges)
                {
                    var ct = ((object)edge.GeometryType).ToString();
                    if (ct.Contains("Line")) straight++;
                    else curved++;
                }
                if (curved < 2 || curved < straight) return false;

                int adjBlend = 0;
                foreach (var edge in face.Edges)
                {
                    foreach (var adj in edge.Faces)
                    {
                        var adjSt = ((object)adj.SurfaceType).ToString();
                        if (adjSt.Contains("Cylinder") || adjSt.Contains("Cone") ||
                            adjSt.Contains("Sphere") || adjSt.Contains("Torus"))
                        {
                            adjBlend++;
                        }
                    }
                }
                return adjBlend > 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Filter faces by appearance color. Reads the face's diffuse color from
        /// its Appearance asset and checks if it's within ±10 RGB tolerance.
        /// Same approach as ColoringTool's GetDiffuseColorFromAppearance.
        /// </summary>
        private void FilterByColor(FaceListData src, FaceListData dst, int tr, int tg, int tb)
        {
            const int COLOR_TOLERANCE = 10;

            foreach (var faceObj in src.Faces)
            {
                try
                {
                    dynamic face = faceObj;
                    var appearance = face.Appearance;
                    if (appearance == null) continue;

                    var diffuse = GetDiffuseColor(appearance);
                    if (diffuse == null) continue;

                    int fr = diffuse.Value.r, fg = diffuse.Value.g, fb = diffuse.Value.b;
                    if (Math.Abs(fr - tr) <= COLOR_TOLERANCE &&
                        Math.Abs(fg - tg) <= COLOR_TOLERANCE &&
                        Math.Abs(fb - tb) <= COLOR_TOLERANCE)
                    {
                        dst.Faces.Add(faceObj);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Extracts diffuse RGB from an Inventor Asset. Searches for
        /// generic_diffuse / diffuse_color parameters (same as ColoringTool).
        /// </summary>
        private (int r, int g, int b)? GetDiffuseColor(dynamic appearance)
        {
            try
            {
                for (int i = 1; i <= (int)appearance.Count; i++)
                {
                    try
                    {
                        var av = appearance[i];
                        string name = ((string)av.Name).ToLower();
                        if (name.Contains("generic_diffuse") ||
                            name.Contains("diffuse_color") ||
                            (name.Contains("diffuse") && !name.Contains("map") && !name.Contains("texture")))
                        {
                            // Check if it's a ColorAssetValue
                            try
                            {
                                var invColor = av.Value;
                                return ((int)invColor.Red, (int)invColor.Green, (int)invColor.Blue);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                // Fallback: any color value not reflect/emit
                for (int i = 1; i <= (int)appearance.Count; i++)
                {
                    try
                    {
                        var av = appearance[i];
                        string name = ((string)av.Name).ToLower();
                        if (name.Contains("reflect") || name.Contains("emit")) continue;
                        try
                        {
                            var invColor = av.Value;
                            int testR = (int)invColor.Red; // throws if not ColorAssetValue
                            return (testR, (int)invColor.Green, (int)invColor.Blue);
                        }
                        catch { }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Filter faces by feature — all faces created by the same feature
        /// as the seed face. Uses Face.CreatedByFeature (same as CaseSelection).
        /// </summary>
        private void FilterByFeature(FaceListData src, FaceListData dst, int seedIdx)
        {
            if (seedIdx < 0 || seedIdx >= src.Faces.Count)
            {
                HasError = true;
                ErrorMessage = $"Seed index {seedIdx} out of range";
                return;
            }

            try
            {
                dynamic seedFace = src.Faces[seedIdx];
                var feature = seedFace.CreatedByFeature;
                if (feature == null)
                {
                    // Can't determine feature — just return the seed
                    dst.Faces.Add(src.Faces[seedIdx]);
                    return;
                }

                string featureName = (string)feature.Name;

                foreach (var faceObj in src.Faces)
                {
                    try
                    {
                        dynamic face = faceObj;
                        var ff = face.CreatedByFeature;
                        if (ff != null && (string)ff.Name == featureName)
                            dst.Faces.Add(faceObj);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Feature filter failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets tangentially connected faces starting from the seed face.
        /// Uses Face.TangentiallyConnectedFaces API (same as CaseSelection TopologyAnalyzer).
        /// </summary>
        private void FilterTangent(FaceListData src, FaceListData dst, int seedIdx)
        {
            if (seedIdx < 0 || seedIdx >= src.Faces.Count)
            {
                HasError = true;
                ErrorMessage = $"Seed index {seedIdx} out of range";
                return;
            }

            try
            {
                dynamic seedFace = src.Faces[seedIdx];
                var tangentFaces = seedFace.TangentiallyConnectedFaces;

                // Build a set of tangent face references for fast lookup
                var tangentSet = new HashSet<object>();
                tangentSet.Add(src.Faces[seedIdx]);
                if (tangentFaces != null)
                {
                    foreach (var tf in tangentFaces)
                    {
                        tangentSet.Add(tf);
                    }
                }

                // Only include faces that are in both the source list and tangent set
                foreach (var faceObj in src.Faces)
                {
                    if (tangentSet.Contains(faceObj))
                        dst.Faces.Add(faceObj);
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Tangent filter failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Filter faces by surface area (mm²).
        /// </summary>
        private void FilterByArea(FaceListData src, FaceListData dst, double minMM2, double maxMM2)
        {
            // Inventor area is in cm², convert mm² thresholds
            double minCm2 = minMM2 * 0.01; // 1 mm² = 0.01 cm²
            double maxCm2 = maxMM2 * 0.01;

            foreach (var faceObj in src.Faces)
            {
                try
                {
                    dynamic face = faceObj;
                    double area = (double)face.Evaluator.Area;
                    if (area >= minCm2 && area <= maxCm2)
                        dst.Faces.Add(faceObj);
                }
                catch { }
            }
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
                    Choices = new[] { "All", "Planar", "Cylindrical", "Spherical",
                                     "Conical", "Blend", "ByColor", "ByFeature",
                                     "Tangent", "ByArea" },
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
                    "All", "Planar", "Cylindrical", "Spherical", "Conical",
                    "Blend", "ByColor", "ByFeature", "Tangent", "ByArea"
                }.Contains(s) ? s : "All";
            }
        }
    }
}
