using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static ComputerLive.ClubLedger;

namespace ComputerLive;

public partial class ClubGame : Node3D
{
    private const float Reach = 3.4f;
    private const float RideDuration = 3.8f;
    private static readonly Vector3 Spawn = new(0, 0.94f, 2.8f);
    private static readonly string[] AssemblyTasks =
    [
        "Закрепить системную плату на стойках",
        "Установить процессор и радиатор",
        "Установить память, подключить питание и дисковод",
        "Подать питание и проверить запуск",
    ];

    private ClubLedger _club = new();
    private ClubInterface _ui = null!;
    private WorldSound _sound = null!;
    private Basement _basement = null!;
    private CharacterBody3D _player = null!;
    private Camera3D _camera = null!;
    private MeshInstance3D _carriedBox = null!;
    private Place _place = Place.Club;
    private Place? _tripDestination;
    private City? _city;
    private float _rideTime;
    private readonly Dictionary<Fixture, StockOffer> _quotes = new();
    private readonly HashSet<Fixture> _inspected = new();
    private Fixture? _target;
    private string _result = "";
    private double _visitorTime;

    public override void _Ready()
    {
        _basement = new Basement(this);
        _player = new CharacterBody3D { Name = "Player", Position = Spawn };
        AddChild(_player);
        _player.AddChild(new CollisionShape3D { Shape = new CapsuleShape3D { Height = 1.80f, Radius = 0.32f } });
        _camera = new Camera3D { Name = "View", Position = new Vector3(0, 0.68f, 0), Current = true, Fov = 71 };
        _player.AddChild(_camera);
        _carriedBox = WorldGeometry.Box(_camera, new(0.26f, -0.43f, -0.78f), new(0.53f, 0.37f, 0.42f), WorldGeometry.Material("#ab8358"));
        _carriedBox.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        var tape = WorldGeometry.Box(_carriedBox, new(0, 0.187f, 0), new(0.10f, 0.008f, 0.42f), WorldGeometry.Material("#d4ba87"));
        tape.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _carriedBox.RotationDegrees = new(2, -8, 1);
        _ui = new ClubInterface(this);
        _sound = new WorldSound(this);
        Restart();
    }

    public override void _ExitTree()
    {
        if (_basement != null && _basement.Root.GetParent() == null)
            _basement.Root.Free();
    }

    public override void _Input(InputEvent input)
    {
        if (_place == Place.Tram || input is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        if (key.Keycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            if (_ui.IsOpen)
                CloseMenu();
            else
                PauseMenu();
        }
        if (key.Keycode == Key.Tab && !_ui.IsOpen)
        {
            GetViewport().SetInputAsHandled();
            Notebook();
        }
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (_ui.IsOpen)
            return;

        if (input is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _player.RotateY(-motion.ScreenRelative.X * 0.0022f);
            float pitch = Mathf.Clamp(_camera.Rotation.X - motion.ScreenRelative.Y * 0.0022f, -1.44f, 1.44f);
            _camera.Rotation = new Vector3(pitch, 0, 0);
        }

        if (_place != Place.Tram && input is InputEventKey { Pressed: true, Echo: false } key &&
            (key.PhysicalKeycode == Key.E || key.Keycode == Key.E) && _target is { } fixture)
            Interact(fixture);
    }

    public override void _Process(double delta)
    {
        if (_place == Place.Club)
            _basement.Television.Advance(delta, _club.Day);

        if (_tripDestination is not { } destination)
            return;

        _rideTime += (float)delta;
        _city?.AdvanceRide(_rideTime * 17.0f);
        var look = _camera.Rotation;
        look.Z = Mathf.Sin(_rideTime * 5) * 0.024f;
        _camera.Rotation = look;
        if (_rideTime < RideDuration)
            return;

        _tripDestination = null;
        _result = destination == Place.Courtyard ? "Трамвай пришёл во двор клуба." : $"Остановка «{City.NameOf(destination)}». Обратный билет уже оплачен.";
        Enter(destination);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_ui.IsOpen || _place == Place.Tram)
            return;

        var direction = new Vector3(
            Axis(Key.A, Key.D, Key.Left, Key.Right),
            0,
            Axis(Key.W, Key.S, Key.Up, Key.Down));
        var motion = _player.Basis * direction.Normalized() * (_club.CarriedComputer == null ? 3.8f : 3.25f);
        motion.Y = _player.IsOnFloor() ? -0.1f : _player.Velocity.Y - 14 * (float)delta;
        _player.Velocity = motion;
        _player.MoveAndSlide();
        var move = _player.GetPositionDelta();
        _sound.Footsteps(new Vector2(move.X, move.Z).Length(), _place is Place.Courtyard or Place.Market or Place.Shopping);

        var from = _camera.GlobalPosition;
        var query = PhysicsRayQueryParameters3D.Create(from, from - _camera.GlobalBasis.Z * Reach, 3);
        query.CollideWithAreas = true;
        query.Exclude = [_player.GetRid()];
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        _target = hit.Count > 0 && hit["collider"].AsGodotObject() is Area3D area && area.HasMeta("fixture")
            ? (Fixture)area.GetMeta("fixture").AsInt32()
            : null;
        _ui.Target(_target is { } fixture ? FixtureName(fixture) : null, corner: _target == Fixture.Television);

        if (_visitorTime > 0 && (_visitorTime -= delta) <= 0)
            _basement.HideVisitor();
    }

