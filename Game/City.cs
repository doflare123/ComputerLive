using Godot;
using static ComputerLive.WorldGeometry;

namespace ComputerLive;

internal sealed class City
{
    private readonly StandardMaterial3D _iron = Material("#323631");
    private readonly StandardMaterial3D _wood = Material("#695040");
    private readonly StandardMaterial3D _paper = Material("#bca779");
    private readonly StandardMaterial3D _cream = Material("#b4a68c");
    private readonly StandardMaterial3D _glass = Material("#53656a");
    private Node3D? _passingBuildings;
    private readonly CampaignDay _day;

    public Node3D Root { get; }

    public City(Place place, CampaignDay day)
    {
        _day = day;
        Root = new Node3D { Name = place.ToString() };
        switch (place)
        {
            case Place.Tram: TramInterior(); break;
            case Place.PartsStore: Store(parts: true); break;
            case Place.ComputerStore: Store(parts: false); break;
            default: Street(place); break;
        }
    }

    public void AdvanceRide(float distance)
    {
        if (_passingBuildings != null)
            _passingBuildings.Position = new Vector3(0, 0, distance);
    }

    public static string NameOf(Place place) => place switch
    {
        Place.Club => "Подвал клуба",
        Place.Courtyard => "Двор клуба",
        Place.Market => "Радиорынок",
        Place.Shopping => "Торговая улица",
        Place.PartsStore => "Радиодеталь",
        Place.ComputerStore => "Электроника",
        _ => "Трамвай",
    };

    private void Light(string background, string ambient, float energy, bool outdoors = false)
    {
        Root.AddChild(new WorldEnvironment
        {
            Environment = new Environment
            {
                BackgroundMode = Environment.BGMode.Color, BackgroundColor = new Color(background),
                AmbientLightSource = Environment.AmbientSource.Color, AmbientLightColor = new Color(ambient), AmbientLightEnergy = energy,
            },
        });
        if (outdoors)
            Root.AddChild(new DirectionalLight3D { RotationDegrees = new(-51, -28, 0), LightColor = new Color("#fff0ca"), LightEnergy = 0.66f, ShadowEnabled = true });
    }

