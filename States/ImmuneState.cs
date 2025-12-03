using System.Windows.Media;

namespace InfectionSimulation
{
    public class ImmuneState : IIndividualState
    {
        public string Name => "immune";

        public Color GetColor() => Color.FromRgb(59, 130, 246);

        public void Update(Individual individual, double deltaTime, List<Individual> allIndividuals)
        {
            // Immune individuals don't change state
        }
    }
}