    private static int Axis(Key negative, Key positive, Key alternateNegative, Key alternatePositive) =>
        (Input.IsPhysicalKeyPressed(positive) || Input.IsPhysicalKeyPressed(alternatePositive) ? 1 : 0) -
        (Input.IsPhysicalKeyPressed(negative) || Input.IsPhysicalKeyPressed(alternateNegative) ? 1 : 0);

    private void Restart()
    {
        _club = new ClubLedger();
        _result = "";
        _target = null;
        _tripDestination = null;
        _quotes.Clear();
        _inspected.Clear();
        _visitorTime = 0;
        _basement.HideVisitor();
        _basement.Television.Power(false, _club.Day);
        Enter(Place.Club, restart: true);
        Open("Компьютерный клуб", "Петербург, 1993 год. У вас есть деньги на первый компьютер и подвал на примете. Осмотрите помещение, подпишите договор и проведите первую неделю.\n\nWASD или стрелки — ходить. Мышь — осмотреться. E — взаимодействовать. Esc — меню.",
            [new("Осмотреть подвал", CloseMenu)]);
    }

    private void Interact(Fixture fixture)
    {
        if (MarketStock.TryGet(fixture, out var offer))
        {
            OfferMenu(offer);
            return;
        }

        switch (fixture)
        {
            case Fixture.Lease: LeaseMenu(); break;
            case Fixture.Phone: Notebook(); break;
            case Fixture.Bench: Bench(); break;
            case Fixture.Desk: Desk(); break;
            case Fixture.Television: _basement.Television.Power(!_basement.Television.On, _club.Day); break;
            case Fixture.ClubExit: Enter(Place.Courtyard); break;
            case Fixture.ClubEntry: Enter(Place.Club); break;
            case Fixture.TramStop: TramMenu(); break;
            case Fixture.PartsEntrance: Enter(Place.PartsStore); break;
            case Fixture.ComputerEntrance: Enter(Place.ComputerStore); break;
            case Fixture.ShopExit: Enter(Place.Shopping); break;
            case Fixture.Station1 or Fixture.Station2 or Fixture.Station3: UseStation(fixture - Fixture.Station1); break;
        }
    }

    private void LeaseMenu()
    {
        if (_club.Leased)
        {
            Open("Подвал арендован", $"Внесён залог {Money(RentDeposit)} Аренда за неделю — {Money(WeeklyRent)} Счёт можно оплатить в журнале смен после седьмого дня.", []);
            return;
        }

        Open("Договор аренды", $"Подвал с тремя рабочими местами. Залог сегодня — {Money(RentDeposit)} Аренда в конце недели — {Money(WeeklyRent)} Электричество и вода оплачиваются по расходу.",
            [new($"Подписать и внести залог   {Money(RentDeposit)}", () =>
            {
                if (_club.Lease())
                    _result = "Ключи у вас. Выйдите во двор и сядьте на трамвай до рынка или магазинов.";
                Refresh();
                CloseMenu();
            }, _club.Cash >= RentDeposit)]);
    }