    private void Street(Place place)
    {
        Light("#939c9d", "#d8e1de", 0.74f, outdoors: true);
        Box(Root, new(0, -0.16f, 0), new(27, 0.31f, 21), Plaster("#5c6058", 0.33f), solid: true);
        Box(Root, new(0, -0.013f, -4.96f), new(27, 0.024f, 4.65f), Material("#505553"));
        Box(Root, new(0, 0.012f, -2.52f), new(27, 0.055f, 0.16f), _cream);
        foreach (float z in new[] { -5.57f, -4.13f })
            Box(Root, new(0, 0.012f, z), new(27, 0.026f, 0.063f), _iron);

        for (int i = -6; i <= 6; i++)
        {
            Box(Root, new(i * 2.04f, 0.002f, 0.12f), new(0.016f, 0.017f, 5.2f), _iron);
            Box(Root, new(i * 2.00f, 0.005f, -4.84f), new(0.18f, 0.018f, 1.64f), _wood);
        }

        Collider(Root, new(0, 1.4f, -8.7f), new(27, 2.8f, 0.22f));
        Collider(Root, new(0, 1.4f, 9.4f), new(27, 2.8f, 0.22f));
        Collider(Root, new(-12.7f, 1.4f, 0.25f), new(0.25f, 2.8f, 19.5f));
        Collider(Root, new(12.7f, 1.4f, 0.25f), new(0.25f, 2.8f, 19.5f));
        Facade(-9.3f, -8.9f, 8.0f, 6.0f, "#8a826c", facing: 1);
        Facade(-0.6f, -8.9f, 9.4f, 7.5f, "#958477", facing: 1);
        Facade(8.9f, -9.2f, 9.8f, 5.8f, "#717e74", facing: 1);
        for (int side = -1; side <= 1; side += 2)
            Box(Root, new(side * 13f, 1.28f, 1.4f), new(0.38f, 2.61f, 17.6f), Plaster("#736b5d", 0.82f));
        ExteriorTram();

        switch (place)
        {
            case Place.Courtyard:
                Facade(-5.4f, 7.8f, 10.4f, 7.1f, "#a29171");
                Facade(5.5f, 8.0f, 11.3f, 6.1f, "#8a795f");
                Door(Fixture.ClubEntry, "КОМПЬЮТЕРНЫЙ КЛУБ", -5.4f, 7.40f, _wood);
                Box(Root, new(-5.4f, 0.07f, 6.94f), new(2.15f, 0.15f, 1.04f), _iron);
                Box(Root, new(3.3f, 0.48f, 6.49f), new(2.23f, 0.12f, 0.45f), _wood, solid: true);
                Sign(Root, "К ОСТАНОВКЕ  ←", new(-3.5f, 1.44f, 7.43f), 180, 0.004f);
                Tree(-10.4f, 4.75f);
                Tree(9.65f, 4.95f);
                break;
            case Place.Market:
                Facade(0, 8.45f, 23.9f, 5.6f, "#92846e");
                Stall(Fixture.UsedCheap, "ПАЛАТКА У ВОРОТ", -4.15f, 3.75f, "#815647");
                Stall(Fixture.UsedTested, "СКУПКА «РАДИОРЯД»", 4.15f, 3.75f, "#4c6669");
                Stall(Fixture.MarketDisks, "ДИСКЕТЫ И ИГРЫ", 0, 6.63f, "#656b46");
                Box(Root, new(0, 4.58f, 8.17f), new(3.5f, 0.63f, 0.22f), _iron);
                Sign(Root, "РАДИОРЫНОК", new(0, 4.58f, 8.048f), 180, 0.0085f);
                break;
            case Place.Shopping:
                Facade(-5.48f, 7.6f, 10.4f, 6.9f, "#8f9279");
                Facade(5.48f, 7.6f, 10.4f, 6.2f, "#a08a75");
                Door(Fixture.PartsEntrance, "РАДИОДЕТАЛЬ", -5.48f, 7.14f, Material("#597070"));
                Door(Fixture.ComputerEntrance, "ЭЛЕКТРОНИКА", 5.48f, 7.14f, Material("#766245"));
                foreach (float x in new[] { -8.25f, -2.73f, 2.73f, 8.25f })
                {
                    Box(Root, new(x, 1.49f, 7.33f), new(1.89f, 1.44f, 0.1f), _iron);
                    Box(Root, new(x, 1.49f, 7.258f), new(1.74f, 1.29f, 0.07f), _glass);
                    Box(Root, new(x, 1.49f, 7.218f), new(0.085f, 1.39f, 0.04f), _cream);
                }
                break;
        }
    }

    private void Facade(float x, float z, float width, float height, string tint, int facing = -1)
    {
        Box(Root, new(x, height / 2, z), new(width, height, 0.46f), Plaster(tint, 0.58f), solid: true);
        Box(Root, new(x, height - 0.22f, z + facing * 0.26f), new(width, 0.21f, 0.22f), _cream);
        Box(Root, new(x, 2.58f, z + facing * 0.24f), new(width, 0.16f, 0.18f), _cream);
        for (float windowX = x - width / 2 + 0.88f; windowX < x + width / 2 - 0.47f; windowX += 1.49f)
        for (float y = 3.55f; y < height - 0.7f; y += 1.65f)
        {
            Box(Root, new(windowX, y, z + facing * 0.244f), new(0.78f, 1.08f, 0.13f), _cream);
            Box(Root, new(windowX, y, z + facing * 0.319f), new(0.64f, 0.93f, 0.04f), _glass);
            Box(Root, new(windowX, y, z + facing * 0.347f), new(0.047f, 0.94f, 0.038f), _wood);
        }
    }

    private void Door(Fixture fixture, string name, float x, float z, Material color)
    {
        Box(Root, new(x, 1.18f, z), new(1.66f, 2.37f, 0.19f), _iron);
        Box(Root, new(x, 1.12f, z - 0.105f), new(1.43f, 2.24f, 0.08f), color);
        Box(Root, new(x - 0.49f, 1.05f, z - 0.166f), new(0.22f, 0.051f, 0.075f), _paper);
        Box(Root, new(x, 2.45f, z - 0.15f), new(2.85f, 0.38f, 0.18f), color);
        Sign(Root, name, new(x, 2.45f, z - 0.25f), 180, 0.0049f);
        Target(Root, fixture, new(x, 1.23f, z - 0.29f), new(1.52f, 1.86f, 0.33f));
    }

