using System.Windows.Media;

namespace InfectionSimulation
{
    public static class SimConfig
    {
        public const double PIXELS_PER_METER = 10.0; // Skala: 1m = 10px
        //public const double INFECTION_DISTANCE = 2.0 * PIXELS_PER_METER; // 2 metry
        public const double INFECTION_DISTANCE = 2.0 * PIXELS_PER_METER; // 2 metry

        public const double CONTACT_TIME_REQUIRED = 3; // 3 sekundy // 3 tu bylo
    }
    public interface IIndividualState
    {
        string Name { get; }
        Color GetColor();
        void Update(Individual individual, double deltaTime, List<Individual> allIndividuals, Random random);
    }
}