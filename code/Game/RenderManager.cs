using Sandbox;

public sealed class RenderManager : Component, Component.ExecuteInEditor
{
	[Property] public GameManager GameManager { get; set; }
	[Property] public Material MaskMaterial { get; set; }
	[Property] public Material ObjectMaterial { get; set; }

	// Controls the render order
	private SceneCustomObject _sceneCustomObject;

	protected override void OnPreRender()
	{
		base.OnPreRender();

		if (_sceneCustomObject.IsValid())
			_sceneCustomObject.Transform = WorldTransform;
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		_sceneCustomObject = new SceneCustomObject(Scene.SceneWorld) { RenderOverride = Render };
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		if (_sceneCustomObject.IsValid())
			_sceneCustomObject.Delete();

		_sceneCustomObject = null;
	}

	private void Render(SceneObject sceneObject)
	{
		// Look at each room, starting at the closest, then further out
		for (int distance = 0; distance < GameManager.ActiveRoomsByDistance.Count; distance++)
		{
			// Give shadows only to one room occupying a given space
			List<Vector3> locations = new();

			// Iterate through each room at the given distance
			List<RoomObject> rooms_by_given_distance = GameManager.ActiveRoomsByDistance[distance];
			foreach (RoomObject room in rooms_by_given_distance)
			{
				// Render the plane mask
				int stencil_ref = room.StencilRef;
				if ( room.WriteStencil )
				{
					ModelRenderer plane_renderer = room.PlaneRenderer;
					if ( plane_renderer != null ) {
						SceneObject plane_so = plane_renderer.SceneObject;
						if ( plane_so != null )
						{
							plane_so.Attributes.Set("stencil_ref", stencil_ref);
							plane_so.Attributes.Set("stencil_read", room.StencilMaskRead);
							plane_so.Attributes.Set("stencil_write", room.StencilMaskWrite);

							Graphics.Render(plane_so, plane_renderer.WorldTransform, material: MaskMaterial);
						}
					}
				}
				
				//Determine whether shadows should be rendered
				Vector3 location = room.GameObject.WorldPosition;
				bool do_shadows = false;
				if ( locations.IndexOf(location) == -1 )
				{
					locations.Add(location);
					do_shadows = true;
				}

				int stencil_object_read = room.StencilObjectRead;
				foreach (ModelRenderer object_renderer in room.ObjectRenderers)
				{
					if ( object_renderer != null )
					{
						// Toggle shadows
						if ( do_shadows && object_renderer.Tags.Has("do_shadows") )
						{
							object_renderer.RenderType = ModelRenderer.ShadowRenderType.On;
						}
						else
						{
							object_renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
						}

						SceneObject object_so = object_renderer.SceneObject;
						if ( object_so != null )
						{
							object_so.Attributes.Set("stencil_ref", stencil_ref);
							object_so.Attributes.Set("stencil_read", distance < 2 ? 0 : stencil_object_read);
							object_so.Attributes.Set("tint_val", object_renderer.Tint);

							Graphics.Render(object_so, object_renderer.WorldTransform, material: ObjectMaterial);
						}
					}
				}
			}
		}
	}
}
