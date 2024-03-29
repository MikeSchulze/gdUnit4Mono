namespace GdUnit4;

using System;
using System.Threading.Tasks;

using Godot;

using static Assertions;

public static class GdUnitAwaiter
{
    public sealed class GodotMethodAwaiter<[MustBeVariant] TVariant> where TVariant : notnull
    {
        private string MethodName { get; }
        private Node Instance { get; }
        private Variant[] Args { get; }

        public GodotMethodAwaiter(Node instance, string methodName, params Variant[] args)
        {
            Instance = instance;
            MethodName = methodName;
            Args = args;
            if (!Instance.HasMethod(MethodName) && Instance.GetType().GetMethod(methodName) == null)
                throw new MissingMethodException($"The method '{MethodName}' not exist on loaded scene.");
        }

        public async Task IsEqual(TVariant expected) =>
            await CallAndWaitIsFinished((current) => AssertThat(current).IsEqual(expected));

        public async Task IsNull() =>
            await CallAndWaitIsFinished((current) => AssertThat(current).IsNull());

        public async Task IsNotNull() =>
            await CallAndWaitIsFinished((current) => AssertThat(current).IsNotNull());

        private delegate void Predicate(object? current);

        private async Task CallAndWaitIsFinished(Predicate comparator) => await Task.Run(async () =>
        {
            // sync to main thread
            await ISceneRunner.SyncProcessFrame;
            var value = await GodotObjectExtensions.Invoke(Instance, MethodName, Args);
            comparator(value);
        });
    }

    public static async Task AwaitSignal(this Node node, string signal, params Variant[] expectedArgs)
    {
        while (true)
        {
            var signalArgs = await Engine.GetMainLoop().ToSignal(node, signal);
            if (expectedArgs?.Length == 0 || signalArgs.VariantEquals(expectedArgs))
                return;
        }
    }
}
