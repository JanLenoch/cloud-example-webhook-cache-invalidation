using System;

namespace WebhookCacheInvalidationMvc.Models
{
    public class IdentifierSet : IComparable<IdentifierSet>
    {
        public string Type { get; set; }
        public string Codename { get; set; }
        
        public int CompareTo(IdentifierSet other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var typeComparison = string.Compare(Type, other.Type, StringComparison.Ordinal);
            if (typeComparison != 0) return typeComparison;
            return string.Compare(Codename, other.Codename, StringComparison.Ordinal);
        }
    }
}
