using System;
using Sandbox;

public sealed class GameManager : Component
{
	// TODO: World Gen Screen
		// Mode (Minigame)
		
	// Settings
	// [Category("Settings")] [Property] public int Layers { get; set; } // Layers
	// [Category("Settings")] [Property] public int RoomsPerVertex { get; set; } // Rooms Per Vertex
	// [Category("Settings")] [Property] public int RoomChoice { get; set; } // Room (Wall Type) Choice
	// [Category("Settings")] [Property] public float PropDensity { get; set; } // Prop Density
	// [Category("Settings")] [Property] public float RenderDistance { get; set; } // Render Distance
	// [Category("Settings")] [Property] public bool CenterSpawn { get; set; } // Center vs Random Spawn
	// [Category("Settings")] [Property] public int RoomActionsPerFrame { get; set; } // Room Loading Speed

	[Property] public List<GameObject> RoomPrefabs { get; set; }
	[Property] public PlayerController Player { get; set; }
	[Property] public CameraComponent Camera { get; set; }
	[Property] public GradientFog Fog { get; set; }
	[Property] public NodeGraph NodeGraphs { get; set; }
	[Property] public GameObject CeilingObject { get; set; }
	[Property] public Ceiling CeilingComponent { get; set; }
	[Property] public GameObject PropPrefab { get; set; }

	private GameConfig GameConfig;
	private List<NodeGraph.RoomNode> Nodes;
	private List<List<int>> Directions; 

	private RoomObject Origin;
	private Vector3 CeilingHeight = new Vector3(0, 0, 175);

	private float RoomChangeDelay = 0.1f;
	private float TimeUntilRoomChange = 0f;

	private List<RoomObject> Rooms = new(); // All
	private List<RoomObject> ActiveRooms = new(); // Loaded and enabled
	private List<(RoomObject NewRoom, RoomObject Enabler)> NewRooms = new(); // Queued to activate
	private List<RoomObject> OldRooms = new(); // Queued to disable

	private int MaxDistance = 0; // Updated automatically
	public List<List<RoomObject>> ActiveRoomsByDistance = new();

	private List<RoomObject> RoomsVisited = new(); // For tracking unique rooms loaded

	public struct PropInfo
	{
		public Model Model;
		public float MinHeight;
		public float MaxHeight;
		public float MinRadius;
		public float MaxRadius;
		public float MinScale;
		public float MaxScale;

		public PropInfo(Model model, float min_height = 0, float max_height = 0, float min_radius = 0f, float max_radius = 0f, float min_scale = 1, float max_scale = 1)
		{
			Model = model;
			MinHeight = min_height;
			MaxHeight = max_height;
			MinRadius = min_radius;
			MaxRadius = max_radius;
			MinScale = min_scale;
			MaxScale = max_scale;
		}
	}
	
