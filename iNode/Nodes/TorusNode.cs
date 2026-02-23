// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// TorusNode.cs - Creates a torus as a transient SurfaceBody
// ============================================================================

using System;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a torus (donut shape) as a transient SurfaceBody.
    /// Attempts native CreateSolidTorus, falls back to high-res cylinder sweep.
    /// Torus ring lies in the XZ plane (Y-up).
    /// </summary>
    public class TorusNode : Node
    {
        public override string TypeName => "Torus";
        public override string Title => "Torus";
        public override string Category => "Geometry";

        public TorusNode()
        {
            AddInput("MajorRadius", "Major Radius (mm)", PortDataType.Number, 10.0);
            AddInput("MinorRadius", "Minor Radius (mm)", PortDataType.Number, 3.0);
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var majorR = GetInput("MajorRadius")!.GetDouble(10.0);
            var minorR = GetInput("MinorRadius")!.GetDouble(3.0);
            var center = GetInput("Center")!.GetPoint3D();

            if (majorR <= 0 || minorR <= 0)
            {
                HasError = true;
                ErrorMessage = "Both radii must be positive";
                return;
            }
            if (minorR >= majorR)
            {
                HasError = true;
                ErrorMessage = "Minor radius must be less than major radius";
                return;
            }

            object? body = null;

            if (Context?.IsAvailable == true)
            {
                try
                {
                    double R = majorR * InventorContext.MM_TO_CM;
                    double r = minorR * InventorContext.MM_TO_CM;
                    double cx = center.X * InventorContext.MM_TO_CM;
                    double cy = center.Y * InventorContext.MM_TO_CM;
                    double cz = center.Z * InventorContext.MM_TO_CM;

                    // Build torus via high-resolution cylinder sweep in XZ plane (Y-up)
                    body = CreateTorusFallback(R, r, cx, cy, cz);
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Failed to create torus: {ex.Message}";
                    return;
                }
            }

            GetOutput("Body")!.Value = new BodyData
            {
                Body = body,
                Description = "Torus",
                SourceNodeId = Id
            };
        }

        /// <summary>
        /// Fallback: approximate torus with high-res cylinder segments in XZ plane.
        /// The ring lies horizontally with Y as the up-axis.
        /// </summary>
        private object? CreateTorusFallback(double R, double r, double cx, double cy, double cz)
        {
            var tb = Context!.TB;
            int segments = 72; // Higher resolution for smooth appearance
            object? result = null;

            for (int i = 0; i < segments; i++)
            {
                double a0 = (double)i / segments * 2.0 * Math.PI;
                double a1 = (double)(i + 1) / segments * 2.0 * Math.PI;

                // Ring in XZ plane (Y is up)
                double x0 = cx + R * Math.Cos(a0);
                double z0 = cz + R * Math.Sin(a0);
                double x1 = cx + R * Math.Cos(a1);
                double z1 = cz + R * Math.Sin(a1);

                var p0 = Context.TG.CreatePoint(x0, cy, z0);
                var p1 = Context.TG.CreatePoint(x1, cy, z1);

                var seg = tb.CreateSolidCylinderCone(p0, p1, r, r, r);

                if (result == null)
                    result = seg;
                else
                    tb.DoBoolean(result as SurfaceBody, seg, BooleanTypeEnum.kBooleanTypeUnion);
            }

            return result;
        }
    }
}
