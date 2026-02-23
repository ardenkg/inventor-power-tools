// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// WorkflowSerializer.cs - Save/Load node graphs as JSON
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace iNode.Core
{
    /// <summary>
    /// Serializes and deserializes NodeGraph instances to/from JSON files.
    /// </summary>
    public static class WorkflowSerializer
    {
        private const int FORMAT_VERSION = 1;

        #region Data Transfer Objects

        private class WorkflowDto
        {
            public int Version { get; set; } = FORMAT_VERSION;
            public List<NodeDto> Nodes { get; set; } = new List<NodeDto>();
            public List<ConnectionDto> Connections { get; set; } = new List<ConnectionDto>();
        }

        private class NodeDto
        {
            public string Id { get; set; } = "";
            public string TypeName { get; set; } = "";
            public float X { get; set; }
            public float Y { get; set; }
            public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
        }

        private class ConnectionDto
        {
            public string SourceNodeId { get; set; } = "";
            public string SourcePort { get; set; } = "";
            public string TargetNodeId { get; set; } = "";
            public string TargetPort { get; set; } = "";
        }

        #endregion

        #region Save

        /// <summary>
        /// Saves the node graph to a JSON file.
        /// </summary>
        public static void Save(NodeGraph graph, string filePath)
        {
            var dto = new WorkflowDto();

            // Serialize nodes
            foreach (var node in graph.Nodes)
            {
                var nodeDto = new NodeDto
                {
                    Id = node.Id.ToString(),
                    TypeName = node.TypeName,
                    X = node.Position.X,
                    Y = node.Position.Y,
                    Parameters = node.GetParameters()
                };
                dto.Nodes.Add(nodeDto);
            }

            // Serialize connections
            foreach (var conn in graph.Connections)
            {
                dto.Connections.Add(new ConnectionDto
                {
                    SourceNodeId = conn.SourceNode.Id.ToString(),
                    SourcePort = conn.SourcePortName,
                    TargetNodeId = conn.TargetNode.Id.ToString(),
                    TargetPort = conn.TargetPortName
                });
            }

            var json = JsonConvert.SerializeObject(dto, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            });

            File.WriteAllText(filePath, json);
        }

        #endregion

        #region Load

        /// <summary>
        /// Loads a node graph from a JSON file.
        /// </summary>
        public static NodeGraph Load(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var dto = JsonConvert.DeserializeObject<WorkflowDto>(json);
            if (dto == null) throw new InvalidOperationException("Failed to parse workflow file.");

            var graph = new NodeGraph();
            var idMap = new Dictionary<string, Node>();

            // Create nodes
            foreach (var nodeDto in dto.Nodes)
            {
                var node = NodeFactory.Create(nodeDto.TypeName);
                if (node == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Unknown node type: {nodeDto.TypeName}");
                    continue;
                }

                node.Id = Guid.Parse(nodeDto.Id);
                node.Position = new System.Drawing.PointF(nodeDto.X, nodeDto.Y);

                // Restore parameters
                if (nodeDto.Parameters != null && nodeDto.Parameters.Count > 0)
                {
                    // Convert JToken values to proper types
                    var converted = new Dictionary<string, object?>();
                    foreach (var kvp in nodeDto.Parameters)
                    {
                        if (kvp.Value is JToken jToken)
                        {
                            converted[kvp.Key] = ConvertJToken(jToken);
                        }
                        else
                        {
                            converted[kvp.Key] = kvp.Value;
                        }
                    }
                    node.SetParameters(converted);
                }

                graph.Nodes.Add(node);
                idMap[nodeDto.Id] = node;
            }

            // Create connections
            foreach (var connDto in dto.Connections)
            {
                if (idMap.TryGetValue(connDto.SourceNodeId, out var sourceNode) &&
                    idMap.TryGetValue(connDto.TargetNodeId, out var targetNode))
                {
                    graph.Connect(sourceNode, connDto.SourcePort, targetNode, connDto.TargetPort);
                }
            }

            return graph;
        }

        /// <summary>
        /// Converts a JToken to a CLR value.
        /// </summary>
        private static object? ConvertJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Float:
                    return token.Value<double>();
                case JTokenType.Integer:
                    return (double)token.Value<long>();
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Null:
                    return null;
                default:
                    return token.ToString();
            }
        }

        #endregion
    }
}
