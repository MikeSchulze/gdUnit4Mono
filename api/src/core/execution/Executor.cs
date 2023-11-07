using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace GdUnit4.Executions
{
    public sealed partial class Executor : Godot.RefCounted, IExecutor
    {
        [Godot.Signal]
        public delegate void ExecutionCompletedEventHandler();

        private List<ITestEventListener> _eventListeners = new List<ITestEventListener>();

        private class GdTestEventListenerDelegator : ITestEventListener
        {
            private readonly Godot.GodotObject _listener;

            public GdTestEventListenerDelegator(Godot.GodotObject listener)
            {
                _listener = listener;
            }
            public void PublishEvent(TestEvent testEvent) => _listener.Call("PublishEvent", testEvent);
        }

        public IExecutor AddGdTestEventListener(Godot.GodotObject listener)
        {
            // I want to using anonymus implementation to remove the extra delegator class
            _eventListeners.Add(new GdTestEventListenerDelegator(listener));
            return this;
        }

        public void AddTestEventListener(ITestEventListener listener)
        {
            _eventListeners.Add(listener);
        }

        public bool ReportOrphanNodesEnabled { get; set; } = true;

        public bool IsExecutable(Godot.Node node) => node is CsNode;


        /// <summary>
        /// Execute a testsuite, is called externally from Godot test suite runner
        /// </summary>
        /// <param name="testSuite"></param>
        public void Execute(CsNode testSuite)
        {
            try
            {
                var includedTests = testSuite.GetChildren()
                    .Cast<CsNode>()
                    .ToList()
                    .Select(node => node.Name.ToString())
                    .ToList();
                var task = ExecuteInternally(new TestSuite(testSuite.ResourcePath(), includedTests));
                task.GetAwaiter().OnCompleted(() => EmitSignal(SignalName.ExecutionCompleted));
            }
            catch (Exception e)
            {
                Godot.GD.PushError(e.Message);
            }
            finally
            {
                testSuite.Free();
            }
        }

        internal async Task ExecuteInternally(TestSuite testSuite)
        {
            try
            {
                if (!ReportOrphanNodesEnabled)
                    Godot.GD.PushWarning("!!! Reporting orphan nodes is disabled. Please check GdUnit settings.");
                await ISceneRunner.SyncProcessFrame;
                using ExecutionContext context = new(testSuite, _eventListeners, ReportOrphanNodesEnabled);
                await new TestSuiteExecutionStage(testSuite).Execute(context);
            }
            catch (Exception e)
            {
                // unexpected exceptions
                Godot.GD.PushError(e.Message);
                Godot.GD.PushError(e.StackTrace);
            }
            finally
            {
                testSuite.Dispose();
            }
        }
    }
}
