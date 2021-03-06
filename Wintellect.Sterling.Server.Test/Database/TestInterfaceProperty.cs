
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

using System.Collections.Generic;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;

namespace Wintellect.Sterling.Test.Database
{
    public interface IInterface
    {
        int Id { get; }
        int Value { get; }
    }

    public class InterfaceClass : IInterface
    {
        public int Id { get; set; }
        public int Value { get; set; }        
    }

    public class TargetClass
    {
        public int Id { get; set; }
        public IInterface SubInterface { get; set; }
    }

    public class InterfaceDatabase : BaseDatabaseInstance
    {
        /// <summary>
        ///     Method called from the constructor to register tables
        /// </summary>
        /// <returns>The list of tables for the database</returns>
        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
                           {
                               CreateTableDefinition<TargetClass, int>(n => n.Id)
                           };
        }
    }

#if SILVERLIGHT
    [Tag("Interface")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestInterfacePropertyAltDriver : TestInterfaceProperty
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
    [Tag("Interface")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestInterfaceProperty : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {            
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<InterfaceDatabase>( TestContext.TestName, GetDriver() );
            _databaseInstance.PurgeAsync().Wait();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod]
        public void TestInterface()
        {
            var test = new TargetClass { Id = 1, SubInterface = new InterfaceClass { Id = 5, Value = 6 }};

            _databaseInstance.SaveAsync( test ).Wait();

            var actual = _databaseInstance.LoadAsync<TargetClass>( 1 ).Result;
            
            Assert.AreEqual(test.Id, actual.Id, "Failed to load class with interface property: key mismatch.");
            Assert.IsNotNull(test.SubInterface, "Failed to load class with interface property: interface property is null.");
            Assert.AreEqual(test.SubInterface.Id, actual.SubInterface.Id, "Failed to load class with interface property: interface id mismatch.");
            Assert.AreEqual(test.SubInterface.Value, actual.SubInterface.Value, "Failed to load class with interface property: value mismatch.");            
        }       
    }
}