namespace GdUnit3
{

    public interface IExecutor
    {
        // this method is called form gdScript and can't handle 'Task'
        // we used explicit 'async void' to avoid  'Attempted to convert an unmarshallable managed type to Variant Task'
        public void Execute(Godot.Node node);


        public IExecutor AddGdTestEventListener(Godot.Object listener);

    }
}