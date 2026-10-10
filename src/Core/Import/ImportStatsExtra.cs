using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class ImportStatsExtra
{
    public static string Summary<T>(this ImportResult<T> result) =>
        Format(result.Items.Count, result.Errors.Count);

    public static string Summary(this MixedImportResult result) =>
        Format(result.Products.Count + result.Customers.Count, result.Errors.Count);

    private static string Format(int accepted, int skipped)
    {
        int total = accepted + skipped;
        double percent = total == 0 ? 0 : 100.0 * skipped / total;

        return $"Усього: {total} | прийнято: {accepted} | пропущено: {skipped} " +
               $"| помилок: {percent.ToString("F1", CultureInfo.InvariantCulture)}%";
    }
}