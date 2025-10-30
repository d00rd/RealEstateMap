using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RealEstateMap
{

    public static class PersistenceService
    {
        private static readonly string _dataFile = DetermineDataFile();
        public static string DataFilePath => _dataFile;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task SaveAsync(IEnumerable<RealEstateAgency> agencies)
        {
            if (agencies == null) throw new ArgumentNullException(nameof(agencies));

            var dir = Path.GetDirectoryName(_dataFile);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var dtos = agencies.Select(MapAgencyToDto).ToList();

            string json = JsonSerializer.Serialize(dtos, JsonOptions);
            await File.WriteAllTextAsync(_dataFile, json, new UTF8Encoding(false)).ConfigureAwait(false);
        }

        public static async Task<List<RealEstateAgency>> LoadAsync()
        {
            try
            {
                if (!File.Exists(_dataFile))
                    return new List<RealEstateAgency>();

                using FileStream fs = File.OpenRead(_dataFile);
                var dtos = await JsonSerializer.DeserializeAsync<List<AgencyDto>>(fs, JsonOptions).ConfigureAwait(false)
                           ?? new List<AgencyDto>();

                var agencies = dtos.Select(MapDtoToAgency).ToList();
                return agencies;
            }
            catch
            {
                return new List<RealEstateAgency>();
            }
        }

        private static string DetermineDataFile()
        {

            string devCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data.json"));

            if (File.Exists(devCandidate))
                return devCandidate;
            return null;

        }



        private class AgencyDto
        {
            public string Name { get; set; } = string.Empty;
            public List<PropertyDto> Properties { get; set; } = new List<PropertyDto>();
        }

        private class PropertyDto
        {
            public string Type { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public double IndoorArea { get; set; }
            public double PropertyValue { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }

            public int? Floor { get; set; }
            public bool? HasElevator { get; set; }
            public double? MonthlyRent { get; set; }
            public bool? IsRented { get; set; }
            public double? OutdoorArea { get; set; }
        }

        private static AgencyDto MapAgencyToDto(RealEstateAgency agency)
        {
            return new AgencyDto
            {
                Name = agency.Name,
                Properties = agency.Properties.Select(MapPropertyToDto).ToList()
            };
        }

        private static PropertyDto MapPropertyToDto(Property p)
        {
            var dto = new PropertyDto
            {
                Address = p.Address,
                IndoorArea = p.IndoorArea,
                PropertyValue = p.PropertyValue,
                Latitude = p.Latitude,
                Longitude = p.Longitude
            };

            if (p is RentableApartment ra)
            {
                dto.Type = "RentableApartment";
                dto.Floor = ra.Floor;
                dto.HasElevator = ra.HasElevator;
                dto.MonthlyRent = ra.MonthlyRent;
                dto.IsRented = ra.IsRented;
            }
            else if (p is Apartment a)
            {
                dto.Type = "Apartment";
                dto.Floor = a.Floor;
                dto.HasElevator = a.HasElevator;
            }
            else if (p is House h)
            {
                dto.Type = "House";
                dto.OutdoorArea = h.OutdoorArea;
            }
            else
            {
                dto.Type = "Unknown";
            }

            return dto;
        }

        private static RealEstateAgency MapDtoToAgency(AgencyDto dto)
        {
            var agency = new RealEstateAgency(dto.Name);
            foreach (var p in dto.Properties)
            {
                try
                {
                    var prop = MapDtoToProperty(p);
                    if (prop != null)
                        agency.AddProperty(prop);
                }
                catch
                {
                    // skip invalid entries
                }
            }
            return agency;
        }

        private static Property? MapDtoToProperty(PropertyDto dto)
        {
            return dto.Type switch
            {
                "RentableApartment" => CreateRentableApartment(dto),
                "Apartment" => CreateApartment(dto),
                "House" => CreateHouse(dto),
                _ => null
            };
        }

        private static Apartment CreateApartment(PropertyDto d)
        {
            int floor = d.Floor ?? 0;
            bool elevator = d.HasElevator ?? false;
            return new Apartment(d.Address, d.IndoorArea, floor, elevator, d.PropertyValue, d.Latitude, d.Longitude);
        }

        private static RentableApartment CreateRentableApartment(PropertyDto d)
        {
            int floor = d.Floor ?? 0;
            bool elevator = d.HasElevator ?? false;
            double monthly = d.MonthlyRent ?? 0.0;
            var ra = new RentableApartment(d.Address, d.IndoorArea, floor, elevator, monthly, d.PropertyValue, d.Latitude, d.Longitude);
            if (d.IsRented == true)
                ra.IsRented = true;
            return ra;
        }

        private static House CreateHouse(PropertyDto d)
        {
            double outdoor = d.OutdoorArea ?? 0.0;
            return new House(d.Address, d.IndoorArea, outdoor, d.PropertyValue, d.Latitude, d.Longitude);
        }

    }
}