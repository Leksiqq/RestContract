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

    
}

