using vectors2D;

namespace InfectionSimulation
{
    public class Individual
    {
        public int Id { get; set; }
        public Vector2D Position { get; set; }
        public Vector2D Velocity { get; set; }
        public IIndividualState State { get; set; }
        public Dictionary<int, double> CloseContactTime { get; set; }
        public double InfectionTime { get; set; }
        public double InfectionDuration { get; set; }
        public bool IsDeadOrLeft { get; set; }

        private const double PIXELS_PER_METER = 10.0;
        private const double MIN_SPEED_PIXELS = 0.5 * PIXELS_PER_METER;
        private const double MAX_SPEED_PIXELS = 2.5 * PIXELS_PER_METER;


        public Individual(int id, double x, double y, bool isImmune, Random random)
        {
            Id = id;
            Position = new Vector2D(x, y);

            double angle = random.NextDouble() * 2.0 * Math.PI;
            double speed = MIN_SPEED_PIXELS + random.NextDouble() * (MAX_SPEED_PIXELS - MIN_SPEED_PIXELS);
            Velocity = new Vector2D(Math.Cos(angle) * speed, Math.Sin(angle) * speed);

            State = isImmune ? new ImmuneState() : new HealthyState();
            CloseContactTime = new Dictionary<int, double>();
            InfectionDuration = 20.0 + random.NextDouble() * 10.0;
        }

        public void Update(double dt, int width, int height, List<Individual> all, Random random)
        {
            // Losowe skręty (10% szans na sekundę)
            if (random.NextDouble() < 0.1 * dt)
            {
                double turn = (random.NextDouble() - 0.5) * 0.3;
                double cos = Math.Cos(turn);
                double sin = Math.Sin(turn);
                double newX = Velocity.X * cos - Velocity.Y * sin;
                double newY = Velocity.X * sin + Velocity.Y * cos;

                Velocity = new Vector2D(newX, newY);

                // Utrzymuj prędkość w zakresie
                double speed = Velocity.Abs();
                if (speed < MIN_SPEED_PIXELS || speed > MAX_SPEED_PIXELS)
                {
                    double target = Math.Clamp(speed, MIN_SPEED_PIXELS, MAX_SPEED_PIXELS);
                    double scale = target / speed;
                    Velocity = new Vector2D(Velocity.X * scale, Velocity.Y * scale);
                }
            }

            // Ruch
            Position = new Vector2D(
                Position.X + Velocity.X * dt,
                Position.Y + Velocity.Y * dt
            );

            // Granice - odbicie lub wyjście
            if (Position.X <= 0 || Position.X >= width)
            {
                if (random.NextDouble() < 0.5)
                    Velocity = new Vector2D(-Velocity.X, Velocity.Y);
                else
                    IsDeadOrLeft = true;
            }

            if (Position.Y <= 0 || Position.Y >= height)
            {
                if (random.NextDouble() < 0.5)
                    Velocity = new Vector2D(Velocity.X, -Velocity.Y);
                else
                    IsDeadOrLeft = true;
            }

            Position = new Vector2D(
                Math.Clamp(Position.X, 0, width),
                Math.Clamp(Position.Y, 0, height)
            );

            // Logika stanu (zarażanie)
            State.Update(this, dt, all, random);
        }

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

        public static Individual RestoreState(IndividualMemento m, Random random)
        {
            var ind = new Individual(m.Id, m.PositionX, m.PositionY, false, random);
            ind.Velocity = new Vector2D(m.VelocityX, m.VelocityY);
            ind.InfectionTime = m.InfectionTime;
            ind.InfectionDuration = m.InfectionDuration;
            ind.State = m.StateName switch
            {
                "healthy" => new HealthyState(),
                "infected_asymptomatic" => new InfectedAsymptomaticState(),
                "infected_symptomatic" => new InfectedSymptomaticState(),
                "immune" => new ImmuneState(),
                _ => new HealthyState()
            };
            return ind;
        }
    }
}