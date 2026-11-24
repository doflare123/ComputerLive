using Godot;
using static ComputerLive.WorldGeometry;

namespace ComputerLive;

internal enum Fixture
{
    Lease, Phone, Bench, Station1, Station2, Station3, Desk, Television, ClubExit, ClubEntry, TramStop,
    PartsEntrance, ComputerEntrance, ShopExit, UsedCheap, UsedTested, MarketDisks, PartsKit, PartsPc,
    PartsDisks, ComputerPc, ComputerDisks,
}

internal sealed class Basement
{
    private static readonly Vector3[] PcPositions = [new(2.25f, 0, -3.28f), new(3.65f, 0, -3.28f), new(5.05f, 0, -3.28f)];
    private readonly Node3D _root = new() { Name = "Basement" };
    private readonly Node3D[] _computers = new Node3D[3];
    private readonly Node3D[] _crates = new Node3D[3];
    private readonly MeshInstance3D[] _screens = new MeshInstance3D[3];
    private readonly Node3D _guest = new() { Name = "Visitor" };
    private readonly MeshInstance3D _northWall;
    private readonly ShaderMaterial _plaster = Plaster("#756453", 1.0f);
    private readonly ShaderMaterial _newPaint = Plaster("#586d64", 0.15f);
    private readonly StandardMaterial3D _wood = Material("#624533");
    private readonly StandardMaterial3D _iron = Material("#262b2a");
    private readonly StandardMaterial3D _casing = Material("#b6aa88");
    private readonly StandardMaterial3D _rubber = Material("#1b2623");
    private readonly StandardMaterial3D _off = Material("#1a2825");
    private readonly StandardMaterial3D _on = Material("#66c8a3", lit: true);
    private readonly StandardMaterial3D _error = Material("#bc5942", lit: true);
    private readonly StandardMaterial3D _paper = Material("#d4c394");
    private readonly ShaderMaterial _terminal = new() { Shader = GD.Load<Shader>("res://Game/terminal.gdshader") };

    public Node3D Root => _root;
    public NewsTelevision Television { get; }