	private List<PropInfo> Props = new List<PropInfo>
	{
		// Model Ident, MinHeight, MaxHeight, MinRadius MaxRadius, MinScale, MaxScale
		//new PropInfo(Cloud.Model("Ident"), MinHeight, MaxHeight, MinRadius, MaxRadius, MinScale, MaxScale),

		new PropInfo(Cloud.Model("facepunch.toilet_a"), 0, 0, 0, 0, 2.5f, 3.5f),
		new PropInfo(Cloud.Model("facepunch.mountainbike"), 35, 50, 0, 0, 1.5f, 2.5f),
		new PropInfo(Cloud.Model("facepunch.car_dev"), 0, 0, 0, 0, 0.65f, 0.85f),
		new PropInfo(Cloud.Model("facepunch.pickup_dev"), 0, 0, 0, 0, 0.65f, 0.85f),
		new PropInfo(Cloud.Model("facepunch.van_dev"), 0, 0, 0, 0, 0.65f, 0.85f),
		new PropInfo(Cloud.Model("facepunch.office_chair"), 0, 0, 0, 0, 2.5f, 3f),
		new PropInfo(Cloud.Model("facepunch.dentist_chair"), 0, 0, 0, 0, 1.5f, 2.5f),
		new PropInfo(Cloud.Model("facepunch.watering_can_03"), 0, 0, 0, 0, 5.0f, 6.5f),
		new PropInfo(Cloud.Model("facepunch.trolley"), 0, 0, 0, 0, 2.0f, 2.5f),
		new PropInfo(Cloud.Model("facepunch.bathtub"), 0, 0, 0, 0, 1.0f, 1.5f),
		new PropInfo(Cloud.Model("facepunch.metalwheelbarrow"), 0, 0, 0, 0, 2.5f, 3.0f),
		new PropInfo(Cloud.Model("facepunch.generator_dev"), 0, 0, 0, 0, 0.9f, 1.1f),
		new PropInfo(Cloud.Model("facepunch.waste_bin_dev"), 0, 0, 0, 0, 1.0f, 1.0f),
		new PropInfo(Cloud.Model("facepunch.weapon_crate_stacka"), 0, 0, 0, 0, 1.0f, 1.25f),
		new PropInfo(Cloud.Model("facepunch.tyre_side_stack_dev"), 0, 0, 0, 0, 1.25f, 1.5f),
		new PropInfo(Cloud.Model("facepunch.forklift_down_dev"), 0, 0, 0, 0, 0.9f, 1.1f),
		new PropInfo(Cloud.Model("facepunch.forklift_up_dev"), 0, 0, 0, 0, 0.9f, 1.1f),
		new PropInfo(Cloud.Model("facepunch.truck_dev"), 0, 0, 0, 0, 0.5f, 0.6f),
		new PropInfo(Cloud.Model("facepunch.swat_dev"), 0, 0, 0, 0, 0.6f, 0.7f),
		new PropInfo(Cloud.Model("facepunch.step_ladder_02"), 0, 0, 0, 0, 2.0f, 2.5f),
		new PropInfo(Cloud.Model("facepunch.beech_shrub_tall_large"), 0, 0, 0, 0, 0.5f, 0.65f),
		new PropInfo(Cloud.Model("facepunch.star_bin"), 0, 0, 0, 0, 1.75f, 2.0f),
		new PropInfo(Cloud.Model("facepunch.wheely_bin"), 0, 0, 0, 0, 1.75f, 2.0f),
		new PropInfo(Cloud.Model("facepunch.binbag_a1"), 0, 0, 0, 0, 3.0f, 4.0f),
		new PropInfo(Cloud.Model("facepunch.binbag_pile_a2"), 0, 0, 0, 0, 2.5f, 3.0f),
		new PropInfo(Cloud.Model("facepunch.old_bench"), 0, 0, 0, 0, 1.5f, 1.75f),
		new PropInfo(Cloud.Model("facepunch.folding_construction_sign"), 0, 0, 0, 0, 2.5f, 3.0f),
		new PropInfo(Cloud.Model("fish.old_red_lada"), 0, 0, 0, 0, 0.9f, 1.1f),
		new PropInfo(Cloud.Model("fish.gt_house_a"), 0, 0, 0, 0, 0.2f, 0.25f),
		new PropInfo(Cloud.Model("fish.gt_house_b"), 0, 0, 0, 0, 0.3f, 0.35f),
		new PropInfo(Cloud.Model("fish.gt_house_c"), 0, 0, 0, 0, 0.25f, 0.25f),
		new PropInfo(Cloud.Model("fish.gt_house_d"), 0, 0, 0, 0, 0.2f, 0.25f),
		new PropInfo(Cloud.Model("fish.gt_training_camp"), -3, -3, 0, 0, 0.5f, 0.55f),
		new PropInfo(Cloud.Model("fish.specter"), 0, 0, 0, 0, 1.5f, 1.75f),
		new PropInfo(Cloud.Model("fish.fox"), 0, 0, 0, 0, 3.5f, 4.0f),
		new PropInfo(Cloud.Model("fish.elk"), 0, 0, 0, 0, 1.25f, 1.45f),
		new PropInfo(Cloud.Model("fish.reindeer"), 0, 0, 0, 0, 1.35f, 1.65f),
		new PropInfo(Cloud.Model("fish.moose"), 0, 0, 0, 0, 1.0f, 1.2f),
		new PropInfo(Cloud.Model("fish.doob"), 0, 0, 0, 0, 2.0f, 2.5f),
		new PropInfo(Cloud.Model("fish.gt_goblin"), 0, 0, 0, 0, 2.25f, 2.75f),
		new PropInfo(Cloud.Model("fish.msc_bed"), 0, 0, 0, 0, 1.25f, 1.5f),
		new PropInfo(Cloud.Model("fish.gt_goblin_spawner"), -3, -3, 0, 0, 0.6f, 0.65f),
		new PropInfo(Cloud.Model("luke.skeleman"), 0, 0, 0, 0, 1.5f, 1.75f),
		new PropInfo(Cloud.Model("gvar.citizen_zombie"), 0, 0, 0, 0, 1.5f, 1.75f),
		new PropInfo(Cloud.Model("azumangafans.shrek-ragdoll"), 0, 0, 0, 0, 1.25f, 1.5f),
		new PropInfo(Cloud.Model("mong.capybara"), 0, 0, 0, 0, 1.75f, 2.25f),
		new PropInfo(Cloud.Model("azumangafans.garfieldmovie"), 0, 0, 0, 0, 1.5f, 2.0f),
		new PropInfo(Cloud.Model("marauders.feathersmcgraw"), 0, 0, 0, 0, 1.75f, 2.25f),
		new PropInfo(Cloud.Model("neptunux.littlekitty1"), 0, 0, 0, 0, 1.5f, 2.0f),
		new PropInfo(Cloud.Model("apetavern.gatorkid"), 0, 0, 0, 0, 1.75f, 2.0f),
		new PropInfo(Cloud.Model("apetavern.gatorman"), 0, 0, 0, 0, 0.75f, 1.0f),
	};

