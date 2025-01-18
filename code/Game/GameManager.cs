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
	[Property] public PaintHUD PaintHUD { get; set; }

	private GameConfig GameConfig;
	private List<NodeGraph.RoomNode> Nodes;
	private List<List<int>> Directions; 
	private Color PaintColor;

	private bool DoPaint = false;
	private bool DoMaze = false;

	private RoomObject Origin;
	private Vector3 CeilingHeight = new Vector3(0, 0, 175);

	private float RoomChangeDelay = 0.1f;
	private float TimeUntilRoomChange = 0f;
	private float TimePlayed = 0;

	private List<RoomObject> Rooms = new(); // All
	private List<RoomObject> ActiveRooms = new(); // Loaded and enabled
	private List<(RoomObject NewRoom, RoomObject Enabler)> NewRooms = new(); // Queued to activate
	private List<RoomObject> OldRooms = new(); // Queued to disable

	private int MaxDistance = 0; // Updated automatically
	public List<List<RoomObject>> ActiveRoomsByDistance = new();

	private List<RoomObject> RoomsVisited = new(); // For tracking unique rooms loaded

	private Random Random = new Random();

	//protected override void OnFixedUpdate()
	protected override void OnUpdate()
	{
		if ( Input.EscapePressed  )
		{
			Input.EscapePressed = false;
			Scene.LoadFromFile("scenes/setup.scene");
			return;
		}

		// Check for a room change
		float delta = (float)Time.Delta;
		TimeUntilRoomChange -= delta;
		if ( TimeUntilRoomChange <= 0 )
		{
			RoomObject PlayerRoom = Origin.CheckForPlayer(Player.WorldPosition);
			if ( PlayerRoom != Origin )
			{
				TimeUntilRoomChange = RoomChangeDelay;
				EnterRoom(PlayerRoom);
			}
		}

		// Game timer
		TimePlayed += delta;

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

			// Paint Game Mode
			if (DoPaint)
			{
				PaintHUD.RoomFound();

				foreach (ModelRenderer object_renderer in Origin.ObjectRenderers)
				{
					object_renderer.Tint = PaintColor;
				}

				// Winner
				if (RoomsVisited.Count == Rooms.Count)
				{
					CeilingComponent.SetColor(PaintColor);

				}
			}
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
			GameConfig.PropInfo prop_info = GameConfig.ReadyProps[Random.Next(GameConfig.ReadyProps.Count)];

			GameObject prop_object = PropPrefab.Clone();
			prop_object.BreakFromPrefab();

			PropObject prop = prop_object.GetComponent<PropObject>();
			prop.SetInfo(GameConfig, prop_info);

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

		DoPaint = (GameConfig.GameMode == 1 || GameConfig.GameMode == 3);
		DoMaze = (GameConfig.GameMode == 2 || GameConfig.GameMode == 3);

		Nodes = NodeGraphs.GenerateNodeGraph(GameConfig.Layers, GameConfig.RoomsPerVertex, GameConfig.RemoveDeadEnds, DoMaze);

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

		// Choose a random color if in paint mode
		Color color = Origin.RandomColor();
		if (DoPaint)
		{
			PaintColor = Origin.InvertColor(color);
			CeilingComponent.SetColor(color);
			PaintHUD.Enable(Rooms.Count);
		}

		// Finish room setup
		foreach (RoomObject room in Rooms)
		{
			if (GameConfig.ReadyProps.Count > 0)
			{
				room.FinishSetup(GenerateProp(), DoPaint ? color : room.RandomColor(true));
			}
			else
			{
				room.FinishSetup(null, DoPaint ? color : room.RandomColor(true));
			}
		}

		// Render distance
		Fog.StartDistance = (GameConfig.RenderDistance - 1.0f) * 250;
		Fog.EndDistance = (GameConfig.RenderDistance - 0.5f) * 250;
		Camera.ZFar = (int)(GameConfig.RenderDistance + 4) * 250;

		EnterRoom(Origin);
	}
}
