using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PlayTable
{
    public static class PTGraph
    {
        /// <summary>
        /// Returns the shortest distance between nodeA and node B. 
        /// </summary>
        /// <param name="nodeA"></param>
        /// <param name="nodeB"></param>
        /// <returns>Returns the distance if two nodes are connected. Returns -1 if they are not.</returns>
        public static int ShortestDistanceBetween(PTGraphNode nodeA, PTGraphNode nodeB)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Get the length of a collection(list, set for example) of edges.
        /// </summary>
        /// <param name="edges"></param>
        /// <returns>Returns -1 if input is invalid</returns>
        public static int LengthOfPath(ICollection<PTGraphEdge> edges)
        {
            if (edges == null)
            {
                return -1;
            }

            int length = 0;
            foreach (PTGraphEdge edge in edges)
            {
                length += edge.tilesA2B.childCount;
            }
            return length;
        }
    }
}