    private void Tree(float x, float z)
    {
        Box(Root, new(x, 1.17f, z), new(0.19f, 2.34f, 0.18f), _wood, solid: true);
        var leaves = Material("#776e4a");
        Sphere(Root, new(x, 2.84f, z), new(1.94f, 1.87f, 1.7f), leaves);
        Sphere(Root, new(x + 0.58f, 2.45f, z - 0.28f), new(1.48f, 1.15f, 1.4f), leaves);
    }

    private void ExteriorTram()
    {
        var red = Material("#784a3c");
        Box(Root, new(0, 0.51f, -4.87f), new(9.85f, 1.0f, 2.34f), red, solid: true);
        Box(Root, new(0, 1.91f, -4.87f), new(9.85f, 1.79f, 2.31f), _cream, solid: true);
        Box(Root, new(0, 2.86f, -4.87f), new(9.43f, 0.21f, 2.21f), _iron);
        Box(Root, new(0, 0.98f, -3.687f), new(9.56f, 0.12f, 0.049f), _wood);
        for (int i = -4; i <= 4; i++)
        {
            if (i == 0)
                continue;
            Box(Root, new(i * 1.06f, 1.98f, -3.694f), new(0.86f, 1.03f, 0.055f), _glass);
            Box(Root, new(i * 1.06f, 1.98f, -3.659f), new(0.057f, 1.03f, 0.025f), _iron);
        }
        Box(Root, new(0, 1.42f, -3.662f), new(1.28f, 1.95f, 0.11f), _iron);
        Box(Root, new(-0.30f, 1.42f, -3.586f), new(0.53f, 1.85f, 0.052f), _glass);
        Box(Root, new(0.30f, 1.42f, -3.586f), new(0.53f, 1.85f, 0.052f), _glass);
        Sign(Root, "ТРАМВАЙ", new(0, 2.55f, -3.68f), size: 0.0035f);
        Target(Root, Fixture.TramStop, new(0, 1.36f, -3.39f), new(1.49f, 1.88f, 0.51f));
        Box(Root, new(-5.25f, 1.24f, -2.31f), new(0.08f, 2.48f, 0.08f), _iron);
        Box(Root, new(-5.25f, 2.40f, -2.30f), new(1.28f, 0.32f, 0.085f), _glass);
        Sign(Root, "ОСТАНОВКА", new(-5.25f, 2.40f, -2.25f), size: 0.0033f);
    }

    private void Stall(Fixture fixture, string title, float x, float z, string tint)
    {
        var canvas = Material(tint);
        foreach (float side in new[] { -1.53f, 1.53f })
        {
            Box(Root, new(x + side, 1.19f, z - 0.58f), new(0.075f, 2.38f, 0.075f), _iron, solid: true);
            Box(Root, new(x + side, 1.19f, z + 1.12f), new(0.075f, 2.38f, 0.075f), _iron, solid: true);
        }
        Box(Root, new(x, 2.4f, z + 0.24f), new(3.25f, 0.095f, 1.97f), canvas);
        Box(Root, new(x, 2.27f, z - 0.73f), new(3.25f, 0.28f, 0.075f), canvas);
        Sign(Root, title, new(x, 2.27f, z - 0.781f), 180, 0.0048f);
        Box(Root, new(x, 1.1f, z + 1.13f), new(3.1f, 2.2f, 0.08f), canvas);
        Counter(fixture, x, z, 2.95f, -1);
        Vendor(x + 0.82f, z + 0.71f, tint);
    }

