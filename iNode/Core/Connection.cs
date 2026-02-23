// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// Connection.cs - Represents a wire between two ports
// ============================================================================

using System;

namespace iNode.Core
{
    /// <summary>
    /// Represents a wire/connection between an output port of one node
    /// and an input port of another node.
    /// </summary>
    public class Connection
    {
        /// <summary>Unique identifier.</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>The node containing the source (output) port.</summary>
        public Node SourceNode { get; set; }

        /// <summary>The name of the output port on the source node.</summary>
        public string SourcePortName { get; set; }

        /// <summary>The node containing the target (input) port.</summary>
        public Node TargetNode { get; set; }

        /// <summary>The name of the input port on the target node.</summary>
        public string TargetPortName { get; set; }

        public Connection(Node sourceNode, string sourcePortName, Node targetNode, string targetPortName)
        {
            SourceNode = sourceNode;
            SourcePortName = sourcePortName;
            TargetNode = targetNode;
            TargetPortName = targetPortName;
        }

        /// <summary>
        /// Gets the data type of this connection based on the source port.
        /// </summary>
        public PortDataType GetDataType()
        {
            var port = SourceNode.GetOutput(SourcePortName);
            return port?.DataType ?? PortDataType.Any;
        }
    }
}
