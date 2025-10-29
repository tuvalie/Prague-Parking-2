namespace PragueParking.Domain;

public abstract class Vehicle
{
    public string RegNo { get; }
    public DateTime CheckInUtc { get; }

    protected Vehicle(string regNo, DateTime? checkInUtc = null)
    {
        RegNo = regNo.ToUpperInvariant();
        CheckInUtc = (checkInUtc ?? DateTime.UtcNow);
    }

    public abstract string Type { get; } // "CAR" eller "MC"
    public abstract int SizeUnits { get; }
}

public sealed class Car : Vehicle
{
    public Car(string regNo, DateTime? checkInUtc = null) : base(regNo, checkInUtc) { }
    public override string Type => "CAR";
    public override int SizeUnits => 2;
}

public sealed class Motorcycle : Vehicle
{
    public Motorcycle(string regNo, DateTime? checkInUtc = null) : base(regNo, checkInUtc) { }
    public override string Type => "MC";
    public override int SizeUnits => 1;
}

public class ParkingSpot
{
    public int MaxUnits { get; set; } = 2;
    public List<Vehicle> Vehicles { get; } = new();
    

    public bool CanPark(Vehicle v)
    {
        var used = Vehicles.Sum(x => x.SizeUnits);
        return used + v.SizeUnits <= MaxUnits;
    }

    public bool TryPark(Vehicle v)
    {
        if (!CanPark(v)) return false;
        Vehicles.Add(v);
        return true;
    }

    public bool Remove(string regNo)
    {
        var i = Vehicles.FindIndex(v => v.RegNo == regNo.ToUpperInvariant());
        if (i < 0) return false;
        Vehicles.RemoveAt(i);
        return true;
    }

    public bool Contains(string regNo) => Vehicles.Any(v => v.RegNo == regNo.ToUpperInvariant());
}

public class ParkingGarage
{
    public List<ParkingSpot> Spots { get; }
    public int Capacity => Spots.Count;

    public ParkingGarage(int capacity = 100)
    {
        Spots = Enumerable.Range(0, capacity)
                          .Select(_ => new ParkingSpot { MaxUnits = 2 }) 
                          .ToList();
    }

    public bool Park(Vehicle v)
    {
        if (v is Motorcycle)
        {
            foreach (var s in Spots)
                if (s.Vehicles.Count == 1 && s.CanPark(v))
                    return s.TryPark(v);
        }

        foreach (var s in Spots)
            if (s.CanPark(v))
                return s.TryPark(v);

        return false;
    }

    public bool Remove(string regNo, out int spotIndex, out Vehicle? removed)
    {
        for (int i = 0; i < Spots.Count; i++)
        {
            var s = Spots[i];
            removed = s.Vehicles.FirstOrDefault(v => v.RegNo == regNo.ToUpperInvariant());
            if (removed != null)
            {
                s.Remove(regNo);
                spotIndex = i;
                return true;
            }
        }
        removed = null;
        spotIndex = -1;
        return false;
    }

    public int Find(string regNo)
    {
        for (int i = 0; i < Spots.Count; i++)
            if (Spots[i].Contains(regNo)) return i;
        return -1;
    }

    public bool Move(string regNo, int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= Spots.Count) return false;

        for (int i = 0; i < Spots.Count; i++)
        {
            var s = Spots[i];
            var v = s.Vehicles.FirstOrDefault(x => x.RegNo == regNo.ToUpperInvariant());
            if (v == null) continue;

            var dest = Spots[targetIndex];
            if (!dest.CanPark(v)) return false;

            s.Remove(regNo);
            dest.TryPark(v);
            return true;
        }
        return false;
    }

}

public static class PricingService
{
    public static decimal CalculateFee(
        Vehicle v,
        DateTime checkoutUtc,
        decimal pricePerHourCar,
        decimal pricePerHourMc,
        int freeMinutes)
    {
        var minutes = (checkoutUtc - v.CheckInUtc).TotalMinutes;

        // Om tiden är mindre eller lika med friminuter → ingen avgift
        if (minutes <= freeMinutes)
            return 0m;

        // Avrunda hela vistelsen uppåt per påbörjad timme
        var hours = Math.Ceiling(minutes / 60.0);
        var rate = v is Car ? pricePerHourCar : pricePerHourMc;

        return (decimal)hours * rate;
    }
}
