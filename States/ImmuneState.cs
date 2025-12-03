using System.Windows.Media;

namespace InfectionSimulation
{
    public class ImmuneState : IIndividualState
    {
        public string Name => "immune";

        public Color GetColor() => Color.FromRgb(59, 130, 246);

        public void Update(Individual me, double dt, List<Individual> allIndividuals, Random random)
        {
            // Immune individuals don't change state
        }
    }
}