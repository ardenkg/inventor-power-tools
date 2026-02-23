// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// AreaNode.cs - Surface area of a body
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Calculates the total surface area of a body.
    /// Output is in mm².
    /// </summary>
    public class AreaNode : Node
    {
        public override string TypeName => "Area";
        public override string Title => "Area";
        public override string Category => "Measure";

        public AreaNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddOutput("Area", "Area (mm\u00B2)", PortDataType.Number);
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
                // Inventor returns area in cm², convert to mm²
                double areaCm2 = 0;
                foreach (var faceObj in body.Faces)
                {
                    dynamic face = faceObj;
                    var evaluator = face.Evaluator;
                    double area = 0;
                    evaluator.GetArea(ref area);
                    areaCm2 += area;
                }
                // 1 cm² = 100 mm²
                GetOutput("Area")!.Value = areaCm2 * 100.0;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Area calculation failed: {ex.Message}";
            }
        }
    }
}
