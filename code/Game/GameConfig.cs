using Sandbox;
using System;

public sealed class GameConfig : Component
{

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

	// Game mode
	private int _game_mode;
	[Property] public int GameMode
	{
		get { return _game_mode; }
		set
		{
			_game_mode = Math.Clamp(value, 0, 3);
			if (GameMode != _game_mode)
			{
				GameMode = _game_mode;
			}
		}
	}
	
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

	// Room Loading Speed
	private int _room_actions_per_frame;
	[Property] public int RoomActionsPerFrame
	{
		get { return _room_actions_per_frame; }
		set
		{
			_room_actions_per_frame = Math.Max(value, 32);
			if (RoomActionsPerFrame != _room_actions_per_frame)
			{
				RoomActionsPerFrame = _room_actions_per_frame;
			}
		}
	}
	
	[Property] public bool CenterSpawn { get; set; } // Center vs Random Spawn
	[Property] public bool RemoveDeadEnds { get; set; } // Remove dead ends during node generation

	public bool Ready = false;

	public struct PropInfo
	{
		public Model PropModel;
		public string ModelName;
		public float MinHeight;
		public float MaxHeight;
		public float MinRadius;
		public float MaxRadius;
		public float MinScale;
		public float MaxScale;

		public PropInfo(string model_name, float min_height = 0, float max_height = 0, float min_radius = 0f, float max_radius = 0f, float min_scale = 1, float max_scale = 1)
		{
			ModelName = model_name;
			MinHeight = min_height;
			MaxHeight = max_height;
			MinRadius = min_radius;
			MaxRadius = max_radius;
			MinScale = min_scale;
			MaxScale = max_scale;
		}
	}
	
