namespace Net.Leksi.RestContract;

public class TypeHolder
{
    internal Type Type{ get; set; }
    internal string TypeName { get; set; }
    internal string Namespace { get; set; }
    internal TypeHolder[]? GenericArguments { get; set; }
    internal TypeHolder? Source { get; set; }
    internal TypeHolder(Type type)
    {
        Type = type;
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments().First();
                Namespace = type.Namespace ?? String.Empty;
                TypeName = $"{type.Name}?";
                GenericArguments = null;
            }
            else
            {
                Namespace = type.Namespace ?? String.Empty;
                TypeName = type.Name.Substring(0, type.Name.IndexOf("`"));
                GenericArguments = type.GetGenericArguments().Select(v =>
                {
                    return new TypeHolder(v);
                }).ToArray();
            }
        }
        else
        {
            TypeName = type.Name;
            Namespace = type.Namespace ?? String.Empty;
            GenericArguments = null;
        }
    }
    public override string ToString()
    {
        return TypeName
            + (GenericArguments is null ? String.Empty : $"<{(string.Join(", ", GenericArguments.Select(v => v.ToString())))}>");
    }

}

