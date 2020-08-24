using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using CsvHelper;

namespace Netptune.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public async static Task<MemoryStream> ToCsvStream<T>(this IEnumerable<T> enumerable)
        {
            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8);
            var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            await csv.WriteRecordsAsync(enumerable);
            await writer.FlushAsync();

            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }
    }
}
