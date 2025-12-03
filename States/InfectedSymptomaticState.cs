using System.Windows.Media;

namespace InfectionSimulation
{
    public class InfectedSymptomaticState : IIndividualState
    {
        public string Name => "infected_symptomatic";
        public Color GetColor() => Colors.Red;

        public void Update(Individual me, double deltaTime, List<Individual> allIndividuals, Random random)
        {
            me.InfectionTime += deltaTime;

            if (me.InfectionTime >= me.InfectionDuration)
            {
                me.State = new ImmuneState();
            }
        }
    }
}