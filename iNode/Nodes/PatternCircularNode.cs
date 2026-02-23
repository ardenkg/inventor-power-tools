// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// PatternCircularNode.cs - Circular/polar pattern of a body
// ============================================================================

using System;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a circular (polar) pattern of the input body by copying,
    /// rotating, and unioning instances around an axis.
    /// </summary>
    public class PatternCircularNode : Node
    {
        public override string TypeName => "PatternCircular";
        public override string Title => "Circular Pattern";
        public override string Category => "Transform";

        public PatternCircularNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Count", "Count", PortDataType.Number, 6.0);
            AddInput("Angle", "Total Angle (\u00B0)", PortDataType.Number, 360.0);
            AddInput("Axis", "Axis", PortDataType.Point3D, (0.0, 1.0, 0.0));
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var geomVal = GetInput("Body")!.GetEffectiveValue();
            var count = (int)GetInput("Count")!.GetDouble(6);
            var totalAngle = GetInput("Angle")!.GetDouble(360);
            var axis = GetInput("Axis")!.GetPoint3D();
            var center = GetInput("Center")!.GetPoint3D();

            if (count < 1) { HasError = true; ErrorMessage = "Count must be at least 1"; return; }
            if (geomVal is not BodyData srcBody) { HasError = true; ErrorMessage = "No body connected"; return; }

            if (Context?.IsAvailable != true || srcBody.Body == null)
            {
                GetOutput("Body")!.Value = new BodyData
                {
                    Description = $"Circular Pattern ({count}\u00D7)",
                    SourceNodeId = Id
                };
                return;
            }

            try
            {
                var tb = Context.TB;
                var result = tb.Copy(srcBody.Body as SurfaceBody);

                double stepAngle = totalAngle / count;

                for (int i = 1; i < count; i++)
                {
                    double angleDeg = stepAngle * i;
                    double angleRad = angleDeg * Math.PI / 180.0;

                    var copy = tb.Copy(srcBody.Body as SurfaceBody);
                    var matrix = Context.CreateRotationMatrix(
                        center.X, center.Y, center.Z,
                        axis.X, axis.Y, axis.Z,
                        angleRad);
                    tb.Transform(copy, matrix);
                    tb.DoBoolean(result, copy, BooleanTypeEnum.kBooleanTypeUnion);
                }

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = result,
                    Description = $"Circular Pattern ({count}\u00D7)",
                    SourceNodeId = Id
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Circular pattern failed: {ex.Message}";
            }
        }
    }
}
