using System;
using System.Collections.Generic;
using System.Reflection;
using Extensions;
using FluentAssertions;
using ORM.Contracts;
using ORM.Db;

namespace ORM
{
    public class DataContext : IDataContext
    {
        private readonly IDbEngine dbEngine;
        private Dictionary<string, Book> cashForUpdate = new Dictionary<string, Book>();
        private Dictionary<string, Book> cashForPaste = new Dictionary<string, Book>();
        //private List<Book> cashForUpdateList = new List<Book>();
        //private List<Book> cashForPasteList = new List<Book>();

        public DataContext(IDbEngine dbEngine)
        {
            this.dbEngine = dbEngine;
        }

        public Book Find(string id)
        {
            if (id.Contains(","))
            {
                throw new Exception("Write only one Id");
            }
            var resString = dbEngine.Execute($"get Id={id};");
            if (resString == ";")
            {
                return null;
            }
            else
            {
                var resBook = ParseStringToBook(resString);

                if (!cashForUpdate.ContainsKey(id))
                {
                    cashForUpdate[id] = resBook;
                }

                return cashForUpdate[id];
            }

        }

        public Book Read(string id)
        {
            var resString = dbEngine.Execute($"get Id={id};");
            if (resString == ";")
            {
                throw new Exception();
            }

            var resBook = ParseStringToBook(resString);
            if (!cashForUpdate.ContainsKey(id))
            {
                cashForUpdate[id] = resBook;
                
            }
            return cashForUpdate[id];
            
        }

        public void Insert(Book entity)
        {
            if (entity == null)
            {
                throw new Exception();
            }
            if (!cashForPaste.ContainsKey(entity.Id))
            {
                cashForPaste[entity.Id] = entity;
            }
            
        }

        public void SubmitChanges()
        {
            // зачем всё в один резалт класть? он же вроде только для одной команды?
            //+команды заканчиваются с ;
            var result = "";
            foreach (var book in cashForPaste)
            {
                var strBook = ParseBookToString(book.Value);
                result += "add " + strBook;
            }
            foreach (var book in cashForUpdate)
            {
                var strBook = ParseBookToString(book.Value);
                result += "upd " + strBook;
            }
            if (dbEngine.Execute(result).Contains("err"))
            {
                throw new Exception(dbEngine.Execute(result)+" with out line "+result);
            }

            try
            {
                cashForUpdate.Clear();
            }
            catch
            {
                
            }

            foreach (var book in cashForPaste)
            {
                cashForUpdate[book.Key] = book.Value;
            }
            cashForPaste.Clear();
        }

        public Book ParseStringToBook(string input)
        {
            Book newBook = new Book();
            input = input.Replace(";", "");
            string[] parsedString = input.Split(',');
            bool alreadyExist = false;
            foreach (var field in parsedString)
            {
                string[] subFields = field.Split('=');
                switch (subFields[0])
                {
                    case "Id":
                        if (alreadyExist)
                        {
                            throw new Exception();
                        }
                        newBook.Id = subFields[1];
                        alreadyExist = true;
                        break;
                    case "Title":
                        newBook.Title = subFields[1];
                        break;
                    case "Price":
                        try
                        {
                            newBook.Price = int.Parse(subFields[1]);
                        }
                        catch
                        {
                            throw new Exception();
                        }
                        break;
                    case "Weight":
                        try
                        {
                            newBook.Weight = decimal.Parse(subFields[1]);
                        }
                        catch
                        {
                            throw new Exception();
                        }
                        break;
                    case "Author":
                        newBook.Author = subFields[1];
                        break;
                    case "Skill":
                        newBook.Skill = subFields[1];
                        break;
                }
            }
            return newBook;
        }

        public string ParseBookToString(Book book)
        {
            return $"Id={book.Id},Title={book.Title},Author={book.Author},Price={book.Price}," +
                      $"Weight={book.Weight},Skill={book.Skill};";
        }
    }
}

namespace Extensions
{
    using ORM.Contracts;

    public static class BookParsersExtentions
    {
        public static string BookToString(this Book book)
        {
            var result = "";
            foreach (var field in book.GetType().Properties())
            {
                switch (field.Name)
                {
                    case "Id":
                        result = result.Insert(0, field.Name+"="+field.GetValue(book).ToString()+",");
                        break;
                    default:
                        result += field.Name + "=" + field.GetValue(book) + ",";
                        break;
                }
            }
            if (result!="")
            {
                result = result.Remove(result.Length);
            }

            return result;
        }
    }
}