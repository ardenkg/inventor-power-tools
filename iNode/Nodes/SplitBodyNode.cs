// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SplitBodyNode.cs - Split a body with a plane
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Splits a body at a specified Z height using a boolean intersection
    /// with a large box. Mode selects which half to keep: Top or Bottom.
    /// </summary>
    public class SplitBodyNode : Node
    {
        public override string TypeName => "SplitBody";
        public override string Title => "Split Body";
        public override string Category => "Operations";

        public string Mode { get; set; } = "Bottom";

        public SplitBodyNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("PlanePoint", "Plane Point", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddInput("PlaneNormal", "Plane Normal", PortDataType.Point3D, (0.0, 1.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();
            var point = GetInput("PlanePoint")!.GetPoint3D();
            var normal = GetInput("PlaneNormal")!.GetPoint3D();

            if (bodyVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            if (Context?.IsAvailable != true || srcBody.Body == null)
            {
                GetOutput("Body")!.Value = new BodyData
                {
                    Description = $"Split ({Mode})",
                    SourceNodeId = Id
                };
                return;
            }

            try
            {
                // Normalize the normal vector
                double len = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                if (len < 1e-10) { normal = (0, 1, 0); len = 1; }
                double nx = normal.X / len, ny = normal.Y / len, nz = normal.Z / len;

                // Create a very large cutting box on one side of the plane
                double bigSize = 1000.0; // 1000mm = 100cm

                // Offset center to one side of the plane
                double sign = Mode == "Bottom" ? -1.0 : 1.0;
                double ocx = point.X + nx * bigSize * sign * 0.5;
                double ocy = point.Y + ny * bigSize * sign * 0.5;
                double ocz = point.Z + nz * bigSize * sign * 0.5;

                // Build an axis-aligned cutting box centered at offset
                double halfCm = bigSize * 0.5 * InventorContext.MM_TO_CM;
                double cxCm = ocx * InventorContext.MM_TO_CM;
                double cyCm = ocy * InventorContext.MM_TO_CM;
                double czCm = ocz * InventorContext.MM_TO_CM;

                var box = Context.TG.CreateBox();
                box.MinPoint = Context.TG.CreatePoint(cxCm - halfCm, cyCm - halfCm, czCm - halfCm);
                box.MaxPoint = Context.TG.CreatePoint(cxCm + halfCm, cyCm + halfCm, czCm + halfCm);
                var cuttingBody = Context.TB.CreateSolidBlock(box);

                var copy = Context.TB.Copy(srcBody.Body as SurfaceBody);
                Context.TB.DoBoolean(copy, cuttingBody, BooleanTypeEnum.kBooleanTypeIntersect);

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = copy,
                    Description = $"Split ({Mode})",
                    SourceNodeId = Id,
                    PendingOperations = new List<PendingOperation>(srcBody.PendingOperations)
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Split failed: {ex.Message}";
            }
        }

        public override string? GetDisplaySummary() => Mode;

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Keep",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { "Top", "Bottom" },
                    DisplayOnNode = Mode
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?> { ["Mode"] = Mode };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Mode", out var m) && m is string s)
                if (s == "Top" || s == "Bottom") Mode = s;
        }
    }
}
