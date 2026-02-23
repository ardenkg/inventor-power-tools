// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SphereNode.cs - Creates a sphere as a transient SurfaceBody
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a sphere as a transient SurfaceBody via TransientBRep.
    /// </summary>
    public class SphereNode : Node
    {
        public override string TypeName => "Sphere";
        public override string Title => "Sphere";
        public override string Category => "Geometry";

        public SphereNode()
        {
            AddInput("Radius", "Radius", PortDataType.Number, 5.0);
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var radius = GetInput("Radius")!.GetDouble(5.0);
            var center = GetInput("Center")!.GetPoint3D();

            if (radius <= 0)
            {
                HasError = true;
                ErrorMessage = "Radius must be positive";
                return;
            }

            object? body = null;

            if (Context?.IsAvailable == true)
            {
                try
                {
                    double r = radius * InventorContext.MM_TO_CM;
                    double cx = center.X * InventorContext.MM_TO_CM;
                    double cy = center.Y * InventorContext.MM_TO_CM;
                    double cz = center.Z * InventorContext.MM_TO_CM;

                    body = Context.TB.CreateSolidSphere(
                        Context.TG.CreatePoint(cx, cy, cz), r);
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Failed to create sphere: {ex.Message}";
                    return;
                }
            }

            GetOutput("Body")!.Value = new BodyData
            {
                Body = body,
                Description = "Sphere",
                SourceNodeId = Id
            };
        }
    }
}
