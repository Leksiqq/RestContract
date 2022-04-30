using System.Reflection;
using System.Text;

namespace Net.Leksi.RestContract;

internal class AttributeHolder
{
    internal TypeHolder TypeHolder { get; set; }
    internal Attribute Attribute { get; set; }

    internal AttributeHolder(Attribute attribute)
    {
        TypeHolder = new TypeHolder(attribute.GetType());
        Attribute = attribute;
    }

    public string GetNameWithoutAttribute()
    {
        return TypeHolder.TypeName.Substring(0, TypeHolder.TypeName.IndexOf(nameof(Attribute)));
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append(Constants.LeftBraket)
            .Append(GetNameWithoutAttribute()).Append(Constants.LeftParen);
        int length = sb.Length;
        sb.AppendJoin(", ", Attribute.GetType().GetProperties()
            .Where(p => p.CanWrite && p.GetValue(Attribute) is { }).Select(p => SelectFormat(p, Attribute)));
        if (sb.Length == length)
        {
            sb.Length--;
        }
        else
        {
            sb.Append(Constants.RightParen);
        }
        sb.Append(Constants.RightBraket);

        return sb.ToString();
    }

    private string SelectFormat(PropertyInfo property, object value)
    {
        if (property.PropertyType == typeof(Type))
        {
            return string.Format(Constants.TypePropertyFormat, property.Name, new TypeHolder((Type)property.GetValue(value)!).TypeName);
        }
        return string.Format(Constants.PropertyFormat, property.Name, property.GetValue(value));
    }

}

