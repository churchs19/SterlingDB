
#if NETFX_CORE
using Wintellect.Sterling.WinRT.WindowsStorage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#elif SILVERLIGHT
using Microsoft.Phone.Testing;
#if WP7
using Wintellect.Sterling.WP7.IsolatedStorage;
#else
using Wintellect.Sterling.WP8.IsolatedStorage;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Wintellect.Sterling.Server.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Linq;

using System.Collections.Generic;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;

namespace Wintellect.Sterling.Test.Database
{
    public abstract class BaseNested
    {
        public Guid Id { get; set; }
    }

    public class Bill : BaseNested
    {
        public Bill()
        {
            Partakers = new List<Partaker>();
        }

        public string Name { get; set; }
        public List<Partaker> Partakers { get; set; }
        public double Total { get; set; }
    }

    public class Person : BaseNested
    {
        public string Name { get; set; }
    }

    public class Partaker : BaseNested
    {
        public double Paid { get; set; }
        public Person Person { get; set; }
    }

    public class NestedInstancesDatabase : BaseDatabaseInstance
    {       
        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
        {
            CreateTableDefinition<Bill, Guid>( b => b.Id ),
            CreateTableDefinition<Person, Guid>( p => p.Id )
        };
        }
    }

#if SILVERLIGHT
    [Tag("Nested")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestNestedInstanceAltDriver : TestNestedInstance
    {
        protected override ISterlingDriver GetDriver()
        {
#if NETFX_CORE
            return new WindowsStorageDriver();
#elif SILVERLIGHT
            return new IsolatedStorageDriver();
#elif AZURE_DRIVER
            return new Wintellect.Sterling.Server.Azure.TableStorage.Driver();
#else
            return new FileSystemDriver();
#endif
        }
    }

#if SILVERLIGHT
    [Tag("Nested")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestNestedInstance : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _database;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Init()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _database = _engine.SterlingDatabase.RegisterDatabase<NestedInstancesDatabase>(TestContext.TestName, GetDriver());
        }

        [TestCleanup]
        public void Shutdown()
        {
            if (_engine == null) return;

            _engine.Dispose();
            _engine = null;
            _database = null;
        }

        [TestMethod]
        public void TestAddBill()
        {
            _database.PurgeAsync().Wait();

            var bill = new Bill
                           {
                Id = Guid.NewGuid(),
                Name = "Test"
            };

            _database.SaveAsync( bill ).Wait();
            
            var person1 = new Person
                              {
                Id = Guid.NewGuid(),
                Name = "Martin"
            };

            _database.SaveAsync( person1 ).Wait();

            var partaker1 = new Partaker
                                {
                Id = Guid.NewGuid(),
                Paid = 42,
                Person = person1
            };

            bill.Partakers.Add(partaker1);

            _database.SaveAsync( bill ).Wait();

            var person2 = new Person
                              {
                Id = Guid.NewGuid(),
                Name = "Jeremy"
            };

            _database.SaveAsync( person2 ).Wait();
            
            var partaker2 = new Partaker
                                {
                Id = Guid.NewGuid(),
                Paid = 0,
                Person = person2
            };

            bill.Partakers.Add(partaker2);

            _database.SaveAsync( bill ).Wait();

            var partaker3 = new Partaker()
                                {
                                    Id = Guid.NewGuid(),
                                    Paid = 1,
                                    Person = person1
                                };

            bill.Partakers.Add(partaker3);

            _database.SaveAsync( bill ).Wait();

            _database.FlushAsync().Wait();
            
            var billKeys = _database.Query<Bill, Guid>();

            Assert.IsTrue(billKeys.Count == 1);
            Assert.AreEqual(billKeys[0].Key, bill.Id);

            var freshBill = billKeys[0].LazyValue.Value;

            Assert.IsTrue(freshBill.Partakers.Count == 3, "Bill should have exactly 3 partakers.");            

            var personKeys = _database.Query<Person, Guid>();

            Assert.IsTrue(personKeys.Count == 2, "Failed to save exactly 2 persons.");            
            
            // Compare loaded instances and verify they are equal 
            var persons = (from p in freshBill.Partakers where p.Person.Id.Equals(person1.Id) select p.Person).ToList();

            // should be two of these
            Assert.AreEqual(2, persons.Count, "Failed to grab two instances of the same person.");
            Assert.AreEqual(persons[0], persons[1], "Instances were not equal.");
        }
    }
}