    private void Enter(Place place, bool restart = false)
    {
        var previous = _place;
        if (_city != null)
        {
            RemoveChild(_city.Root);
            _city.Root.QueueFree();
            _city = null;
        }

        if (place == Place.Club)
        {
            if (_basement.Root.GetParent() == null)
                AddChild(_basement.Root);
        }
        else
        {
            if (_basement.Root.GetParent() != null)
                RemoveChild(_basement.Root);
            _city = new City(place);
            AddChild(_city.Root);
        }

        (Vector3 Position, float Yaw) arrival = (place, previous) switch
        {
            (Place.Club, _) => (restart ? Spawn : new(0.88f, 0.94f, 3.17f), 0),
            (Place.Courtyard, Place.Club) => (new(-5.4f, 0.94f, 6.00f), -0.37f),
            (Place.Shopping, Place.PartsStore) => (new(-5.48f, 0.94f, 5.63f), 0),
            (Place.Shopping, Place.ComputerStore) => (new(5.48f, 0.94f, 5.63f), 0),
            (Place.PartsStore or Place.ComputerStore, _) => (new(0, 0.94f, 2.68f), 0),
            (Place.Tram, _) => (new(0, 0.94f, 3.38f), -0.40f),
            _ => (new(0, 0.94f, -1.52f), Mathf.Pi),
        };
        _place = place;
        _sound.ChangeLocation(place);
        _player.Position = arrival.Position;
        _player.Rotation = new Vector3(0, arrival.Yaw, 0);
        _camera.Rotation = Vector3.Zero;
        _player.Velocity = Vector3.Zero;
        _target = null;
        CloseMenu();
        _ui.Target(null);
        Refresh();
    }

    private void TramMenu()
    {
        if (_place != Place.Courtyard)
        {
            Open("Трамвай до клуба", "Обратный проезд входит в билет. Коробку можно взять с собой.",
                [new("Сесть в трамвай", () => TakeTram(Place.Courtyard))]);
            return;
        }

        bool available = !_club.ReviewDue && _club.Cash >= MarketStock.ReturnTicket;
        Open("Трамвайная остановка", $"Билет туда и обратно стоит {Money(MarketStock.ReturnTicket)}\nНа радиорынке продают б/у ПК и дискеты. На торговой улице есть два магазина.",
            [new("Поехать на радиорынок", () => TakeTram(Place.Market), available), new("Поехать на торговую улицу", () => TakeTram(Place.Shopping), available)]);
    }

    private void TakeTram(Place destination)
    {
        if (_place == Place.Courtyard && !_club.BuyTicket())
            return;

        _rideTime = 0;
        _tripDestination = destination;
        _result = $"Следующая остановка — {City.NameOf(destination)}.";
        Enter(Place.Tram);
    }

    private void Notebook()
    {
        var quotes = _quotes.Values.OrderBy(offer => offer.Name).ThenBy(offer => offer.Price).Select(offer =>
            $"{offer.Name} · {Money(offer.Price)} · {offer.Seller}" + (offer.WornFan && _inspected.Contains(offer.Fixture) ? " · изношен вентилятор" : ""));
        string description = _quotes.Count == 0
            ? "Подойдите к прилавку и осмотрите товар. Цена и имя продавца появятся здесь. Блокнот можно открыть клавишей Tab."
            : string.Join("\n", quotes);
        string inventory = $"В сумке лицензионных комплектов — {_club.LicenseDisks}, пиратских — {_club.CopiedDisks}.";
        Open("Блокнот / цены и адреса", description + $"\n\n{inventory}\nРадиорынок — скупка и дискеты. Торговая улица — «Радиодеталь» и «Электроника».", []);
    }

