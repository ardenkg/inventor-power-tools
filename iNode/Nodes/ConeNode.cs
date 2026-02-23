// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ConeNode.cs - Creates a cone as a transient SurfaceBody
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a cone (or truncated cone) as a transient SurfaceBody.
    /// TopRadius=0 gives a pointed cone, TopRadius>0 gives a frustum.
    /// </summary>
    public class ConeNode : Node
    {
        public override string TypeName => "Cone";
        public override string Title => "Cone";
        public override string Category => "Geometry";

        public ConeNode()
        {
            AddInput("BottomRadius", "Bottom Radius (mm)", PortDataType.Number, 5.0);
            AddInput("TopRadius", "Top Radius (mm)", PortDataType.Number, 0.0);
            AddInput("Height", "Height (mm)", PortDataType.Number, 10.0);
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bottomRadius = GetInput("BottomRadius")!.GetDouble(5.0);
            var topRadius = GetInput("TopRadius")!.GetDouble(0.0);
            var height = GetInput("Height")!.GetDouble(10.0);
            var center = GetInput("Center")!.GetPoint3D();

            if (bottomRadius <= 0 || height <= 0)
            {
                HasError = true;
                ErrorMessage = "Bottom radius and height must be positive";
                return;
            }
            if (topRadius < 0)
            {
                HasError = true;
                ErrorMessage = "Top radius cannot be negative";
                return;
            }

            object? body = null;

            if (Context?.IsAvailable == true)
            {
                try
                {
                    double br = bottomRadius * InventorContext.MM_TO_CM;
                    double tr = topRadius * InventorContext.MM_TO_CM;
                    double h = height * InventorContext.MM_TO_CM;
                    double cx = center.X * InventorContext.MM_TO_CM;
                    double cy = center.Y * InventorContext.MM_TO_CM;
                    double cz = center.Z * InventorContext.MM_TO_CM;

                    var basePoint = Context.TG.CreatePoint(cx, cy, cz);
                    var topPoint = Context.TG.CreatePoint(cx, cy + h, cz);

                    body = Context.TB.CreateSolidCylinderCone(basePoint, topPoint, br, tr, br);
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Failed to create cone: {ex.Message}";
                    return;
                }
            }

            GetOutput("Body")!.Value = new BodyData
            {
                Body = body,
                Description = topRadius > 0 ? "Frustum" : "Cone",
                SourceNodeId = Id
            };
        }
    }
}
