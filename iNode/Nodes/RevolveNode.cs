// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// RevolveNode.cs - Creates a revolution from a sketch profile around an axis
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Revolves a sketch profile around an axis to create geometry.
    /// In Preview mode: creates a transient body approximation (torus/cylinder).
    /// In Commit mode: creates a real RevolveFeature in the part.
    /// </summary>
    public class RevolveNode : Node
    {
        public override string TypeName => "Revolve";
        public override string Title => "Revolve";
        public override string Category => "Geometry";

        /// <summary>
        /// Angle of revolution in degrees.
        /// </summary>
        public double Angle { get; set; } = 360.0;

        /// <summary>
        /// Operation type when combined with existing body.
        /// </summary>
        public string Operation { get; set; } = "NewBody";

        public RevolveNode()
        {
            AddInput("Sketch", "Sketch", PortDataType.SketchRef, null);
            AddInput("Angle", "Angle (°)", PortDataType.Number, 360.0);
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var sketchRef = GetInput("Sketch")?.Value as SketchRefData;
            Angle = GetInput("Angle")!.GetDouble(360.0);

            if (sketchRef == null || sketchRef.Sketch == null)
            {
                HasError = true;
                ErrorMessage = "No sketch connected";
                return;
            }

            if (Angle <= 0 || Angle > 360)
            {
                HasError = true;
                ErrorMessage = "Angle must be between 0 and 360 degrees";
                return;
            }

            if (Context?.IsAvailable != true)
            {
                HasError = true;
                ErrorMessage = "Inventor context not available";
                return;
            }

            try
            {
                if (Context.Mode == ExecutionMode.Commit)
                {
                    ComputeCommit(sketchRef);
                }
                else
                {
                    ComputePreview(sketchRef);
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Revolve failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Commit mode: create a real RevolveFeature in the target part.
        /// Requirements: sketch must have a profile and an axis line.
        /// The axis line is the first SketchLine in the sketch marked as
        /// a construction line, or the first sketch line if none are construction.
        /// </summary>
        private void ComputeCommit(SketchRefData sketchRef)
        {
            dynamic sketch = sketchRef.Sketch!;
            var compDef = Context!.TargetCompDef;
            if (compDef == null)
            {
                HasError = true;
                ErrorMessage = "No target part document for commit";
                return;
            }

            dynamic features = compDef.Features;
            dynamic revolveFeatures = features.RevolveFeatures;

            // Get profile
            dynamic profiles = sketch.Profiles;
            if (profiles.Count == 0)
            {
                HasError = true;
                ErrorMessage = "Sketch has no profiles";
                return;
            }
            dynamic profile = profiles.AddForSolid();

            // Find the axis line - prefer construction lines
            dynamic axisLine = FindAxisLine(sketch);
            if (axisLine == null)
            {
                HasError = true;
                ErrorMessage = "Sketch needs a construction line (axis) for revolution";
                return;
            }

            // Map operation string to PartFeatureOperationEnum
            PartFeatureOperationEnum opEnum;
            switch (Operation)
            {
                case "Join":
                    opEnum = PartFeatureOperationEnum.kJoinOperation;
                    break;
                case "Cut":
                    opEnum = PartFeatureOperationEnum.kCutOperation;
                    break;
                case "Intersect":
                    opEnum = PartFeatureOperationEnum.kIntersectOperation;
                    break;
                default:
                    opEnum = PartFeatureOperationEnum.kNewBodyOperation;
                    break;
            }

            // Create revolve definition
            dynamic revolveDef = revolveFeatures.CreateRevolveDefinition(
                profile, axisLine, opEnum);

            // Set angle extent (convert degrees to radians)
            double angleRad = Angle * Math.PI / 180.0;
            if (Math.Abs(Angle - 360.0) < 0.001)
            {
                revolveDef.SetFullExtent(
                    PartFeatureExtentDirectionEnum.kPositiveExtentDirection);
            }
            else
            {
                revolveDef.SetAngleExtent(
                    angleRad,
                    PartFeatureExtentDirectionEnum.kPositiveExtentDirection);
            }

            // Create the revolve feature
            dynamic revolveFeature = revolveFeatures.Add(revolveDef);

            // Get the resulting body
            dynamic bodies = revolveFeature.SurfaceBodies;
            if (bodies.Count > 0)
            {
                dynamic resultBody = bodies[1];
                var bodyData = new BodyData
                {
                    Body = resultBody,
                    SourceNodeId = Id,
                    Description = $"Revolve({sketchRef.SketchName}, {Angle}°, {Operation})"
                };
                GetOutput("Body")!.Value = bodyData;
            }
        }

        /// <summary>
        /// Preview mode: create a transient approximation.
        /// For a full 360° revolve, create a cylinder or torus.
        /// </summary>
        private void ComputePreview(SketchRefData sketchRef)
        {
            try
            {
                var tBrep = Context!.App.TransientBRep;
                var tGeom = Context.App.TransientGeometry;

                // For preview, create a simple cylinder as approximation
                // centered at origin, with radius based on sketch bounds
                dynamic sketch = sketchRef.Sketch!;

                double radius = 1.0;
                double height = 1.0;

                try
                {
                    // Estimate radius from sketch extents
                    dynamic sketchLines = sketch.SketchLines;
                    double maxDist = 0;
                    foreach (dynamic line in sketchLines)
                    {
                        dynamic sp = line.StartSketchPoint.Geometry;
                        dynamic ep = line.EndSketchPoint.Geometry;
                        double d1 = Math.Sqrt(sp.X * sp.X + sp.Y * sp.Y);
                        double d2 = Math.Sqrt(ep.X * ep.X + ep.Y * ep.Y);
                        maxDist = Math.Max(maxDist, Math.Max(d1, d2));

                        double lineHeight = Math.Abs(ep.Y - sp.Y);
                        height = Math.Max(height, lineHeight);
                    }
                    if (maxDist > 0.01) radius = maxDist;
                }
                catch { /* Use defaults */ }

                // Create a transient cylinder as revolve preview
                var centerBottom = tGeom.CreatePoint(0, 0, 0);
                var centerTop = tGeom.CreatePoint(0, height, 0);
                var body = tBrep.CreateSolidCylinderCone(
                    centerBottom, centerTop, radius, radius, 0);

                if (body != null)
                {
                    var bodyData = new BodyData
                    {
                        Body = body,
                        SourceNodeId = Id,
                        Description = $"RevolvePreview({sketchRef.SketchName}, {Angle}°)"
                    };
                    GetOutput("Body")!.Value = bodyData;
                }
            }
            catch (Exception ex)
            {
                // Fallback: simple cylinder
                try
                {
                    var tBrep = Context!.App.TransientBRep;
                    var tGeom = Context.App.TransientGeometry;
                    var bottom = tGeom.CreatePoint(0, 0, 0);
                    var top = tGeom.CreatePoint(0, 1, 0);
                    var body = tBrep.CreateSolidCylinderCone(bottom, top, 1, 1, 0);

                    GetOutput("Body")!.Value = new BodyData
                    {
                        Body = body,
                        SourceNodeId = Id,
                        Description = "RevolvePreview(fallback)"
                    };
                }
                catch
                {
                    HasError = true;
                    ErrorMessage = $"Preview revolve failed: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Find the best axis line in the sketch.
        /// Prefers construction lines; falls back to the first SketchLine.
        /// </summary>
        private static dynamic? FindAxisLine(dynamic sketch)
        {
            try
            {
                dynamic lines = sketch.SketchLines;
                if (lines.Count == 0) return null;

                // First pass: find a construction line
                foreach (dynamic line in lines)
                {
                    try
                    {
                        if (line.Construction) return line;
                    }
                    catch { }
                }

                // Fallback: first line
                return lines[1];
            }
            catch
            {
                return null;
            }
        }

        public override string? GetDisplaySummary() => $"{Angle}°, {Operation}";

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Operation",
                    Key = "Operation",
                    Value = Operation,
                    Choices = new[] { "NewBody", "Join", "Cut", "Intersect" },
                    DisplayOnNode = Operation
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["Angle"] = Angle,
                ["Operation"] = Operation
            };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Angle", out var angle))
            {
                if (angle is double d) Angle = Math.Max(0.1, Math.Min(360.0, d));
            }
            if (parameters.TryGetValue("Operation", out var op) && op is string o)
            {
                if (o == "NewBody" || o == "Join" || o == "Cut" || o == "Intersect")
                    Operation = o;
            }
        }
    }
}
