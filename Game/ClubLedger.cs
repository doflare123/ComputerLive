using System;
using System.Globalization;
using System.Linq;

namespace ComputerLive;

internal enum Purchase { Kit, Used, Retail }
internal enum Hardware { Empty, Kit, Working, Broken }
internal enum Software { None, Licensed, Copied }

internal sealed class Station
{
    public Hardware Hardware { get; set; }
    public Purchase Origin { get; set; }
    public Software Software { get; set; }
    public int AssemblyStep { get; set; }
    public bool Configured { get; set; }
    public bool NeedsService { get; set; }
    public bool WornFan { get; set; }
    public bool Ready => Hardware == Hardware.Working && Configured && !NeedsService;
    public int BookableHours => Origin == Purchase.Used ? 4 : ClubLedger.ShiftHours;
    public (decimal Idle, decimal Active) PowerKilowatts => Origin switch
    {
        Purchase.Used => (0.12m, 0.21m),
        Purchase.Retail => (0.075m, 0.15m),
        _ => (0.09m, 0.18m),
    };
}

internal sealed record Shift(CampaignDay Day, int Visitors, int Revenue, string Incidents);

internal sealed class ClubLedger
{
    public const int RentDeposit = 45_000;
    public const int WeeklyRent = 43_000;
    public const int Renovation = 32_000;
    // Цена платы без процессора по объявлению KC Computers от 27 июля 1993 года, ориентир для ремонта.
    private const int ReplacementBoardDollars = 110;
    public const int MaxDay = 7;
    public const int ShiftHours = 6;
    public const int RateStep = 100;
    public const int MinimumRate = RateStep;
    public const int MaximumRate = 2_000;
    // Игровой ориентир: около 1% средней зарплаты за май 1993 года (37 505 р., Госкомстат РФ).
    private const int StartingRate = 400;
    // Два комплекта и лицензии по максимальной цене недели, залог и запас на первый счёт.
    private const int StartingCash = 2_350_000;

    private readonly Random _random = new(1993);
    private static readonly CultureInfo RubleFormat = CultureInfo.GetCultureInfo("ru-RU");

    public Station[] Stations { get; } = [new(), new(), new()];
    public int Cash { get; private set; } = StartingCash;
    public int Rate { get; private set; } = StartingRate;
    public int Day { get; private set; } = 1;
    public CampaignDay Today => new(CampaignDay.FirstDate.AddDays(Day - 1));
    public int ReturnTicket => Today.ReturnTicket;
    public int BoardReplacementCost => ReplacementBoardDollars * Today.RublesPerDollar;
    public int ExpectedVisitors => DemandFor(Today);
    public bool Leased { get; private set; }
    public bool Renovated { get; private set; }
    public StockOffer? CarriedComputer { get; private set; }
    public int LicenseDisks { get; private set; }
    public int CopiedDisks { get; private set; }
    public int Visitors { get; private set; }
    public int Revenue { get; private set; }
    public decimal ElectricityKwh { get; private set; }
    public decimal WaterCubicMeters { get; private set; }
    public int ElectricityBill => (int)Math.Ceiling(ElectricityKwh * 235m);
    public int WaterBill => (int)Math.Ceiling(WaterCubicMeters * 720m);
    public int AmountDue => WeeklyRent + ElectricityBill + WaterBill;
    public Shift? LastShift { get; private set; }
    public bool ReviewDue => Day > MaxDay;
    public bool Settled { get; private set; }
    public bool Bankrupt { get; private set; }

    public bool Lease()
    {
        if (Leased || !Spend(RentDeposit))
            return false;

        Leased = true;
        return true;
    }

    public bool BuyTicket() => !ReviewDue && Spend(ReturnTicket);

    public bool CanBuy(StockOffer offer) => Leased && !ReviewDue && Cash >= offer.PriceOn(Today) &&
        (offer.Hardware == null || (CarriedComputer == null && Stations.Any(station => station.Hardware == Hardware.Empty)));

    public bool Buy(StockOffer offer)
    {
        if (!CanBuy(offer) || !Spend(offer.PriceOn(Today)))
            return false;

        if (offer.Hardware != null)
            CarriedComputer = offer;
        else if (offer.Software == Software.Licensed)
            LicenseDisks++;
        else
            CopiedDisks++;
        return true;
    }

