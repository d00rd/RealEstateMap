using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateMap
{
    public class House : Property
    {
        public double OutdoorArea { get; set; }

        public double TotalArea => IndoorArea + OutdoorArea;

        public House(string address, double indoorArea, double outdoorArea, double propertyValue, double latitude, double longitude)
            : base(address, indoorArea, propertyValue, latitude, longitude)
        {
            OutdoorArea = outdoorArea;
        }

        public override string ToString()
        {
            return $"House -> {base.ToString()}, OutdoorArea: {OutdoorArea} , TotalArea: {TotalArea} ";
        }
    }
}
