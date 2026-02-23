// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// VolumeNode.cs - Volume of a body
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Calculates the volume of a body.
    /// Output is in mm³.
    /// </summary>
    public class VolumeNode : Node
    {
        public override string TypeName => "Volume";
        public override string Title => "Volume";
        public override string Category => "Measure";

        public VolumeNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddOutput("Volume", "Volume (mm\u00B3)", PortDataType.Number);
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
                // Inventor returns volume in cm³, convert to mm³
                double volCm3 = (double)body.Volume;
                // 1 cm³ = 1000 mm³
                GetOutput("Volume")!.Value = volCm3 * 1000.0;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Volume calculation failed: {ex.Message}";
            }
        }
    }
}
