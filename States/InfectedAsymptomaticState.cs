using System.Windows.Media;

namespace InfectionSimulation
{
    public class InfectedAsymptomaticState : IIndividualState
    {
        public string Name => "infected_asymptomatic";
        public Color GetColor() => Colors.Yellow;

        public void Update(Individual me, double deltaTime, List<Individual> allIndividuals, Random random)
        {
            me.InfectionTime += deltaTime;

            // Czy wyzdrowiał? (20-30s ustalone przy tworzeniu osobnika)
            if (me.InfectionTime >= me.InfectionDuration)
            {
                me.State = new ImmuneState();
            }
        }
    }
}