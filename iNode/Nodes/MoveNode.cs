// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// MoveNode.cs - Translates a body using TransientBRep.Transform
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Translates a body by a vector using TransientBRep.Transform.
    /// Creates a copy of the input body and applies the translation.
    /// Use a Point (X,Y,Z) or Vector node to construct the direction.
    /// </summary>
    public class MoveNode : Node
    {
        public override string TypeName => "Move";
        public override string Title => "Move";
        public override string Category => "Transform";

        public MoveNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Vector", "Vector (mm)", PortDataType.Point3D, null);
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var geomVal = GetInput("Body")!.GetEffectiveValue();

            var vectorVal = GetInput("Vector")!.GetEffectiveValue();
            if (vectorVal is not ValueTuple<double, double, double> vec)
            {
                HasError = true;
                ErrorMessage = "Connect a Vector or Point node";
                return;
            }

            double x = vec.Item1, y = vec.Item2, z = vec.Item3;

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
                    Description = "Move",
                    SourceNodeId = Id,
                    IsFromActivePart = srcBody.IsFromActivePart,
                    PendingOperations = new List<PendingOperation>(srcBody.PendingOperations)
                };
                return;
            }

            try
            {
                var copy = Context.TB.Copy(srcBody.Body as SurfaceBody);
                var matrix = Context.CreateTranslationMatrix(x, y, z);
                Context.TB.Transform(copy, matrix);

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = copy,
                    Description = "Move",
                    SourceNodeId = Id,
                    IsFromActivePart = srcBody.IsFromActivePart,
                    PendingOperations = new List<PendingOperation>(srcBody.PendingOperations)
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Move failed: {ex.Message}";
            }
        }
    }
}