	private Random Random = new Random();

	//protected override void OnFixedUpdate()
	protected override void OnUpdate()
	{
		// Check to switch scenes
		if ( RoomsVisited.Count > 3 )
		{
			Scene.LoadFromFile("scenes/setup.scene");
			return;
		}

		// Check for a room change
		TimeUntilRoomChange -= (float)Time.Delta;
		if ( TimeUntilRoomChange <= 0 )
		{
			RoomObject PlayerRoom = Origin.CheckForPlayer(Player.WorldPosition);
			if ( PlayerRoom != Origin )
			{
				TimeUntilRoomChange = RoomChangeDelay;
				EnterRoom(PlayerRoom);
			}
		}

		// Room enable queue (FIFO)
		int actions_remaining = GameConfig.RoomActionsPerFrame;
		while (actions_remaining > 0 && NewRooms.Count > 0)
		{
			actions_remaining--;

			var new_room_result = NewRooms[0];
			NewRooms.RemoveAt(0);
			EnableRoom(new_room_result);
		}

		// Room disable queue (FIFO)
		while (actions_remaining > 0 && OldRooms.Count > 0)
		{
			if ( OldRooms.Count == 0 ) break;
			
			RoomObject old_room = OldRooms[0];
			OldRooms.RemoveAt(0);
			DisableRoom(old_room);
		}
	}

	// For debug only
	private void PrintList(List<int> list)
	{
		string to_print = "List: ";
		foreach (int i in list)
		{
			to_print += i.ToString() + " ";
		}
		Log.Info(to_print);
	}
	
	private void RemoveFromActive(RoomObject room)
	{
		if ( ActiveRooms.Remove(room) )
		{
			foreach (List<RoomObject> distance_list in ActiveRoomsByDistance)
			{
				distance_list.Remove(room);
			}
		}
	}

	private void EnableRoom((RoomObject NewRoom, RoomObject Enabler) room_result)
	{
		RoomObject room = room_result.NewRoom;
		RoomObject enabler = room_result.Enabler;

		OldRooms.Remove(room);
		RemoveFromActive(room);

		room.EnableSelf(enabler);
		ActiveRooms.Add(room);
		ActiveRoomsByDistance[room.Distance].Add(room);
	}

	private void DisableRoom(RoomObject room)
	{
		room.DisableSelf();
		RemoveFromActive(room);
	}

	// Adds a room to the loading queue
	private void AddToQueue((RoomObject NewRoom, RoomObject Enabler) new_room_result)
	{
		RoomObject new_room = new_room_result.NewRoom;
		if ( new_room == null ) return;

		NewRooms.Add(new_room_result);
		OldRooms.Remove(new_room); // No need to remove
	}

	// Resets the current queue, if there is one
	private void ResetQueue()
	{
		// Remove all rooms from the current loading queue
		NewRooms = new();

		// Remove all rooms from the current removal queue
		OldRooms = new();
	}

	// Called whenever the player first enters the orom
	private void EnterRoom(RoomObject room)
	{
		// Interrupt the current queue
		ResetQueue();

		// Mark old rooms for disabling
		OldRooms = ActiveRooms.ToList();

		// Enable new origin
		Origin.IsOrigin = false;
		Origin = room;
		Origin.IsOrigin = true;
		Origin.Distance = 0;
		AddToQueue((Origin, Origin));
		CeilingObject.WorldPosition = Origin.WorldPosition + CeilingHeight;

		// Add to rooms visited if new
		if ( RoomsVisited.IndexOf(Origin) == -1 )
		{
			RoomsVisited.Add(Origin);
			//Log.Info("Unique Rooms Visited: " + RoomsVisited.Count.ToString());
		}
		
		// Enable new neighbors
		for (int i = 0; i < 4; i++) // Rotations
		{
			foreach (List<int> _directions in Directions) // Directions
			{
				List<int> directions = _directions.ToList();

				// Initial rotations
				directions.Insert(0, i);
				List<int> reflected_directions = NodeGraphs.ReflectDirections(directions);

				// Enable neighbor at end of path
				AddToQueue(Origin.QueueNeighbor(null, directions, 0));

				// Enable neighbor at end of reflected path
				if ( reflected_directions is null ) continue;
				AddToQueue(Origin.QueueNeighbor(null, reflected_directions, 0));
			}
		}

		//Log.Info(OldRooms.Count.ToString() + " - " + NewRooms.Count.ToString());
	}

