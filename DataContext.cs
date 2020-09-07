using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Extensions;
using ORM.Contracts;
using ORM.Db;

namespace ORM
{
    public class DataContext : IDataContext
    {
        private readonly IDbEngine _dbEngine;
        private Dictionary<string, Book> _cashForUpdate = new Dictionary<string, Book>();
        private Dictionary<string, Book> _cashForPaste = new Dictionary<string, Book>();
        private Dictionary<string, Book> _cashForUpdateDB = new Dictionary<string, Book>();

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
            var resBook = StringToBookParser(resString);
            _cashForUpdateDB[id] = resBook.Copy();
            _cashForUpdate[id] = resBook;

            return _cashForUpdate[id];
            

        }

        public Book Read(string id)
        {
            if (_cashForUpdate.ContainsKey(id)) return _cashForUpdate[id];
            var resString = _dbEngine.Execute($"get Id={id};");
            if (resString == ";")
            {
                throw new Exception("Id doesn't exist");
            }
            var resBook = StringToBookParser(resString);
            _cashForUpdateDB[id] = resBook.Copy();
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
            var stringRequest = "";
            foreach (var book in _cashForPaste)
            {
                var screenedBook = book.Value.ScreenSymbols();
                var strBook = screenedBook.ToStringRequest();
                stringRequest += "add " + strBook;
            }
            foreach (var book in _cashForUpdate)
            {
                var screenedBook = book.Value.ScreenSymbols();
                var strBook = screenedBook.ToStringRequest();
                
                if (_cashForUpdate[book.Key] != _cashForUpdateDB[book.Key])
                {
                    stringRequest += "upd " + strBook;
                }
            }
            if (_dbEngine.Execute(stringRequest).Contains("err"))
            {
                throw new Exception(_dbEngine.Execute(stringRequest)+" with request "+stringRequest);
            }

            foreach (var book in _cashForPaste)
            {
                _cashForUpdate[book.Key] = book.Value;
            }
            _cashForPaste.Clear();
        }

        private Book StringToBookParser(string input)
        {
            input = input.TrimEnd(';');
            var book = new Book();
            var regField = new Regex(@"[A-Z]{1}[A-Za-z0-9]*=");
            
            var fieldValues = regField.Split(input);
            var fieldNamesMatches = regField.Matches(input);
            var fieldDict = new Dictionary<string, string>();
            
            for (var i = 0; i < fieldNamesMatches.Count; i++)
            {
                fieldDict[fieldNamesMatches[i].Value.TrimEnd('=')] = Regex.Unescape(fieldValues[i+1]).TrimEnd(',');
            }

            foreach (var field in fieldDict)
            {
                switch (field.Key)
                {
                    case "Id":
                        book.Id = field.Value;
                        break;
                    case "Title":
                        book.Title = field.Value;
                        break;
                    case "Price":
                        try
                        {
                            book.Price = int.Parse(field.Value);
                        }
                        catch
                        {
                            throw new Exception("Invalid Price value");
                        }
                        break;
                    case "Weight":
                        try
                        {
                            book.Weight = decimal.Parse(field.Value);
                        }
                        catch
                        {
                            throw new Exception("Invalid Weight value");
                        }
                        break;
                    case "Author":
                        book.Author = field.Value;
                        break;
                    case "Skill":
                        book.Skill = field.Value;
                        break;
                }
            }
            return book;
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
            foreach (var propertyInfo in book.GetType().GetProperties())
            {
                switch (propertyInfo.Name)
                {
                    case "Id":
                        result = result.Insert(0, propertyInfo.Name+"="+propertyInfo.GetValue(book)+",");
                        break;
                    default:
                        if (propertyInfo.GetValue(book) != null)
                        {
                            if (propertyInfo.GetValue(book).ToString() != "0")
                            {
                                result += propertyInfo.Name + "=" + propertyInfo.GetValue(book) + ",";
                            }
                        }
                        break;
                }
            }

            if (result != "")
            {
                result = result.TrimEnd(',');
            }
            return result+";";
        }
        
        public static Book ScreenSymbols(this Book inputBook)
        {
            var screenedBook = new Book();
            foreach (var property in inputBook.GetType().GetProperties())
            {
                if (property.GetValue(inputBook)==null) continue;
                var value = property.GetValue(inputBook).ToString();
                var screenedValue = "";
                foreach (var latter in value)
                {
                    if (latter == ',' || latter == ';' || latter == '=' || latter == '\\')
                    {
                        screenedValue += $@"\{latter}";
                    }
                    else
                    {
                        screenedValue += latter;
                    }
                }
                value = screenedValue;
                var typeName = property.PropertyType.Name ;
                switch (typeName)
                {
                    case "Int32":
                        try
                        {
                            property.SetValue(screenedBook, int.Parse(value));
                        }
                        catch
                        {
                            throw new Exception("Invalid int field value");
                        }
                        break;
                    case "Decimal":
                        try
                        {
                            property.SetValue(screenedBook, decimal.Parse(value));
                        }
                        catch
                        {
                            throw new Exception("Invalid decimal field value");
                        }
                        break;
                    default:
                            property.SetValue(screenedBook, value);
                        break;
                }
            }

            return screenedBook;
        }

        public static Book Copy(this Book inputBook)
        {
            var resBook = new Book();
            foreach (var propertyInfo in inputBook.GetType().GetProperties())
            {
                propertyInfo.SetValue(resBook, propertyInfo.GetValue(inputBook));
            }
            return resBook;
        }
    }
}