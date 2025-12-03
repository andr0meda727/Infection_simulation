using System.IO;
using System.Text.Json;

namespace InfectionSimulation
{
    public static class SimulationSerializer
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        // Zapisz symulację do pliku
        public static void Save(SimulationMemento memento, string filePath)
        {
            var json = JsonSerializer.Serialize(memento, _jsonOptions);
            File.WriteAllText(filePath, json);
        }

        // Wczytaj symulację z pliku
        public static SimulationMemento Load(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<SimulationMemento>(json)
                ?? throw new InvalidDataException("Nieprawidłowy format pliku");
        }

        // Generuj nazwę pliku z datą
        public static string GenerateFileName()
        {
            return $"simulation_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        }
    }
}