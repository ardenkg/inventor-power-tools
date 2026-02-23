// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// BoundingBoxNode.cs - Bounding box extents of a body
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Computes the axis-aligned bounding box of a body.
    /// Outputs the min/max points and dimensions in mm.
    /// </summary>
    public class BoundingBoxNode : Node
    {
        public override string TypeName => "BoundingBox";
        public override string Title => "Bounding Box";
        public override string Category => "Measure";

        public BoundingBoxNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddOutput("Min", "Min Point", PortDataType.Point3D);
            AddOutput("Max", "Max Point", PortDataType.Point3D);
            AddOutput("SizeX", "Size X (mm)", PortDataType.Number);
            AddOutput("SizeY", "Size Y (mm)", PortDataType.Number);
            AddOutput("SizeZ", "Size Z (mm)", PortDataType.Number);
            AddOutput("Center", "Center", PortDataType.Point3D);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();

            if (bodyVal is not BodyData srcBody || srcBody.Body == null)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            try
            {
                dynamic body = srcBody.Body;
                var range = body.RangeBox;
                var minPt = range.MinPoint;
                var maxPt = range.MaxPoint;

                // Convert from cm to mm
                double minX = (double)minPt.X / InventorContext.MM_TO_CM;
                double minY = (double)minPt.Y / InventorContext.MM_TO_CM;
                double minZ = (double)minPt.Z / InventorContext.MM_TO_CM;
                double maxX = (double)maxPt.X / InventorContext.MM_TO_CM;
                double maxY = (double)maxPt.Y / InventorContext.MM_TO_CM;
                double maxZ = (double)maxPt.Z / InventorContext.MM_TO_CM;

                GetOutput("Min")!.Value = (minX, minY, minZ);
                GetOutput("Max")!.Value = (maxX, maxY, maxZ);
                GetOutput("SizeX")!.Value = maxX - minX;
                GetOutput("SizeY")!.Value = maxY - minY;
                GetOutput("SizeZ")!.Value = maxZ - minZ;
                GetOutput("Center")!.Value = ((minX + maxX) / 2.0, (minY + maxY) / 2.0, (minZ + maxZ) / 2.0);
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Bounding box failed: {ex.Message}";
            }
        }
    }
}
