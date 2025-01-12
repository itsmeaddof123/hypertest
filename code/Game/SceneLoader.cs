using Sandbox;

public sealed class SceneLoader : Component
{
	[Property] public GameObject Obj { get; set; }

	protected override void OnUpdate()
	{

	}

	protected override void OnStart()
	{
		// GameConfig game_config = GameConfig();
		// game_config.Print(); 

		Obj.Flags = GameObjectFlags.DontDestroyOnLoad;
		Log.Info(Obj.Flags);

		Log.Info("Loaded");
		Scene.LoadFromFile("scenes/main.scene");
	}
}
