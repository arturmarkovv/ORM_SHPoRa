using System;
using NUnit.Framework;
using ORM.Contracts;
using ORM.Db;
using FluentAssertions;

namespace ORM.Tests
{
    [TestFixture]
    public class DataContextTests
    {
        [Test]
        public void FindWhenOkTest()
        {
            var dbEngine = new DbEngine();
            dbEngine.Execute("add Id=000243DE,Title=The Ransom of Zarek,Price=35,Weight=1,Author=Marobar Sul,Skill=Athletics;");
            dbEngine.Execute("add Id=000243EC,Title=The Warp in the West,Price=25,Weight=1,Author=Ulvius Tero,Skill=Block;");

            var dataContext = new DataContext(dbEngine);
            var book = dataContext.Find("000243EC");
            book.Title.Should().Be("The Warp in the West");
        }
    }
}