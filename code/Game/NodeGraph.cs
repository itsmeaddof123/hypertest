using Sandbox;
using System;
using System.Text;

public sealed class NodeGraph : Component
{
	// Number of rays to cast from 0 to pi/2
	private int Rays = 45;

	private Dictionary<string, List<float>> ViewPoints = new Dictionary<string, List<float>>
	{
		{ "squareroomnone", new List<float> { 0.1f, 0.25f, 0.5f, 0.75f, 0.9f } },
		{ "squareroomthin", new List<float> { 0.15f, 0.3f, 0.5f, 0.7f, 0.85f } },
		{ "squareroomthick", new List<float> { 0.4f, 0.5f, 0.6f } }
	};

	private Dictionary<string, float> CullDistances = new Dictionary<string, float>
	{
		{ "squareroomnone", 0.0f },
		{ "squareroomthin", 0.11f },
		{ "squareroomthick", 0.3f }
	};

	// Used when creating and positioning rooms
	public class NodeGraphInstructions
	{
		public Dictionary<string, string[]> Graph { get; set; }
		public int Sides { get; set; }
		public int Vertices { get; set; }
	}

	// Room nodes used in procedurally generating 
	public class RoomNode
	{
		public string Name;
		public RoomNode[] Neighbors;
		public RoomObject Room;

		public RoomNode(string name)
		{
			Name = name;
			Neighbors = new RoomNode[4];
		}

		// Give the a room a number based on its layer and generation order
		public string DetermineName(int layer, int num)
		{
			StringBuilder result = new StringBuilder();
			while (num > 0)
			{
				num--;
				char letter = (char)('A' + (num % 26));
				result.Insert(0, letter);
				num /= 26;
			}

			return layer.ToString() + result.ToString();
		}

		// Search along the graph for a neighbor
		public void ConnectToNeighbor(RoomNode node, RoomNode caller, bool left, int steps_left)
		{
			// Get the edge of the neighbor who called me
			int edge = Array.IndexOf(Neighbors, caller);
			if ( edge == -1 )
			{
				Log.Error("Invalid neighbor " + caller.Name + " to " + this.Name);
				return;
			}

			edge = left ? ( edge + 1 ) % 4 : ( edge + 3 ) % 4;
			RoomNode neighbor = Neighbors[edge];

			if ( steps_left == 0 ) // End search, connect neighbors
			{
				if ( neighbor != null )
				{
					Log.Error("Unexpected neighbor found!");
					return;
				}

				Neighbors[edge] = node;
				node.Neighbors[left ? 1 : 3] = this;
			}
			else if ( neighbor != null ) // Find the next neighbor in line
			{
				neighbor.ConnectToNeighbor(node, this, left, steps_left - 1);
			}
		}

		// Generate a new node for each empty edge
		public List<RoomNode> GenerateNodes(int layer, int num, int rooms_per_vertex)
		{	
			List<RoomNode> new_nodes = new List<RoomNode>();

			// Create a neighbor at each empty edge
			for (int i = 0; i < 4; i++)
			{
				RoomNode node = Neighbors[i];
				if ( node != null )
				{
					continue;
				}

				// Create neighbor
				node = new RoomNode(DetermineName(layer, num));
				new_nodes.Add(node);

				// Add connections
				Neighbors[i] = node;
				node.Neighbors[2] = this;
				
				// Seeif there are neighbors
				ConnectToNeighbor(node, node, true, rooms_per_vertex - 2);
				ConnectToNeighbor(node, node, false, rooms_per_vertex - 2);
				
				num++;
			}

			return new_nodes;
		}

		public bool CullCheck()
		{
			RoomNode node = null;
			int index = -1;
			for (int i = 0; i < 4; i++)
			{
				RoomNode new_node = Neighbors[i];
				if ( new_node != null ) // At least one neighbor
				{
					if ( node != null ) // Two neighbors, do not cull
					{
						return false;
					}
					else
					{
						node = new_node;
						index = i;
					}
				}
			}

			// Only one neighbor was found
			if ( node != null )
			{
				RoomNode neighbor = Neighbors[index];
				int edge = Array.IndexOf(neighbor.Neighbors, this);
				if ( edge != -1 )
				{
					neighbor.Neighbors[edge] = null;
				}

				Neighbors[index] = null;
				return true;
			}

			// No neighbors were found
			Log.Error("No neighbors found");
			return false;
		}

