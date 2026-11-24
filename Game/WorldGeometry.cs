using Godot;

namespace ComputerLive;

internal static class WorldGeometry
{
    public static StandardMaterial3D Material(string hex, bool lit = false)
    {
        var color = new Color(hex);
        return new StandardMaterial3D { AlbedoColor = color, Roughness = 0.86f, EmissionEnabled = lit, Emission = color, EmissionEnergyMultiplier = 0.62f };
    }

    public static ShaderMaterial Plaster(string tint, float wear)
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Game/plaster.gdshader") };
        material.SetShaderParameter("tint", new Color(tint));
        material.SetShaderParameter("wear", wear);
        return material;
    }

    public static MeshInstance3D Box(Node3D parent, Vector3 position, Vector3 size, Material material, bool solid = false)
    {
        var mesh = new MeshInstance3D { Position = position, Mesh = new BoxMesh { Size = size }, MaterialOverride = material };
        parent.AddChild(mesh);
        if (solid)
            Collider(mesh, Vector3.Zero, size);
        return mesh;
    }

    public static void Collider(Node3D parent, Vector3 position, Vector3 size)
    {
        var body = new StaticBody3D { Position = position };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        parent.AddChild(body);
    }

    public static void Sphere(Node3D parent, Vector3 position, Vector3 size, Material material) =>
        parent.AddChild(new MeshInstance3D
        {
            Position = position, Scale = size,
            Mesh = new SphereMesh { Radius = 0.50f, Height = 1f, RadialSegments = 12, Rings = 6 },
            MaterialOverride = material,
        });

    public static void Target(Node3D parent, Fixture fixture, Vector3 position, Vector3 size)
    {
        var area = new Area3D
        {
            Name = fixture.ToString(), Position = position, CollisionLayer = 2, CollisionMask = 0, Monitoring = false,
        };
        area.SetMeta("fixture", (int)fixture);
        area.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        parent.AddChild(area);
    }

    public static void Sign(Node3D parent, string title, Vector3 position, float yaw = 0, float size = 0.006f) =>
        parent.AddChild(new Label3D
        {
            Text = title, Position = position, RotationDegrees = new(0, yaw, 0), PixelSize = size,
            FontSize = 32, Modulate = new Color("#eed8a8"), OutlineSize = 2,
        });
}
