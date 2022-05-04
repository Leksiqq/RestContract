namespace Net.Leksi.RestContract;

/// <summary>
/// <para xml:lang="ru">
/// Компаратор для сортировки операторов <code>using</code>
/// </para>
/// </summary>
public class NamespaceComparer : IComparer<string>
{
    /// <inheritdoc/>
    public int Compare(string? s1, string? s2)
    {
        if (s1 == "System" && s2 != "System")
        {
            return 1;
        }
        if (s1 != "System" && s2 == "System")
        {
            return -1;
        }
        if (s1!.StartsWith("System.") && !s2!.StartsWith("System."))
        {
            return 1;
        }
        if (!s1!.StartsWith("System.") && s2!.StartsWith("System."))
        {
            return -1;
        }
        return -string.Compare(s1, s2);
    }
}
