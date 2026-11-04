using System;
using System.Collections.Generic;
using DefendAndCollect.FSM;
using NUnit.Framework;

namespace DefendAndCollect.Tests.EditMode
{
    public sealed class StateMachineTests
    {
        private enum TestState
        {
            A = 0,
            B = 1,
            C = 2
        }

        private sealed class TestContext
        {
            public readonly List<string> Log = new List<string>();
        }

        private sealed class TestStateImplementation : IFsmState<TestState, TestContext>
        {
            private readonly string name;

            public TestStateImplementation(string name)
            {
                this.name = name;
            }

            public void Enter(TestContext context)
            {
                context.Log.Add($"enter:{name}");
            }

            public void Exit(TestContext context)
            {
                context.Log.Add($"exit:{name}");
            }

            public void Tick(TestContext context, float deltaTime)
            {
                context.Log.Add($"tick:{name}");
            }
        }

        [Test]
        public void ChangeState_EntersAndExitsStates_AndRaisesEvents()
        {
            TestContext context = new TestContext();
            StateMachine<TestState, TestContext> machine = new StateMachine<TestState, TestContext>(context);
            machine.Register(TestState.A, new TestStateImplementation("A"));
            machine.Register(TestState.B, new TestStateImplementation("B"));

            List<string> transitions = new List<string>();
            machine.StateChanged += (previous, current) => transitions.Add($"{previous?.ToString() ?? "none"}->{current}");

            machine.ChangeState(TestState.A);
            machine.Tick(0.25f);
            machine.ChangeState(TestState.B);
            machine.ChangeState(TestState.B);

            Assert.That(machine.CurrentState, Is.EqualTo(TestState.B));
            CollectionAssert.AreEqual(
                new[] { "enter:A", "tick:A", "exit:A", "enter:B" },
                context.Log);
            CollectionAssert.AreEqual(
                new[] { "none->A", "A->B" },
                transitions);
        }

        [Test]
        public void Tick_WithoutCurrentState_DoesNothing()
        {
            TestContext context = new TestContext();
            StateMachine<TestState, TestContext> machine = new StateMachine<TestState, TestContext>(context);
            machine.Register(TestState.A, new TestStateImplementation("A"));

            machine.Tick(1f);

            Assert.That(machine.HasCurrentState, Is.False);
            Assert.That(context.Log, Is.Empty);
        }

        [Test]
        public void ChangeState_UnregisteredState_Throws()
        {
            TestContext context = new TestContext();
            StateMachine<TestState, TestContext> machine = new StateMachine<TestState, TestContext>(context);
            machine.Register(TestState.A, new TestStateImplementation("A"));

            Assert.Throws<ArgumentOutOfRangeException>(() => machine.ChangeState(TestState.C));
        }

        [Test]
        public void Register_DuplicateState_Throws()
        {
            TestContext context = new TestContext();
            StateMachine<TestState, TestContext> machine = new StateMachine<TestState, TestContext>(context);
            machine.Register(TestState.A, new TestStateImplementation("A"));

            Assert.Throws<ArgumentException>(() => machine.Register(TestState.A, new TestStateImplementation("A2")));
        }
    }
}
