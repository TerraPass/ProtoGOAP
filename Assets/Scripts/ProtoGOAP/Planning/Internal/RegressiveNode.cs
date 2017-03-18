﻿using System;
using System.Linq;
using System.Collections.Generic;

using Terrapass.Debug;

using ProtoGOAP.Graphs;

using ProtoGOAP.Planning.Preconditions;

namespace ProtoGOAP.Planning.Internal
{
	internal sealed class RegressiveNode : IGraphNode<RegressiveNode>, IEquatable<RegressiveNode>
	{
		private readonly RegressiveState currentConstraints;
		private readonly WorldState initialState;

		private readonly INodeExpander<RegressiveNode> nodeExpander;

		private IEnumerable<IGraphEdge<RegressiveNode>> outgoingEdges;

		public RegressiveState CurrentConstraints
		{
			get {
				if(IsTarget)
				{
					throw new InvalidOperationException(
						string.Format(
							"CurrentConstraints cannot be retrieved from a target {0}", this.GetType()
						)
					);
				}
				return currentConstraints;
			}
		}

		public WorldState InitialState
		{
			get {
//				if(!IsTarget)
//				{
//					throw new InvalidOperationException(
//						string.Format(
//							"InitialState cannot be retrieved from a regular (non-target) {0}", this.GetType()
//						)
//					);
//				}
				return initialState;
			}
		}

		public bool IsTarget { get; }

		private RegressiveNode(
			RegressiveState currentConstraints,
			WorldState initialState,
			INodeExpander<RegressiveNode> nodeExpander,
			bool isTarget
		)
		{
			DebugUtils.Assert(
				isTarget || nodeExpander != null,
				"Unless isTarget is true, nodeExpander must not be null"
			);

			this.currentConstraints = currentConstraints;
			this.initialState = initialState;
			this.nodeExpander = nodeExpander;
			this.IsTarget = isTarget;
		}

		public static RegressiveNode MakeRegular(
			RegressiveState currentConstraints,
			WorldState initialState,
			INodeExpander<RegressiveNode> nodeExpander
		)
		{
			return new RegressiveNode(
				currentConstraints,
				//default(WorldState),
				initialState,
				PreconditionUtils.EnsureNotNull(nodeExpander, "nodeExpander"),
				false
			);
		}

		public static RegressiveNode MakeTarget(WorldState initialWorldState)
		{
			return new RegressiveNode(default(RegressiveState), initialWorldState, null, true);
		}

		#region IGraphNode implementation

		public IEnumerable<IGraphEdge<RegressiveNode>> OutgoingEdges
		{
			get {
				if(this.outgoingEdges == null)
				{
					this.outgoingEdges = this.nodeExpander.ExpandNode(this);
				}

				DebugUtils.Assert(
					outgoingEdges.All((edge) => edge.SourceNode == this),
					"this edge must be the source node of every outgoing edge"
				);

				return this.outgoingEdges;
			}
		}

		#endregion

		#region IEquatable implementation

		public bool Equals(RegressiveNode other)
		{
			if(this.IsTarget && other.IsTarget)
			{
				throw new InvalidOperationException(
					string.Format(
						"{0}.Equals() cannot compare two target nodes",
						this.GetType()
					)
				);
			}

			if(!this.IsTarget && !other.IsTarget)
			{
				return this.CurrentConstraints.Equals(other.CurrentConstraints);
			}

			// If one is a target node, and another is a regular node
			var regularNode = this.IsTarget ? other : this;
			var targetNode = this.IsTarget ? this : other;

			return targetNode.InitialState.All((kvp) => regularNode.CurrentConstraints[kvp.Key].Contains(kvp.Value));
		}

		#endregion

		public override string ToString()
		{
			return string.Format("[RegressiveNode: currentConstraints={0}, initialState={1}, IsTarget={2}]", currentConstraints, initialState, IsTarget);
		}				
	}
}

