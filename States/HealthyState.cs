using System.Windows.Media;

namespace InfectionSimulation
{
    public class HealthyState : IIndividualState
    {
        public string Name => "healthy";

        public Color GetColor() => Color.FromRgb(34, 197, 94);

        public void Update(Individual me, double deltaTime, List<Individual> allIndividuals)
        {
            // Lista ID obecnych w pobliżu w tej klatce (do czyszczenia starych kontaktów)
            var currentContacts = new HashSet<int>();

            foreach (var other in allIndividuals)
            {
                if (other.Id == me.Id) continue; // Nie sprawdzamy siebie
                if (other.IsDeadOrLeft) continue;

                // Sprawdzamy tylko chorych (źródła zakażenia)
                bool isContagious = other.State is InfectedAsymptomaticState ||
                                    other.State is InfectedSymptomaticState;

                if (!isContagious) continue;

                // a) Warunek dystansu (< 2m)
                double dist = me.Position.Distance(other.Position);
                if (dist <= SimConfig.INFECTION_DISTANCE)
                {
                    currentContacts.Add(other.Id);

                    // Zliczamy czas kontaktu
                    if (!me.CloseContactTime.ContainsKey(other.Id))
                        me.CloseContactTime[other.Id] = 0;

                    me.CloseContactTime[other.Id] += deltaTime;

                    // b) Warunek czasu (> 3s)
                    if (me.CloseContactTime[other.Id] >= SimConfig.CONTACT_TIME_REQUIRED)
                    {
                        // Próba zarażenia
                        TryInfect(me, other);

                        // Po próbie (udanej lub nie) resetujemy licznik dla tej pary, 
                        // żeby nie losować co klatkę setki razy
                        me.CloseContactTime[other.Id] = 0;
                    }
                }
            }

            // Czyszczenie kontaktów, które się oddaliły
            // Jeśli kogoś nie ma w currentContacts, usuwamy go z licznika czasu
            var idsToRemove = me.CloseContactTime.Keys.Where(k => !currentContacts.Contains(k)).ToList();
            foreach (var id in idsToRemove)
            {
                me.CloseContactTime.Remove(id);
            }
        }

        private void TryInfect(Individual me, Individual source)
        {
            double chance = 0;

            // Prawdopodobieństwo zależne od objawów źródła
            if (source.State is InfectedAsymptomaticState) chance = 0.50; // 50%
            else if (source.State is InfectedSymptomaticState) chance = 1.00; // 100%

            // Rzut kostką
            if (new Random().NextDouble() < chance)
            {
                // Zarażenie! Losujemy czy będzie objawowy czy nie (50/50 - założenie z kodu głównego)
                bool symptomatic = new Random().NextDouble() < 0.5;

                if (symptomatic) me.State = new InfectedSymptomaticState();
                else me.State = new InfectedAsymptomaticState();

                // Reset czasu infekcji
                me.InfectionTime = 0;
            }
        }
    }
}