	public List<PropInfo> Props = new List<PropInfo>
	{
		new PropInfo("facepunch.toilet_a", 0, 0, 0, 0, 2.5f, 3.5f),
		new PropInfo("facepunch.mountainbike", 35, 50, 0, 0, 1.5f, 2.5f),
		new PropInfo("facepunch.car_dev", 0, 0, 0, 0, 0.65f, 0.85f),
		new PropInfo("facepunch.pickup_dev", 0, 0, 0, 0, 0.65f, 0.85f),
		new PropInfo("facepunch.van_dev", 0, 0, 0, 0, 0.65f, 0.85f),
		//new PropInfo("facepunch.office_chair", 0, 0, 0, 0, 2.5f, 3f),
		new PropInfo("facepunch.dentist_chair", 0, 0, 0, 0, 1.5f, 2.5f),
		//new PropInfo("facepunch.watering_can_03", 0, 0, 0, 0, 5.0f, 6.5f),
		new PropInfo("facepunch.trolley", 0, 0, 0, 0, 2.0f, 2.5f),
		new PropInfo("facepunch.bathtub", 0, 0, 0, 0, 1.0f, 1.5f),
		//new PropInfo("facepunch.metalwheelbarrow", 0, 0, 0, 0, 2.5f, 3.0f),
		//new PropInfo("facepunch.generator_dev", 0, 0, 0, 0, 0.9f, 1.1f),
		new PropInfo("facepunch.waste_bin_dev", 0, 0, 0, 0, 1.0f, 1.0f),
		//new PropInfo("facepunch.weapon_crate_stacka", 0, 0, 0, 0, 1.0f, 1.25f),
		//new PropInfo("facepunch.tyre_side_stack_dev", 0, 0, 0, 0, 1.25f, 1.5f),
		//new PropInfo("facepunch.forklift_down_dev", 0, 0, 0, 0, 0.9f, 1.1f),
		new PropInfo("facepunch.forklift_up_dev", 0, 0, 0, 0, 0.9f, 1.1f),
		new PropInfo("facepunch.truck_dev", 0, 0, 0, 0, 0.5f, 0.6f),
		//new PropInfo("facepunch.swat_dev", 0, 0, 0, 0, 0.6f, 0.7f),
		new PropInfo("facepunch.step_ladder_02", 0, 0, 0, 0, 2.0f, 2.5f),
		//new PropInfo("facepunch.beech_shrub_tall_large", 0, 0, 0, 0, 0.5f, 0.65f),
		//new PropInfo("facepunch.star_bin", 0, 0, 0, 0, 1.75f, 2.0f),
		//new PropInfo("facepunch.wheely_bin", 0, 0, 0, 0, 1.75f, 2.0f),
		//new PropInfo("facepunch.binbag_a1", 0, 0, 0, 0, 3.0f, 4.0f),
		//new PropInfo("facepunch.binbag_pile_a2", 0, 0, 0, 0, 2.5f, 3.0f),
		new PropInfo("facepunch.old_bench", 0, 0, 0, 0, 1.5f, 1.75f),
		//new PropInfo("facepunch.folding_construction_sign", 0, 0, 0, 0, 2.5f, 3.0f),
		new PropInfo("fish.old_red_lada", 0, 0, 0, 0, 0.9f, 1.1f),
		new PropInfo("fish.gt_house_a", 0, 0, 0, 0, 0.2f, 0.25f),
		//new PropInfo("fish.gt_house_b", 0, 0, 0, 0, 0.3f, 0.35f),
		//new PropInfo("fish.gt_house_c", 0, 0, 0, 0, 0.25f, 0.25f),
		new PropInfo("fish.gt_house_d", 0, 0, 0, 0, 0.2f, 0.25f),
		new PropInfo("fish.gt_training_camp", -3, -3, 0, 0, 0.5f, 0.55f),
		new PropInfo("fish.specter", 0, 0, 0, 0, 1.5f, 1.75f),
		//new PropInfo("fish.fox", 0, 0, 0, 0, 3.5f, 4.0f),
		new PropInfo("fish.elk", 0, 0, 0, 0, 1.25f, 1.45f),
		//new PropInfo("fish.reindeer", 0, 0, 0, 0, 1.35f, 1.65f),
		new PropInfo("fish.moose", 0, 0, 0, 0, 1.0f, 1.2f),
		//new PropInfo("fish.doob", 0, 0, 0, 0, 2.0f, 2.5f),
		//new PropInfo("fish.gt_goblin", 0, 0, 0, 0, 2.25f, 2.75f),
		new PropInfo("fish.msc_bed", 0, 0, 0, 0, 1.25f, 1.5f),
		new PropInfo("fish.gt_goblin_spawner", -3, -3, 0, 0, 0.6f, 0.65f),
		//new PropInfo("luke.skeleman", 0, 0, 0, 0, 1.5f, 1.75f),
		//ew PropInfo("gvar.citizen_zombie", 0, 0, 0, 0, 1.5f, 1.75f),
		//new PropInfo("azumangafans.shrek-ragdoll", 0, 0, 0, 0, 1.25f, 1.5f),
		//new PropInfo("mong.capybara", 0, 0, 0, 0, 1.75f, 2.25f),
		//new PropInfo("azumangafans.garfieldmovie", 0, 0, 0, 0, 1.5f, 2.0f),
		//new PropInfo("marauders.feathersmcgraw", 0, 0, 0, 0, 1.75f, 2.25f),
		//new PropInfo("neptunux.littlekitty1", 0, 0, 0, 0, 1.5f, 2.0f),
		//new PropInfo("apetavern.gatorkid", 0, 0, 0, 0, 1.75f, 2.0f),
		//new PropInfo("apetavern.gatorman", 0, 0, 0, 0, 0.75f, 1.0f),
	};

	public List<PropInfo> ReadyProps = new List<PropInfo>();

	protected override async void OnStart()
	{
		for (int i = 0; i < Props.Count; i++)
		{
			PropInfo prop_info = Props[i];
			string model_name = prop_info.ModelName;
			var package = await Package.Fetch(model_name, false);
			await package.MountAsync();
			var primary_asset = package.GetMeta("PrimaryAsset", "");
			prop_info.PropModel = Model.Load(primary_asset);
			ReadyProps.Add(prop_info);
		}
	}
}