		// Used for debugging
		public void Print()
		{
			string msg = Name + ", Neighbors: ";
			for (int i = 0; i < 4; i++)
			{
				RoomNode neighbor = Neighbors[i];
				if ( neighbor == null )
				{
					msg += "null ";
				}
				else
				{
					msg += neighbor.Name + " ";
				}
			}
			Log.Info(msg);
		}
	}

	// Generates a new nodegraph based on the given layer count and rooms per corner
	public List<RoomNode> GenerateNodeGraph(int layers, int rooms_per_vertex, bool remove_dead_ends)
	{
		RoomNode center = new RoomNode("0A"); // Center node to begin the branching
		List<RoomNode> nodes = new List<RoomNode>{center}; // All nodes in the graph	
		List<RoomNode> last_layer = new List<RoomNode>{center};// Only the newest nodes

		// Branch the graph out layer by layer
		for (int layer = 1; layer <= layers; layer++)
		{
			int num = 1;
			List<RoomNode> new_layer = new List<RoomNode>();

			// Fill in each previous node's empty edges
			foreach (RoomNode node in last_layer)
			{
				// Add each new node 
				foreach (RoomNode new_node in node.GenerateNodes(layer, num, rooms_per_vertex))
				{
					num++;
					nodes.Add(new_node);
					new_layer.Add(new_node);
				}
			}

			last_layer = new_layer;
		}

		// Cull nodes with only one neighbor
		if (remove_dead_ends)
		{
			bool culled;
			do
			{
				culled = false;
				for (int i = nodes.Count - 1; i >= 0; i--)
				{
					RoomNode node = nodes[i];
					if (node.CullCheck())
					{
						nodes.RemoveAt(i);
						node = null;
						culled = true;
					}
				}
			} while ( culled );
		}

		return nodes;
	}

	// Try to add the set of directions to the list of all directions
	public void TryAddDirections(List<List<int>> all_directions, List<int> directions, List<string> all_hash, string hash)
	{
		if (!all_hash.Contains(hash))
		{
			all_hash.Add(hash);
			all_directions.Add(directions.ToList());
		}
	}

