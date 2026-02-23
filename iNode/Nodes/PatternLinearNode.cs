// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// PatternLinearNode.cs - Linear pattern via copy + translate + union
// ============================================================================

using System;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a linear pattern of the input body by copying, translating,
    /// and unioning instances using TransientBRep.
    /// </summary>
    public class PatternLinearNode : Node
    {
        public override string TypeName => "PatternLinear";
        public override string Title => "Linear Pattern";
        public override string Category => "Transform";

        public PatternLinearNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Count", "Count", PortDataType.Number, 3.0);
            AddInput("Spacing", "Spacing", PortDataType.Number, 20.0);
            AddInput("Direction", "Direction", PortDataType.Point3D, (1.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var geomVal = GetInput("Body")!.GetEffectiveValue();
            var count = (int)GetInput("Count")!.GetDouble(3);
            var spacing = GetInput("Spacing")!.GetDouble(20);
            var dir = GetInput("Direction")!.GetPoint3D();

            if (count < 1)
            {
                HasError = true;
                ErrorMessage = "Count must be at least 1";
                return;
            }

            if (geomVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            if (Context?.IsAvailable != true || srcBody.Body == null)
            {
                GetOutput("Body")!.Value = new BodyData
                {
                    Description = $"Pattern ({count}\u00D7)",
                    SourceNodeId = Id
                };
                return;
            }

            try
            {
                // Normalize direction
                double len = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
                if (len < 1e-10) { dir = (1, 0, 0); len = 1; }
                double dx = dir.X / len * spacing;
                double dy = dir.Y / len * spacing;
                double dz = dir.Z / len * spacing;

                var tb = Context.TB;

                // Start with a copy of the original
                var result = tb.Copy(srcBody.Body as SurfaceBody);

                // Add count-1 additional copies, each offset further
                for (int i = 1; i < count; i++)
                {
                    var copy = tb.Copy(srcBody.Body as SurfaceBody);
                    var matrix = Context.CreateTranslationMatrix(dx * i, dy * i, dz * i);
                    tb.Transform(copy, matrix);
                    tb.DoBoolean(result, copy, BooleanTypeEnum.kBooleanTypeUnion);
                }

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = result,
                    Description = $"Pattern ({count}\u00D7)",
                    SourceNodeId = Id
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Pattern failed: {ex.Message}";
            }
        }
    }
}
