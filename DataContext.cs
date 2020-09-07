using System;
using System.Collections.Generic;
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
            //var resBook = ParseStringToBook(resString);
            var resBook = RegParser(resString);
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
            
            //var resBook = ParseStringToBook(resString);
            var resBook = RegParser(resString);
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

        private Book RegParser(string input)
        {
            var book = new Book();
            var regField = new Regex(@"[A-Z]{1}[A-Za-z0-9]*=");
            var fieldValues = regField.Split(input);
            var fieldNamesMatches = regField.Matches(input);
            var fieldDict = new Dictionary<string, string>();
            for (var i = 0;i<fieldNamesMatches.Count;i++)
            {
                fieldDict[fieldNamesMatches[i].Value.TrimEnd('=')] = UnScreen(fieldValues[i+1].TrimEnd(',',';'));
            }

            foreach (var field in fieldDict)
            {
                var idAlreadyExist = false;
                switch (field.Key)
                {
                    case "Id":
                        if (idAlreadyExist)
                        {
                            throw new Exception();
                        }
                        book.Id = field.Value;
                        idAlreadyExist = true;
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

        private string UnScreen(string input)
        {
			if (input.Contains(@"\"))
            {
                return input.Replace(@"\", "");
            }
            
            return input;
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
                var delimiters = new string[] {"/", ",", ";", "=",@"\"};
                foreach (var delimiter in delimiters)
                {
                    if (target.Contains(delimiter))
                    {
                        result = result.Replace(delimiter, @"\" + delimiter);
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