	// Generate a list of sets of directions by simulating raycasts through walls
	public List<List<int>> GenerateDirections(float render_distance, string room_name)
	{
		// Hard limit the render distance
		render_distance = Math.Min(render_distance, 30f);

		List<List<int>> all_directions = new List<List<int>>();
		List<string> all_hash = new List<string>(); // Pseudo-hash for performance

		// Add all of the directly straight directions since we won't check for them algorithmically
		List<int> straight = new List<int>();
		string straight_hash = "";
		all_directions.Add(straight.ToList());
		all_hash.Add(straight_hash);
		for (int i = 0; i < render_distance - 1; i++)
		{
			straight.Add(0);
			straight_hash += "0";
			all_directions.Add(straight.ToList());
			all_hash.Add(straight_hash);
		}

		// Constant for now
		float point_y = 0.95f;

		// Prevent dictionary key error
		if (!ViewPoints.ContainsKey(room_name))
		{
			Log.Error("Room name \"" + room_name + "\" invalid!");
			return all_directions;
		}
		List<float> view_points = ViewPoints[room_name];

		// Prevent dictionary key error
		float cull_distance = 0;
		if (CullDistances.ContainsKey(room_name))
		{
			cull_distance = CullDistances[room_name];
		}

		double increment = Math.PI / 2f / Rays;
		for (int i = 0; i <= Rays; i++)
		{
			// Define a point on the circle
			double angle = i * increment;
			float circle_x = (float)Math.Cos(angle) * render_distance + 0.5f;
			float circle_y = (float)Math.Sin(angle) * render_distance + 0.5f;

			// Skip ray ending points that down or left of (1, 1)
			if (circle_x <= 1f) continue;
			if (circle_y <= 1f) continue;

			//Log.Info("(" + circle_x.ToString() + ", " + circle_y.ToString() + ")");

			// Each ray starting point (point_x, point_y)
			foreach (float point_x in view_points)
			{
				// Calculate line connecting points
				float m = (circle_y - point_y) / (circle_x - point_x);
				float b = point_y - (m * point_x);

				//Log.Info("(" + point_x.ToString() + ", 0.9)");
				//Log.Info("y = " + m.ToString() + "x + " + b.ToString());

				List<int> directions = new List<int>();
				string hash = "";

				int x = 1;
				int y = 1;
				int next_turn = 1;
				bool last_direction_up = true;
				while (x < circle_x || y + 1 < circle_y)
				{
					float next_y = m * x + b;

					// Cull ray if hitting a wall
					if (cull_distance != 0)
					{
						float mid_y = m * (x - cull_distance) + b;

						// Cull ray
						if ((mid_y < y + 1 && next_y > y + 1) || (next_y > y + 1 - cull_distance && next_y < y + 1))
						{
							x = (int)render_distance + 1;
							y = (int)render_distance + 1;
							continue; // End loop
						}
					}

					// Check which direction to follow
					if (next_y < y + 1) // Right, go to next x
					{
						if (last_direction_up) // Turning after going up
						{
							directions.Add(next_turn);
							hash += next_turn.ToString();
							next_turn = (next_turn + 2) % 4; // Alternates between 1 and 3
							TryAddDirections(all_directions, directions, all_hash, hash);
						}
						else // Continuing straight after going right
						{
							directions.Add(0);
							hash += "0";
							TryAddDirections(all_directions, directions, all_hash, hash);
						}

						last_direction_up = false;
						x++;
					}
					else // Up, go to next y
					{
						if (last_direction_up) // Continuing straight after going up
						{
							directions.Add(0);
							hash += "0";
							TryAddDirections(all_directions, directions, all_hash, hash);
						}
						else // Turning after going right
						{
							directions.Add(next_turn);
							hash += next_turn.ToString();
							next_turn = (next_turn + 2) % 4; // Alternates between 1 and 3
							TryAddDirections(all_directions, directions, all_hash, hash);
						}

						last_direction_up = true;
						y++;
					}
				}
			}
		}

		return all_directions;
	}

	// Converts a set of directions to world coordinates
	public (int x, int y) DirectionsToWorld(List<int> directions)
	{
		int x = 0;
		int y = 0;
		int previous_direction = 0;
		foreach (int direction in directions)
		{
			previous_direction = ( previous_direction + direction ) % 4;

			if ( previous_direction == 0 )
				x += 1;
			else if ( previous_direction == 1 )
				y += 1;
			else if ( previous_direction == 2 )
				x -= 1;
			else
				y -= 1;
		}
		return (x, y);
	}
	
	public List<int> ReflectDirections(List<int> directions)
	{
		List<int> reflected_directions = new();
		reflected_directions.Add(directions[0]);
		bool different = false;

		for (int i = 1; i < directions.Count; i++)
		{
			int num = directions[i];
			if (num % 2 == 0)
			{
				reflected_directions.Add(num);
			}
			else
			{
				reflected_directions.Add((num + 2) % 4);
				different = true;
			}
		}

		if (different)
		{
			return reflected_directions;
		}

		return null;
	}

