using System;
using Sandbox;

public sealed class PropObject : Component
{
	[Property] public ModelRenderer PropRenderer { get; set; }
	[Property] public ModelCollider PropCollider {	get; set; }

	public Vector3 DesiredPosition = Vector3.Zero;
	public Angles DesiredRotation = Angles.Zero;

	public void SetInfo(GameConfig GameConfig, GameConfig.PropInfo prop_info)
	{
		Random random = new Random();
		
		// Object scale
		float scale = prop_info.MinScale;
		float max_scale = prop_info.MaxScale;
		if ( scale != max_scale )
		{
			scale = (float)(scale + random.NextDouble() * (max_scale - scale));
		}
		GameObject.WorldScale = new Vector3(scale, scale, scale);

		// Object rotation		
		DesiredRotation.yaw = random.Next(0, 360);

		// Object height
		float height = prop_info.MinHeight;
		float max_height = prop_info.MaxHeight;
		if ( height != max_height )
		{
			height = (float)(height + random.NextDouble() * (max_height - height));
		}

		// Object radius
		float radius = prop_info.MinRadius;
		float max_radius = prop_info.MaxRadius;
		if ( radius != max_radius )
		{
			radius = (float)(radius + random.NextDouble() * (max_radius - radius));
		}

		if ( radius == 0 ) // Center of room
		{
			DesiredPosition = new Vector3(0, 0, height);
		}
		else // Off-center
		{
			double theta = random.NextDouble() * Math.PI * 2;
			DesiredPosition = new Vector3((float)(radius * Math.Cos(theta)), (float)(radius * Math.Sin(theta)), height);
		}

		PropRenderer.Model = prop_info.PropModel;

		if (GameConfig.PropCollisions)
		{
			PropCollider.Model = prop_info.PropModel;
		}
		else
		{
			PropCollider.Destroy();
			PropCollider = null;
		}
	}
}
