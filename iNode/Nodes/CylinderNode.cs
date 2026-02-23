// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// CylinderNode.cs - Creates a cylinder as a transient SurfaceBody
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a cylinder as a transient SurfaceBody via TransientBRep.
    /// </summary>
    public class CylinderNode : Node
    {
        public override string TypeName => "Cylinder";
        public override string Title => "Cylinder";
        public override string Category => "Geometry";

        public CylinderNode()
        {
            AddInput("Radius", "Radius", PortDataType.Number, 5.0);
            AddInput("Height", "Height", PortDataType.Number, 10.0);
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var radius = GetInput("Radius")!.GetDouble(5.0);
            var height = GetInput("Height")!.GetDouble(10.0);
            var center = GetInput("Center")!.GetPoint3D();

            if (radius <= 0 || height <= 0)
            {
                HasError = true;
                ErrorMessage = "Radius and Height must be positive";
                return;
            }

            object? body = null;

            if (Context?.IsAvailable == true)
            {
                try
                {
                    double r = radius * InventorContext.MM_TO_CM;
                    double h = height * InventorContext.MM_TO_CM;
                    double cx = center.X * InventorContext.MM_TO_CM;
                    double cy = center.Y * InventorContext.MM_TO_CM;
                    double cz = center.Z * InventorContext.MM_TO_CM;

                    body = Context.TB.CreateSolidCylinderCone(
                        Context.TG.CreatePoint(cx, cy, cz),
                        Context.TG.CreatePoint(cx, cy + h, cz),
                        r, r, r);
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Failed to create cylinder: {ex.Message}";
                    return;
                }
            }

            GetOutput("Body")!.Value = new BodyData
            {
                Body = body,
                Description = "Cylinder",
                SourceNodeId = Id
            };
        }
    }
}
