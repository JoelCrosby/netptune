using System;

using CsvHelper;
using CsvHelper.TypeConversion;

namespace Netptune.Core.Extensions;

public static class CsvHelperExtensions
{
    private static readonly TypeConverterOptions ConverterOptions = new ()
    {
        Formats = new[]
        {
            "dd/MM/yyyy HH:mm:ss",
        },
    };

    public static CsvWriter AddDateFormatting(this CsvWriter csv)
    {
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(ConverterOptions);
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(ConverterOptions);

        return csv;
    }

    public static CsvReader AddDateFormatting(this CsvReader csv)
    {
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(ConverterOptions);
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(ConverterOptions);

        return csv;
    }
}