	//! ORDER MATTERS! A ROOM MUST BE MOVED BEFORE IT CAN BE USED AS A DIRECTION
	public Dictionary<string, List<List<int>>> Directions = new Dictionary<string, List<List<int>>>
	{
		{ 
			"SquareRoomThick", new List<List<int>>
			{
				// Regular rendering
				new List<int> {},
				new List<int> {0},
				new List<int> {0, 0},
				new List<int> {0, 0, 0},
				new List<int> {0, 0, 0, 0},
				new List<int> {0, 0, 0, 0, 0},
				new List<int> {0, 0, 0, 0, 0, 0},
				new List<int> {0, 0, 0, 0, 0, 0, 0},
				new List<int> {0, 0, 0, 0, 0, 0, 0, 0},

				// Turn branch
				new List<int> {1},
				// Turn-straight sub-branch
				new List<int> {1, 0},
				new List<int> {1, 0, 0},
				new List<int> {1, 0, 0, 0},
				new List<int> {1, 0, 3},
				new List<int> {1, 0, 3, 1},
				new List<int> {1, 0, 3, 1, 0},
				new List<int> {1, 0, 3, 1, 0, 3},
				// Turn-turn sub-branch
				new List<int> {1, 3},
				new List<int> {1, 3, 0},
				new List<int> {1, 3, 0, 1},
				new List<int> {1, 3, 0, 1, 3},
				new List<int> {1, 3, 1},
				new List<int> {1, 3, 1, 3},
				new List<int> {1, 3, 1, 3, 1},
				new List<int> {1, 3, 1, 3, 1, 3},
				new List<int> {1, 3, 1, 3, 1, 3, 1},
				new List<int> {1, 3, 1, 3, 1, 3, 1, 3},

				// Straight branch
				new List<int> {0, 1},
				new List<int> {0, 1, 3},
				new List<int> {0, 1, 3, 0},
				new List<int> {0, 1, 3, 0, 1},

				// 2 Straight branch
				new List<int> {0, 0, 1},

				// 3 Straight branch
				new List<int> {0, 0, 0, 1},
			}
		},
		{
			"SquareRoomThin", new List<List<int>>
			{
				// Regular rendering
				new List<int> {},
				new List<int> {0},
				new List<int> {0, 0},
				new List<int> {0, 0, 0},
				new List<int> {0, 0, 0, 0},
				new List<int> {0, 0, 0, 0, 0},
				new List<int> {0, 0, 0, 0, 0, 0},
				new List<int> {0, 0, 0, 0, 0, 0, 0},
				new List<int> {0, 0, 0, 0,0, 0, 0, 0},

				// Turn branch
				new List<int> {1},
				// Turn-straight sub-branch
				new List<int> {1, 0},
				new List<int> {1, 0, 0},
				new List<int> {1, 0, 0, 3},
				new List<int> {1, 0, 0, 3, 1},
				new List<int> {1, 0, 0, 3, 1, 0},
				new List<int> {1, 0, 0, 0},
				new List<int> {1, 0, 0, 0, 3},
				new List<int> {1, 0, 0, 0, 3, 1},
				new List<int> {1, 0, 0, 0, 0, 3},
				new List<int> {1, 0, 0, 0, 0, 3, 1},
				new List<int> {1, 0, 3},
				new List<int> {1, 0, 3, 1},
				new List<int> {1, 0, 3, 1, 3},
				new List<int> {1, 0, 3, 1, 3, 1},
				new List<int> {1, 0, 3, 1, 3, 1, 3},
				new List<int> {1, 0, 3, 1, 0},
				new List<int> {1, 0, 3, 1, 0, 3},
				// Turn-turn sub-branch
				new List<int> {1, 3},
				new List<int> {1, 3, 0},
				new List<int> {1, 3, 0, 1},
				new List<int> {1, 3, 0, 1, 3},
				new List<int> {1, 3, 0, 1, 3, 1},
				new List<int> {1, 3, 0, 0},
				new List<int> {1, 3, 0, 0, 1},
				new List<int> {1, 3, 1},
				new List<int> {1, 3, 1, 0},
				new List<int> {1, 3, 1, 0, 3},
				new List<int> {1, 3, 1, 0, 3, 1},
				new List<int> {1, 3, 1, 0, 3, 1, 3},
				new List<int> {1, 3, 1, 3},
				new List<int> {1, 3, 1, 3, 0},
				new List<int> {1, 3, 1, 3, 0, 1},
				new List<int> {1, 3, 1, 3, 1},
				new List<int> {1, 3, 1, 3, 1, 0},
				new List<int> {1, 3, 1, 3, 1, 0, 3},
				new List<int> {1, 3, 1, 3, 1, 3},
				new List<int> {1, 3, 1, 3, 1, 3, 1},
				new List<int> {1, 3, 1, 3, 1, 3, 1, 3},

				// Straight branch
				new List<int> {0, 1},
				new List<int> {0, 1, 3},
				new List<int> {0, 1, 3, 1},
				new List<int> {0, 1, 3, 1, 3},
				new List<int> {0, 1, 3, 1, 3, 1},
				new List<int> {0, 1, 3, 1, 3, 1, 3},
				new List<int> {0, 1, 3, 0},
				new List<int> {0, 1, 3, 0, 1},

				// 2 Straight branch
				new List<int> {0, 0, 1},
				new List<int> {0, 0, 1, 3},
				new List<int> {0, 0, 1, 3, 0},

				// 3 Straight branch
				new List<int> {0, 0, 0, 1},
				new List<int> {0, 0, 0, 1, 3},
				new List<int> {0, 0, 0, 0, 1},
				new List<int> {0, 0, 0, 0, 1, 3},
				new List<int> {0, 0, 0, 0, 0, 1},
			}
		},
	};

