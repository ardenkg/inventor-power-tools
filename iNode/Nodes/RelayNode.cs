// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// RelayNode.cs - Pass-through node for organizing wires
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Pass-through node that forwards any value unchanged.
    /// Useful for organizing complex wire layouts by adding waypoints.
    /// Compact size — acts as a wire routing point.
    /// </summary>
    public class RelayNode : Node
    {
        public override string TypeName => "Relay";
        public override string Title => "Relay";
        public override string Category => "Utility";

        /// <summary>Compact width for relay nodes — just enough for "In" / "Out" labels.</summary>
        public override int NodeWidth => 200;

        public RelayNode()
        {
            AddInput("In", "In", PortDataType.Any, null);
            AddOutput("Out", "Out", PortDataType.Any);
        }

        protected override void Compute()
        {
            GetOutput("Out")!.Value = GetInput("In")!.GetEffectiveValue();
        }
    }
}
