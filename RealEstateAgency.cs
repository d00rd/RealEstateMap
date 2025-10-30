using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateMap
{
    public class RealEstateAgency
    {
        private List<Property> properties = new List<Property>();

        public string Name { get; }

        public RealEstateAgency(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void AddProperty(Property property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (properties.Any(p => string.Equals(p.Address, property.Address, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A property with address '{property.Address}' is already in the list.");
            }

            properties.Add(property);
        }

        public IReadOnlyList<Property> Properties => properties.AsReadOnly();

        public bool RentProperty(string address)
        {
            Property property = properties.Find(p => p.Address.Equals(address, StringComparison.OrdinalIgnoreCase));

            if (property == null)
                return false;

            if (property is IRentable rentable)
            {
                if (rentable.IsRented)
                    return false;

                rentable.IsRented = true;
                return true;
            }

            return false;
        }
    }

}