    private void OfferMenu(StockOffer offer)
    {
        _quotes[offer.Fixture] = offer;
        string details = _inspected.Contains(offer.Fixture) ? offer.Details : "Можно осмотреть товар у прилавка перед покупкой.";
        if (offer.Hardware != null && _club.CarriedComputer != null)
            details += "\nУ вас уже есть коробка. Отнесите её в клуб, чтобы освободить руки.";
        else if (offer.Hardware != null && _club.Stations.All(station => station.Hardware != Hardware.Empty))
            details += "\nВ клубе заняты все рабочие места.";
        else if (!_club.Leased)
            details += "\nПодпишите договор аренды, чтобы подготовить место для покупки.";

        var choices = new List<Choice>();
        if (!_inspected.Contains(offer.Fixture))
            choices.Add(new("Осмотреть товар", () =>
            {
                _inspected.Add(offer.Fixture);
                OfferMenu(offer);
            }));
        choices.Add(new($"Купить   {Money(offer.Price)}", () =>
        {
            if (!_club.Buy(offer))
                return;

            _result = offer.Hardware != null ? "Коробка у вас. Вернитесь в клуб и поставьте её на свободное место." :
                $"В сумке — {offer.Name.ToLowerInvariant()}. Используйте дискеты у компьютера в клубе.";
            Refresh();
            CloseMenu();
        }, _club.CanBuy(offer)));
        Open(offer.Name, $"{offer.Seller} · {Money(offer.Price)}\n{details}", choices);
    }

    private void Bench()
    {
        var choices = new List<Choice>();
        for (int i = 0; i < _club.Stations.Length; i++)
        {
            int index = i;
            if (!_club.ReviewDue && _club.Stations[i].Hardware is Hardware.Kit or Hardware.Broken)
                choices.Add(new Choice($"Открыть комплект для места {Number(i)}", () => AssembleMenu(index)));
        }

        Open("Верстак", choices.Count > 0 ? "Подготовьте комплект к запуску." : "Здесь можно собрать компьютер из комплекта или заменить сгоревшую плату.", choices);
    }

    private void AssembleMenu(int index)
    {
        var station = _club.Stations[index];
        if (station.Hardware == Hardware.Broken)
        {
            Open($"Верстак / место {Number(index)}", "Плата сгорела при запуске незавершённой сборки. Замените её, чтобы собрать компьютер заново.",
                [new($"Заменить плату   {Money(Repair)}", () =>
                {
                    _club.RepairStation(station);
                    Refresh();
                    AssembleMenu(index);
                }, _club.Cash >= Repair)]);
            return;
        }

        var choices = new List<Choice>
        {
            new(AssemblyTasks[station.AssemblyStep], () =>
            {
                _club.Assemble(station);
                if (station.Hardware == Hardware.Working)
                {
                    _result = $"POST пройден. Система устанавливается у компьютера на месте {Number(index)}.";
                    Refresh();
                    CloseMenu();
                }
                else
                {
                    Refresh();
                    AssembleMenu(index);
                }
            }),
        };
        if (station.AssemblyStep < 3)
            choices.Add(new("Подать питание сейчас (риск поломки)", () =>
            {
                _club.Assemble(station, testEarly: true);
                Refresh();
                AssembleMenu(index);
            }));

        Open($"Верстак / место {Number(index)}", $"Этап {station.AssemblyStep + 1} из {AssemblyTasks.Length}. Перед пробным запуском проверьте крепления и подключение.", choices);
    }

    private void UseStation(int index)
    {
        var station = _club.Stations[index];
        if (!_club.Unpack(station))
        {
            Computer(index);
            return;
        }

        _result = station.Hardware == Hardware.Kit ? "Комплект на столе. Соберите его на верстаке." :
            $"ПК на месте {Number(index)}. Установите систему с купленных дискет.";
        Refresh();
    }

