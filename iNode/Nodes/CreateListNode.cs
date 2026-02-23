// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// CreateListNode.cs - Creates a DataList from individual inputs
// ============================================================================

using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a DataList from up to 6 individual value inputs.
    /// Unconnected inputs are skipped. Useful for packaging multiple
    /// values into a list for batch operations.
    /// </summary>
    public class CreateListNode : Node
    {
        public override string TypeName => "CreateList";
        public override string Title => "Create List";
        public override string Category => "Utility";

        public CreateListNode()
        {
            AddOptionalInput("Item0", "Item 0", PortDataType.Any, null);
            AddOptionalInput("Item1", "Item 1", PortDataType.Any, null);
            AddOptionalInput("Item2", "Item 2", PortDataType.Any, null);
            AddOptionalInput("Item3", "Item 3", PortDataType.Any, null);
            AddOptionalInput("Item4", "Item 4", PortDataType.Any, null);
            AddOptionalInput("Item5", "Item 5", PortDataType.Any, null);
            AddOutput("List", "List", PortDataType.List);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var list = new DataList();

            for (int i = 0; i < 6; i++)
            {
                var port = GetInput($"Item{i}");
                if (port != null)
                {
                    var val = port.GetEffectiveValue();
                    if (val != null)
                        list.Items.Add(val);
                }
            }

            GetOutput("List")!.Value = list;
            GetOutput("Count")!.Value = (double)list.Count;
        }
    }
}
