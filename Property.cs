using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateMap
{
    public abstract class Property
    {
        public string Address { get; set; }
        public double IndoorArea { get; set; }   
        public double PropertyValue { get; set; }

        // New: coordinates
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        protected Property(string address, double indoorArea, double propertyValue, double latitude, double longitude)
        {
            Address = address;
            IndoorArea = indoorArea;
            PropertyValue = propertyValue;
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
        {
            return $"Address: {Address}, IndoorArea: {IndoorArea} m², Value: {PropertyValue}, Lat: {Latitude}, Lon: {Longitude}";
        }
    }

}