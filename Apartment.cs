using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateMap
{
    public class Apartment : Property
    {
        public int Floor { get; set; }
        public bool HasElevator { get; set; }

        public Apartment(string address, double indoorArea, int floor, bool hasElevator, double propertyValue, double latitude, double longitude)
            : base(address, indoorArea, propertyValue, latitude, longitude)
        {
            Floor = floor;
            HasElevator = hasElevator;
        }

        public override string ToString()
        {
            return $"Apartment -> {base.ToString()}, Floor: {Floor}, Elevator: {(HasElevator ? "Yes" : "No")}";
        }
    }
}
