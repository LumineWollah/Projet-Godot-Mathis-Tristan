using Godot;
using System;

public partial class RailCam : Path3D
{
    [Export] public float Radius = 5f;
    [Export] public int Segments = 64;

    public override void _Ready()
    {
        // gestion d'erreur
        if (Curve is { PointCount: > 0 }) return;

        // instanciation dynamique du cercle
        var c = new Curve3D();
        for (int i = 0; i < Segments; i++)
        {
            float t = (float)i / Segments * Mathf.Tau;
            //points possibles de cam
            c.AddPoint(new Vector3(Radius * Mathf.Sin(t), 0, Radius * Mathf.Cos(t)));
        }
        c.Closed = true;
        Curve = c; // assigne la courbe au Path3D
    }
}
