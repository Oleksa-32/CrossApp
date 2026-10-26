namespace Core.Reports;

/// <summary>Власні оператори LINQ для звітів (додаткове завдання 2).</summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Перші <paramref name="n"/> елементів за спаданням ключа. Другий ключ —
    /// унікальний рядок, порівняний Ordinal, — робить порядок однаковим за кожного
    /// запуску, навіть коли основні ключі збігаються, а порядок джерела змінився.
    /// </summary>
    /// <remarks>
    /// Метод не є ітератором (у ньому немає yield), тому аргументи перевіряються
    /// одразу під час виклику, а сам запит лишається відкладеним до ToList/foreach.
    /// </remarks>
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<T, string> tieBreaker,
        int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(tieBreaker);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return source
            .OrderByDescending(keySelector)
            .ThenBy(tieBreaker, StringComparer.Ordinal)
            .Take(n);
    }
}
