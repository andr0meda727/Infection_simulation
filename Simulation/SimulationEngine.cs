using InfectionSimulation;

namespace Infection_Simulation.Simulation
{
    public class SimulationEngine
    {
        private readonly List<Individual> _individuals;
        private readonly Random _random;
        private double _simulationTime;
        private int _nextId;
        private bool _hasImmunity;

        public IReadOnlyList<Individual> Individuals => _individuals.AsReadOnly();
        public double SimulationTime => _simulationTime;
        public int PopulationCount => _individuals.Count;


        public SimulationEngine()
        {
            _individuals = new List<Individual>();
            _random = new Random();
            _simulationTime = 0;
            _nextId = 0;
        }

        // Inicjalizacja symulacji
        public void Initialize(bool withImmunity)
        {
            _individuals.Clear();
            _simulationTime = 0;
            _nextId = 0;
            _hasImmunity = withImmunity;

            for (int i = 0; i < SimConfig.INITIAL_POPULATION; i++)
                SpawnIndividual(true);
        }

        // Główna pętla aktualizacji
        public void Update(double deltaTime)
        {
            Parallel.ForEach(_individuals, ind =>
                ind.Update(deltaTime, SimConfig.WIDTH, SimConfig.HEIGHT, _individuals, _random));

            // Usuwanie osobników którzy wyszli
            _individuals.RemoveAll(i => i.IsDeadOrLeft);

            // Losowe spawnowanie nowych
            if (_random.NextDouble() < SimConfig.SPAWN_CHANCE && _individuals.Count < SimConfig.MAX_POPULATION)
            {
                SpawnIndividual(false);
            }

            _simulationTime += deltaTime;
        }

        // Tworzenie osobnika
        private void SpawnIndividual(bool isInitial)
        {
            double x = 0, y = 0;

            if (isInitial)
            {
                x = _random.NextDouble() * SimConfig.WIDTH;
                y = _random.NextDouble() * SimConfig.HEIGHT;
            }
            else
            {
                int side = _random.Next(4);
                switch (side)
                {
                    case 0: x = 0; y = _random.NextDouble() * SimConfig.HEIGHT; break;
                    case 1: x = SimConfig.WIDTH; y = _random.NextDouble() * SimConfig.HEIGHT; break;
                    case 2: x = _random.NextDouble() * SimConfig.WIDTH; y = 0; break;
                    case 3: x = _random.NextDouble() * SimConfig.WIDTH; y = SimConfig.HEIGHT; break;
                }
            }

            bool isImmune = _hasImmunity && _random.NextDouble() < 0.3;
            var individual = new Individual(_nextId++, x, y, isImmune, _random);

            // 10% szans na początkowe zarażenie
            if (!isImmune && _random.NextDouble() < 0.1)
            {
                individual.State = _random.NextDouble() < 0.5
                    ? new InfectedAsymptomaticState()
                    : new InfectedSymptomaticState();
            }

            _individuals.Add(individual);
        }

        public SimulationStats GetStats()
        {
            int healthy = 0, infected = 0, immune = 0;

            foreach (var ind in _individuals)
            {
                if (ind.State is HealthyState) healthy++;
                else if (ind.State is InfectedAsymptomaticState or InfectedSymptomaticState) infected++;
                else if (ind.State is ImmuneState) immune++;
            }

            return new SimulationStats
            {
                Time = _simulationTime,
                Population = _individuals.Count,
                Healthy = healthy,
                Infected = infected,
                Immune = immune
            };
        }


        public SimulationMemento CreateMemento()
        {
            return new SimulationMemento
            {
                Individuals = _individuals.Select(i => i.SaveState()).ToList(),
                SimulationTime = _simulationTime,
                HasImmunity = _hasImmunity,
                NextId = _nextId
            };
        }

        public void RestoreMemento(SimulationMemento memento)
        {
            _individuals.Clear();

            foreach (var m in memento.Individuals)
                _individuals.Add(Individual.RestoreState(m, _random));

            _simulationTime = memento.SimulationTime;
            _hasImmunity = memento.HasImmunity;
            _nextId = memento.NextId;
        }
    }

    public struct SimulationStats
    {
        public double Time { get; init; }
        public int Population { get; init; }
        public int Healthy { get; init; }
        public int Infected { get; init; }
        public int Immune { get; init; }
    }
}