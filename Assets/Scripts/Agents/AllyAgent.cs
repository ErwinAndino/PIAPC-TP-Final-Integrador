using System;
using System.Collections.Generic;
using DefendAndCollect.FSM;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
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

        private readonly List<Vector2Int> resourceCells = new List<Vector2Int>();

        private StateMachine<AllyState, AllyAgent> stateMachine;
        private int resourceIndex;
        private Vector2Int currentResource;
        private Vector2Int depositCell;
        private float collectElapsed;
        private float retryElapsed;

        public AllyState CurrentState => stateMachine != null && stateMachine.CurrentState.HasValue
            ? stateMachine.CurrentState.Value
            : AllyState.Idle;

        public override string CurrentStateName => CurrentState.ToString();

        public IReadOnlyList<Vector2Int> ResourceCells => resourceCells;

        public Vector2Int DepositCell => depositCell;

        public Vector2Int CurrentResource => currentResource;

        public int CollectCount { get; private set; }

        public void Configure(
            GameGrid grid,
            Vector2Int startCell,
            IReadOnlyList<Vector2Int> resources,
            Vector2Int deposit,
            float speed = 3.5f,
            PathfindingAlgorithm algorithm = PathfindingAlgorithm.AStar,
            NavigationMode mode = NavigationMode.UniformCost,
            Color? color = null)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (!grid.IsPassable(deposit))
            {
                throw new ArgumentOutOfRangeException(nameof(deposit), deposit, "Deposit cell must be passable.");
            }

            resourceCells.Clear();

            if (resources != null)
            {
                for (int i = 0; i < resources.Count; i++)
                {
                    if (grid.IsPassable(resources[i]))
                    {
                        resourceCells.Add(resources[i]);
                    }
                }
            }

            depositCell = deposit;
            base.Initialize(grid, startCell, speed, algorithm, mode, color ?? new Color(0.35f, 1f, 0.45f, 1f));
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

        private bool TrySelectNextResource()
        {
            if (resourceCells.Count == 0)
            {
                return false;
            }

            for (int attempt = 0; attempt < resourceCells.Count; attempt++)
            {
                Vector2Int candidate = resourceCells[resourceIndex % resourceCells.Count];
                resourceIndex = (resourceIndex + 1) % resourceCells.Count;

                if (MoveTo(candidate))
                {
                    currentResource = candidate;
                    return true;
                }
            }

            return false;
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
            }

            public void Exit(AllyAgent context)
            {
            }

            public void Tick(AllyAgent context, float deltaTime)
            {
                elapsed += deltaTime;

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
                context.TrySelectNextResource();
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
                    context.TransitionTo(AllyState.Collect);
                    return;
                }

                context.retryElapsed += deltaTime;

                if (context.retryElapsed < RetryDuration)
                {
                    return;
                }

                context.retryElapsed = 0f;

                if (!context.TrySelectNextResource())
                {
                    context.TransitionTo(AllyState.Idle);
                }
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
                context.collectElapsed += deltaTime;

                if (context.collectElapsed < CollectDuration)
                {
                    return;
                }

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
