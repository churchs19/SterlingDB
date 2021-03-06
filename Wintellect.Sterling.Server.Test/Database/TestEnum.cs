
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
    public enum TestEnums : short
    {
        Value1,
        Value2,
        Value3
    }

    public enum TestEnumsLong : long
    {
        LongValue = 500,
        LongerValue = 1000
    }

    public class EnumClass
    {
        public int Id { get; set; }
        public TestEnums Value { get; set; }
        public TestEnumsLong ValueLong { get; set; }
    }

    public class EnumDatabase : BaseDatabaseInstance
    {
        /// <summary>
        ///     Method called from the constructor to register tables
        /// </summary>
        /// <returns>The list of tables for the database</returns>
        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
                           {
                               CreateTableDefinition<EnumClass, int>(e => e.Id)
                           };
        }
    }

#if SILVERLIGHT
    [Tag("Enum")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestEnumAltDriver : TestEnum
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
    [Tag("Enum")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestEnum : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {           
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<EnumDatabase>( TestContext.TestName, GetDriver() );
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
        public void TestEnumSaveAndLoad()
        {
            var test = new EnumClass() { Id = 1, Value = TestEnums.Value2, ValueLong = TestEnumsLong.LongerValue };
            _databaseInstance.SaveAsync( test ).Wait();
            var actual = _databaseInstance.LoadAsync<EnumClass>( 1 ).Result;
            Assert.AreEqual(test.Id, actual.Id, "Failed to load enum: key mismatch.");
            Assert.AreEqual(test.Value, actual.Value, "Failed to load enum: value mismatch.");
            Assert.AreEqual(test.ValueLong, actual.ValueLong, "Failed to load enum: value mismatch.");
        }

        [TestMethod]
        public void TestMultipleEnumSaveAndLoad()
        {
            var test1 = new EnumClass { Id = 1, Value = TestEnums.Value1, ValueLong = TestEnumsLong.LongValue };
            var test2 = new EnumClass { Id = 2, Value = TestEnums.Value2, ValueLong = TestEnumsLong.LongerValue };

            _databaseInstance.SaveAsync( test1 ).Wait();
            _databaseInstance.SaveAsync( test2 ).Wait();

            var actual1 = _databaseInstance.LoadAsync<EnumClass>( 1 ).Result;
            var actual2 = _databaseInstance.LoadAsync<EnumClass>( 2 ).Result;

            Assert.AreEqual(test1.Id, actual1.Id, "Failed to load enum: key 1 mismatch.");
            Assert.AreEqual(test1.Value, actual1.Value, "Failed to load enum: value 1 mismatch.");
            Assert.AreEqual(test1.ValueLong, actual1.ValueLong, "Failed to load enum: value 1 mismatch.");

            Assert.AreEqual(test2.Id, actual2.Id, "Failed to load enum: key 2 mismatch.");
            Assert.AreEqual(test2.Value, actual2.Value, "Failed to load enum: value 2 mismatch.");
            Assert.AreEqual(test2.ValueLong, actual2.ValueLong, "Failed to load enum: value 2 mismatch.");
        }

    }
}