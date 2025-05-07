using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlayTable
{
    public class PTGraphNode : MonoBehaviour
    {
        #region fields
        public HashSet<PTGraphEdge> historyEdges { get { return _historyEdges; } }
        private HashSet<PTGraphEdge> _historyEdges = new HashSet<PTGraphEdge>();
        #endregion

        #region property
        public HashSet<PTGraphEdge> neighborEdges { get; private set; }
        public HashSet<PTGraphNode> neighborNodes { get; private set ; }
        #endregion

        #region api
        public bool AddConnectionHistory(PTGraphEdge edge)
        {
            try
            {
                _historyEdges.Add(edge);
                UpdateEdges();
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Find the shortest path to a node from this node
        /// </summary>
        /// <param name="destination"></param>
        /// <returns>The list with right sequence</returns>
        public List<PTGraphEdge> ShortestDistanceTo(PTGraphNode destination)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// The other node the node is connected to by an edge
        /// </summary>
        /// <param name="edge"></param>
        /// <returns>Return null of input is invalid</returns>
        public PTGraphNode DestinationThrough(PTGraphEdge edge)
        {
            if (edge == null)
            {
                return null;
            }
            if (this == edge.nodeA)
            {
                return edge.nodeB;
            }
            if (this == edge.nodeB)
            {
                return edge.nodeA;
            }
            return null;
        }
        #endregion

        #region helper
        private void UpdateEdges()
        {
            neighborEdges = new HashSet<PTGraphEdge>();
            foreach (PTGraphEdge edge in historyEdges)
            {
                if (edge == null)
                {
                    Debug.Log("edge == null");
                    continue;
                }
                if (edge.ContainsNode(this))
                {
                    neighborEdges.Add(edge);
                }
            }
            UpdateNeighbors();
        }
        private void UpdateNeighbors()
        {
            neighborNodes = new HashSet<PTGraphNode>();
            foreach (PTGraphEdge edge in neighborEdges)
            {
                try { neighborNodes.Add(DestinationThrough(edge)); } catch { }
            }
        }
        #endregion
    }

    [Serializable]
    public class EdgeConnectionRecord
    {
        public int id;
        public string nodeA;
        public string nodeB;
        public int length;
        public string datetimeConnected;
        public string datetimeDisconnected;
    }
}

