using System;
using System.Collections.Generic;
using DefendAndCollect.FSM;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using DefendAndCollect.World;
using UnityEngine;

namespace DefendAndCollect.Agents
{
    public enum AllyState
    {
        Idle = 0,
        SeekResource = 1,
        Collect = 2,
        TransportToDeposit = 3
    }

    public sealed class AllyAgent : NpcAgent
    {
        private const float IdleDuration = 0.5f;
        private const float CollectDuration = 1.2f;
        private const float RetryDuration = 1f;
        private const float PerceptionInterval = 0.25f;

        private readonly List<ResourceNode> resources = new List<ResourceNode>();
        private readonly List<ResourceNode> candidateBuffer = new List<ResourceNode>();

        private StateMachine<AllyState, AllyAgent> stateMachine;
        private ResourceNode currentResource;
        private Vector2Int depositCell;
        private float collectElapsed;
        private float retryElapsed;
        private float perceptionElapsed;

        public AllyState CurrentState => stateMachine != null && stateMachine.CurrentState.HasValue
            ? stateMachine.CurrentState.Value
            : AllyState.Idle;

        public override string CurrentStateName => CurrentState.ToString();

        public IReadOnlyList<ResourceNode> Resources => resources;

        public Vector2Int DepositCell => depositCell;

        public ResourceNode CurrentResource => currentResource;

        public bool HasResource => currentResource != null && currentResource.IsAvailable;

        public bool IsCarrying => CurrentState == AllyState.TransportToDeposit;

        public int CollectCount { get; private set; }

        public int DeliveredCount { get; private set; }

        public void Configure(
            GameGrid grid,
            Vector2Int startCell,
            IReadOnlyList<ResourceNode> resourceNodes,
            Vector2Int deposit,
            float speed = 3.5f,
            PathfindingAlgorithm algorithm = PathfindingAlgorithm.AStar,
            NavigationMode mode = NavigationMode.UniformCost,
            Color? color = null,
            float detectionRadius = 6f)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (!grid.IsPassable(deposit))
            {
                throw new ArgumentOutOfRangeException(nameof(deposit), deposit, "Deposit cell must be passable.");
            }

            resources.Clear();

            if (resourceNodes != null)
            {
                for (int i = 0; i < resourceNodes.Count; i++)
                {
                    ResourceNode node = resourceNodes[i];

                    if (node != null && grid.IsPassable(node.Cell))
                    {
                        resources.Add(node);
                    }
                }
            }

            depositCell = deposit;
            base.Initialize(
                grid,
                startCell,
                speed,
                algorithm,
                mode,
                color ?? new Color(0.35f, 1f, 0.45f, 1f),
                detectionRadius);
        }

        protected override void OnConfigured()
        {
            BuildStateMachine();
            stateMachine.ChangeState(AllyState.Idle);
        }

        protected override void TickBehavior(float deltaTime)
        {
            stateMachine?.Tick(deltaTime);
        }

        private void BuildStateMachine()
        {
            stateMachine = new StateMachine<AllyState, AllyAgent>(this);
            stateMachine.Register(AllyState.Idle, new IdleState());
            stateMachine.Register(AllyState.SeekResource, new SeekResourceState());
            stateMachine.Register(AllyState.Collect, new CollectState());
            stateMachine.Register(AllyState.TransportToDeposit, new TransportToDepositState());
            stateMachine.StateChanged += (previous, current) => NotifyStateChanged();
        }

        private void TransitionTo(AllyState nextState)
        {
            stateMachine?.ChangeState(nextState);
        }

        private bool TrySelectResource()
        {
            currentResource = null;

            if (Grid == null)
            {
                return false;
            }

            candidateBuffer.Clear();

            for (int i = 0; i < resources.Count; i++)
            {
                ResourceNode node = resources[i];

                if (node == null || !node.IsAvailable || !Grid.IsPassable(node.Cell))
                {
                    continue;
                }

                if (!Perception.IsWithinRadius(Cell, node.Cell, DetectionRadius))
                {
                    continue;
                }

                candidateBuffer.Add(node);
            }

            while (candidateBuffer.Count > 0)
            {
                int nearestIndex = 0;
                float nearestDistance = Perception.Distance(Cell, candidateBuffer[0].Cell);

                for (int i = 1; i < candidateBuffer.Count; i++)
                {
                    float distance = Perception.Distance(Cell, candidateBuffer[i].Cell);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestIndex = i;
                    }
                }

                ResourceNode candidate = candidateBuffer[nearestIndex];
                candidateBuffer.RemoveAt(nearestIndex);

                if (MoveTo(candidate.Cell))
                {
                    currentResource = candidate;
                    return true;
                }
            }

