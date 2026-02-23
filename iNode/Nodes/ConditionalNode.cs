// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ConditionalNode.cs - If-then-else for values
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Outputs TrueValue if Condition is non-zero, otherwise FalseValue.
    /// Use with Compare node: Compare → Conditional.
    /// </summary>
    public class ConditionalNode : Node
    {
        public override string TypeName => "Conditional";
        public override string Title => "Conditional";
        public override string Category => "Logic";

        public ConditionalNode()
        {
            AddInput("Condition", "Condition (0/1)", PortDataType.Number, 0.0);
            AddInput("True", "True Value", PortDataType.Number, 1.0);
            AddInput("False", "False Value", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var cond = GetInput("Condition")!.GetDouble();
            var trueVal = GetInput("True")!.GetDouble(1.0);
            var falseVal = GetInput("False")!.GetDouble(0.0);

            GetOutput("Result")!.Value = cond != 0 ? trueVal : falseVal;
        }
    }
}
