using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.Entities
{
    public class Tire
    {
        public long Id { get; set; }
        public string? Brand { get; set; }

        public ICollection<Bolt> Bolts { get; set; }

        [ForeignKey(nameof(Tire))]
        public Car ParentCar { get; set; }
    }
}