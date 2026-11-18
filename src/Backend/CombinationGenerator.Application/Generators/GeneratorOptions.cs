namespace CombinationGenerator.Application.Generators;

/// <summary>הגדרות של ה-Service. נקראות מ-appsettings.json, מהקטע "Generator".</summary>
public sealed class GeneratorOptions
{
    /// <summary>שם הקטע ב-appsettings.json.</summary>
    public const string SectionName = "Generator";

    /// <summary>
    /// כמה שורות לכל היותר בעמוד. הגבול מגן על השרת: עמוד של מיליון שורות היה תופס זיכרון וזמן.
    /// </summary>
    public int MaxPageSize { get; set; } = 200;
}
