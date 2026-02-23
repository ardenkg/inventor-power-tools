// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NodeGraph.cs - The complete node graph, execution engine, validation
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace iNode.Core
{
    /// <summary>
    /// Manages the collection of nodes and connections, handles execution order,
    /// validation, and graph operations.
    /// </summary>
    public class NodeGraph
    {
        #region Properties

        /// <summary>All nodes in the graph.</summary>
        public List<Node> Nodes { get; } = new List<Node>();

        /// <summary>All connections (wires) in the graph.</summary>
        public List<Connection> Connections { get; } = new List<Connection>();

        /// <summary>Event raised when the graph structure changes.</summary>
        public event EventHandler? GraphChanged;

        /// <summary>Event raised when a node starts executing.</summary>
        public event EventHandler<Node>? NodeExecuting;

        /// <summary>Event raised when execution completes.</summary>
        public event EventHandler<bool>? ExecutionCompleted;

        /// <summary>Event raised when nodes are removed, passing their IDs.</summary>
        public event EventHandler<List<Guid>>? NodesRemoved;

        #endregion

        #region Node Management

        /// <summary>
        /// Adds a node to the graph.
        /// </summary>
        public void AddNode(Node node)
        {
            Nodes.Add(node);
            GraphChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Removes a node and all its connections from the graph.
        /// </summary>
        public void RemoveNode(Node node)
        {
            // Remove all connections involving this node
            Connections.RemoveAll(c => c.SourceNode == node || c.TargetNode == node);
            Nodes.Remove(node);
            NodesRemoved?.Invoke(this, new List<Guid> { node.Id });
            GraphChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Removes all selected nodes and their connections.
        /// </summary>
        public void RemoveSelectedNodes()
        {
            var selected = Nodes.Where(n => n.IsSelected).ToList();
            var removedIds = new List<Guid>();
            foreach (var node in selected)
            {
                Connections.RemoveAll(c => c.SourceNode == node || c.TargetNode == node);
                Nodes.Remove(node);
                removedIds.Add(node.Id);
            }
            if (selected.Count > 0)
            {
                NodesRemoved?.Invoke(this, removedIds);
                GraphChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Finds a node by its ID.
        /// </summary>
        public Node? FindNode(Guid id) => Nodes.FirstOrDefault(n => n.Id == id);

        /// <summary>
        /// Clears the entire graph.
        /// </summary>
        public void Clear()
        {
            Nodes.Clear();
            Connections.Clear();
            GraphChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Creates a connection between two ports if valid.
        /// Returns the connection, or null if invalid.
        /// </summary>
        public Connection? Connect(Node sourceNode, string sourcePortName, Node targetNode, string targetPortName)
        {
            // Validate ports exist
            var sourcePort = sourceNode.GetOutput(sourcePortName);
            var targetPort = targetNode.GetInput(targetPortName);
            if (sourcePort == null || targetPort == null) return null;

            // Prevent self-connection
            if (sourceNode == targetNode) return null;

            // Check type compatibility
            if (!AreTypesCompatible(sourcePort.DataType, targetPort.DataType)) return null;

            // Check for circular dependency
            if (WouldCreateCycle(sourceNode, targetNode)) return null;

            // Remove any existing connection to this input (only one connection per input)
            Connections.RemoveAll(c => c.TargetNode == targetNode && c.TargetPortName == targetPortName);

            // Create the connection
            var conn = new Connection(sourceNode, sourcePortName, targetNode, targetPortName);
            Connections.Add(conn);
            GraphChanged?.Invoke(this, EventArgs.Empty);
            return conn;
        }

        /// <summary>
        /// Removes a specific connection.
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            Connections.Remove(connection);
            GraphChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets the connection feeding into a specific input port.
        /// </summary>
        public Connection? GetConnectionToInput(Node node, string inputPortName)
        {
            return Connections.FirstOrDefault(c => c.TargetNode == node && c.TargetPortName == inputPortName);
        }

        /// <summary>
        /// Gets all connections from a specific output port.
        /// </summary>
        public List<Connection> GetConnectionsFromOutput(Node node, string outputPortName)
        {
            return Connections.Where(c => c.SourceNode == node && c.SourcePortName == outputPortName).ToList();
        }

        /// <summary>
        /// Checks if a port has a connection.
        /// </summary>
        public bool IsPortConnected(Node node, string portName, bool isInput)
        {
            if (isInput)
                return Connections.Any(c => c.TargetNode == node && c.TargetPortName == portName);
            else
                return Connections.Any(c => c.SourceNode == node && c.SourcePortName == portName);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the graph for execution readiness.
        /// Returns a list of error messages (empty if valid).
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Check each node
            foreach (var node in Nodes)
            {
                // Check for required inputs that are not connected and have no default
                foreach (var input in node.Inputs)
                {
                    if (input.IsOptional) continue;
                    if (!IsPortConnected(node, input.Name, true) && input.GetEffectiveValue() == null)
                    {
                        errors.Add($"Node '{node.Title}': Input '{input.DisplayName}' is not connected and has no value.");
                    }
                }
            }

            // Check for cycles
            if (HasCycles())
            {
                errors.Add("Graph contains circular dependencies.");
            }

            return errors;
        }

        /// <summary>
        /// Checks if two data types are compatible for connection.
        /// </summary>
        private bool AreTypesCompatible(PortDataType source, PortDataType target)
        {
            if (source == PortDataType.Any || target == PortDataType.Any) return true;
            return source == target;
        }

        /// <summary>
        /// Checks if connecting sourceNode -> targetNode would create a cycle.
        /// </summary>
        private bool WouldCreateCycle(Node sourceNode, Node targetNode)
        {
            // DFS from targetNode to see if we can reach sourceNode through existing connections
            var visited = new HashSet<Guid>();
            return CanReach(targetNode, sourceNode, visited);
        }

        private bool CanReach(Node from, Node target, HashSet<Guid> visited)
        {
            if (from == target) return true;
            if (!visited.Add(from.Id)) return false;

            foreach (var conn in Connections.Where(c => c.SourceNode == from))
            {
                if (CanReach(conn.TargetNode, target, visited))
                    return true;
            }

            // Also check reverse: does 'from' feed into anything that reaches 'target'?
            // Actually for cycle detection: we need to check if target can reach source
            // via existing outgoing connections from target (downstream of target)
            return false;
        }

        /// <summary>
        /// Checks if the graph has any cycles using Kahn's algorithm.
        /// </summary>
        private bool HasCycles()
        {
            var sorted = TopologicalSort();
            return sorted == null;
        }

        #endregion

        #region Execution

        /// <summary>
        /// Executes the entire graph in topological order.
        /// Returns true if all nodes executed successfully.
        /// </summary>
        public bool Execute(InventorContext? context = null)
        {
            // Reset all nodes
            foreach (var node in Nodes)
                node.ResetExecution();

            // Get execution order
            var order = TopologicalSort();
            if (order == null)
            {
                ExecutionCompleted?.Invoke(this, false);
                return false;
            }

            bool allSuccess = true;

            // Execute each node in order, providing Inventor context
            foreach (var node in order)
            {
                node.Context = context;
                NodeExecuting?.Invoke(this, node);
                node.Execute(this);

                if (node.HasError)
                {
                    allSuccess = false;
                    // Continue executing other branches if possible
                }
            }

            ExecutionCompleted?.Invoke(this, allSuccess);
            return allSuccess;
        }

        /// <summary>
        /// Returns nodes in topological order (dependencies first).
        /// Returns null if the graph has cycles.
        /// </summary>
        public List<Node>? TopologicalSort()
        {
            // Build in-degree map
            var inDegree = new Dictionary<Guid, int>();
            foreach (var node in Nodes)
                inDegree[node.Id] = 0;

            foreach (var conn in Connections)
            {
                if (inDegree.ContainsKey(conn.TargetNode.Id))
                    inDegree[conn.TargetNode.Id]++;
            }

            // Start with nodes that have no incoming connections
            var queue = new Queue<Node>();
            foreach (var node in Nodes)
            {
                if (inDegree[node.Id] == 0)
                    queue.Enqueue(node);
            }

            var sorted = new List<Node>();
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                sorted.Add(node);

                // For each outgoing connection
                foreach (var conn in Connections.Where(c => c.SourceNode == node))
                {
                    inDegree[conn.TargetNode.Id]--;
                    if (inDegree[conn.TargetNode.Id] == 0)
                        queue.Enqueue(conn.TargetNode);
                }
            }

            // If not all nodes were sorted, there's a cycle
            return sorted.Count == Nodes.Count ? sorted : null;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Selects all nodes.
        /// </summary>
        public void SelectAll()
        {
            foreach (var node in Nodes)
                node.IsSelected = true;
        }

        /// <summary>
        /// Deselects all nodes.
        /// </summary>
        public void DeselectAll()
        {
            foreach (var node in Nodes)
                node.IsSelected = false;
        }

        /// <summary>
        /// Gets all selected nodes.
        /// </summary>
        public List<Node> GetSelectedNodes() => Nodes.Where(n => n.IsSelected).ToList();

        #endregion
    }
}
