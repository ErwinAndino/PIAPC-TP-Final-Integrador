using System;
using System.Collections.Generic;

namespace DefendAndCollect.FSM
{
    public sealed class StateMachine<TState, TContext>
        where TState : struct, Enum
    {
        private readonly Dictionary<TState, IFsmState<TState, TContext>> states =
            new Dictionary<TState, IFsmState<TState, TContext>>();

        private readonly TContext context;

        public StateMachine(TContext context)
        {
            this.context = context;
        }

        public TState? CurrentState { get; private set; }

        public bool HasCurrentState => CurrentState.HasValue;

        public event Action<TState?, TState> StateChanged;

        public void Register(TState state, IFsmState<TState, TContext> implementation)
        {
            if (implementation == null)
            {
                throw new ArgumentNullException(nameof(implementation));
            }

            if (states.ContainsKey(state))
            {
                throw new ArgumentException($"State '{state}' is already registered.", nameof(state));
            }

            states.Add(state, implementation);
        }

        public void ChangeState(TState nextState)
        {
            if (!states.TryGetValue(nextState, out IFsmState<TState, TContext> next))
            {
                throw new ArgumentOutOfRangeException(nameof(nextState), nextState, "State is not registered.");
            }

            if (CurrentState.HasValue && EqualityComparer<TState>.Default.Equals(CurrentState.Value, nextState))
            {
                return;
            }

            TState? previousState = CurrentState;

            if (CurrentState.HasValue && states.TryGetValue(CurrentState.Value, out IFsmState<TState, TContext> current))
            {
                current.Exit(context);
            }

            CurrentState = nextState;
            next.Enter(context);
            StateChanged?.Invoke(previousState, nextState);
        }

        public void Tick(float deltaTime)
        {
            if (!CurrentState.HasValue)
            {
                return;
            }

            if (states.TryGetValue(CurrentState.Value, out IFsmState<TState, TContext> current))
            {
                current.Tick(context, deltaTime);
            }
        }
    }
}
