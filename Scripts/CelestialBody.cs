using Godot;
using System;
using System.Drawing;

public partial class CelestialBody : MeshInstance3D
{
	private ImageTexture _cachedTexture;
	private StandardMaterial3D _material;
	private int _currentWidth;
	private int _currentHeight;
	private RenderingDevice rd;
	private Rid shader;

	[Export] public Camera3D ParentCamera;
	[Export] public float QuadWidth = 1.0f;



	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		rd = RenderingServer.CreateLocalRenderingDevice();
		var shaderFile = GD.Load<RDShaderFile>("res://Scripts/compute_example.glsl");
		var shaderBytecode = shaderFile.GetSpirV();
		shader = rd.ShaderCreateFromSpirV(shaderBytecode);

		_material = (StandardMaterial3D)GetSurfaceOverrideMaterial(0);

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Create image buffer
		int width = GD.RandRange(50, 100);
		int height = GD.RandRange(50, 100);

		//GD.Print(height, " ", width);
		//GD.Print(GetPixelWidth(ParentCamera));

		var format = new RDTextureFormat
		{
			Width = (uint)width,
			Height = (uint)height,
			Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
			UsageBits = RenderingDevice.TextureUsageBits.StorageBit |
						RenderingDevice.TextureUsageBits.CanCopyFromBit |
						RenderingDevice.TextureUsageBits.CanUpdateBit
		};

		var texture = rd.TextureCreate(format, new RDTextureView());

		var uniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.Image,
			Binding = 0
		};
		uniform.AddId(texture);

		var uniformSet = rd.UniformSetCreate([uniform], shader, 0);
		var pipeline = rd.ComputePipelineCreate(shader);

		var computeList = rd.ComputeListBegin();
		rd.ComputeListBindComputePipeline(computeList, pipeline);
		rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
		rd.ComputeListDispatch(computeList,
			xGroups: (uint)Mathf.CeilToInt(width / 16.0f),
			yGroups: (uint)Mathf.CeilToInt(height / 16.0f),
			zGroups: 1);

		rd.ComputeListEnd();
		rd.Submit();
		rd.Sync();

		var imageData = rd.TextureGetData(texture, 0);
		var image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, imageData);
		var imageTexture = ImageTexture.CreateFromImage(image);

		if (_cachedTexture == null || _currentWidth != width || _currentHeight != height)
		{
			_cachedTexture = ImageTexture.CreateFromImage(image);
			_material.AlbedoTexture = _cachedTexture;
			_currentWidth = width;
			_currentHeight = height;
		}
		else
		{
			_cachedTexture.Update(image); // Reuses existing texture - faster!
		}

		//GD.Print($"Updated sprite texture: {width}x{height}");
	}

	public float GetPixelWidth(Camera3D camera)
	{
		// Get the quad's center position (this node's global position)
		Vector3 center = GlobalPosition;

		// Calculate world-space positions of left and right edges
		// Assuming the quad is axis-aligned (facing camera)
		Vector3 right = camera.GlobalTransform.Basis.X; // Camera's right vector
		Vector3 leftEdge = center - right * (QuadWidth / 2);
		Vector3 rightEdge = center + right * (QuadWidth / 2);

		// Project to screen space
		Vector2 leftScreen = camera.UnprojectPosition(leftEdge);
		Vector2 rightScreen = camera.UnprojectPosition(rightEdge);

		// Calculate pixel distance
		float pixelWidth = leftScreen.DistanceTo(rightScreen);

		return pixelWidth;
	}
}
