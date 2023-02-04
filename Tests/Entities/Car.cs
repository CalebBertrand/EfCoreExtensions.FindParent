namespace Tests.Entities
{
    public class Car
    {
        public long Id { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }

        public ICollection<Tire> Tires { get; set; }
    }
}