    private void Computer(int index)
    {
        var station = _club.Stations[index];
        string title = $"Компьютер / место {Number(index)}";
        if (station.Hardware != Hardware.Working || _club.ReviewDue)
        {
            string status = _club.ReviewDue ? "Неделя закончилась. Подведите итог в журнале смен." : station.Hardware switch
            {
                Hardware.Empty => "Стол пуст. Купите компьютер или комплект в городе и принесите коробку сюда.",
                _ => "Комплект ждёт на верстаке.",
            };
            Open(title, status, []);
            return;
        }

        var choices = new List<Choice>();
        string description = station.Software switch
        {
            Software.None when _club.LicenseDisks + _club.CopiedDisks == 0 => "Нужны дискеты с системой и играми. Лицензии продают в обоих магазинах, а дешёвые копии есть на радиорынке.",
            Software.None => "Выберите купленные дискеты. Лицензионный комплект надёжен. Пиратская сборка может зависнуть во время смены.",
            _ when station.NeedsService => "ПК отключили в последнюю смену. Перезапустите его и проверьте, что игра запускается.",
            _ when !station.Configured => "Система установлена. Настройте звук и запустите игру для проверки.",
            _ => "Игры запускаются. Место готово принять посетителя.",
        };

        if (station.Software != Software.None && !station.Ready)
            choices.Add(new(station.NeedsService ? "Перезагрузить и проверить игру" : "Настроить звук и запустить игру", () =>
            {
                _club.Configure(station);
                _result = $"Место {Number(index)} готово. Откройте смену в журнале у входа слева.";
                Refresh();
                CloseMenu();
            }));
        if (station.Software != Software.Licensed && _club.LicenseDisks > 0)
            choices.Add(InstallChoice(index, Software.Licensed, "Установить с лицензионных дискет"));
        if (station.Software == Software.None && _club.CopiedDisks > 0)
            choices.Add(InstallChoice(index, Software.Copied, "Установить с пиратских дискет"));

        Open(title, description, choices);
    }

    private Choice InstallChoice(int index, Software software, string title) =>
        new(title, () =>
        {
            _club.Install(_club.Stations[index], software);
            Refresh();
            Computer(index);
        });

    private void Desk()
    {
        if (!_club.Leased)
        {
            Open("Журнал смен", "Подпишите договор аренды у двери, чтобы начать работу.", []);
            return;
        }

        if (_club.ReviewDue)
        {
            Review();
            return;
        }

        int ready = _club.Stations.Count(station => station.Ready);
        string room = _club.Renovated ? "Стены покрашены, помещение выглядит ухоженным." :
            "Запущенное помещение отпугивает тех, кто готов платить дороже.";
        string last = _club.LastShift is { } shift ? $"\nДень {shift.Day} — посетителей {shift.Visitors}, выручка {Money(shift.Revenue)} {shift.Incidents}" : "";

        var choices = new List<Choice>
        {
            new($"Открыть смену на {ShiftHours} часов", () => RunShift(true), ready > 0),
            new("Пропустить день без открытия", () => RunShift(false)),
            new("Снизить тариф на 400 р.", () => Rate(-400), _club.Rate > 800),
            new("Поднять тариф на 400 р.", () => Rate(400), _club.Rate < 8_000),
        };
        if (!_club.Renovated)
            choices.Add(new($"Покрасить стены   {Money(Renovation)}", () =>
            {
                _club.Renovate();
                Refresh();
                Desk();
            }, _club.Cash >= Renovation));

        Open($"Журнал смен / день {_club.Day}", $"Тариф за час — {Money(_club.Rate)} Готовых мест {ready} из {_club.Stations.Length}.\n{room}\nПредстоящий платёж от {Money(_club.AmountDue)}{last}", choices);
    }

    private void Rate(int delta)
    {
        _club.ChangeRate(delta);
        Desk();
    }

    private void RunShift(bool open)
    {
        if (_club.RunShift(open) is not { } shift)
            return;

        _basement.HideVisitor();
        _visitorTime = 0;
        _result = open ? $"День {shift.Day}. Посетителей {shift.Visitors}   Выручка {Money(shift.Revenue)}" :
            $"День {shift.Day}. Клуб был закрыт.";
        if (shift.Visitors > 0)
        {
            _basement.ShowVisitor(Array.FindIndex(_club.Stations, station => station.Ready));
            _visitorTime = 18;
        }
        if (shift.Incidents.Length != 0)
            _result += $"   {shift.Incidents}";

        Refresh();
        CloseMenu();
    }

    private void Review()
    {
        string description = $"Посетителей за неделю — {_club.Visitors}. Выручка — {Money(_club.Revenue)}\n" +
            $"Аренда {Money(WeeklyRent)} · Свет {Money(_club.ElectricityBill)} ({_club.ElectricityKwh:0.00} кВт·ч) · " +
            $"Вода {Money(_club.WaterBill)} ({_club.WaterCubicMeters:0.000} м³).\nК оплате {Money(_club.AmountDue)}";
        if (_club.Settled)
        {
            Open(_club.Bankrupt ? "Банкротство" : "Первая неделя позади", description + (_club.Bankrupt
                ? "\nВ кассе не хватило денег на обязательные платежи."
                : $"\nСчета оплачены. В кассе осталось {Money(_club.Cash)}"), [new("Начать неделю заново", Restart)]);
            return;
        }

        Open("Итоги недели", description, [new(_club.Cash >= _club.AmountDue ? "Оплатить счета" : "Закрыть неделю с долгом", () =>
        {
            _club.Settle();
            Refresh();
            Review();
        })]);
    }

