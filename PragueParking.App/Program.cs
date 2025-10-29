using Spectre.Console;
using PragueParking.Domain;
using PragueParking.Data;

//config + data paths
string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
var cfg = ConfigManager.Load(configPath);

string dataPath = Path.Combine(AppContext.BaseDirectory, "garage.json");
var garage = FileManager.LoadGarage(dataPath, defaultCapacity: cfg.Capacity);

foreach (var s in garage.Spots)
    s.MaxUnits = cfg.SpotMaxUnits;

while (true)
{
    Console.Clear();
    Console.WriteLine("=== Prague Parking 2.0 ===");
    Console.WriteLine("1) Park vehicle");
    Console.WriteLine("2) Retrieve vehicle");
    Console.WriteLine("3) Search vehicle");
    Console.WriteLine("4) Show spots");
    Console.WriteLine("5) Show map");
    Console.WriteLine("6) Move vehicle");
    Console.WriteLine("7) Reload configuration (prices/capacity/units)");
    Console.WriteLine("0) Exit");
    Console.Write("Choice: ");
    var choice = (Console.ReadLine() ?? "").Trim();

    if (choice == "0") break;

    switch (choice)
    {
        case "1":
            Console.Write("Type (CAR/MC): ");
            var type = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
            Console.Write("regNr: ");
            var reg = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            Vehicle v = type == "CAR" ? new Car(reg) : new Motorcycle(reg);
            if (garage.Park(v)) Console.WriteLine("Parked.");
            else Console.WriteLine("No space.");
            FileManager.SaveGarage(dataPath, garage);
            Console.ReadKey();
            break;

        case "2":
            Console.Write("regNr: ");
            reg = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            if (garage.Remove(reg, out int idx, out Vehicle? removed))
            {
                var now = DateTime.UtcNow;
                var minutes = (now - removed!.CheckInUtc).TotalMinutes;
                var price = PricingService.CalculateFee(
                    removed, now, cfg.PricePerHourCar, cfg.PricePerHourMc, cfg.FreeMinutes);

                Console.WriteLine($"Removed from spot {idx + 1}.");
                Console.WriteLine($"Parked ~{minutes:F0} min. Price: {price} CZK");

                FileManager.SaveGarage(dataPath, garage);
            }
            else
            {
                Console.WriteLine("Not found.");
            }
            Console.ReadKey();
            break;


        case "3":
            Console.Write("regNr: ");
            reg = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
            var spot = garage.Find(reg);
            Console.WriteLine(spot >= 0 ? $"Found at spot {spot + 1}" : "Not found.");
            Console.ReadKey();
            break;

        case "4":
            for (int i = 0; i < garage.Spots.Count; i++)
            {
                var vs = garage.Spots[i].Vehicles;
                var text = vs.Count == 0 ? "Empty" : string.Join(" | ", vs.Select(x => $"{x.Type}#{x.RegNo}"));
                Console.WriteLine($"Spot {i + 1:D2}: {text}");
            }
            Console.ReadKey();
            break;

        case "5":
            ShowMap(garage);
            break;

        case "6":
            Console.Write("Reg number: ");
            reg = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            Console.Write("Target spot (1..N): ");
            var spotText = (Console.ReadLine() ?? "").Trim();

            if (!int.TryParse(spotText, out var spotNum) || spotNum < 1 || spotNum > garage.Capacity)
            {
                Console.WriteLine("Invalid spot number.");
                Console.ReadKey();
                break;
            }

            var ok = garage.Move(reg, spotNum - 1);
            Console.WriteLine(ok ? "Moved." : "Move not possible.");

            if (ok)
                FileManager.SaveGarage(dataPath, garage);

            Console.ReadKey();
            break;

        case "7":
            try
            {
                cfg = ConfigManager.Load(configPath);

                foreach (var s in garage.Spots)
                    s.MaxUnits = cfg.SpotMaxUnits;

                Console.WriteLine("Configuration reloaded.");
                Console.WriteLine($"Capacity: {garage.Capacity}, SpotMaxUnits: {cfg.SpotMaxUnits}");
                Console.WriteLine($"Prices: Car={cfg.PricePerHourCar}/h, MC={cfg.PricePerHourMc}/h, Free={cfg.FreeMinutes} min");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to reload configuration: " + ex.Message);
            }
            Console.ReadKey();
            break;


        default:
            Console.WriteLine("Invalid choice."); Console.ReadKey();
            break;
    }
}

static void ShowMap(PragueParking.Domain.ParkingGarage garage)
{
    AnsiConsole.Clear();
    AnsiConsole.Write(
        new Panel("[bold underline]Prague Parking – Map[/]")
            .Border(BoxBorder.Rounded)
            .Expand());

    
    AnsiConsole.MarkupLine(
        "\nLegend: " +
        "[green]░[/] Empty   " +
        "[yellow]▒[/] 1 MC   " +
        "[magenta]█[/] 2 MC   " +
        "[blue]█[/] Car\n");

    int cols = 10; // 10×10 rutor
    for (int i = 0; i < garage.Spots.Count; i++)
    {
        var spot = garage.Spots[i];
        string color;
        char ch;

        if (spot.Vehicles.Count == 0)
        {
            color = "green";
            ch = '░';
        }
        else if (spot.Vehicles[0] is PragueParking.Domain.Car)
        {
            color = "blue";
            ch = '█';
        }
        else
        {
            // MC: 1 eller 2?
            int mc = spot.Vehicles.Count; // i din modell får bara MC dela
            if (mc == 1)
            {
                color = "yellow";
                ch = '▒';
            }
            else
            {
                color = "magenta";
                ch = '█';
            }
        }

        AnsiConsole.Markup($"[{color}]{ch}[/]");

        // Radbryt varje 10:e
        if ((i + 1) % cols == 0)
        {
            // visa intervall till höger för översikt
            int start = i - (cols - 2);
            int end = i + 1;
            AnsiConsole.MarkupLine($"   [grey]spots {start:00}-{end:00}[/]");
        }
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
    Console.ReadKey(true);
}


Console.WriteLine("Bye!");
