using System;

namespace DefendAndCollect.FSM
{
    public interface IFsmState<TState, TContext>
        where TState : struct, Enum
    {
        void Enter(TContext context);

        void Exit(TContext context);

        void Tick(TContext context, float deltaTime);
    }
}