    public Basement(Node3D parent)
    {
        parent.AddChild(_root);
        _root.AddChild(new WorldEnvironment
        {
            Environment = new Environment
            {
                BackgroundMode = Environment.BGMode.Color,
                BackgroundColor = new Color("#141716"),
                AmbientLightSource = Environment.AmbientSource.Color,
                AmbientLightColor = new Color("#aeb6ab"),
                AmbientLightEnergy = 0.54f,
            },
        });

        Box(_root, new(0, -0.14f, 0), new(11.8f, 0.27f, 9.2f), Material("#434844"), solid: true);
        Box(_root, new(0, 3.04f, 0), new(11.8f, 0.22f, 9.2f), Material("#4a4539"), solid: true);
        _northWall = Box(_root, new(0, 1.45f, -4.5f), new(11.8f, 3, 0.25f), _plaster, solid: true);
        Box(_root, new(0, 1.45f, 4.5f), new(11.8f, 3, 0.25f), _plaster, solid: true);
        Box(_root, new(-5.8f, 1.45f, 0), new(0.24f, 3, 9.2f), _plaster, solid: true);
        Box(_root, new(5.8f, 1.45f, 0), new(0.24f, 3, 9.2f), _plaster, solid: true);
        DressRoom();

        Desk(new(-0.45f, 0, -3.72f), 2.05f, "ВЕРСТАК");
        Box(_root, new(-0.68f, 0.83f, -3.68f), new(0.65f, 0.02f, 0.48f), _rubber);
        Box(_root, new(-0.2f, 0.86f, -3.51f), new(0.22f, 0.04f, 0.04f), _casing);
        Target(Fixture.Bench, new(-0.45f, 1.04f, -3.54f), new(1.99f, 0.90f, 0.87f));

        Desk(new(-4.43f, 0, -3.64f), 1.40f, "СНАБЖЕНИЕ");
        Box(_root, new(-4.57f, 0.89f, -3.56f), new(0.38f, 0.13f, 0.28f), _rubber);
        Box(_root, new(-4.57f, 0.98f, -3.61f), new(0.36f, 0.066f, 0.08f), _iron);
        Box(_root, new(-4.12f, 0.83f, -3.54f), new(0.25f, 0.024f, 0.32f), _paper);
        Target(Fixture.Phone, new(-4.43f, 1.05f, -3.55f), new(1.36f, 0.90f, 0.91f));

        Desk(new(-4.35f, 0, 2.87f), 2.05f, "ЖУРНАЛ СМЕН");
        Box(_root, new(-4.23f, 0.84f, 2.89f), new(0.37f, 0.04f, 0.28f), _paper);
        Box(_root, new(-4.47f, 0.85f, 2.79f), new(0.032f, 0.025f, 0.21f), _rubber);
        Target(Fixture.Desk, new(-4.35f, 1.05f, 2.87f), new(1.99f, 0.90f, 0.85f));

        Desk(new(4.27f, 0, 2.8f), 1.48f, "ТЕЛЕВИЗОР");
        Box(_root, new(4.27f, 1.14f, 2.77f), new(0.74f, 0.64f, 0.51f), _iron);
        Television = new NewsTelevision(_root, new(4.2f, 1.16f, 2.505f));
        Box(_root, new(4.58f, 1.31f, 2.504f), new(0.078f, 0.068f, 0.027f), _casing);
        Box(_root, new(4.29f, 1.58f, 2.75f), new(0.013f, 0.28f, 0.013f), _casing).RotationDegrees = new(0, 0, -24);
        Target(Fixture.Television, new(4.27f, 1.22f, 2.8f), new(1.45f, 1.19f, 0.91f));

        Box(_root, new(-5.63f, 1.16f, 0.63f), new(0.18f, 2.32f, 1.43f), _iron);
        Box(_root, new(-5.57f, 1.13f, 0.63f), new(0.16f, 2.20f, 1.24f), _wood);
        Box(_root, new(-5.49f, 1.05f, 0.16f), new(0.12f, 0.058f, 0.15f), _casing);
        Box(_root, new(-5.482f, 1.59f, 0.63f), new(0.015f, 0.24f, 0.62f), _iron);
        Sign("ДОГОВОР АРЕНДЫ", new(-5.469f, 1.59f, 0.63f), 90, 0.00165f);
        Target(Fixture.Lease, new(-5.38f, 1.32f, 0.63f), new(0.29f, 1.65f, 1.36f));
        Box(_root, new(0.87f, 1.14f, 4.31f), new(1.42f, 2.29f, 0.17f), _iron);
        Box(_root, new(0.87f, 1.12f, 4.20f), new(1.26f, 2.20f, 0.14f), _wood);
        Box(_root, new(1.35f, 1.06f, 4.09f), new(0.11f, 0.05f, 0.13f), _casing);
        Sign("ВЫХОД ВО ДВОР", new(0.87f, 1.86f, 4.109f), 180, 0.0032f);
        Target(Fixture.ClubExit, new(0.87f, 1.23f, 4.08f), new(1.37f, 1.72f, 0.30f));

        for (int i = 0; i < PcPositions.Length; i++)
            Workstation(i);

        ModelVisitor();
        _guest.Hide();
    }

    public void Update(ClubLedger club)
    {
        _northWall.MaterialOverride = club.Renovated ? _newPaint : _plaster;
        for (int i = 0; i < club.Stations.Length; i++)
        {
            var station = club.Stations[i];
            _crates[i].Visible = station.Hardware is Hardware.Kit or Hardware.Broken;
            _computers[i].Visible = station.Hardware == Hardware.Working;
            _screens[i].MaterialOverride = station.NeedsService ? _error : station.Ready ? _terminal : _off;
        }
    }

