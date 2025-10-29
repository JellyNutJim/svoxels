using Godot;
using System;

public partial class Planet : Control
{
    private RenderingDevice rd;
    private Rid shader;

    [Export] public TextureRect TargetRect;

    public override void _Ready()
    {
        rd = RenderingServer.CreateLocalRenderingDevice();
        var shaderFile = GD.Load<RDShaderFile>("res://Scripts/compute_example.glsl");
        var shaderBytecode = shaderFile.GetSpirV();
        shader = rd.ShaderCreateFromSpirV(shaderBytecode);

        // Create image buffer
        int width = TargetRect.Texture.GetWidth();
        int height = TargetRect.Texture.GetHeight();

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

        TargetRect.Texture = imageTexture;

        GD.Print($"Updated sprite texture: {width}x{height}");
    }
}
