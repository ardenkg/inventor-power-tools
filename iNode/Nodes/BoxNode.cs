// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// BoxNode.cs - Creates a box as a transient SurfaceBody
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a rectangular box as a transient SurfaceBody via TransientBRep.
    /// The body flows through the graph and can be combined, transformed,
    /// deconstructed, and finally applied to the part document.
    /// </summary>
    public class BoxNode : Node
    {
        public override string TypeName => "Box";
        public override string Title => "Box";
        public override string Category => "Geometry";

        public BoxNode()
        {
            AddInput("X", "X", PortDataType.Number, 10.0);
            AddInput("Y", "Y", PortDataType.Number, 10.0);
            AddInput("Z", "Z", PortDataType.Number, 10.0);
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var width = GetInput("X")!.GetDouble(10.0);
            var length = GetInput("Y")!.GetDouble(10.0);
            var height = GetInput("Z")!.GetDouble(10.0);
            var center = GetInput("Center")!.GetPoint3D();

            if (width <= 0 || length <= 0 || height <= 0)
            {
                HasError = true;
                ErrorMessage = "Dimensions must be positive";
                return;
            }

            object? body = null;

            if (Context?.IsAvailable == true)
            {
                try
                {
                    double w = width * InventorContext.MM_TO_CM;
                    double d = length * InventorContext.MM_TO_CM;
                    double h = height * InventorContext.MM_TO_CM;
                    double cx = center.X * InventorContext.MM_TO_CM;
                    double cy = center.Y * InventorContext.MM_TO_CM;
                    double cz = center.Z * InventorContext.MM_TO_CM;

                    var box = Context.TG.CreateBox();
                    box.MinPoint = Context.TG.CreatePoint(cx - w / 2, cy, cz - h / 2);
                    box.MaxPoint = Context.TG.CreatePoint(cx + w / 2, cy + d, cz + h / 2);
                    body = Context.TB.CreateSolidBlock(box);
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Failed to create box: {ex.Message}";
                    return;
                }
            }

            GetOutput("Body")!.Value = new BodyData
            {
                Body = body,
                Description = "Box",
                SourceNodeId = Id
            };
        }
    }
}