    public void ShowVisitor(int station)
    {
        _guest.Position = PcPositions[station] + new Vector3(0, 0, 0.86f);
        _guest.Show();
    }

    public void HideVisitor() => _guest.Hide();

    private void DressRoom()
    {
        var trim = Material("#503d34");
        var patch = Material("#665848");
        var tile = Material("#4b5047");
        for (int x = -5; x <= 5; x++)
        for (int z = -4; z <= 4; z++)
        {
            if ((x + z) % 2 == 0)
                Box(_root, new(x, 0, z), new(0.98f, 0.008f, 0.98f), tile);
        }

        foreach (float z in new[] { -4.34f, 4.34f })
            Box(_root, new(0, 0.15f, z), new(11.4f, 0.25f, 0.054f), trim);

        Box(_root, new(-4.48f, 2.34f, -4.34f), new(0.92f, 0.38f, 0.11f), _iron);
        Box(_root, new(-4.48f, 2.34f, -4.27f), new(0.79f, 0.26f, 0.028f), Material("#6a8d89", lit: true));
        Box(_root, new(-4.48f, 2.34f, -4.23f), new(0.06f, 0.28f, 0.025f), _iron);
        Box(_root, new(-2.45f, 1.47f, -4.37f), new(0.42f, 0.35f, 0.031f), patch);
        Box(_root, new(-2.29f, 1.2f, -4.36f), new(0.74f, 0.22f, 0.033f), patch);
        Box(_root, new(-3.07f, 0.44f, -4.35f), new(1.12f, 0.24f, 0.033f), trim);
        Box(_root, new(0.25f, 2.16f, -4.34f), new(1.35f, 0.27f, 0.065f), _wood);
        Sign("КОМПЬЮТЕРНЫЙ КЛУБ", new(0.25f, 2.16f, -4.302f), size: 0.0030f);

        var pipe = Material("#544840");
        Box(_root, new(0, 2.75f, -4.20f), new(11.2f, 0.10f, 0.13f), pipe);
        Box(_root, new(-5.35f, 1.5f, -4.20f), new(0.1f, 2.55f, 0.13f), pipe);
        Box(_root, new(5.34f, 1.5f, -4.20f), new(0.1f, 2.55f, 0.13f), pipe);
        foreach (float x in new[] { -3.8f, -1.23f, 1.33f, 3.88f })
            Box(_root, new(x, 2.75f, -4.20f), new(0.105f, 0.15f, 0.16f), _iron);

        Lamp(new(-3.28f, 2.72f, -1.8f), new Color("#ffd69c"));
        Lamp(new(3.12f, 2.72f, -2.0f), new Color("#bde2d0"));
        Lamp(new(-0.40f, 2.72f, 2.39f), new Color("#e9bf83"));
    }

    private void Desk(Vector3 position, float width, string? title = null)
    {
        Box(_root, position + new Vector3(0, 0.76f, 0), new(width, 0.11f, 0.79f), _wood, solid: true);
        foreach (var x in new[] { -width / 2 + 0.10f, width / 2 - 0.10f })
        foreach (var z in new[] { -0.28f, 0.28f })
            Box(_root, position + new Vector3(x, 0.36f, z), new(0.065f, 0.72f, 0.065f), _iron);

        if (title != null)
            Sign(title, position + new Vector3(0, 0.69f, 0.408f), size: 0.0037f);
    }

