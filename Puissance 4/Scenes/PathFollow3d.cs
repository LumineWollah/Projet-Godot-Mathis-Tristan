using Godot;
using System;
using System.Diagnostics;

public partial class PathFollow3d : PathFollow3D
{
    [Export] public float MoveSpeed = 0.25f;
    [Export] public float ZoomSpeed = 2f;
    [Export] public float MinZoom = 3f;
    [Export] public float MaxZoom = 14f;
    [Export] public Node3D Node3D;

    private SpringArm3D _arm = default!;
    private Camera3D _cam = default!;

    public override void _Ready()
    {
        //initialisation des nodes enfants
        _arm = GetNode<SpringArm3D>("Anti_collision");
        _cam = _arm.GetNode<Camera3D>("Camera3D");
        _arm.SpringLength = Mathf.Clamp(_arm.SpringLength, MinZoom, MaxZoom);
        _cam.MakeCurrent();
    }

    public override void _Process(double delta)
    {
        // gestion des inputs pour le mouvement
        if (Input.IsKeyPressed(Key.Left))
        {
            ProgressRatio = Mathf.PosMod(ProgressRatio - MoveSpeed * (float)delta, 1f);
        }
        if (Input.IsKeyPressed(Key.Right))
        {
            ProgressRatio = Mathf.PosMod(ProgressRatio + MoveSpeed * (float)delta, 1f);
        }

        if (Node3D != null)
            _cam.LookAt(Node3D.GlobalPosition, Vector3.Up);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        //gestion du zoom cam
        if (e is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
                _arm.SpringLength = Mathf.Clamp(_arm.SpringLength - ZoomSpeed, MinZoom, MaxZoom);
            else if (mb.ButtonIndex == MouseButton.WheelDown)
                _arm.SpringLength = Mathf.Clamp(_arm.SpringLength + ZoomSpeed, MinZoom, MaxZoom);
        }
    }
}
