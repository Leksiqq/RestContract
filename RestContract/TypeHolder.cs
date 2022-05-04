namespace Net.Leksi.RestContract;

public class TypeHolder
{
    public Type Type{ get; set; }
    public string TypeName { get; set; }
    public string Namespace { get; set; }
    public TypeHolder[]? GenericArguments { get; set; }
    public TypeHolder? Source { get; set; }
    public TypeHolder(Type type)
    {
        Type = type;
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

