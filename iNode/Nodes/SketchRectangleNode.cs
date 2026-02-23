// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SketchRectangleNode.cs - Creates a rectangular profile on a plane
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a rectangular sketch profile on a reference plane.
    /// The profile can be fed into Extrude or Revolve to create geometry.
    /// All dimensions are in mm (user units).
    /// </summary>
    public class SketchRectangleNode : Node
    {
        public override string TypeName => "SketchRectangle";
        public override string Title => "Sketch Rectangle";
        public override string Category => "Sketch";

        public SketchRectangleNode()
        {
            AddOptionalInput("Plane", "Plane", PortDataType.WorkPlane, null);
            AddInput("CenterX", "Center X (mm)", PortDataType.Number, 0.0);
            AddInput("CenterY", "Center Y (mm)", PortDataType.Number, 0.0);
            AddInput("Width", "Width (mm)", PortDataType.Number, 10.0);
            AddInput("Height", "Height (mm)", PortDataType.Number, 10.0);
            AddOutput("Profile", "Profile", PortDataType.Profile);
        }

        protected override void Compute()
        {
            var planeObj = GetInput("Plane")?.Value;
            double cx = GetInput("CenterX")!.GetDouble(0.0);
            double cy = GetInput("CenterY")!.GetDouble(0.0);
            double width = GetInput("Width")!.GetDouble(10.0);
            double height = GetInput("Height")!.GetDouble(10.0);

            if (width <= 0 || height <= 0)
            {
                HasError = true;
                ErrorMessage = "Width and Height must be positive";
                return;
            }

            PlaneData? plane = planeObj as PlaneData;
            if (plane == null)
            {
                plane = PlaneData.XY();
            }

            var profile = new SketchProfileData
            {
                Plane = plane,
                Curves = new List<ProfileCurve>
                {
                    new RectangleProfileCurve
                    {
                        CenterX = cx,
                        CenterY = cy,
                        Width = width,
                        Height = height
                    }
                }
            };

            GetOutput("Profile")!.Value = profile;
        }
    }
}
