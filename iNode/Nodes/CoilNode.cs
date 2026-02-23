// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// CoilNode.cs - Creates a coil/spring as a transient body
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a coil (helix/spring) geometry.
    /// Preview: high-resolution cylinder segments along a helical path (Y-up).
    /// The helix spirals in the XZ plane and rises along Y.
    /// </summary>
    public class CoilNode : Node
    {
        public override string TypeName => "Coil";
        public override string Title => "Coil";
        public override string Category => "Geometry";

        public CoilNode()
        {
            AddInput("CoilRadius", "Coil Radius (mm)", PortDataType.Number, 10.0);
            AddInput("WireRadius", "Wire Radius (mm)", PortDataType.Number, 1.0);
            AddInput("Pitch", "Pitch (mm)", PortDataType.Number, 5.0);
            AddInput("Turns", "Turns", PortDataType.Number, 5.0);
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var coilRadius = GetInput("CoilRadius")!.GetDouble(10);
            var wireRadius = GetInput("WireRadius")!.GetDouble(1);
            var pitch = GetInput("Pitch")!.GetDouble(5);
            var turns = GetInput("Turns")!.GetDouble(5);
            var center = GetInput("Center")!.GetPoint3D();

            if (coilRadius <= 0 || wireRadius <= 0 || pitch <= 0 || turns <= 0)
            {
                HasError = true;
                ErrorMessage = "All parameters must be positive";
                return;
            }
            if (wireRadius >= coilRadius)
            {
                HasError = true;
                ErrorMessage = "Wire radius must be smaller than coil radius";
                return;
            }

            if (Context?.IsAvailable != true)
            {
                GetOutput("Body")!.Value = new BodyData
                {
                    Description = $"Coil ({turns:F0} turns)",
                    SourceNodeId = Id
                };
                return;
            }

            try
            {
                // High-resolution cylinder segments along helix path (Y-up)
                var tb = Context.TB;
                int segmentsPerTurn = 48; // Higher resolution for smooth coil
                int totalSegments = (int)(segmentsPerTurn * turns);
                double totalAngle = turns * 2 * Math.PI;
                double totalHeight = turns * pitch;

                double R = coilRadius * InventorContext.MM_TO_CM;
                double r = wireRadius * InventorContext.MM_TO_CM;
                double cx = center.X * InventorContext.MM_TO_CM;
                double cy = center.Y * InventorContext.MM_TO_CM;
                double cz = center.Z * InventorContext.MM_TO_CM;

                object? result = null;

                for (int i = 0; i < totalSegments; i++)
                {
                    double t0 = (double)i / totalSegments;
                    double t1 = (double)(i + 1) / totalSegments;

                    double a0 = t0 * totalAngle;
                    double a1 = t1 * totalAngle;
                    // Y is up — helix rises along Y
                    double y0 = t0 * totalHeight * InventorContext.MM_TO_CM;
                    double y1 = t1 * totalHeight * InventorContext.MM_TO_CM;

                    // Helix in XZ plane, rising along Y
                    double x0 = cx + R * Math.Cos(a0);
                    double z0 = cz + R * Math.Sin(a0);
                    double x1 = cx + R * Math.Cos(a1);
                    double z1 = cz + R * Math.Sin(a1);

                    var p0 = Context.TG.CreatePoint(x0, cy + y0, z0);
                    var p1 = Context.TG.CreatePoint(x1, cy + y1, z1);

                    var seg = tb.CreateSolidCylinderCone(p0, p1, r, r, r);

                    if (result == null)
                        result = seg;
                    else
                        tb.DoBoolean(result as SurfaceBody, seg, BooleanTypeEnum.kBooleanTypeUnion);
                }

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = result,
                    Description = $"Coil ({turns:F0} turns)",
                    SourceNodeId = Id
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Coil creation failed: {ex.Message}";
            }
        }
    }
}
