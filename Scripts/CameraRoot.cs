using Godot;
using System;

public partial class CameraRoot : Node3D
{
    [Export] public float RotationSpeed = 0.005f;
    [Export] public float MinPitch = -89.0f;
    [Export] public float MaxPitch = 89.0f;
    
    private float _yaw = 0.0f;
    private float _pitch = 0.0f;
    private bool _isRotating = false;

    public override void _Ready()
    {
        // Initialize angles from current rotation
        _yaw = Rotation.Y;
        _pitch = Rotation.X;
    }

    public override void _Input(InputEvent @event)
    {
        //GD.Print(@event.AsText());
        // Start/stop rotation on right mouse button
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                _isRotating = mouseButton.Pressed;
            }
        }

        // Handle mouse motion when right button is held
        if (@event is InputEventMouseMotion mouseMotion && _isRotating)
        {
            _yaw -= mouseMotion.Relative.X * RotationSpeed;
            _pitch -= mouseMotion.Relative.Y * RotationSpeed;

            // Clamp pitch to prevent flipping
            _pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(MinPitch), Mathf.DegToRad(MaxPitch));

            // Apply rotation to the pivot node
            Rotation = new Vector3(_pitch, _yaw, 0);
        }

    }
}
