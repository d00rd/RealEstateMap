using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateMap
{
    public class RentableApartment : Apartment, IRentable
    {
        public bool IsRented { get; set; }
        public double MonthlyRent { get; set; }

        public RentableApartment(string address, double indoorArea, int floor, bool hasElevator, double monthlyRent, double propertyValue, double latitude, double longitude)
            : base(address, indoorArea, floor, hasElevator, propertyValue, latitude, longitude)
        {
            MonthlyRent = monthlyRent;
            IsRented = false;
        }

        public override string ToString()
        {
            return $"RentableApartment -> {base.ToString()}, MonthlyRent: {MonthlyRent}, IsRented: {IsRented}";
        }
    }
}
