namespace GdUnit4.Tests.Asserts;
// GdUnit generated TestSuite

using System.Threading.Tasks;

using GdUnit4.Asserts;

using static Assertions;


[TestSuite]
public partial class SignalAssertTest
{
    private sealed partial class TestEmitter : Godot.Node
    {
        [Godot.Signal]
        public delegate void SignalAEventHandler();

        [Godot.Signal]
        public delegate void SignalBEventHandler(string value);

        [Godot.Signal]
        public delegate void SignalCEventHandler(string value, int count);

        private int frame;

        public override void _Process(double delta)
        {
            switch (frame)
            {
                case 5:
                    EmitSignal(SignalName.SignalA);
                    break;
                case 10:
                    EmitSignal(SignalName.SignalB, "abc");
                    break;
                case 15:
                    EmitSignal(SignalName.SignalC, "abc", 100);
                    break;
                default:
                    break;
            }
            frame++;
        }
    }

    [TestCase]
    public async Task IsEmitted()
    {
        var node = AutoFree(new TestEmitter())!;
        await AssertSignal(node).IsEmitted("SignalA").WithTimeout(200);
        await AssertSignal(node).IsEmitted("SignalB", "abc").WithTimeout(200);
        await AssertSignal(node).IsEmitted("SignalC", "abc", 100).WithTimeout(200);

        await AssertThrown(AssertSignal(node).IsEmitted("SignalC", "abc", 101).WithTimeout(200))
            .ContinueWith(result => result.Result?
                .IsInstanceOf<Exceptions.TestFailedException>()
                .HasMessage("""
                    Expecting do emitting signal:
                        "SignalC(["abc", 101])"
                     by
                        $obj
                    """
                        .Replace("$obj", AssertFailures.AsObjectId(node))));
    }

    [TestCase]
    public async Task IsNoEmitted()
    {
        var node = AddNode(new Godot.Node2D());
        await AssertSignal(node).IsNotEmitted("visibility_changed", 10).WithTimeout(100);
        await AssertSignal(node).IsNotEmitted("visibility_changed", 20).WithTimeout(100);
        await AssertSignal(node).IsNotEmitted("script_changed").WithTimeout(100);

        node.Visible = false;
        await AssertThrown(AssertSignal(node).IsNotEmitted("visibility_changed").WithTimeout(200))
            .ContinueWith(result => result.Result?
                .IsInstanceOf<Exceptions.TestFailedException>()
                .HasMessage("""
                    Expecting do NOT emitting signal:
                        "visibility_changed(<Empty>)"
                     by
                        $obj
                    """
                        .Replace("$obj", AssertFailures.AsObjectId(node))));
    }

    [TestCase]
    public async Task NodeChangedEmittingSignals()
    {
        var node = AddNode(new Godot.Node2D());

        await AssertSignal(node).IsEmitted("draw").WithTimeout(200);

        node.Visible = false;
        await AssertSignal(node).IsEmitted("visibility_changed").WithTimeout(200);

        // expecting to fail, we not changed the visibility
        //node.Visible = false;
        await AssertThrown(AssertSignal(node).IsEmitted("visibility_changed").WithTimeout(200))
           .ContinueWith(result => result.Result?
               .IsInstanceOf<Exceptions.TestFailedException>()
               .HasMessage("""
                    Expecting do emitting signal:
                        "visibility_changed(<Empty>)"
                     by
                        $obj
                    """
                        .Replace("$obj", AssertFailures.AsObjectId(node))));

        node.Show();
        await AssertSignal(node).IsEmitted("draw").WithTimeout(200);
    }

    [TestCase]
    public void IsSignalExists()
    {
        var node = AutoFree(new Godot.Node2D())!;

        AssertSignal(node).IsSignalExists("visibility_changed")
            .IsSignalExists("draw")
            .IsSignalExists("visibility_changed")
            .IsSignalExists("tree_entered")
            .IsSignalExists("tree_exiting")
            .IsSignalExists("tree_exited");

        AssertThrown(() => AssertSignal(node).IsSignalExists("not_existing_signal"))
            .IsInstanceOf<Exceptions.TestFailedException>()
            .HasMessage("""
                Expecting signal exists:
                    "not_existing_signal()"
                 on
                    $obj
                """
                    .Replace("$obj", AssertFailures.AsObjectId(node)));
    }
}
