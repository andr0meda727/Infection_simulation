using System.Windows.Media;

namespace InfectionSimulation
{
    public interface IIndividualState
    {
        string Name { get; }
        Color GetColor();
        void Update(Individual individual, double deltaTime, List<Individual> allIndividuals, Random random);
    }
}