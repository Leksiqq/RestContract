namespace Net.Leksi.RestContract;

internal class TypeHolder
{
    internal string TypeName { get; set; }
    internal string Namespace { get; set; }
    internal TypeHolder[]? GenericArguments { get; set; }
    internal TypeHolder? Source { get; set; }
    internal TypeHolder(Type type)
    {
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments().First();
                Namespace = type.Namespace ?? String.Empty;
                TypeName = $"{type.Name}{Constants.QuestionMark}";
                GenericArguments = null;
            }
            else
            {
                Namespace = type.Namespace ?? String.Empty;
                TypeName = type.Name.Substring(0, type.Name.IndexOf(Constants.Apos));
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
            + (GenericArguments is null ? String.Empty : Constants.BeginGeneric + string.Join(Constants.Comma, GenericArguments.Select(v => v.ToString())) + Constants.EndGeneric);
    }

}

