// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SketchCircleNode.cs - Creates a circular profile on a plane
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a circular sketch profile on a reference plane.
    /// The profile can be fed into Extrude or Revolve to create geometry.
    /// All dimensions are in mm (user units).
    /// </summary>
    public class SketchCircleNode : Node
    {
        public override string TypeName => "SketchCircle";
        public override string Title => "Sketch Circle";
        public override string Category => "Sketch";

        public SketchCircleNode()
        {
            AddOptionalInput("Plane", "Plane", PortDataType.WorkPlane, null);
            AddInput("CenterX", "Center X (mm)", PortDataType.Number, 0.0);
            AddInput("CenterY", "Center Y (mm)", PortDataType.Number, 0.0);
            AddInput("Radius", "Radius (mm)", PortDataType.Number, 5.0);
            AddOutput("Profile", "Profile", PortDataType.Profile);
        }

        protected override void Compute()
        {
            var planeObj = GetInput("Plane")?.Value;
            double cx = GetInput("CenterX")!.GetDouble(0.0);
            double cy = GetInput("CenterY")!.GetDouble(0.0);
            double radius = GetInput("Radius")!.GetDouble(5.0);

            if (radius <= 0)
            {
                HasError = true;
                ErrorMessage = "Radius must be positive";
                return;
            }

            PlaneData? plane = planeObj as PlaneData;
            if (plane == null)
            {
                // Default to XY plane if no plane connected
                plane = PlaneData.XY();
            }

            var profile = new SketchProfileData
            {
                Plane = plane,
                Curves = new List<ProfileCurve>
                {
                    new CircleProfileCurve
                    {
                        CenterX = cx,
                        CenterY = cy,
                        Radius = radius
                    }
                }
            };

            GetOutput("Profile")!.Value = profile;
        }
    }
}
