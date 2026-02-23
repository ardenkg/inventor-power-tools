// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SketchPolygonNode.cs - Regular polygon sketch profile
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a regular polygon profile (triangle, square, hexagon, etc.)
    /// centered at a point on a plane. Useful for hex bolt heads, nuts, etc.
    /// </summary>
    public class SketchPolygonNode : Node
    {
        public override string TypeName => "SketchPolygon";
        public override string Title => "Sketch Polygon";
        public override string Category => "Sketch";

        public SketchPolygonNode()
        {
            AddOptionalInput("Plane", "Plane", PortDataType.WorkPlane, null);
            AddInput("CenterX", "Center X (mm)", PortDataType.Number, 0.0);
            AddInput("CenterY", "Center Y (mm)", PortDataType.Number, 0.0);
            AddInput("Radius", "Radius (mm)", PortDataType.Number, 5.0);
            AddInput("Sides", "Sides", PortDataType.Number, 6.0);
            AddOutput("Profile", "Profile", PortDataType.Profile);
        }

        protected override void Compute()
        {
            var cx = GetInput("CenterX")!.GetDouble(0);
            var cy = GetInput("CenterY")!.GetDouble(0);
            var radius = GetInput("Radius")!.GetDouble(5.0);
            var sides = (int)GetInput("Sides")!.GetDouble(6);

            if (radius <= 0) { HasError = true; ErrorMessage = "Radius must be positive"; return; }
            if (sides < 3) { HasError = true; ErrorMessage = "Need at least 3 sides"; return; }

            var planeObj = GetInput("Plane")?.Value;
            PlaneData? plane = planeObj as PlaneData;
            if (plane == null) plane = PlaneData.XY();

            var profile = new SketchProfileData
            {
                Plane = plane,
                Curves = new List<ProfileCurve>
                {
                    new PolygonProfileCurve
                    {
                        CenterX = cx, CenterY = cy,
                        Radius = radius, Sides = sides
                    }
                }
            };

            GetOutput("Profile")!.Value = profile;
        }
    }
}
