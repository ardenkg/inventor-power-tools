// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// BakeNode.cs - Terminal node that marks geometry for commit to Inventor
// Only bodies/profiles connected to Bake nodes get committed on "Commit"
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Terminal "output" node. Connect a body or sketch profile to mark
    /// it for baking (committing) into the active Inventor part document.
    /// Only geometry connected to Bake nodes is committed — everything else
    /// stays as preview-only.
    /// </summary>
    public class BakeNode : Node
    {
        public override string TypeName => "Bake";
        public override string Title => "Bake";
        public override string Category => "Output";

        /// <summary>Distinct header color for the Output category — bright gold.</summary>
        public override Color HeaderColor => Color.FromArgb(220, 170, 30);

        /// <summary>Optional display name for the committed feature.</summary>
        public string FeatureName { get; set; } = "";

        public BakeNode()
        {
            AddOptionalInput("Body", "Body to Bake", PortDataType.Geometry, null);
            AddOptionalInput("Profile", "Profile to Bake", PortDataType.Profile, null);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();
            var profileVal = GetInput("Profile")!.GetEffectiveValue();

            if (bodyVal is BodyData)
            {
                HasError = false;
                ErrorMessage = "";
            }
            else if (profileVal is SketchProfileData)
            {
                HasError = false;
                ErrorMessage = "";
            }
            else if (bodyVal == null && profileVal == null)
            {
                HasError = true;
                ErrorMessage = "Connect a Body or Profile";
            }
            else
            {
                HasError = true;
                ErrorMessage = "Expected Body or Profile input";
            }
        }

        public override string? GetDisplaySummary()
        {
            var bodyVal = GetInput("Body")?.GetEffectiveValue();
            var profileVal = GetInput("Profile")?.GetEffectiveValue();

            if (bodyVal is BodyData bd)
                return string.IsNullOrEmpty(FeatureName) ? bd.Description : FeatureName;
            if (profileVal is SketchProfileData spd)
                return string.IsNullOrEmpty(FeatureName) ? spd.Description : FeatureName;
            return "No input";
        }

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Feature Name",
                    Key = "FeatureName",
                    Value = FeatureName,
                    Choices = null
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["FeatureName"] = FeatureName
            };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("FeatureName", out var fn))
                FeatureName = fn?.ToString() ?? "";
        }
    }
}
