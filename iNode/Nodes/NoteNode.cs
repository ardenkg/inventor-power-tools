// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NoteNode.cs - Non-functional comment/annotation node
// ============================================================================

using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Non-functional annotation node for documenting workflows.
    /// Has no inputs/outputs — just displays a text note on the canvas body area.
    /// </summary>
    public class NoteNode : Node
    {
        public override string TypeName => "Note";
        public override string Title => "Note";
        public override string Category => "Utility";

        public string NoteText { get; set; } = "Add notes here...";

        public NoteNode()
        {
            // No ports — this is a display-only node
        }

        protected override void Compute()
        {
            // No computation — this is just a label
        }

        // Note: does NOT use GetDisplaySummary — text is rendered in the body
        // via DrawInlineValues in the canvas.

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Note",
                    Key = "NoteText",
                    Value = NoteText,
                    DisplayOnNode = NoteText
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?> { ["NoteText"] = NoteText };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("NoteText", out var t) && t is string s)
                NoteText = s;
        }
    }
}