    private void Store(bool parts)
    {
        Light("#222625", "#d7ddd0", 0.71f);
        var paint = Plaster(parts ? "#6c7f76" : "#8b7d67", 0.24f);
        Box(Root, new(0, -0.12f, 0), new(12.4f, 0.23f, 8.7f), Material(parts ? "#535c55" : "#68615a"), solid: true);
        Box(Root, new(0, 3.02f, 0), new(12.5f, 0.23f, 8.8f), _cream);
        foreach (float x in new[] { -6.13f, 6.13f })
            Box(Root, new(x, 1.45f, 0), new(0.22f, 2.97f, 8.6f), paint, solid: true);
        foreach (float z in new[] { -4.22f, 4.22f })
            Box(Root, new(0, 1.45f, z), new(12.35f, 2.97f, 0.25f), paint, solid: true);
        for (int i = -4; i <= 4; i++)
        {
            Box(Root, new(i * 1.32f, 0.003f, 0), new(0.014f, 0.009f, 8.0f), _iron);
            Box(Root, new(0, 0.004f, i * 0.97f), new(12.0f, 0.009f, 0.016f), _iron);
        }
        foreach (float x in new[] { -3.1f, 0, 3.1f })
        {
            Box(Root, new(x, 2.84f, -1.44f), new(1.63f, 0.073f, 0.24f), Material("#cdded2", lit: true));
            Root.AddChild(new OmniLight3D { Position = new(x, 2.72f, -1.44f), LightColor = new Color("#e9ede0"), LightEnergy = 0.86f, OmniRange = 6.5f });
        }
        Box(Root, new(0, 1.15f, 4.0f), new(1.65f, 2.30f, 0.14f), _iron);
        Sign(Root, "НА УЛИЦУ", new(0, 1.88f, 3.91f), 180, 0.0036f);
        Target(Root, Fixture.ShopExit, new(0, 1.25f, 3.8f), new(1.58f, 1.82f, 0.32f));
        Box(Root, new(0, 2.10f, -4.04f), new(4.25f, 0.57f, 0.16f), _iron);
        Sign(Root, parts ? "РАДИОДЕТАЛЬ  /  КОМПЛЕКТУЮЩИЕ" : "ЭЛЕКТРОНИКА  /  ГОТОВЫЕ ПК", new(0, 2.10f, -3.95f), size: 0.0056f);
        Counter(parts ? Fixture.PartsKit : Fixture.ComputerPc, -3.53f, -1.42f, 2.48f, 1);
        Counter(parts ? Fixture.PartsDisks : Fixture.ComputerDisks, 3.33f, -1.42f, 2.35f, 1);
        if (parts)
            Counter(Fixture.PartsPc, 0, -2.77f, 1.99f, 1);
        Vendor(1.30f, -3.49f, parts ? "#526651" : "#6c535a");
        for (int i = -2; i <= 2; i++)
        {
            Box(Root, new(i * 1.11f, 0.86f, -3.89f), new(0.85f, 0.055f, 0.31f), _wood);
            Box(Root, new(i * 1.11f, 1.01f, -3.89f), new(0.42f, 0.26f, 0.23f), _paper);
        }
    }

    private void Counter(Fixture fixture, float x, float z, float width, int facing)
    {
        var offer = MarketStock.Get(fixture);
        Box(Root, new(x, 0.48f, z), new(width - 0.12f, 0.95f, 0.69f), _wood, solid: true);
        Box(Root, new(x, 0.995f, z), new(width, 0.11f, 0.80f), _cream);
        if (offer.Hardware is Purchase.Used or Purchase.Retail)
        {
            Box(Root, new(x - 0.14f, 1.29f, z), new(0.59f, 0.47f, 0.49f), _cream);
            Box(Root, new(x - 0.14f, 1.30f, z + facing * 0.256f), new(0.456f, 0.334f, 0.035f), _glass);
            Box(Root, new(x + 0.39f, 1.23f, z), new(0.25f, 0.37f, 0.41f), _cream);
        }
        else
        {
            float height = offer.Hardware == null ? 0.15f : 0.31f;
            Box(Root, new(x, 1.05f + height * 0.5f, z), new(0.56f, height, 0.39f), _paper);
            Box(Root, new(x, 1.052f + height, z), new(0.075f, 0.013f, 0.39f), _cream);
        }
        float yaw = facing == 1 ? 0 : 180;
        Box(Root, new(x, 0.76f, z + facing * 0.375f), new(Mathf.Min(width - 0.08f, 1.67f), 0.27f, 0.054f), _iron);
        Sign(Root, $"{offer.Name}\n{ClubLedger.Money(offer.PriceOn(_day))}", new(x, 0.76f, z + facing * 0.413f), yaw, 0.0036f);
        Target(Root, fixture, new(x, 1.19f, z + facing * 0.24f), new(width - 0.09f, 0.98f, 0.57f));
    }

