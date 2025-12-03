namespace InfectionSimulation
{
    public class SimulationMemento
    {
        public List<IndividualMemento> Individuals { get; set; }
        public double SimulationTime { get; set; }
        public bool HasImmunity { get; set; }
        public int NextId { get; set; }
    }
}