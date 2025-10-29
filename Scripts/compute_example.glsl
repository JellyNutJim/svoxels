#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, set = 0, binding = 0) uniform writeonly restrict image2D output_image;

layout(set = 0, binding = 1) buffer MyStorageBuffer {
    float data[];
} myStorageBuffer;

layout(set = 0, binding = 2) uniform MyUniformBuffer {
    float rand;
} params;


void main() {
    ivec2 pixel_coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 image_size = imageSize(output_image);
    
    if (pixel_coords.x >= image_size.x || pixel_coords.y >= image_size.y) {
        return;
    }
    
    // Create a gradient or pattern
    vec2 uv = vec2(pixel_coords) / vec2(image_size);

    vec4 color = vec4(params.rand, 0.0, 0.0, 1.0);

    // if (pixel_coords.x % 2 == 0) {
    //     color = vec4(uv.x, uv.y, 1.0, 1.0);
    // }

    imageStore(output_image, pixel_coords, color);
}