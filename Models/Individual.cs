using vectors2D;

namespace InfectionSimulation
{
    public class Individual
    {
        public int Id { get; set; }
        public Vector2D Position { get; set; }     // w pikselach
        public Vector2D Velocity { get; set; }     // w pikselach na sekundę
        public IIndividualState State { get; set; }
        public Dictionary<int, double> CloseContactTime { get; set; } // sekundy
        public double InfectionTime { get; set; } // sekundy
        public double InfectionDuration { get; set; } // sekundy
        public bool IsDeadOrLeft { get; private set; } = false;

        private Random _random;
        public readonly object StateLock = new object();

        // Stałe symulacji (zgodne z MainWindow)
        private const double PIXELS_PER_METER = 10.0;
        private const double MAX_SPEED_MPS = 2.5;
        private const double MAX_SPEED_PIXELS = MAX_SPEED_MPS * PIXELS_PER_METER;

        // Minimalna akceptowalna prędkość (żeby nie zamarzali)
        private const double MIN_SPEED_PIXELS = 0.05 * PIXELS_PER_METER; // 5 cm/s ~ 0.05 m/s

        public Individual(int id, double x, double y, bool isImmune)
        {
            Id = id;
            Position = new Vector2D(x, y);
            _random = new Random(Guid.NewGuid().GetHashCode());

            // Losujemy kierunek i prędkość w [0, MAX_SPEED_MPS] (m/s), potem konwertujemy do px/s
            double angle = _random.NextDouble() * 2.0 * Math.PI;
            double speedMps = _random.NextDouble() * MAX_SPEED_MPS; // [0..2.5] m/s
            double speedPixels = speedMps * PIXELS_PER_METER;

            Velocity = new Vector2D(Math.Cos(angle) * speedPixels, Math.Sin(angle) * speedPixels);

            State = isImmune ? (IIndividualState)new ImmuneState() : new HealthyState();
            CloseContactTime = new Dictionary<int, double>();
            InfectionTime = 0;
            InfectionDuration = 20.0 + _random.NextDouble() * 10.0;
        }

        public bool Update(double deltaTime, int width, int height, List<Individual> allIndividuals)
        {
            // --- 1. Sterowanie kierunkiem (probabilistycznie) ---
            // Prawdopodobienstwo zmian na 1 sekundę = 0.1 (przykład) -> przeskalowujemy na krok: p_step = p_sec * deltaTime
            double steeringProbabilityPerSecond = 0.10; // oczekiwana liczba zdarzeń na sekundę
            double pStep = steeringProbabilityPerSecond * deltaTime;

            if (_random.NextDouble() < pStep)
            {
                // mały kat skrętu, rzadziej duże
                double maxTurnRad = 0.25; // ~14 deg
                double turnAngle = (_random.NextDouble() - 0.5) * maxTurnRad;

                // Obrót wektora
                double newX = Velocity.X * Math.Cos(turnAngle) - Velocity.Y * Math.Sin(turnAngle);
                double newY = Velocity.X * Math.Sin(turnAngle) + Velocity.Y * Math.Cos(turnAngle);

                // Drobna zmiana prędkości: 0.98 - 1.02 (nie zabijamy prędkości)
                double speedChange = 0.98 + (_random.NextDouble() * 0.04);
                Vector2D potentialVelocity = new Vector2D(newX * speedChange, newY * speedChange);

                // Jeśli wektor jest bliski zeru, nadaj losowy niewielki kierunek z minimalną prędkością
                if (potentialVelocity.Abs() < 1e-6)
                {
                    double angle = _random.NextDouble() * Math.PI * 2.0;
                    double v = MIN_SPEED_PIXELS;
                    potentialVelocity = new Vector2D(Math.Cos(angle) * v, Math.Sin(angle) * v);
                }

                // Limit prędkości (max)
                double currentSpeed = potentialVelocity.Abs();
                if (currentSpeed > MAX_SPEED_PIXELS)
                {
                    double scale = MAX_SPEED_PIXELS / currentSpeed;
                    potentialVelocity = new Vector2D(potentialVelocity.X * scale, potentialVelocity.Y * scale);
                }

                // Minimalna prędkość (żeby nie "umierały" do zera)
                if (potentialVelocity.Abs() < MIN_SPEED_PIXELS)
                {
                    double scale = MIN_SPEED_PIXELS / (potentialVelocity.Abs() < 1e-6 ? 1e-6 : potentialVelocity.Abs());
                    potentialVelocity = new Vector2D(potentialVelocity.X * scale, potentialVelocity.Y * scale);
                }

                Velocity = potentialVelocity;
            }

            // --- 2. Jeżeli prędkość jest prawie zero, nadaj losowy kierunek i min prędkość ---
            if (Velocity.Abs() < MIN_SPEED_PIXELS)
            {
                double angle = _random.NextDouble() * Math.PI * 2.0;
                Velocity = new Vector2D(Math.Cos(angle) * MIN_SPEED_PIXELS, Math.Sin(angle) * MIN_SPEED_PIXELS);
            }

            // --- 3. Ruch: pozycja += velocity (pixels/s) * deltaTime (s) ---
            Position = new Vector2D(
                Position.X + Velocity.X * deltaTime,
                Position.Y + Velocity.Y * deltaTime
            );

            // --- 4. Obsługa granic ---
            if (Position.X <= 0 || Position.X >= width)
            {
                if (_random.NextDouble() < 0.5)
                    Velocity = new Vector2D(-Velocity.X, Velocity.Y); // odbicie
                else
                    IsDeadOrLeft = true; // opuszcza obszar
            }
            if (Position.Y <= 0 || Position.Y >= height)
            {
                if (_random.NextDouble() < 0.5)
                    Velocity = new Vector2D(Velocity.X, -Velocity.Y);
                else
                    IsDeadOrLeft = true;
            }

            // Clamp pozycji (żeby nie wypadli poza widok przy odbiciu)
            Position = new Vector2D(Math.Max(0, Math.Min(width, Position.X)), Math.Max(0, Math.Min(height, Position.Y)));

            // --- 5. Logika stanów (zarażanie i zdrowienie) ---
            lock (StateLock)
            {
                State.Update(this, deltaTime, allIndividuals);
            }

            return !IsDeadOrLeft;
        }

        // Memento pattern (bez zmian)
        public IndividualMemento SaveState()
        {
            return new IndividualMemento
            {
                Id = Id,
                PositionX = Position.X,
                PositionY = Position.Y,
                VelocityX = Velocity.X,
                VelocityY = Velocity.Y,
                StateName = State.Name,
                InfectionTime = InfectionTime,
                InfectionDuration = InfectionDuration
            };
        }

        public static Individual RestoreState(IndividualMemento memento, Random random)
        {
            var individual = new Individual(memento.Id, memento.PositionX, memento.PositionY, false);
            individual.Velocity = new Vector2D(memento.VelocityX, memento.VelocityY);
            individual.InfectionTime = memento.InfectionTime;
            individual.InfectionDuration = memento.InfectionDuration;

            individual.State = memento.StateName switch
            {
                "healthy" => new HealthyState(),
                "infected_asymptomatic" => new InfectedAsymptomaticState(),
                "infected_symptomatic" => new InfectedSymptomaticState(),
                "immune" => new ImmuneState(),
                _ => new HealthyState()
            };

            return individual;
        }
    }
}