    private void Workstation(int index)
    {
        Vector3 position = PcPositions[index];
        Desk(position, 1.28f, $"МЕСТО 0{index + 1}");
        var computer = new Node3D { Position = position };
        _root.AddChild(computer);
        _computers[index] = computer;
        Box(computer, new(-0.16f, 1.093f, -0.11f), new(0.62f, 0.51f, 0.51f), _casing);
        Box(computer, new(-0.16f, 1.12f, 0.153f), new(0.51f, 0.37f, 0.036f), _rubber);
        _screens[index] = new MeshInstance3D
        {
            Position = new(-0.16f, 1.12f, 0.176f), Mesh = new QuadMesh { Size = new Vector2(0.444f, 0.307f) },
            MaterialOverride = _off,
        };
        computer.AddChild(_screens[index]);
        Box(computer, new(0.06f, 0.89f, 0.157f), new(0.023f, 0.017f, 0.021f), _on);
        Box(computer, new(0.36f, 1.04f, -0.07f), new(0.22f, 0.44f, 0.52f), _casing);
        Box(computer, new(0.36f, 1.18f, 0.197f), new(0.142f, 0.032f, 0.019f), _rubber);
        Box(computer, new(0.36f, 1.10f, 0.197f), new(0.142f, 0.049f, 0.019f), _rubber);
        Box(computer, new(-0.09f, 0.832f, 0.289f), new(0.50f, 0.052f, 0.15f), _casing);
        for (int i = 0; i < 4; i++)
            Box(computer, new(-0.09f, 0.859f, 0.241f + i * 0.03f), new(0.436f, 0.013f, 0.019f), _iron);

        var crate = new Node3D { Position = position };
        _root.AddChild(crate);
        _crates[index] = crate;
        Box(crate, new(0, 0.98f, -0.02f), new(0.58f, 0.31f, 0.53f), Material("#93724b"));
        Box(crate, new(0, 1.137f, -0.02f), new(0.11f, 0.016f, 0.53f), _paper);

        Box(_root, position + new Vector3(0, 0.43f, 0.92f), new(0.50f, 0.08f, 0.48f), _iron, solid: true);
        Box(_root, position + new Vector3(0, 0.72f, 1.15f), new(0.50f, 0.53f, 0.06f), _wood);
        Box(_root, position + new Vector3(0, 0.20f, 0.92f), new(0.054f, 0.40f, 0.09f), _iron);
        Target(Fixture.Station1 + index, position + new Vector3(0, 1.06f, 0.025f), new(1.27f, 0.86f, 0.81f));
    }

    private void ModelVisitor()
    {
        _root.AddChild(_guest);
        var jacket = Material("#4c566d");
        var skin = Material("#ad8068");
        var hair = Material("#40352b");
        Sphere(_guest, new(0, 0.78f, 0.06f), new(0.37f, 0.51f, 0.29f), jacket);
        Sphere(_guest, new(0, 1.09f, 0.01f), new(0.23f, 0.30f, 0.22f), skin);
        Sphere(_guest, new(0, 1.16f, 0.049f), new(0.24f, 0.19f, 0.21f), hair);
        for (int side = -1; side <= 1; side += 2)
        {
            Sphere(_guest, new(side * 0.18f, 0.83f, -0.035f), new(0.125f, 0.16f, 0.30f), jacket);
            Sphere(_guest, new(side * 0.19f, 0.83f, -0.194f), new(0.10f, 0.085f, 0.14f), skin);
            Sphere(_guest, new(side * 0.10f, 0.52f, -0.15f), new(0.16f, 0.16f, 0.40f), _iron);
        }
    }

    private void Target(Fixture fixture, Vector3 position, Vector3 size) => WorldGeometry.Target(_root, fixture, position, size);

    private void Lamp(Vector3 position, Color color)
    {
        Box(_root, position + Vector3.Up * 0.125f, new(1.65f, 0.10f, 0.31f), _iron);
        Box(_root, position + Vector3.Up * 0.055f, new(1.44f, 0.057f, 0.21f), Material(color.ToHtml(), lit: true));
        _root.AddChild(new OmniLight3D { Position = position, LightColor = color, OmniRange = 7.4f, LightEnergy = 1.1f, ShadowEnabled = true });
    }

    private void Sign(string title, Vector3 position, float yaw = 0, float size = 0.006f) => WorldGeometry.Sign(_root, title, position, yaw, size);
}