	private PropObject GenerateProp()
	{
		if ( Random.NextDouble() < GameConfig.PropDensity )
		{
			PropInfo prop_info = Props[Random.Next(Props.Count)];

			GameObject prop_object = PropPrefab.Clone();
			prop_object.BreakFromPrefab();

			PropObject prop = prop_object.GetComponent<PropObject>();
			prop.SetInfo(prop_info);

			return prop;
		}
		else
		{
			return null;
		}
	}

	protected override void OnStart()
	{
		List<GameConfig> game_config_list = Scene.GetAllComponents<GameConfig>().ToList();
		if (game_config_list.Count == 0)
		{
			Log.Error("No GameConfig found!");
			return;
		}
		GameConfig = game_config_list[0];

		Nodes = NodeGraphs.GenerateNodeGraph(GameConfig.Layers, GameConfig.RoomsPerVertex, GameConfig.RemoveDeadEnds);

		//Directions = NodeGraphs.Directions["SquareRoomThin"];
		Directions = NodeGraphs.GenerateDirections(GameConfig.RenderDistance, RoomPrefabs[GameConfig.RoomChoice].Name);
		//Log.Info("Length: " + Directions.Count);
		// foreach (List<int> dir_list in Directions)
		// {
		// 	PrintList(dir_list);
		// }

		// Set up ceiling
		float scale = (GameConfig.RenderDistance * 2 + 2) * 5;
		CeilingObject.LocalScale = new Vector3(scale, scale, 1);

		// Create all necessary lights
		List<(int x, int y)> light_locations = new List<(int x, int y)>();
		for (int i = 0; i < 4; i++) // Rotations
		{
			foreach (List<int> _directions in Directions) // Directions
			{
				List<int> directions = _directions.ToList();

				// Initial rotations
				directions.Insert(0, i);
				List<int> reflected_directions = NodeGraphs.ReflectDirections(directions);

				// Enable neighbor at end of path
				(int x, int y) location = NodeGraphs.DirectionsToWorld(directions);
				if ( light_locations.IndexOf(location) == -1 )
				{
					light_locations.Add(location);
				}

				// Enable neighbor at end of reflected path
				if ( reflected_directions is null ) continue;
				(int x, int y) reflected_location = NodeGraphs.DirectionsToWorld(reflected_directions);
				if ( light_locations.IndexOf(reflected_location) == -1 )
				{
					light_locations.Add(reflected_location);
				}
			}
		}
		CeilingComponent.AddLights(light_locations);

		// Length of longest path
		foreach (List<int> directions in Directions)
		{
			int count = directions.Count;
			if ( count > MaxDistance )
				MaxDistance = count;
		}

		// Set up sorted active rooms
		for (int i = 0; i <= MaxDistance + 1; i++)
			ActiveRoomsByDistance.Add(new List<RoomObject>());

		// Instantiate each room using the nodegraph
		foreach (NodeGraph.RoomNode node in Nodes)
		{
			// Create room from prefab
			GameObject room_object = RoomPrefabs[GameConfig.RoomChoice].Clone();
			room_object.BreakFromPrefab();
			
			// Set up room
			RoomObject room = room_object.GetComponent<RoomObject>();
			room.DisableSelf();
			Rooms.Add(room);
			node.Room = room;
		}

		// Assign each room its neighbors
		foreach (NodeGraph.RoomNode node in Nodes)
		{
			RoomObject room = node.Room;

			for (int i = 0; i < 4; i++)
			{
				NodeGraph.RoomNode neighbor_node = node.Neighbors[i];
				if ( neighbor_node != null )
				{
					room.Neighbors[i] = neighbor_node.Room;
				}
			}
		}

		// Choose the origin
		if (GameConfig.CenterSpawn)
		{
			Origin = Rooms[0];
		}
		else
		{
			Origin = Rooms[Random.Next(Rooms.Count)];
		}
		Origin.IsOrigin = true;

		// Finish room setup
		foreach (RoomObject room in Rooms)
		{
			room.FinishSetup(GenerateProp());
		}

		// Render distance
		Fog.StartDistance = (GameConfig.RenderDistance - 1.0f) * 250;
		Fog.EndDistance = (GameConfig.RenderDistance - 0.5f) * 250;
		Camera.ZFar = (int)(GameConfig.RenderDistance + 4) * 250;

		EnterRoom(Origin);
	}
}
