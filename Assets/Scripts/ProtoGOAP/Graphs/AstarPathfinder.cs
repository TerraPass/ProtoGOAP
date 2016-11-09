﻿using System;
using System.Linq;
using System.Collections.Generic;
using ProtoGOAP.Utils.Collections;

namespace ProtoGOAP.Graphs
{
	// TODO: Add the ability to limit search depth! (It's kind of necessary for infinite graphs.)
	/// <summary>
	/// This pathfinder uses A* algorithm to find paths on a graph.
	/// </summary>
	public class AstarPathfinder<GraphNode> : IPathfinder<GraphNode> where GraphNode : IGraphNode<GraphNode>
	{
		/// <summary>
		/// Partial path with estimated cost, comparable by estimated cost.
		/// </summary>
		private class PartialPath : IComparable<PartialPath>
		{
			private IList<IGraphEdge<GraphNode>> edges;

			public double EstimatedTotalCost { get; private set; }

			public PartialPath(double estimatedTotalCost)
			{
				this.edges = new List<IGraphEdge<GraphNode>>();
				this.EstimatedTotalCost = estimatedTotalCost;
			}

			public PartialPath(PartialPath other)
			{
				this.edges = new List<IGraphEdge<GraphNode>>(other.edges);
				this.EstimatedTotalCost = other.EstimatedTotalCost;
			}

			public bool IsEmpty
			{
				get {
					return this.edges.Count == 0;
				}
			}

			public GraphNode LastNode
			{
				get {
					var lastEdge = this.edges.LastOrDefault();
					if(lastEdge == null)
					{
						// Throwing an exception, instead of returning null,
						// to avoid the need to add a new() constraint on GraphNode,
						// thus disallowing the use of structs as GraphNodes
						// (structs and other value types cannot be null in C#).
						throw new InvalidOperationException();
					}
					return lastEdge.TargetNode;
				}
			}

			private double CostSoFar
			{
				get {
					return this.edges.Sum(edge => edge.Cost);
				}
			}

			public void AppendEdge(IGraphEdge<GraphNode> edge, double estimatedRemainingCost)
			{
				this.edges.Add(edge);
				this.EstimatedTotalCost = this.CostSoFar + edge.Cost + estimatedRemainingCost;
			}

			public Path<GraphNode> toPath()
			{
				return new Path<GraphNode>(this.edges);
			}

			#region IComparable implementation

			public int CompareTo(PartialPath other)
			{
				return this.EstimatedTotalCost.CompareTo(other.EstimatedTotalCost);
			}

			#endregion
		}

		private readonly PathCostHeuristic<GraphNode> heuristic;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProtoGOAP.Graphs.AstarPathfinder`1"/> class.
		/// </summary>
		/// <param name="heuristic">Heuristic to be used.</param>
		public AstarPathfinder(PathCostHeuristic<GraphNode> heuristic)
		{
			this.heuristic = heuristic;
		}

		#region IPathfinder implementation
		/// <inheritdoc />
		public Path<GraphNode> FindPath(
			GraphNode sourceNode,
			GraphNode targetNode,
			IEqualityComparer<GraphNode> targetEqualityComparer,
			IEqualityComparer<GraphNode> nodeEqualityComparer
		)
		{
			// Set of already visited nodes
			var closed = new HashSet<GraphNode>(nodeEqualityComparer);
			// Priority queue of partial and potentially complete paths
			var open = new ListBasedPriorityQueue<PartialPath>();
			open.Add(new PartialPath(this.heuristic(sourceNode, targetNode)));

			// While there are unexplored nodes
			while(open.Count > 0)
			{
				// Pop partial path with the lowest estimated cost from the priority queue
				var currentPartialPath = open.PopFront();
				// Node to explore
				GraphNode currentNode;

				// If partial path is empty (and therefore has no last node to speak of),
				// it means we are at the source of our search.
				if(currentPartialPath.IsEmpty)
				{
					// Explore the source node
					currentNode = sourceNode;
				}
				else
				{
					// Explore the last node of the current partial path
					currentNode = currentPartialPath.LastNode;
				}

				// If the currently examined node has been examined previously, don't consider it further.
				// This has the effect of discarding the partial path, which led to it, from the open priority queue.
				// FIXME: As far as I understand, this is correct behaviour only if the heuristic is admissable! 
				// In general case (i.e. with inadmissable heuristics), closed nodes may need to be reopened.
				// To control this behaviour an additional flag may be added to the constructor and checked here.
				if(closed.Contains(currentNode))
				{
					continue;
				}

				// If the currently examined node is the target
				if(targetEqualityComparer.Equals(currentNode, targetNode))
				{
					// Hurray!
					return currentPartialPath.toPath();
				}

				// Mark currentNode as visited by placing it into closed set
				// and insert paths from sourceNode to currentNode's neighbours into the open priority queue.
				closed.Add(currentNode);
				foreach(var outgoingEdge in currentNode.OutgoingEdges)
				{
					var neighbour = outgoingEdge.TargetNode;
					var pathToNeighbour = new PartialPath(currentPartialPath);
					pathToNeighbour.AppendEdge(outgoingEdge, this.heuristic(neighbour, targetNode));
					open.Add(pathToNeighbour);
				}
			}

			// Found no path to targetNode
			throw new PathNotFoundException("Failed to find a path using A*");

		}
		#endregion
	}
}
