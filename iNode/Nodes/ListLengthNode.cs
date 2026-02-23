// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ListLengthNode.cs - Gets the number of items in a DataList
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Outputs the count (length) of a DataList.
    /// Also works with FaceListData and EdgeListData.
    /// </summary>
    public class ListLengthNode : Node
    {
        public override string TypeName => "ListLength";
        public override string Title => "List Length";
        public override string Category => "Utility";

        public ListLengthNode()
        {
            AddInput("List", "List", PortDataType.Any, null);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var val = GetInput("List")!.GetEffectiveValue();

            int count = 0;
            if (val is DataList dl)
                count = dl.Count;
            else if (val is FaceListData fl)
                count = fl.Faces.Count;
            else if (val is EdgeListData el)
                count = el.Edges.Count;
            else if (val != null)
                count = 1; // Single item = count of 1

            GetOutput("Count")!.Value = (double)count;
        }
    }
}
