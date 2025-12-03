namespace InfectionSimulation
{
    public class IndividualMemento
    {
        public int Id { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public string StateName { get; set; }
        public double InfectionTime { get; set; }
        public double InfectionDuration { get; set; }
    }
}