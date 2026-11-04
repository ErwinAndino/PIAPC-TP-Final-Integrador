using System;
using System.Collections.Generic;
using DefendAndCollect.FSM;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using UnityEngine;

namespace DefendAndCollect.Agents
{
    public enum EnemyState
    {
        Idle = 0,
        SeekAlly = 1,
        Chase = 2,
        Attack = 3
    }

    public sealed class EnemyAgent : NpcAgent
    {
        private const float IdleDuration = 0.4f;
        private const float SeekDuration = 0.25f;
        private const float AttackDuration = 1f;
        private const float RepathInterval = 0.2f;
        private const float PerceptionInterval = 0.25f;
        private const int AttackRange = 1;

        public const float DamagePerAttack = 20f;

        private readonly List<AllyAgent> allies = new List<AllyAgent>();

        private StateMachine<EnemyState, EnemyAgent> stateMachine;
        private AllyAgent target;
        private Vector2Int? objectiveCell;
        private float stateElapsed;
        private float repathElapsed;
        private float perceptionElapsed;

        public EnemyState CurrentState => stateMachine != null && stateMachine.CurrentState.HasValue
            ? stateMachine.CurrentState.Value
            : EnemyState.Idle;

        public override string CurrentStateName => CurrentState.ToString();

        public AllyAgent Target => target;

        public IReadOnlyList<AllyAgent> Allies => allies;

        public Vector2Int? ObjectiveCell => objectiveCell;

        public int AttackCount { get; private set; }

        public void Configure(
            GameGrid grid,
            Vector2Int startCell,
            IReadOnlyList<AllyAgent> allyTargets,
            float speed = 3f,
            PathfindingAlgorithm algorithm = PathfindingAlgorithm.AStar,
            NavigationMode mode = NavigationMode.UniformCost,
            Color? color = null,
            float detectionRadius = 6f,
            Vector2Int? objective = null)
        {
            objectiveCell = objective;
            allies.Clear();

            if (allyTargets != null)
            {
                for (int i = 0; i < allyTargets.Count; i++)
                {
                    if (allyTargets[i] != null)
                    {
                        allies.Add(allyTargets[i]);
                    }
                }
            }

            base.Initialize(
                grid,
                startCell,
                speed,
                algorithm,
                mode,
                color ?? new Color(1f, 0.3f, 0.3f, 1f),
                detectionRadius);
        }

        protected override void OnConfigured()
        {
            BuildStateMachine();
            stateMachine.ChangeState(EnemyState.Idle);
        }

        protected override void TickBehavior(float deltaTime)
        {
            stateMachine?.Tick(deltaTime);
        }

        private bool HasValidTarget => target != null && target.IsTargetable;

        private bool IsTargetInRange
        {
            get
            {
                if (!HasValidTarget)
                {
                    return false;
                }

                return Math.Abs(Cell.x - target.Cell.x) + Math.Abs(Cell.y - target.Cell.y) <= AttackRange;
            }
        }

        private void BuildStateMachine()
        {
            stateMachine = new StateMachine<EnemyState, EnemyAgent>(this);
            stateMachine.Register(EnemyState.Idle, new IdleState());
            stateMachine.Register(EnemyState.SeekAlly, new SeekAllyState());
            stateMachine.Register(EnemyState.Chase, new ChaseState());
            stateMachine.Register(EnemyState.Attack, new AttackState());
            stateMachine.StateChanged += (previous, current) => NotifyStateChanged();
        }

        private void TransitionTo(EnemyState nextState)
        {
            stateMachine?.ChangeState(nextState);
        }

        private bool TryAcquireTarget()
        {
            target = Perception.FindNearest(
                Cell,
                allies,
                ally => ally.Cell,
                DetectionRadius,
                ally => ally.IsTargetable);

            return target != null;
        }

        private void ChaseTarget()
        {
            if (!HasValidTarget)
            {
                return;
            }

            MoveTo(target.Cell);
        }

