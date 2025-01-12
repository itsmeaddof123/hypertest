using Sandbox;

public sealed class Ceiling : Component
{
	[Property] public GameObject Light { get; set; }
	[Property] public GameObject Player { get; set; }
	[Property] public int Radius { get; set; }

	private Vector3 x = new Vector3(250, 0, 0);
	private Vector3 y = new Vector3(0, 250, 0);

	// Add lights at each given location
	public void AddLights(List<(int x, int y)> light_locations)
	{
		foreach ((int x, int y) location in light_locations)
		{
			GameObject light = Light.Clone();
			light.Parent = GameObject;
			light.WorldPosition = Light.WorldPosition + location.x * x + location.y * y;
		}
	}
}
