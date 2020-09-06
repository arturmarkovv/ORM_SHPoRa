using System;
using System.Management.Instrumentation;
using Extensions;
using NUnit.Framework;
using ORM.Contracts;

namespace ORM.Tests
{
    /* Написала тебе парочку тестов и поправила конвертер;
     У тебя открыт git-extentions сейчас, там на центральном окне коммиты и комменты к ним,
     можешь посмотреть историю изменений
     
     1) В конвертере ты шёл по полям, а не по свойствам - разные штуки
     2) запрос заполняешь, как 'add id = 1, title = 2; add ..." так точно можно? (хз может я тупень, но проверь это потом)
     3) фигачишь тесты на разные случаи, а потом смотришь, как работает через дебаг и брейкпоинты
     4) пользуйся гитом
     5) лучше назвать extention "ToStringRequest",
      а то выглядит как book.BookToString();
     
     "Название теста: Find_MultipleRequestsBySameId_SendsOnlyOneDbQuery
Сообщение:
  Expected: 1
  But was:  2"
  
  DbQuery - запрос к БД, кажется, они тестят, сколько одинаковых запросов ты кидаешь в бд
  Им не нравится, что ты не юзаешь кэш (книги после Find-a должны класться в update,
   а ты постоянно тыкаешься в банку, что не оптимально
   
   
   Балуй себя вкусняшками, переводи ошибки и
   не сиди больше до 5 утра
   
   С любовью
     
     */
    [TestFixture]
    public class BookExtensionsTests
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
            var bookStr = book.ToStringRequest();
        }

        [Test]
        public void ScreeningTest()
        {
            var book = new Book
            {
                Author = "Robert P, Stiv J",
                Title = "AC/DC",
                Id = "123",
                Skill = "News=sweN;",
                Price = 1,
                Weight = decimal.One
            };
            var bookStr = book.ToStringRequest();
            Console.WriteLine(bookStr);
        }
    }
}