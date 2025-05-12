using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayTable
{
    public class PTGraphEdge : MonoBehaviour
    {
        public PTGraphNode nodeA;
        public PTGraphNode nodeB;
        public bool canA2B = true;
        public bool canB2A = true;
        public Transform tilesA2B;

        public int Length
        {
            get
            {
                if (tilesA2B == null)
                {
                    return 0;
                }
                else
                {
                    return tilesA2B.childCount;
                }
            }
        }
        public DateTime datetimeConstructed { get; private set; }
        public PTDelegateGraphNode OnNodesChanged;

        private PTGraphNode _lastNodeA;
        private PTGraphNode _lastNodeB;

        private void Awake()
        {
            datetimeConstructed = DateTime.Now;
            _lastNodeA = nodeA;
            _lastNodeB = nodeB;
            RegisterToNodes();
            OnNodesChanged = (nodes) => RegisterToNodes();
        }
        private void OnEnable()
        {
            StartCoroutine(CheckNodesChangedCoroutine(PT.DEFAULT_TIMER));
        }

        private IEnumerator CheckNodesChangedCoroutine(float timer)
        {
            while (true)
            {
                yield return new WaitForSeconds(timer);
                CheckNodesChanged();
            }
        }
        private void CheckNodesChanged()
        {
            //Check if the nodes have been changed
            bool hasChangedNodeA = _lastNodeA == null && nodeA != null || _lastNodeA != null && _lastNodeA != nodeA;
            bool hasChangedNodeB = _lastNodeB == null && nodeB != null || _lastNodeB != null && _lastNodeB != nodeB;

            //Invoke OnNodesChanged
            List<PTGraphNode> changedNodes = new List<PTGraphNode>();
            if (hasChangedNodeA)
            {
                changedNodes.Add(nodeA);
            }
            if (hasChangedNodeB)
            {
                changedNodes.Add(nodeB);
            }
            if (changedNodes.Count != 0)
            {
                OnNodesChanged(changedNodes);
            }

            //Update last nodes
            _lastNodeA = nodeA;
            _lastNodeB = nodeB;
        }

        private void RegisterToNodes()
        {
            try { nodeA.AddConnectionHistory(this); } catch { }
            try { nodeB.AddConnectionHistory(this); } catch { }
        }

        public bool ContainsNode(PTGraphNode node)
        {
            return IsNodeA(node) || IsNodeB(node);
        }
        public bool IsNodeA(PTGraphNode node)
        {
            return nodeA!= null && nodeA == node || nodeA == null && node == null;
        }
        public bool IsNodeB(PTGraphNode node)
        {
            return nodeB != null && nodeB == node || nodeB == null && node == null;
        }
    }
}

