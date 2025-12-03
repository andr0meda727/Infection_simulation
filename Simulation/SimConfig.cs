namespace Infection_Simulation.Simulation
{
    public static class SimConfig
    {
        public const int WIDTH = 800; // w pixelach, czyli wymiar
        public const int HEIGHT = 600;

        public const double PIXELS_PER_METER = 10.0; // Skala: 1m = 10px
        public const double INFECTION_DISTANCE = 2.0 * PIXELS_PER_METER; // 2 metry
        public const double CONTACT_TIME_REQUIRED = 0.3; // 3 sekundy

        public const double SPAWN_CHANCE = 0.05;
        public const int MAX_POPULATION = 100;
        public const int INITIAL_POPULATION = 50;
    }
}
