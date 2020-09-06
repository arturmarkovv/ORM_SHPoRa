using System;
using Extensions;
using NUnit.Framework;
using ORM.Contracts;

namespace ORM.Tests
{
    [TestFixture]
    public class BookExtentionsTests
    {
        [Test]
        public void BookToStringTest()
        {
            var book = new Book
            {
                Author = "Auth",
                Title = "T",
                Id = "1",
                Price = 1,
                Skill = "S",
                Weight = decimal.One
            };
            var bookStr = book.BookToString();
        }
    }
}