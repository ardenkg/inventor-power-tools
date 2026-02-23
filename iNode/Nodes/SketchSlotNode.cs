// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SketchSlotNode.cs - Obround/slot profile
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a slot (obround/stadium) profile — a rectangle with
    /// semicircular ends. Common in mechanical design.
    /// </summary>
    public class SketchSlotNode : Node
    {
        public override string TypeName => "SketchSlot";
        public override string Title => "Sketch Slot";
        public override string Category => "Sketch";

        public SketchSlotNode()
        {
            AddOptionalInput("Plane", "Plane", PortDataType.WorkPlane, null);
            AddInput("CenterX", "Center X (mm)", PortDataType.Number, 0.0);
            AddInput("CenterY", "Center Y (mm)", PortDataType.Number, 0.0);
            AddInput("Length", "Length (mm)", PortDataType.Number, 20.0);
            AddInput("Width", "Width (mm)", PortDataType.Number, 6.0);
            AddOutput("Profile", "Profile", PortDataType.Profile);
        }

        protected override void Compute()
        {
            var cx = GetInput("CenterX")!.GetDouble(0);
            var cy = GetInput("CenterY")!.GetDouble(0);
            var length = GetInput("Length")!.GetDouble(20);
            var width = GetInput("Width")!.GetDouble(6);

            if (length <= 0 || width <= 0)
            {
                HasError = true;
                ErrorMessage = "Length and width must be positive";
                return;
            }

            var planeObj = GetInput("Plane")?.Value;
            PlaneData? plane = planeObj as PlaneData;
            if (plane == null) plane = PlaneData.XY();

            var profile = new SketchProfileData
            {
                Plane = plane,
                Curves = new List<ProfileCurve>
                {
                    new SlotProfileCurve
                    {
                        CenterX = cx, CenterY = cy,
                        Length = length, Width = width
                    }
                }
            };

            GetOutput("Profile")!.Value = profile;
        }
    }
}
