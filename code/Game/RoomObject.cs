using Sandbox;
using System;

public sealed class RoomObject : Component
{
	[Property] public GameObject Room;
	[Property] public GameObject[] Doors = new GameObject[4];
	[Property] public ModelRenderer[] PlaneRenderers = new ModelRenderer[4];
	[Property] public ModelRenderer PlaneRenderer;
	[Property] public GameObject RenderList;

	public List<ModelRenderer> ObjectRenderers;
	public RoomObject[] Neighbors = new RoomObject[4];

	public int Distance = 0;
	public int StencilRef = 0;
	public int StencilObjectRead = 0;
	public int StencilMaskRead = 0;
	public int StencilMaskWrite = 0;
	public bool WriteStencil = false;

	public bool IsOrigin = false;
	public ModelCollider PropCollider;

	private int ColorDivisorMin = 2;
	private int ColorDivisorMax = 4;
	private Random Random = new Random();

	private List<Vector3> PositionOffsets = new List<Vector3>
	{
		new Vector3(250, 0, 0),
		new Vector3(0, 250, 0),
		new Vector3(-250, 0, 0),
		new Vector3(0, -250, 0),
	};

	private List<Angles> RotationOffsets = new List<Angles>
	{
		new Angles(0, 0, 0),
		new Angles(0, 90, 0),
		new Angles(0, 180, 0),
		new Angles(0, 270, 0),
	};

	private Vector3 PositionOffset(Vector3 enabler_position, int enabler_edge, int enabler_rotation_direction)
	{
		return enabler_position + PositionOffsets[(enabler_edge + enabler_rotation_direction) % 4];
	}

	private Angles RotationOffset(RoomObject enabler, int enabler_edge, int enabler_rotation_direction)
	{
		int this_edge = Array.IndexOf(Neighbors, enabler);
		if ( this_edge == -1 )
		{
			Log.Error("Enabler is not a neighbor to this room!");
			return GameObject.WorldRotation;
		}

		return RotationOffsets[(-this_edge + enabler_edge + enabler_rotation_direction + 2) % 4];
	}

	public RoomObject EnableSelf(RoomObject enabler)
	{
		if ( enabler !=null && this != enabler )
		{
			int enabler_edge = Array.IndexOf(enabler.Neighbors, this);
			if ( enabler_edge == -1 )
			{
				Log.Error("This room is not a neighbor to enabler!");
				enabler_edge = 0;
			}			

			ModelRenderer plane_renderer = PlaneRenderers[Array.IndexOf(Neighbors, enabler)];
			if ( plane_renderer != PlaneRenderer )
			{
				PlaneRenderer.GameObject.Enabled = false;
				PlaneRenderer = plane_renderer;
				PlaneRenderer.GameObject.Enabled = true;
			}

			Angles rotation = enabler.WorldRotation;
			if ( rotation.yaw < 0 ) rotation.yaw += 360;
			int enabler_rotation_direction = RotationOffsets.IndexOf(rotation);
			if ( enabler_rotation_direction == -1 )
			{
				Log.Error("Room with invalid rotation");
				enabler_rotation_direction = 0;
			}

			Vector3 enabler_position = enabler.WorldPosition;

			GameObject.WorldPosition = PositionOffset(enabler_position, enabler_edge, enabler_rotation_direction);
			GameObject.WorldRotation = RotationOffset(enabler, enabler_edge, enabler_rotation_direction);
		}

		if ( !Room.Enabled )
		{
			Room.Enabled = true;
			if ( PropCollider != null )
				PropCollider.Enabled = true;
		}

		return this;
	}

	// Queue self and cache mask direction
	public (RoomObject NewRoom, RoomObject Enabler) QueueSelf(RoomObject enabler, List<int> directions)
	{
		Distance = directions.Count;

		// Object buffer reference. Binary value based on each direction being straight or a turn
		StencilRef = 0;
		StencilObjectRead = 0;
		StencilMaskRead = 0;
		StencilMaskWrite = 0;

		for (int i = 0; i < Math.Min(8, Distance); i++)
		{
			int bit = (int)Math.Pow(2, i);

			StencilRef += bit * ( directions[i] % 2 );
			StencilObjectRead += bit;
		}

		// Only write onto the mask if the last direction is a turn, and if the number of directions is 8 or less
		if ( Distance == 2 )
		{
			WriteStencil = true;
			StencilMaskWrite = 3;
			StencilMaskRead = 0;
		}
		else if ( Distance > 2 && Distance <= 8 && directions[Distance - 1] % 2 == 1 )
		{
			WriteStencil = true;
			StencilMaskWrite = (int)Math.Pow(2, Distance - 1);
			for (int i = 0; i < Distance - 1; i++)
			{
				int bit = (int)Math.Pow(2, i);;

				StencilMaskRead += bit;
			}
		}
		else // Disable the write mask
		{
			WriteStencil = false;
		}

		return (this, enabler);
	}