    public bool Unpack(Station station)
    {
        if (ReviewDue || station.Hardware != Hardware.Empty || CarriedComputer?.Hardware is not { } purchase)
            return false;

        station.Origin = purchase;
        station.Hardware = purchase == Purchase.Kit ? Hardware.Kit : Hardware.Working;
        station.WornFan = CarriedComputer.WornFan;
        CarriedComputer = null;
        return true;
    }

    public void Assemble(Station station, bool testEarly = false)
    {
        if (ReviewDue || station.Hardware != Hardware.Kit)
            return;

        if (testEarly && station.AssemblyStep < 3)
        {
            station.Hardware = Hardware.Broken;
            return;
        }

        if (++station.AssemblyStep == 4)
            station.Hardware = Hardware.Working;
    }

    public bool RepairStation(Station station)
    {
        if (ReviewDue || station.Hardware != Hardware.Broken || !Spend(BoardReplacementCost))
            return false;

        station.Hardware = Hardware.Kit;
        station.AssemblyStep = 0;
        return true;
    }

    public bool Install(Station station, Software software)
    {
        if (ReviewDue || station.Hardware != Hardware.Working || software == Software.None || station.Software == software ||
            (software == Software.Licensed ? LicenseDisks : CopiedDisks) == 0)
            return false;

        if (software == Software.Licensed)
            LicenseDisks--;
        else
            CopiedDisks--;
        station.Software = software;
        station.Configured = false;
        station.NeedsService = false;
        return true;
    }

    public void Configure(Station station)
    {
        if (!ReviewDue && station.Hardware == Hardware.Working && station.Software != Software.None)
        {
            station.Configured = true;
            station.NeedsService = false;
        }
    }

    public bool Renovate()
    {
        if (!Leased || ReviewDue || Renovated || !Spend(Renovation))
            return false;

        Renovated = true;
        return true;
    }

    public void ChangeRate(int amount) => Rate = (int)Math.Clamp((long)Rate + amount, MinimumRate, MaximumRate);

    private int DemandFor(CampaignDay day)
    {
        int budgetVisitors = Rate <= StartingRate ? (day.IsWeekend ? 12 : 8) : 0;
        int regularVisitors = Rate <= 2 * StartingRate ? (day.IsWeekend ? 12 : 10) : 0;
        int premiumVisitors = Rate <= 3 * StartingRate ? (Renovated ? 3 : 1) : 0;
        return budgetVisitors + regularVisitors + premiumVisitors;
    }

    public Shift? RunShift(bool open)
    {
        if (!Leased || ReviewDue || (open && !Stations.Any(station => station.Ready)))
            return null;

        var today = Today;
        var visitors = 0;
        var incidents = "";
        if (open)
        {
            int demand = DemandFor(today);

            foreach (var station in Stations.Where(station => station.Ready))
            {
                var (idle, active) = station.PowerKilowatts;
                ElectricityKwh += idle * ShiftHours;
                if (station.WornFan && Day % 2 == 0)
                {
                    station.NeedsService = true;
                    incidents = "ПК с изношенным вентилятором перегрелся. Дайте ему остыть перед запуском.";
                    ElectricityKwh += active - idle;
                    continue;
                }
                if (Day > 1 && station.Software == Software.Copied && _random.Next(4) == 0)
                {
                    station.NeedsService = true;
                    incidents = "Пиратская сборка зависла. Проверьте ПК перед следующей сменой.";
                    ElectricityKwh += active - idle;
                    continue;
                }

                int hoursSold = Math.Min(station.BookableHours, Math.Max(0, demand - visitors));
                visitors += hoursSold;
                ElectricityKwh += (active - idle) * hoursSold;
            }

            ElectricityKwh += 0.36m * ShiftHours;
            WaterCubicMeters += (visitors * 4 + 20) / 1_000m;
        }

        int revenue = visitors * Rate;
        Cash += revenue;
        Revenue += revenue;
        Visitors += visitors;
        LastShift = new Shift(today, visitors, revenue, incidents);
        Day++;
        return LastShift;
    }

    public void Settle()
    {
        if (!ReviewDue || Settled)
            return;

        Bankrupt = !Spend(AmountDue);
        Settled = true;
    }

    public static string Money(int rubles) => $"{rubles.ToString("N0", RubleFormat)} р.";

    private bool Spend(int amount)
    {
        if (Cash < amount)
            return false;

        Cash -= amount;
        return true;
    }
}