	public NodeGraphInstructions Hyperbolic_4_5_33 = new NodeGraphInstructions
	{
		Graph = new Dictionary<string, string[]>
		{
			{ "0A", new string[] { "1A", "1B", "1C", "1D", } },
			// 1A Branches
			{ "1A", new string[] { "2B", "2C", "0A", "2A", } },
			{ "1B", new string[] { "2E", "2F", "0A", "2D", } },
			{ "1C", new string[] { "2H", "2I", "0A", "2G", } },
			{ "1D", new string[] { "2K", "2L", "0A", "2J", } },
			// 2A Branches
			{ "2A", new string[] { null, "3B", "1A", "2L", } },
			{ "2B", new string[] { null, "3E", "1A", "3C", } },
			{ "2C", new string[] { null, "2D", "1A", "3F", } },
			// 2B Branches
			{ "2D", new string[] { null, "3I", "1B", "2C", } },
			{ "2E", new string[] { null, "3L", "1B", "3J", } },
			{ "2F", new string[] { null, "2G", "1B", "3M", } },
			// 2C Branches
			{ "2G", new string[] { null, "3P", "1C", "2F", } },
			{ "2H", new string[] { null, "3S", "1C", "3Q", } },
			{ "2I", new string[] { null, "2J", "1C", "3T", } },
			// 2D Branches
			{ "2J", new string[] { null, "3W", "1D", "2I", } },
			{ "2K", new string[] { null, "3Z", "1D", "3X", } },
			{ "2L", new string[] { null, "2A", "1D", "3AA", } },
			// 2A Branches
			{ "3B", new string[] { null, "3C", "2A", null, } },
			{ "3C", new string[] { null, null, "2B", "3B", } },
			{ "3E", new string[] { null, "3F", "2B", null, } },
			{ "3F", new string[] { null, null, "2C", "3E", } },
			// 2B Branches
			{ "3I", new string[] { null, "3J", "2D", null, } },
			{ "3J", new string[] { null, null, "2E", "3I", } },
			{ "3L", new string[] { null, "3M", "2E", null, } },
			{ "3M", new string[] { null, null, "2F", "3L", } },
			// 2C Branches
			{ "3P", new string[] { null, "3Q", "2G", null, } },
			{ "3Q", new string[] { null, null, "2H", "3P", } },
			{ "3S", new string[] { null, "3T", "2H", null, } },
			{ "3T", new string[] { null, null, "2I", "3S", } },
			// 2D Branches
			{ "3W", new string[] { null, "3X", "2J", null, } },
			{ "3X", new string[] { null, null, "2K", "3W", } },
			{ "3Z", new string[] { null, "3AA", "2K", null, } },
			{ "3AA", new string[] { null, null, "2L", "3Z", } },
		},
		Sides = 4,
		Vertices = 5,
	};