    private void PauseMenu() => Open("Пауза", "WASD или стрелки — ходить. Мышь — осмотреться. E — взаимодействовать. Tab — блокнот.",
        [new("Продолжить", CloseMenu), new("Начать неделю заново", Restart), new("Выйти из игры", () => GetTree().Quit())]);

    private void Open(string title, string description, IReadOnlyList<Choice> choices)
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _ui.Show(title, description, choices, CloseMenu);
    }

    private void CloseMenu()
    {
        _ui.Close();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void Refresh()
    {
        _basement.Update(_club);
        _carriedBox.Visible = _club.CarriedComputer != null;
        string objective = _place switch
        {
            Place.Tram => "В пути",
            Place.Courtyard => _club.CarriedComputer != null ? "Отнесите коробку в клуб. Вход во дворе." : "Сядьте на трамвай к рынку или магазинам. Вход в клуб во дворе.",
            Place.Market or Place.Shopping when _club.CarriedComputer != null => "Отвезите коробку в клуб. Обратный проезд уже оплачен.",
            Place.Market => "Осмотрите б/у компьютеры и дискеты. Цены сохраняются в блокноте (Tab).",
            Place.Shopping => "Сравните цены в «Радиодетали» и «Электронике». Блокнот — Tab.",
            Place.PartsStore or Place.ComputerStore => "Подойдите к товару или вернитесь на улицу через дверь.",
            _ => ClubObjective(),
        };
        _ui.Update(_club, City.NameOf(_place), objective, _result);
    }

    private string ClubObjective() => _club.Settled ? "Неделя закончена. Итоги — в журнале смен." :
            _club.ReviewDue ? "Подведите итоги в журнале смен у входа слева." :
            !_club.Leased ? "Подпишите договор аренды у двери на левой стене." :
            _club.CarriedComputer != null ? "Поставьте коробку на свободный стол у дальней стены." :
            _club.Stations.Any(station => station.Ready) ? "Откройте смену через журнал у входа слева." :
            _club.Stations.Any(station => station.Hardware == Hardware.Working && station.Software == Software.None) && _club.LicenseDisks + _club.CopiedDisks == 0 ?
                "Купите дискеты на рынке или в магазине. Выход во двор — у входа." :
            _club.Stations.Any(station => station.Hardware == Hardware.Working) ? "Настройте ПК на рабочих местах у дальней стены." :
            _club.Stations.Any(station => station.Hardware is Hardware.Kit or Hardware.Broken) ? "Подойдите к верстаку у дальней стены." :
            "Выйдите во двор и сядьте на трамвай к рынку или магазинам.";

    private static string Number(int index) => $"0{index + 1}";

    private string FixtureName(Fixture fixture)
    {
        if (MarketStock.TryGet(fixture, out var offer))
            return $"{offer.Name} · {Money(offer.Price)}";

        int index = fixture - Fixture.Station1;
        return fixture switch
        {
            Fixture.Lease => "Договор аренды",
            Fixture.Phone => "Блокнот с ценами и адресами",
            Fixture.Bench => "Верстак",
            Fixture.Desk => "Журнал смен",
            Fixture.Television => _basement.Television.On ? "Выключить телевизор" : "Включить телевизор",
            Fixture.ClubExit => "Выйти во двор",
            Fixture.ClubEntry => "Войти в клуб",
            Fixture.TramStop => "Сесть в трамвай",
            Fixture.PartsEntrance => "Войти в «Радиодеталь»",
            Fixture.ComputerEntrance => "Войти в «Электронику»",
            Fixture.ShopExit => "Выйти на улицу",
            _ when _club.CarriedComputer != null && _club.Stations[index].Hardware == Hardware.Empty && !_club.ReviewDue => $"Поставить коробку на место {Number(index)}",
            _ => $"Рабочее место {Number(index)}",
        };
    }
}