        private bool TryAdvanceToObjective()
        {
            if (!objectiveCell.HasValue)
            {
                return false;
            }

            Vector2Int objective = objectiveCell.Value;

            if (Cell == objective)
            {
                return true;
            }

            if (IsMoving && DestinationCell == objective)
            {
                return true;
            }

            return MoveTo(objective);
        }

        private void AttackTarget()
        {
            if (!HasValidTarget)
            {
                return;
            }

            target.TakeDamage(DamagePerAttack);
        }

        private sealed class IdleState : IFsmState<EnemyState, EnemyAgent>
        {
            private float elapsed;

            public void Enter(EnemyAgent context)
            {
                elapsed = 0f;
                context.perceptionElapsed = 0f;
                context.Stop();
            }

            public void Exit(EnemyAgent context)
            {
            }

            public void Tick(EnemyAgent context, float deltaTime)
            {
                elapsed += deltaTime;
                context.perceptionElapsed += deltaTime;

                if (context.perceptionElapsed >= PerceptionInterval)
                {
                    context.perceptionElapsed = 0f;

                    if (context.TryAcquireTarget())
                    {
                        context.TransitionTo(EnemyState.SeekAlly);
                        return;
                    }
                }

                if (elapsed >= IdleDuration)
                {
                    context.TransitionTo(EnemyState.SeekAlly);
                }
            }
        }

        private sealed class SeekAllyState : IFsmState<EnemyState, EnemyAgent>
        {
            public void Enter(EnemyAgent context)
            {
                context.stateElapsed = 0f;
                context.perceptionElapsed = 0f;
                context.TryAcquireTarget();
            }

            public void Exit(EnemyAgent context)
            {
            }

            public void Tick(EnemyAgent context, float deltaTime)
            {
                context.stateElapsed += deltaTime;
                context.perceptionElapsed += deltaTime;

                if (context.perceptionElapsed >= PerceptionInterval)
                {
                    context.perceptionElapsed = 0f;

                    if (!context.HasValidTarget)
                    {
                        context.TryAcquireTarget();
                    }
                }

                if (!context.HasValidTarget)
                {
                    if (context.TryAdvanceToObjective())
                    {
                        return;
                    }

                    if (context.stateElapsed >= SeekDuration)
                    {
                        context.TransitionTo(EnemyState.Idle);
                    }

                    return;
                }

                if (context.IsTargetInRange)
                {
                    context.TransitionTo(EnemyState.Attack);
                    return;
                }

                context.TransitionTo(EnemyState.Chase);
            }
        }

        private sealed class ChaseState : IFsmState<EnemyState, EnemyAgent>
        {
            public void Enter(EnemyAgent context)
            {
                context.repathElapsed = 0f;
                context.ChaseTarget();
            }

            public void Exit(EnemyAgent context)
            {
            }

            public void Tick(EnemyAgent context, float deltaTime)
            {
                if (!context.HasValidTarget)
                {
                    context.TransitionTo(EnemyState.SeekAlly);
                    return;
                }

                if (context.IsTargetInRange)
                {
                    context.TransitionTo(EnemyState.Attack);
                    return;
                }

                context.repathElapsed += deltaTime;

                if (context.repathElapsed >= RepathInterval)
                {
                    context.repathElapsed = 0f;
                    context.ChaseTarget();
                }
            }
        }

        private sealed class AttackState : IFsmState<EnemyState, EnemyAgent>
        {
            public void Enter(EnemyAgent context)
            {
                context.Stop();
                context.stateElapsed = 0f;
            }

            public void Exit(EnemyAgent context)
            {
            }

            public void Tick(EnemyAgent context, float deltaTime)
            {
                if (!context.HasValidTarget)
                {
                    context.TransitionTo(EnemyState.SeekAlly);
                    return;
                }

                if (!context.IsTargetInRange)
                {
                    context.TransitionTo(EnemyState.Chase);
                    return;
                }

                context.stateElapsed += deltaTime;

                if (context.stateElapsed < AttackDuration)
                {
                    return;
                }

                context.AttackCount++;
                context.AttackTarget();
                context.TransitionTo(EnemyState.SeekAlly);
            }
        }
    }
}