    private void Vendor(float x, float z, string jacket)
    {
        var clothing = Material(jacket);
        Sphere(Root, new(x, 1.18f, z), new(0.43f, 0.71f, 0.31f), clothing);
        Sphere(Root, new(x, 1.68f, z), new(0.25f, 0.31f, 0.24f), Material("#ac856a"));
        Sphere(Root, new(x, 1.76f, z), new(0.26f, 0.16f, 0.25f), _iron);
        foreach (float offset in new[] { -0.13f, 0.13f })
            Box(Root, new(x + offset, 0.43f, z), new(0.16f, 0.85f, 0.19f), _iron);
    }

    private void TramInterior()
    {
        Light("#95a1a2", "#eee1c5", 0.91f);
        Box(Root, new(0, -0.11f, 0), new(2.9f, 0.22f, 11.4f), _iron);
        Box(Root, new(0, 2.61f, 0), new(3.0f, 0.21f, 11.4f), _cream);
        Box(Root, new(0, 1.28f, -5.64f), new(2.9f, 2.54f, 0.12f), _cream);
        Box(Root, new(0, 1.28f, 5.64f), new(2.9f, 2.54f, 0.12f), _cream);
        Box(Root, new(0, 1.12f, -5.55f), new(1.12f, 2.24f, 0.059f), _wood);
        Box(Root, new(0, 1.65f, -5.51f), new(0.73f, 0.53f, 0.044f), _glass);
        foreach (float z in new[] { -3.6f, 0, 3.6f })
            Box(Root, new(-0.45f, 2.48f, z), new(0.19f, 0.058f, 1.24f), Material("#d7cba9", lit: true));
        Sign(Root, "ВЫХОД  /  ДЕРЖИТЕСЬ ЗА ПОРУЧНИ", new(0, 2.32f, -5.50f), size: 0.0036f);
        for (int side = -1; side <= 1; side += 2)
        {
            Box(Root, new(side * 1.47f, 0.49f, 0), new(0.15f, 0.99f, 11.4f), _wood);
            Box(Root, new(side * 1.47f, 2.38f, 0), new(0.15f, 0.42f, 11.4f), _cream);
            Box(Root, new(side * 1.43f, 1.07f, 0), new(0.11f, 0.048f, 11.32f), _cream);
            Box(Root, new(side * 1.43f, 2.12f, 0), new(0.12f, 0.048f, 11.32f), _iron);
            for (int i = -2; i <= 2; i++)
            {
                Box(Root, new(side * 1.45f, 1.72f, i * 2.0f), new(0.14f, 1.51f, 0.14f), _cream);
                Box(Root, new(side * 1.06f, 0.46f, i * 2.0f), new(0.62f, 0.14f, 0.61f), _wood);
                Box(Root, new(side * 1.06f, 0.76f, i * 2.0f + 0.32f), new(0.62f, 0.53f, 0.12f), _wood);
            }
        }
        Box(Root, new(0.45f, 2.34f, 0), new(0.036f, 0.036f, 10.2f), _iron);
        _passingBuildings = new Node3D { Name = "PassingBuildings" };
        Root.AddChild(_passingBuildings);
        var colors = new[] { Material("#818779"), Material("#af9f86"), Material("#9e7869") };
        for (int i = 0; i < 12; i++)
        for (int side = -1; side <= 1; side += 2)
        {
            float z = 7 - i * 6.8f;
            Box(_passingBuildings, new(side * 4.65f, 1.7f, z), new(1.3f, 5f + i % 2, 5.94f), colors[i % colors.Length]);
            for (int window = -1; window <= 1; window++)
                Box(_passingBuildings, new(side * 3.973f, 1.96f, z + window * 1.61f), new(0.041f, 0.91f, 0.60f), _glass);
        }
    }
}
