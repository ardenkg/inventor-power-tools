// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// RepeatNode.cs - Creates a list by repeating a value N times
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a DataList by repeating a single value N times.
    /// Useful for creating uniform input arrays for pattern nodes.
    /// </summary>
    public class RepeatNode : Node
    {
        public override string TypeName => "Repeat";
        public override string Title => "Repeat";
        public override string Category => "Utility";

        public RepeatNode()
        {
            AddInput("Value", "Value", PortDataType.Any, null);
            AddInput("Count", "Count", PortDataType.Number, 5.0);
            AddOutput("List", "List", PortDataType.List);
        }

        protected override void Compute()
        {
            var val = GetInput("Value")!.GetEffectiveValue();
            int count = (int)GetInput("Count")!.GetDouble(5);

            if (count < 0) count = 0;
            if (count > 1000) count = 1000; // Safety cap

            var list = new DataList();
            for (int i = 0; i < count; i++)
                list.Items.Add(val);

            GetOutput("List")!.Value = list;
        }
    }
}
