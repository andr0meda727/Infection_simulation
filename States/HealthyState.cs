using System.Windows.Media;

namespace InfectionSimulation
{
    public class HealthyState : IIndividualState
    {
        public string Name => "healthy";
        public Color GetColor() => Color.FromRgb(34, 197, 94);

        public void Update(Individual me, double dt, List<Individual> all, Random random)
        {
            // Sprawdź kontakty z chorymi
            var currentContacts = new HashSet<int>();

            foreach (var other in all)
            {
                if (other.Id == me.Id || other.IsDeadOrLeft)
                    continue;

                bool contagious = other.State is InfectedAsymptomaticState or InfectedSymptomaticState;
                if (!contagious)
                    continue;

                double dist = me.Position.Distance(other.Position);
                if (dist > SimConfig.INFECTION_DISTANCE)
                    continue;

                currentContacts.Add(other.Id);

                // Zliczaj czas kontaktu
                if (!me.CloseContactTime.ContainsKey(other.Id))
                    me.CloseContactTime[other.Id] = 0;

                me.CloseContactTime[other.Id] += dt;

                // Próba zarażenia po 3 sekundach
                if (me.CloseContactTime[other.Id] >= SimConfig.CONTACT_TIME_REQUIRED)
                {
                    double chance = other.State is InfectedAsymptomaticState ? 0.50 : 1.00;

                    if (random.NextDouble() < chance)
                    {
                        me.State = random.NextDouble() < 0.5
                            ? new InfectedSymptomaticState()
                            : new InfectedAsymptomaticState();
                        me.InfectionTime = 0;
                        me.CloseContactTime.Clear();
                        return; // Już zarażony, koniec
                    }

                    me.CloseContactTime[other.Id] = 0; // Reset licznika po próbie
                }
            }

            // Usuń kontakty z oddalonych osobników (proste czyszczenie)
            var toRemove = me.CloseContactTime.Keys.Where(k => !currentContacts.Contains(k)).ToList();
            foreach (var id in toRemove)
                me.CloseContactTime.Remove(id);
        }
    }
}