	// Queue a neighboring room or send the directions further down the line
	public (RoomObject NewRoom, RoomObject Enabler) QueueNeighbor(RoomObject enabler, List<int> directions, int step)
	{
		// Transform direction based on local edges
		int direction = directions[step];
		step++;
		if ( enabler is not null )
		{
			int enabler_direction = Array.IndexOf(Neighbors, enabler);
			if ( enabler_direction != -1 )
			{
				direction = (direction + enabler_direction + 2) % 4;
			}
		}

		// Path leads to a wall
		RoomObject neighbor = Neighbors[direction];
		if ( neighbor is null )
		{
			return (null, null);
		}

		int distance = directions.Count;
		if ( distance == step ) // End of path, queue this room
		{
			return neighbor.QueueSelf(this, directions);
		}
		else // Continue down path
		{
			return neighbor.QueueNeighbor(this, directions, step);
		}
	}

	public void DisableSelf()
	{
		if ( PropCollider != null )
			PropCollider.Enabled = false;
		Room.Enabled = false;
	}

	public void FinishSetup(PropObject prop)
	{
		// Delete doors 
		for (int i = 0; i < 4; i++)
		{
			RoomObject neighbor = Neighbors[i];
			if ( neighbor != null )
			{
				Doors[i].Destroy();
			}
		}

		// Select a random color
		int div = Random.Next(ColorDivisorMin, ColorDivisorMax);
		Color color = new Color(RGB(div), RGB(div), RGB(div));
		while ( color == Color.Black )
		{
			color = new Color(RGB(div), RGB(div), RGB(div));
		}

		// Grab all necessary render models
		ObjectRenderers = RenderList.Components.GetAll<ModelRenderer>(FindMode.InDescendants).ToList();
		foreach (ModelRenderer object_renderer in ObjectRenderers)
		{
			object_renderer.RenderOptions.Game = false;
			object_renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
			object_renderer.Tint = color;
		}

		// Potentially add a centerpiece
		if ( prop != null )
		{
			GameObject prop_object = prop.GameObject;
			if ( !IsOrigin)
			{
				prop_object.Parent = RenderList;
				prop_object.WorldPosition = GameObject.WorldPosition + prop.DesiredPosition;
				prop_object.LocalRotation = prop.DesiredRotation;

				ModelRenderer prop_renderer = prop.PropRenderer;
				prop_renderer.RenderOptions.Game = false;
				prop_renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
				//prop_renderer.Tint = new Color(RGB(div), RGB(div), RGB(div));
				prop_renderer.Tint = new Color(1f - color.r, 1f - color.g, 1f - color.b);
				ObjectRenderers.Add(prop_renderer);

				PropCollider = prop.PropCollider;
				PropCollider.Enabled = false;
			}
			else
			{
				prop_object.Destroy();
			}
		}
	}

	public RoomObject CheckForPlayer(Vector3 position)
	{
		// Get the squared distance between player and room
		Vector3 this_difference = GameObject.WorldPosition - position;
		float distance_squared = this_difference.Dot(this_difference);

		for (int i = 0; i < 4; i++)
		{
			// Compare with squared distance of neighboring rooms
			RoomObject neighbor = Neighbors[i];
			if ( neighbor == null ) continue;

			Vector3 neighbor_difference = neighbor.WorldPosition - position;
			if ( neighbor_difference.Dot(neighbor_difference) < distance_squared )
			{
				return neighbor;
			}
		}

		return this;
	}

	private float RGB(int div)
		=> (float)Random.Next(0, div + 1) / div;

	protected override void OnStart()
	{
	}
}
