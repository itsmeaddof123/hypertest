using Sandbox;
using System;

public sealed class GameConfig : Component
{
	// How many branch layers to generate
	private int _layers;
	[Property] public int Layers
	{
		get { return _layers; }
		set
		{
			_layers = Math.Max(value, _rooms_per_vertex / 2);
			if (Layers != _layers)
			{
				Layers = _layers;
			}
		}
	}

	// How many rooms should surround a single vertex
	private int _rooms_per_vertex;
	[Property] public int RoomsPerVertex
	{
		get { return _rooms_per_vertex; }
		set
		{
			_rooms_per_vertex = Math.Max(value, 5);
			if (RoomsPerVertex != _rooms_per_vertex)
			{
				RoomsPerVertex = _rooms_per_vertex;
			}

			Layers = Math.Max(Layers, _rooms_per_vertex / 2);
		}
	}

	// Which room wall type should be used
	private int _room_choice;
	[Property] public int RoomChoice
	{
		get { return _room_choice; }
		set
		{
			_room_choice = Math.Clamp(value, 0, 2);
			if (RoomChoice != _room_choice)
			{
				RoomChoice = _room_choice;
			}
		}
	}

	// What proportion of rooms should have props
	private float _prop_density;
	[Property] public float PropDensity
	{
		get { return _prop_density; }
		set
		{
			_prop_density = Math.Clamp(value, 0, 1);
			if (PropDensity != _prop_density)
			{
				PropDensity = _prop_density;
			}
		}
	}

	// How many rooms away should be rendered
	private float _render_distance;
	[Property] public float RenderDistance
	{
		get { return _render_distance; }
		set
		{
			_render_distance = Math.Max(value, 2);
			if (RenderDistance != _render_distance)
			{
				RenderDistance = _render_distance;
			}
		}
	}

	[Property] public int RoomActionsPerFrame { get; set; } // Room Loading Speed
	
	[Property] public bool CenterSpawn { get; set; } // Center vs Random Spawn
	[Property] public bool RemoveDeadEnds { get; set; } // Remove dead ends during node generation

	protected override void OnUpdate()
	{

	}
}
