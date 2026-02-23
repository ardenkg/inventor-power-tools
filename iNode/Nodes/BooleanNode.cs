// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// BooleanNode.cs - Boolean operations on real transient bodies
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Performs Boolean operations (Union, Subtract, Intersect) on two
    /// transient SurfaceBodies using TransientBRep.DoBoolean.
    /// The result is a real combined body that can be further processed.
    /// </summary>
    public class BooleanNode : Node
    {
        public override string TypeName => "Boolean";
        public override string Title => "Boolean";
        public override string Category => "Geometry";

        /// <summary>
        /// The operation to perform: "Union", "Subtract", or "Intersect".
        /// </summary>
        public string Operation { get; set; } = "Union";

        public BooleanNode()
        {
            AddInput("A", "Body A", PortDataType.Geometry, null);
            AddInput("B", "Body B", PortDataType.Geometry, null);
            AddOutput("Result", "Result", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var aVal = GetInput("A")!.GetEffectiveValue();
            var bVal = GetInput("B")!.GetEffectiveValue();

            if (aVal is not BodyData bodyA)
            {
                HasError = true;
                ErrorMessage = "Body A not connected";
                return;
            }
            if (bVal is not BodyData bodyB)
            {
                HasError = true;
                ErrorMessage = "Body B not connected";
                return;
            }

            if (Context?.IsAvailable != true || bodyA.Body == null || bodyB.Body == null)
            {
                // No Inventor context — produce a descriptive placeholder
                GetOutput("Result")!.Value = new BodyData
                {
                    Description = $"Boolean ({Operation})",
                    SourceNodeId = Id,
                    IsFromActivePart = bodyA.IsFromActivePart || bodyB.IsFromActivePart
                };
                return;
            }

            try
            {
                var tb = Context.TB;

                // Copy bodies so originals aren't mutated (other nodes may reference them)
                var copyA = tb.Copy(bodyA.Body as SurfaceBody);
                var copyB = tb.Copy(bodyB.Body as SurfaceBody);

                var boolType = Operation switch
                {
                    "Subtract" => BooleanTypeEnum.kBooleanTypeDifference,
                    "Intersect" => BooleanTypeEnum.kBooleanTypeIntersect,
                    _ => BooleanTypeEnum.kBooleanTypeUnion
                };

                tb.DoBoolean(copyA, copyB, boolType);

                // Merge pending operations from both operands
                var pendingOps = new List<PendingOperation>();
                pendingOps.AddRange(bodyA.PendingOperations);
                pendingOps.AddRange(bodyB.PendingOperations);

                GetOutput("Result")!.Value = new BodyData
                {
                    Body = copyA,
                    Description = $"Boolean ({Operation})",
                    SourceNodeId = Id,
                    IsFromActivePart = bodyA.IsFromActivePart || bodyB.IsFromActivePart,
                    PendingOperations = pendingOps
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Boolean failed: {ex.Message}";
            }
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["Operation"] = Operation
            };
        }

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Operation:",
                    Key = "Operation",
                    Value = Operation,
                    Choices = new[] { "Union", "Subtract", "Intersect" },
                    DisplayOnNode = Operation
                }
            };
        }

        public override string? GetDisplaySummary() => Operation;

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Operation", out var op))
            {
                var s = op?.ToString() ?? "Union";
                if (s.Equals("Subtract", StringComparison.OrdinalIgnoreCase))
                    Operation = "Subtract";
                else if (s.Equals("Intersect", StringComparison.OrdinalIgnoreCase))
                    Operation = "Intersect";
                else
                    Operation = "Union";
            }
        }
    }
}
