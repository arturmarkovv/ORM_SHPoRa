using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Extensions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using ORM.Contracts;
using ORM.Db;

namespace ORM
{
    public class DataContext : IDataContext
    {
        private readonly IDbEngine _dbEngine;
        private Dictionary<string, Book> _cashForUpdate = new Dictionary<string, Book>();
        private Dictionary<string, Book> _cashForPaste = new Dictionary<string, Book>();

        public DataContext(IDbEngine dbEngine)
        {
            this._dbEngine = dbEngine;
        }

        public Book Find(string id)
        {
            if (_cashForUpdate.ContainsKey(id)) return _cashForUpdate[id];
            var resString = _dbEngine.Execute($"get Id={id};");
            if (resString == ";")
            {
                return null;
            }
            var resBook = ParseStringToBook(resString);
            _cashForUpdate[id] = resBook;

            return _cashForUpdate[id];
            

        }

        public Book Read(string id)
        {
            if (_cashForUpdate.ContainsKey(id)) return _cashForUpdate[id];
            var resString = _dbEngine.Execute($"get Id={id};");
            if (resString == ";")
            {
                throw new Exception("Doesn't exist");
            }

            var resBook = ParseStringToBook(resString);
            _cashForUpdate[id] = resBook;
            return _cashForUpdate[id];
            
        }

        public void Insert(Book entity)
        {
            if (entity == null)
            {
                throw new Exception("Empty request");
            }
            if (!_cashForPaste.ContainsKey(entity.Id))
            {
                _cashForPaste[entity.Id] = entity;
            }
            
        }

        public void SubmitChanges()
        {
            var result = "";
            foreach (var book in _cashForPaste)
            {
                var screenedBook = book.Value.ScreenSymbols();
                var strBook = screenedBook.ToStringRequest();
                result += "add " + strBook;
            }
            foreach (var book in _cashForUpdate)
            {
                var screenedBook = book.Value.ScreenSymbols();
                var strBook = screenedBook.ToStringRequest();
                result += "upd " + strBook;
            }
            if (_dbEngine.Execute(result).Contains("err"))
            {
                throw new Exception(_dbEngine.Execute(result)+" with request "+result);
            }

            foreach (var book in _cashForPaste)
            {
                _cashForUpdate[book.Key] = book.Value;
            }
            _cashForPaste.Clear();
        }

        private Book ParseStringToBook(string input)
        {
            var newBook = new Book();
            input = input.Replace(";", "");
            var parsedString = input.Split(',');
            var idAlreadyExist = false;
            foreach (var field in parsedString)
            {
                var subFields = field.Split('=');
                switch (subFields[0])
                {
                    case "Id":
                        if (idAlreadyExist)
                        {
                            throw new Exception();
                        }
                        newBook.Id = subFields[1];
                        idAlreadyExist = true;
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
                            throw new Exception("Invalid Price value");
                        }
                        break;
                    case "Weight":
                        try
                        {
                            newBook.Weight = decimal.Parse(subFields[1]);
                        }
                        catch
                        {
                            throw new Exception("Invalid Weight value");
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

    }
}

namespace Extensions
{
    using ORM.Contracts;

    public static class BookParsersExtensions
    {
        public static string ToStringRequest(this Book book)
        {
            var result = "";
            foreach (var field in book.GetType().GetProperties())
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

            if (result != "")
            {
                result = result.TrimEnd(',');
            }
            return result+";";
        }
        
        public static Book ScreenSymbols(this Book book)
        {
            var screenedBook = new Book();
            foreach (var field in book.GetType().GetProperties())
            {
                if (field.GetValue(book)==null) continue;
                var target =  field.GetValue(book).ToString();
                var result = target;
                var delimiters = new string[] {"/", ",", ";", "="};
                foreach (var delimiter in delimiters)
                {
                    if (target.Contains(delimiter))
                    {
                        result = result.Replace(delimiter, "/" + delimiter);
                    }
                }

                var typeName = field.PropertyType.Name ;
                switch (typeName)
                {
                    case "Int32":
                        try
                        {
                            field.SetValue(screenedBook, int.Parse(result));
                        }
                        catch
                        {
                            throw new Exception("Invalid int field value");
                        }
                        break;
                    case "Decimal":
                        try
                        {
                            field.SetValue(screenedBook, decimal.Parse(result));
                        }
                        catch
                        {
                            throw new Exception("Invalid decimal field value");
                        }
                        break;
                    default:
                            field.SetValue(screenedBook, result);
                        break;
                }
            }

            return screenedBook;
        }
    }
}