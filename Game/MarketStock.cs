using System.Collections.Generic;

namespace ComputerLive;

internal enum Place { Club, Courtyard, Market, Shopping, PartsStore, ComputerStore, Tram }

internal sealed record StockOffer(Fixture Fixture, string Seller, string Name, int ListedPrice, Purchase? Hardware = null,
    Software Software = Software.None, bool WornFan = false, bool PricedInDollars = false)
{
    public int PriceOn(CampaignDay day) => PricedInDollars ? ListedPrice * day.RublesPerDollar : ListedPrice;

    public string Details => Hardware switch
    {
        Purchase.Used when WornFan => "Б/у. Проверка показала, что вентилятор задевает кожух. ПК может перегреваться во время смены.",
        Purchase.Used => "Б/у. Продавец показал, что машина запускается и выдерживает проверку под нагрузкой.",
        Purchase.Kit => "Материнская плата, процессор, память и корпус. Комплект нужно собрать на верстаке.",
        Purchase.Retail => "Новый собранный ПК. Его можно сразу поставить на стол. Он потребляет меньше энергии, чем подержанный.",
        _ when Software == Software.Copied => "Дискеты без лицензии. Стоят дешевле, но сборка может зависнуть посреди смены.",
        _ => "Лицензионные дискеты на одно рабочее место. Поддерживаемая сборка системы и игр.",
    };
}

internal static class MarketStock
{
    // «Московские новости», № 18 от 2 мая 1993 года, цены фирм Санкт-Петербурга.
    private const int PartsRetailDollars = 1_781;
    private const int ComputerRetailDollars = 1_999;

    // Комплект оценён в 70% цены готового ПК. Б/у машины — около 44% и 60% цены нового 386 за 1 373 доллара на старте.
    private const int KitDollars = 1_250;
    private const int UsedWornRubles = 500_000;
    private const int UsedTestedRubles = 680_000;

    private static readonly Dictionary<Fixture, StockOffer> Offers = new()
    {
        [Fixture.UsedCheap] = new(Fixture.UsedCheap, "Палатка у ворот", "Б/у Inter 386", UsedWornRubles, Purchase.Used, WornFan: true),
        [Fixture.UsedTested] = new(Fixture.UsedTested, "Скупка «Радиоряд»", "Б/у Inter 386", UsedTestedRubles, Purchase.Used),
        [Fixture.MarketDisks] = new(Fixture.MarketDisks, "Прилавок с дискетами", "Пиратские дискеты", 2_000, Software: Software.Copied),
        [Fixture.PartsKit] = new(Fixture.PartsKit, "Радиодеталь", "Комплект Inter 486", KitDollars, Purchase.Kit, PricedInDollars: true),
        [Fixture.PartsPc] = new(Fixture.PartsPc, "Радиодеталь", "Новый Inter 486", PartsRetailDollars, Purchase.Retail, PricedInDollars: true),
        [Fixture.PartsDisks] = new(Fixture.PartsDisks, "Радиодеталь", "Лицензионные дискеты", 22_000, Software: Software.Licensed),
        [Fixture.ComputerPc] = new(Fixture.ComputerPc, "Электроника", "Новый Inter 486", ComputerRetailDollars, Purchase.Retail, PricedInDollars: true),
        [Fixture.ComputerDisks] = new(Fixture.ComputerDisks, "Электроника", "Лицензионные дискеты", 18_000, Software: Software.Licensed),
    };

    public static StockOffer Get(Fixture fixture) => Offers[fixture];
    public static bool TryGet(Fixture fixture, out StockOffer offer) => Offers.TryGetValue(fixture, out offer!);
}
