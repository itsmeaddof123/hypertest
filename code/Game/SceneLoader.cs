using Sandbox;

public sealed class SceneLoader : Component
{
	[Property] public GameObject GameConfigObject { get; set; }

	protected override void OnStart()
	{
		GameConfigObject.Flags = GameObjectFlags.DontDestroyOnLoad;
	}
}