	public NodeGraphInstructions Hyperbolic_4_5_61 = new NodeGraphInstructions
	{
		Graph = new Dictionary<string, string[]>
		{
			{ "0A", new string[] { "1A", "1B", "1C", "1D", } },
			// 1A Branches
			{ "1A", new string[] { "2B", "2C", "0A", "2A", } },
			{ "1B", new string[] { "2E", "2F", "0A", "2D", } },
			{ "1C", new string[] { "2H", "2I", "0A", "2G", } },
			{ "1D", new string[] { "2K", "2L", "0A", "2J", } },

			// 2A Branches
			{ "2A", new string[] { "3A", "3B", "1A", "2L", } },
			{ "2B", new string[] { null, "3E", "1A", "3C", } },
			{ "2C", new string[] { "3G", "2D", "1A", "3F", } },
			// 2B Branches
			{ "2D", new string[] { "3H", "3I", "1B", "2C", } },
			{ "2E", new string[] { null, "3L", "1B", "3J", } },
			{ "2F", new string[] { "3N", "2G", "1B", "3M", } },
			// 2C Branches
			{ "2G", new string[] { "3O", "3P", "1C", "2F", } },
			{ "2H", new string[] { null, "3S", "1C", "3Q", } },
			{ "2I", new string[] { "3U", "2J", "1C", "3T", } },
			// 2D Branches
			{ "2J", new string[] { "3V", "3W", "1D", "2I", } },
			{ "2K", new string[] { null, "3Z", "1D", "3X", } },
			{ "2L", new string[] { "3AB", "2A", "1D", "3AA", } },

			// 3A Branches
			{ "3A", new string[] { null, "4C", "2A", "4A", } },
			{ "3B", new string[] { null, "3C", "2A", "4D", } },
			// 3B Branches
			{ "3C", new string[] { null, null, "2B", "3B", } },
			{ "3E", new string[] { null, "3F", "2B", null } },
			// 3C Branches
			{ "3F", new string[] { null, "4N", "2C", "3E", } },
			{ "3G", new string[] { null, "4Q", "2C", "4O", } },

			// 3D Branches
			{ "3H", new string[] { null, "4S", "2D", "4Q", } },
			{ "3I", new string[] { null, "3J", "2D", "4T", } },
			// 3E Branches
			{ "3J", new string[] { null, null, "2E", "3I", } },
			{ "3L", new string[] { null, "3M", "2E", null, } },
			// 3F Branches
			{ "3M", new string[] { null, "4AD", "2F", "3L", } },
			{ "3N", new string[] { null, "4AG", "2F", "4AE", } },

			// 3G Branches
			{ "3O", new string[] { null, "4AI", "2G", "4AG", } },
			{ "3P", new string[] { null, "3Q", "2G", "4AJ", } },
			// 3H Branches
			{ "3Q", new string[] { null, null, "2H", "3P", } },
			{ "3S", new string[] { null, "3T", "2H", null, } },
			// 3I Branches
			{ "3T", new string[] { null, "4AT", "2I", "3S", } },
			{ "3U", new string[] { null, "4AW", "2I", "4AU", } },
			
			// 3J Branches
			{ "3V", new string[] { null, "4AY", "2J", "4AW", } },
			{ "3W", new string[] { null, "3X", "2J", "4AZ", } },
			// 3K Branches
			{ "3X", new string[] { null, null, "2K", "3W", } },
			{ "3Z", new string[] { null, "3AA", "2K", null, } },
			// 3L Branches
			{ "3AA", new string[] { null, "4BJ", "2L", "3Z", } },
			{ "3AB", new string[] { null, "4A", "2L", "4BK", } },

			// 3A Branches
			{ "4A", new string[] { null, null, "3A", "3AB", } },
			{ "4C", new string[] { null, "4D", "3A", null, } },
			{ "4D", new string[] { null, null, "3B", "4C", } },
			// 3C Branches
			{ "4N", new string[] { null, "4O", "3F", null, } },
			{ "4O", new string[] { null, null, "3G", "4N", } },

			// 3D Branches
			{ "4Q", new string[] { null, null, "3H", "3G", } },
			{ "4S", new string[] { null, "4T", "3H", null, } },
			{ "4T", new string[] { null, null, "3I", "4S", } },
			// 3F Branches
			{ "4AD", new string[] { null, "4AE", "3M", null, } },
			{ "4AE", new string[] { null, null, "3N", "4AD", } },

			// 3G Branches
			{ "4AG", new string[] { null, null, "3O", "3N", } },
			{ "4AI", new string[] { null, "4AJ", "3O", null, } },
			{ "4AJ", new string[] { null, null, "3P", "4AI", } },
			// 3I Branches
			{ "4AT", new string[] { null, "4AU", "3T", null, } },
			{ "4AU", new string[] { null, null, "3U", "4AT", } },

			// 3J Branches
			{ "4AW", new string[] { null, null, "3V", "3U", } },
			{ "4AY", new string[] { null, "4AZ", "3V", null, } },
			{ "4AZ", new string[] { null, null, "3W", "4AY", } },
			// 3L Branches
			{ "4BJ", new string[] { null, "4BK", "3AA", null, } },
			{ "4BK", new string[] { null, null, "3AB", "4BJ", } },
		},
		Sides = 4,
		Vertices = 5,
	};
}
