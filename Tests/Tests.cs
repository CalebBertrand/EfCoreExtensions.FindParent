using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Tests.Entities;
using static FindParent.Extensions;

namespace Tests
{
    public class UnitTest1
    {
        public static readonly DbContextOptions ContextOptions = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        public static MyDbContext Context;

        public UnitTest1()
        {
            Context = SetUpContext();
        }
        
        [Fact]
        public void FindDirentParent()
        {
            var boltOnTire1 = Context.Tires
                .Where(t => t.Brand == "First Tire Brand")
                .Select(t => t.Bolts.First()).First();

            var parentTire = Context.Bolts
                .Where(t => t.Id == boltOnTire1.Id)
                .FindParent<Bolt, Tire>().First();

            Assert.Equal("First Tire Brand", parentTire.Brand);
        }
        
        [Fact]
        public void FindDistantParent()
        {
            var boltOnTire1 = Context.Tires
                .Where(t => t.Brand == "First Tire Brand")
                .Select(t => t.Bolts.First()).First();

            var parentCar = Context.Bolts
                .Where(t => t.Id == boltOnTire1.Id)
                .FindParent<Bolt, Car>().First();

            Assert.Equal("Kia", parentCar.Make);
        }
        
        [Fact]
        public void FindParentForMultiple()
        {
            var boltsOnTire1Ids = Context.Tires
                .First(t => t.Brand == "First Tire Brand")
                .Bolts.Select(b => b.Id)
                .ToImmutableHashSet();

            var parentCars = Context.Bolts
                .Where(t => boltsOnTire1Ids.Contains(t.Id))
                .FindParent<Bolt, Car>()
                .ToList();

            Assert.Equivalent(3, parentCars.Count);
            Assert.Equivalent(1, parentCars.DistinctBy(c => c.Id).Count());
            Assert.Equal("Kia", parentCars.First().Make);
        }

        private static MyDbContext SetUpContext()
        {
            var context = new MyDbContext(ContextOptions);
            SetContextModel(context.Model);

            var car = new Car
            {
                Make = "Kia",
                Tires = new List<Tire>
                {
                    new Tire
                    {
                        Brand = "First Tire Brand",
                        Bolts = new List<Bolt>{ new Bolt(), new Bolt(), new Bolt() }
                    },
                    new Tire
                    {
                        Brand = "Second Tire Brand",
                        Bolts = new List<Bolt>{ new Bolt(), new Bolt(), new Bolt() }
                    }
                }
            };
            context.Cars.Add(car);
            context.SaveChanges();

            return context;
        }
    }
}