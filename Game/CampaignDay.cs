using System;
using System.Globalization;

namespace ComputerLive;

internal readonly record struct CampaignDay(DateOnly Date)
{
    public static readonly DateOnly FirstDate = new(1993, 5, 10);

    // Распоряжения мэра Санкт-Петербурга № 92-р от 10.02.1993 и № 354-р от 14.05.1993.
    private static readonly DateOnly FareAnnounced = new(1993, 5, 14);
    private static readonly DateOnly FareStarts = new(1993, 5, 15);
    private const int PreviousTramFare = 5;
    private const int NewTramFare = 10;
    // Банк России, динамика официального курса доллара США за май 1993 года.
    private static readonly DateOnly FirstDollarIncrease = new(1993, 5, 12);
    private static readonly DateOnly SecondDollarIncrease = new(1993, 5, 14);
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public int TramFare => Date >= FareStarts ? NewTramFare : PreviousTramFare;
    public int ReturnTicket => 2 * TramFare;
    public int RublesPerDollar => Date >= SecondDollarIncrease ? 886 : Date >= FirstDollarIncrease ? 859 : 829;
    public string Label => Date.ToString("d MMMM yyyy", Russian);

    public string NewsCaption(int segment) => segment switch
    {
        0 when Date == FareAnnounced => "Мэрия объявила новый тариф на трамвай",
        0 when Date == FareStarts => "Сегодня подорожал проезд в трамвае",
        0 when Date == FirstDollarIncrease => "Импортные компьютеры дорожают",
        0 => $"Городские новости за {Date.ToString("dd.MM.yyyy", Russian)}",
        1 when Date == FirstDollarIncrease || Date == SecondDollarIncrease =>
            $"Доллар — {RublesPerDollar} р. Импорт дорожает",
        1 => $"Поездка на трамвае — {TramFare} р.",
        2 when Date == FareAnnounced => $"С {FareStarts.Day} мая проезд будет стоить {NewTramFare} р.",
        _ => IsWeekend ? "В выходные клубы ждут больше гостей" : "Выпуск городских новостей окончен",
    };
}
