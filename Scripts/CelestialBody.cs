using Godot;
using System;
using System.Drawing;

public partial class CelestialBody : MeshInstance3D
{
	private ImageTexture _cachedTexture;
	private ShaderMaterial _material;
	private int _currentWidth;
	private int _currentHeight;
	private Vector2I _currentScreenSize;
	private int imageHeight;
	private RenderingDevice rd;
	private Rid shader;

	[Export] public Camera3D ParentCamera;
	[Export] public float QuadWidth = 1.0f;

	// Static Buffers
	private Rid _storageBuffer;
	private Rid _uniformBuffer;
	private RDUniform _uniformBufferUniform;
	private RDUniform _storageUniform;

	// variables updated on screen size change:
	private uint _dispatachSize;
	private RDTextureFormat _format;
	private Rid _texture;
	private Rid _pipeline;
	private Rid _uniformSet;
	private Rid _serverTexture;


	private double total;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_currentScreenSize = new Vector2I(0, 0);
		imageHeight = GetPixelWidth(ParentCamera);

		rd = RenderingServer.GetRenderingDevice();
		var shaderFile = GD.Load<RDShaderFile>("res://Scripts/compute_example.glsl");
		var shaderBytecode = shaderFile.GetSpirV();
		shader = rd.ShaderCreateFromSpirV(shaderBytecode);

		var shaderMaterial = new ShaderMaterial();
		shaderMaterial.Shader = GD.Load<Shader>("res://Scripts/display_texture.gdshader");
		SetSurfaceOverrideMaterial(0, shaderMaterial);
		_material = shaderMaterial;

		// Create buffers that dont change in size
		int bufferSize = 1;

		var storageBufferData = new byte[bufferSize];
		_storageBuffer = rd.StorageBufferCreate((uint)storageBufferData.Length, storageBufferData);

		var uniformBufferData = new byte[64];
		_uniformBuffer = rd.UniformBufferCreate((uint)uniformBufferData.Length, uniformBufferData);

		_storageUniform = new RDUniform { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 1 };
		_storageUniform.AddId(_storageBuffer);

		_uniformBufferUniform = new RDUniform { UniformType = RenderingDevice.UniformType.UniformBuffer, Binding = 2 };
		_uniformBufferUniform.AddId(_uniformBuffer);

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		total += delta;
		total = total % 1.0;
		// Create image buffer
		bool createNewImage = false;

		if (ParentCamera.GetWindow().Size != _currentScreenSize)
		{
			_currentScreenSize = ParentCamera.GetWindow().Size;
			imageHeight = GetPixelWidth(ParentCamera);

			GD.Print(imageHeight);

			updateComputePipeline(imageHeight);
			_dispatachSize = (uint)Mathf.CeilToInt(imageHeight / 8.0f);
			createNewImage = true;
		}

		// Update buffers
		var uniformBufferData = new byte[64];

		// Write a float at the start (offset 0)
		float timeValue = (float)(total);


		byte[] timeBytes = BitConverter.GetBytes(timeValue);
		Array.Copy(timeBytes, 0, uniformBufferData, 0, 4); // float = 4 bytes

		rd.BufferUpdate(_uniformBuffer, 0, 64, uniformBufferData);

		var computeList = rd.ComputeListBegin();
		rd.ComputeListBindComputePipeline(computeList, _pipeline);
		rd.ComputeListBindUniformSet(computeList, _uniformSet, 0);
		rd.ComputeListDispatch(computeList,
			xGroups: _dispatachSize,
			yGroups: _dispatachSize,
			zGroups: 1);

		rd.ComputeListEnd();


		//var imageData = rd.TextureGetData(_texture, 0);
		//var image = Image.CreateFromData(imageHeight, imageHeight, false, Image.Format.Rgba8, imageData);

		if (createNewImage)
		{
			((ShaderMaterial)_material).SetShaderParameter("compute_texture", _serverTexture);
		}
	}
	
	
	
	private void updateComputePipeline(int imageHeight)
	{
		if (_uniformSet.IsValid) rd.FreeRid(_uniformSet);
		if (_pipeline.IsValid) rd.FreeRid(_pipeline);
		if (_serverTexture.IsValid) RenderingServer.FreeRid(_serverTexture); // Add this!
		if (_texture.IsValid) rd.FreeRid(_texture);

		_format = new RDTextureFormat
		{
			Width = (uint)imageHeight,
			Height = (uint)imageHeight,
			Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
			UsageBits = RenderingDevice.TextureUsageBits.StorageBit |
				RenderingDevice.TextureUsageBits.CanCopyFromBit |
				RenderingDevice.TextureUsageBits.SamplingBit |
				RenderingDevice.TextureUsageBits.CanUpdateBit
		};

		_texture = rd.TextureCreate(_format, new RDTextureView());
		_serverTexture = RenderingServer.TextureRdCreate(_texture);

		var uniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.Image,
			Binding = 0
		};
		uniform.AddId(_texture);

		_uniformSet = rd.UniformSetCreate([uniform, _storageUniform, _uniformBufferUniform], shader, 0);
		_pipeline = rd.ComputePipelineCreate(shader);
    }

	public int GetPixelWidth(Camera3D camera)
	{
		// Get the quad's center position (this node's global position)
		Vector3 center = GlobalPosition;

		// Calculate world-space positions of left and right edges
		Vector3 right = camera.GlobalTransform.Basis.X; // Camera's right vector
		Vector3 leftEdge = center - right * (QuadWidth / 2);
		Vector3 rightEdge = center + right * (QuadWidth / 2);

		// Project to screen space
		Vector2 leftScreen = camera.UnprojectPosition(leftEdge);
		Vector2 rightScreen = camera.UnprojectPosition(rightEdge);

		// Calculate pixel distance
		float pixelWidth = leftScreen.DistanceTo(rightScreen);

		return (int)pixelWidth;
	}

	// Free Rids
	public override void _ExitTree()
    {
        if (_uniformSet.IsValid) rd.FreeRid(_uniformSet);
        if (_pipeline.IsValid) rd.FreeRid(_pipeline);
        if (_serverTexture.IsValid) RenderingServer.FreeRid(_serverTexture);
        if (_texture.IsValid) rd.FreeRid(_texture);
        if (_uniformBuffer.IsValid) rd.FreeRid(_uniformBuffer);
        if (_storageBuffer.IsValid) rd.FreeRid(_storageBuffer);
        if (shader.IsValid) rd.FreeRid(shader);
    }
}
