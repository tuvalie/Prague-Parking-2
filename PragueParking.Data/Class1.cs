using PragueParking.Domain;
using System.Text.Json;

namespace PragueParking.Data;

public class GarageDto
{
    public int Capacity { get; set; }
    public List<SpotDto> Spots { get; set; } = new();
}

public class SpotDto
{
    public List<VehicleDto> Vehicles { get; set; } = new();
}

public class VehicleDto
{
    public string Type { get; set; } = ""; // "CAR" / "MC"
    public string RegNo { get; set; } = "";
    public DateTime CheckInUtc { get; set; }
}

public static class GarageMapper
{
    public static GarageDto ToDto(ParkingGarage g)
        => new GarageDto
        {
            Capacity = g.Capacity,
            Spots = g.Spots.Select(s => new SpotDto
            {
                Vehicles = s.Vehicles.Select(v => new VehicleDto
                {
                    Type = v.Type,
                    RegNo = v.RegNo,
                    CheckInUtc = v.CheckInUtc
                }).ToList()
            }).ToList()
        };

    public static ParkingGarage FromDto(GarageDto dto)
    {
        var g = new ParkingGarage(dto.Capacity);
        for (int i = 0; i < dto.Spots.Count && i < g.Spots.Count; i++)
        {
            foreach (var v in dto.Spots[i].Vehicles)
            {
                Vehicle model = v.Type == "CAR"
                    ? new Car(v.RegNo, v.CheckInUtc)
                    : new Motorcycle(v.RegNo, v.CheckInUtc);
                g.Spots[i].TryPark(model);
            }
        }
        return g;
    }
}

public static class FileManager
{
    public static ParkingGarage LoadGarage(string path, int defaultCapacity = 100)
    {
        if (!File.Exists(path)) return new ParkingGarage(defaultCapacity);

        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<GarageDto>(json) ?? new GarageDto { Capacity = defaultCapacity };
        return GarageMapper.FromDto(dto);
    }

    public static void SaveGarage(string path, ParkingGarage garage)
    {
        var dto = GarageMapper.ToDto(garage);
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}

public sealed class AppConfig
{
    public int Capacity { get; set; } = 100;

    // Pris per påbörjad timme
    public decimal PricePerHourCar { get; set; } = 20m;
    public decimal PricePerHourMc { get; set; } = 10m;

    // Gratisperiod i minuter
    public int FreeMinutes { get; set; } = 10;

    public int SpotMaxUnits { get; set; } = 2;
}

public static class ConfigManager
{
    public static AppConfig Load(string path)
    {
        if (!File.Exists(path)) return new AppConfig();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    public static void Save(string path, AppConfig cfg)
    {
        var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
