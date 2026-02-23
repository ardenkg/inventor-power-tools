// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// PipeNode.cs - Creates a hollow cylinder (pipe/tube)
// ============================================================================

using System;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a hollow cylinder (pipe/tube) by subtracting an inner
    /// cylinder from an outer cylinder.
    /// </summary>
    public class PipeNode : Node
    {
        public override string TypeName => "Pipe";
        public override string Title => "Pipe";
        public override string Category => "Geometry";

        public PipeNode()
        {
            AddInput("OuterRadius", "Outer Radius (mm)", PortDataType.Number, 5.0);
            AddInput("InnerRadius", "Inner Radius (mm)", PortDataType.Number, 3.0);
            AddInput("Height", "Height (mm)", PortDataType.Number, 10.0);
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var outerR = GetInput("OuterRadius")!.GetDouble(5.0);
            var innerR = GetInput("InnerRadius")!.GetDouble(3.0);
            var height = GetInput("Height")!.GetDouble(10.0);
            var center = GetInput("Center")!.GetPoint3D();

            if (outerR <= 0 || innerR <= 0 || height <= 0)
            {
                HasError = true;
                ErrorMessage = "All dimensions must be positive";
                return;
            }
            if (innerR >= outerR)
            {
                HasError = true;
                ErrorMessage = "Inner radius must be less than outer radius";
                return;
            }

            object? body = null;

            if (Context?.IsAvailable == true)
            {
                try
                {
                    double or_ = outerR * InventorContext.MM_TO_CM;
                    double ir = innerR * InventorContext.MM_TO_CM;
                    double h = height * InventorContext.MM_TO_CM;
                    double cx = center.X * InventorContext.MM_TO_CM;
                    double cy = center.Y * InventorContext.MM_TO_CM;
                    double cz = center.Z * InventorContext.MM_TO_CM;

                    var basePoint = Context.TG.CreatePoint(cx, cy, cz);
                    var topPoint = Context.TG.CreatePoint(cx, cy + h, cz);

                    var outer = Context.TB.CreateSolidCylinderCone(
                        basePoint, topPoint, or_, or_, or_);
                    var inner = Context.TB.CreateSolidCylinderCone(
                        Context.TG.CreatePoint(cx, cy, cz),
                        Context.TG.CreatePoint(cx, cy + h, cz),
                        ir, ir, ir);

                    Context.TB.DoBoolean(outer, inner, BooleanTypeEnum.kBooleanTypeDifference);
                    body = outer;
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Failed to create pipe: {ex.Message}";
                    return;
                }
            }

            GetOutput("Body")!.Value = new BodyData
            {
                Body = body,
                Description = "Pipe",
                SourceNodeId = Id
            };
        }
    }
}
