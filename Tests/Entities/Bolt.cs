using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.Entities
{
    public class Bolt
    {
        public long Id { get; set; }
        
        [ForeignKey(nameof(Tire))]
        public Tire ParentTire { get; set; }
    }
}