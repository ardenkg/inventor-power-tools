// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ThickenNode.cs - Creates a solid by thickening/offsetting all faces
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Thickens a body by creating an offset version and unioning/subtracting.
    /// For preview: applies a scale transform as an approximation.
    /// This is better handled as a pending operation for commit mode.
    /// </summary>
    public class ThickenNode : Node
    {
        public override string TypeName => "Thicken";
        public override string Title => "Thicken";
        public override string Category => "Operations";

        public ThickenNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Thickness", "Thickness (mm)", PortDataType.Number, 2.0);
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();
            var thickness = GetInput("Thickness")!.GetDouble(2.0);

            if (bodyVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }
            if (thickness == 0)
            {
                HasError = true;
                ErrorMessage = "Thickness cannot be zero";
                return;
            }

            // Thicken is a feature-level operation: record as pending
            var result = srcBody.CloneMetadata();
            result.Description = $"Thicken ({thickness:F1}mm)";
            result.SourceNodeId = Id;
            result.PendingOperations.Add(new PendingThicken
            {
                Thickness = thickness,
                OriginNodeId = Id
            });

            GetOutput("Body")!.Value = result;
        }
    }
}
