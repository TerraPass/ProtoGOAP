﻿using System;
using System.Collections.Generic;
using System.Linq;

using Terrapass.Debug;

using ProtoGOAP.Planning;

namespace ProtoGOAP.Agents
{
	public class Agent : IAgent
	{
		private readonly AgentEnvironment env;

		private Goal currentGoal;

		public Agent(AgentEnvironment environment)
		{
			this.env = PreconditionUtils.EnsureNotNull(environment, "environment");
			this.currentGoal = environment.GoalSelector.DefaultGoal;
		}

		public void Update()
		{
			// If the plan has been completed, interrupted or there is no plan yet,
			// select a new goal, plan for it and execute the plan.
			if(env.CurrentPlanExecution.Status == ExecutionStatus.None
			   || env.CurrentPlanExecution.Status == ExecutionStatus.Complete
			   || env.CurrentPlanExecution.Status == ExecutionStatus.Interrupted)
			{
				this.AchieveRelevantGoal(env.CurrentPlanExecution.Status == ExecutionStatus.Interrupted);
			}
			// If the execution of the current plan has failed, attepmt to replan for the same goal,
			// if replanning fails, select a new goal and proceed with it.
			else if(env.CurrentPlanExecution.Status == ExecutionStatus.Failed)
			{
				try
				{
					// Attempt to re-plan
					env.PlanExecutor.SubmitForExecution(
						env.Planner.FormulatePlan(env.KnowledgeProvider, env.SupportedPlanningActions, this.currentGoal)
					);
				}
				catch(PlanNotFoundException)
				{
					// TODO: Log
					this.AchieveRelevantGoal(false);
				}
			}
			else if(env.CurrentPlanExecution.Status == ExecutionStatus.InProgress)
			{
				// TODO: Read reevaluation sensor and, if needed interrupt the current plan.
				// AchieveRelevantGoal() will be called during one of the next updates, 
				// when plan executor's current execution reaches Interrupted status.
//				if(env.Sensor.IsReevaluationNeeded)
//				{
//					env.PlanExecutor.InterruptExecution();
//				}
			}

			env.PlanExecutor.Update();
		}

		private Plan GetPlanFor(Goal goal)
		{
			return env.Planner.FormulatePlan(env.KnowledgeProvider, env.SupportedPlanningActions, goal);
		}

		private void AchieveRelevantGoal(bool forceReevaluation)
		{
			if(forceReevaluation)
			{
				env.GoalSelector.ForceReevaluation();
			}

			var relevantGoals = env.GoalSelector.RelevantGoals;
			bool planSubmitted = false;
			foreach(var goal in relevantGoals)
			{
				try
				{						
					env.PlanExecutor.SubmitForExecution(this.GetPlanFor(goal));
					this.currentGoal = goal;
					break;
				}
				catch(PlanNotFoundException)
				{
					// TODO: Log
					continue;
				}
			}

			// If no relevant goal can be achieved, use the default goal.
			if(!planSubmitted)
			{
				env.PlanExecutor.SubmitForExecution(this.GetPlanFor(env.GoalSelector.DefaultGoal));
			}
		}
	}
}