            return false;
        }

        private void DropInvalidResource()
        {
            if (currentResource == null)
            {
                return;
            }

            if (currentResource.IsAvailable && Grid != null && Grid.IsPassable(currentResource.Cell))
            {
                return;
            }

            currentResource = null;
            Stop();
        }

        private bool TryGoToDeposit()
        {
            return MoveTo(depositCell);
        }

        private sealed class IdleState : IFsmState<AllyState, AllyAgent>
        {
            private float elapsed;

            public void Enter(AllyAgent context)
            {
                elapsed = 0f;
                context.Stop();
                context.currentResource = null;
            }

            public void Exit(AllyAgent context)
            {
            }

            public void Tick(AllyAgent context, float deltaTime)
            {
                elapsed += deltaTime;
                context.perceptionElapsed += deltaTime;

                if (context.perceptionElapsed >= PerceptionInterval)
                {
                    context.perceptionElapsed = 0f;

                    if (context.TrySelectResource())
                    {
                        context.TransitionTo(AllyState.SeekResource);
                        return;
                    }
                }

                if (elapsed >= IdleDuration)
                {
                    context.TransitionTo(AllyState.SeekResource);
                }
            }
        }

        private sealed class SeekResourceState : IFsmState<AllyState, AllyAgent>
        {
            public void Enter(AllyAgent context)
            {
                context.retryElapsed = 0f;

                if (!context.HasResource)
                {
                    context.TrySelectResource();
                }
            }

            public void Exit(AllyAgent context)
            {
            }

            public void Tick(AllyAgent context, float deltaTime)
            {
                context.DropInvalidResource();

                if (context.HasResource)
                {
                    if (context.IsMoving)
                    {
                        return;
                    }

                    if (context.HasArrived)
                    {
                        context.TransitionTo(AllyState.Collect);
                        return;
                    }

                    context.retryElapsed += deltaTime;

                    if (context.retryElapsed >= RetryDuration)
                    {
                        context.retryElapsed = 0f;
                        context.TrySelectResource();
                    }

                    return;
                }

                context.perceptionElapsed += deltaTime;

                if (context.perceptionElapsed < PerceptionInterval)
                {
                    return;
                }

                context.perceptionElapsed = 0f;
                context.TrySelectResource();
            }
        }

        private sealed class CollectState : IFsmState<AllyState, AllyAgent>
        {
            public void Enter(AllyAgent context)
            {
                context.Stop();
                context.collectElapsed = 0f;
            }

            public void Exit(AllyAgent context)
            {
            }

            public void Tick(AllyAgent context, float deltaTime)
            {
                if (context.currentResource == null || !context.currentResource.IsAvailable)
                {
                    context.currentResource = null;
                    context.TransitionTo(AllyState.SeekResource);
                    return;
                }

                context.collectElapsed += deltaTime;

                if (context.collectElapsed < CollectDuration)
                {
                    return;
                }

                context.currentResource.MarkTaken();
                context.currentResource = null;
                context.CollectCount++;
                context.TransitionTo(AllyState.TransportToDeposit);
            }
        }

        private sealed class TransportToDepositState : IFsmState<AllyState, AllyAgent>
        {
            public void Enter(AllyAgent context)
            {
                context.retryElapsed = 0f;

                if (!context.TryGoToDeposit())
                {
                    context.retryElapsed = RetryDuration;
                }
            }

            public void Exit(AllyAgent context)
            {
            }

            public void Tick(AllyAgent context, float deltaTime)
            {
                if (context.IsMoving)
                {
                    return;
                }

                if (context.HasArrived)
                {
                    context.DeliveredCount++;
                    context.TransitionTo(AllyState.Idle);
                    return;
                }

                context.retryElapsed += deltaTime;

                if (context.retryElapsed < RetryDuration)
                {
                    return;
                }

                context.retryElapsed = 0f;
                context.TryGoToDeposit();
            }
        }
    }
}
