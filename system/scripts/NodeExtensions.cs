using Godot;

public static class NodeExtensions
{
	public static void RemoveAndQueueFreeChildren(this Node node)
	{
		foreach (Node child in node.GetChildren())
			child.QueueFree();
	}
}
