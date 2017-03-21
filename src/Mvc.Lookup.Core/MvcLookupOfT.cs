﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;

namespace NonFactors.Mvc.Lookup
{
    public abstract class MvcLookup<T> : MvcLookup where T : class
    {
        public Func<T, String> Id { get; set; }
        public Func<T, String> Autocomplete { get; set; }
        public virtual IEnumerable<PropertyInfo> AttributedProperties
        {
            get
            {
                return typeof(T)
                    .GetProperties()
                    .Where(property => property.IsDefined(typeof(LookupColumnAttribute), false))
                    .OrderBy(property => property.GetCustomAttribute<LookupColumnAttribute>(false).Position);
            }
        }

        protected MvcLookup()
        {
            Id = (model) => GetValue(model, "Id");
            Autocomplete = (model) => GetValue(model, Columns.Where(col => !col.Hidden).Select(col => col.Key).FirstOrDefault() ?? "");

            foreach (PropertyInfo property in AttributedProperties)
                Columns.Add(new LookupColumn(GetColumnKey(property), GetColumnHeader(property))
                {
                    Hidden = property.GetCustomAttribute<LookupColumnAttribute>(false).Hidden
                });
        }
        public virtual String GetColumnKey(PropertyInfo property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            return property.Name;
        }
        public virtual String GetColumnHeader(PropertyInfo property)
        {
            return property?.GetCustomAttribute<DisplayAttribute>(false)?.GetShortName();
        }

        public override LookupData GetData()
        {
            IQueryable<T> models = GetModels();
            IQueryable<T> selected = new T[0].AsQueryable();
            IQueryable<T> notSelected = Sort(FilterByRequest(models));

            if (Filter.Ids.Count == 0 && Filter.Selected.Count > 0)
                selected = Sort(FilterByIds(models, Filter.Selected));

            return FormLookupData(notSelected, selected, Page(notSelected));
        }
        public abstract IQueryable<T> GetModels();

        private IQueryable<T> FilterByRequest(IQueryable<T> models)
        {
            if (Filter.Ids.Count > 0)
                return FilterByIds(models, Filter.Ids);

            if (Filter.Selected.Count > 0)
                models = FilterByNotIds(models, Filter.Selected);

            if (Filter.AdditionalFilters.Count > 0)
                models = FilterByAdditionalFilters(models);

            return FilterBySearch(models);
        }
        public virtual IQueryable<T> FilterBySearch(IQueryable<T> models)
        {
            if (String.IsNullOrEmpty(Filter.Search))
                return models;

            List<String> queries = new List<String>();
            foreach (String property in Columns.Where(column => !column.Hidden).Select(column => column.Key))
                if (typeof(T).GetProperty(property)?.PropertyType == typeof(String))
                    queries.Add($"({property} != null && {property}.ToLower().Contains(@0))");

            if (queries.Count == 0) return models;

            return models.Where(String.Join(" || ", queries), Filter.Search.ToLower());
        }
        public virtual IQueryable<T> FilterByAdditionalFilters(IQueryable<T> models)
        {
            foreach (KeyValuePair<String, Object> filter in Filter.AdditionalFilters.Where(item => item.Value != null))
                if (filter.Value is IEnumerable && !(filter.Value is String))
                    models = models.Where($"@0.Contains({filter.Key})", filter.Value);
                else
                    models = models.Where($"({filter.Key} != null && {filter.Key} == @0)", filter.Value);

            return models;
        }
        public virtual IQueryable<T> FilterByIds(IQueryable<T> models, IList<String> ids)
        {
            PropertyInfo key = typeof(T).GetProperties()
                .FirstOrDefault(prop => prop.IsDefined(typeof(KeyAttribute))) ?? typeof(T).GetProperty("Id");

            if (key == null)
                throw new LookupException($"'{typeof(T).Name}' type does not have key or property named 'Id', required for automatic id filtering.");

            if (key.PropertyType == typeof(String))
                return models.Where($"@0.Contains({key.Name})", ids);

            if (IsNumeric(key.PropertyType))
                return models.Where($"@0.Contains(decimal({key.Name}))", TryParseDecimals(ids));

            throw new LookupException($"'{typeof(T).Name}.{key.Name}' property type has to be a string or a number.");
        }
        public virtual IQueryable<T> FilterByNotIds(IQueryable<T> models, IList<String> ids)
        {
            PropertyInfo key = typeof(T).GetProperties()
                .FirstOrDefault(prop => prop.IsDefined(typeof(KeyAttribute))) ?? typeof(T).GetProperty("Id");

            if (key == null)
                throw new LookupException($"'{typeof(T).Name}' type does not have key or property named 'Id', required for automatic id filtering.");

            if (key.PropertyType == typeof(String))
                return models.Where($"!@0.Contains({key.Name})", ids);

            if (IsNumeric(key.PropertyType))
                return models.Where($"!@0.Contains(decimal({key.Name}))", TryParseDecimals(ids));

            throw new LookupException($"'{typeof(T).Name}.{key.Name}' property type has to be a string or a number.");
        }

        public virtual IQueryable<T> Sort(IQueryable<T> models)
        {
            if (String.IsNullOrWhiteSpace(Filter.Sort))
                return models.OrderBy(model => 0);

            return models.OrderBy(Filter.Sort + " " + Filter.Order);
        }

        public virtual IQueryable<T> Page(IQueryable<T> models)
        {
            return models
                .Skip(Filter.Page * Filter.Rows)
                .Take(Math.Min(Filter.Rows, 99));
        }

        public virtual LookupData FormLookupData(IQueryable<T> filtered, IQueryable<T> selected, IQueryable<T> notSelected)
        {
            LookupData data = new LookupData();
            data.FilteredRows = filtered.Count();
            data.Columns = Columns;

            foreach (T model in selected.Concat(notSelected))
            {
                Dictionary<String, String> row = new Dictionary<String, String>();
                AddId(row, model);
                AddAutocomplete(row, model);
                AddData(row, model);

                data.Rows.Add(row);
            }

            return data;
        }
        public virtual void AddId(Dictionary<String, String> row, T model)
        {
            row[IdKey] = Id(model);
        }
        public virtual void AddAutocomplete(Dictionary<String, String> row, T model)
        {
            row[AcKey] = Autocomplete(model);
        }
        public virtual void AddData(Dictionary<String, String> row, T model)
        {
            foreach (LookupColumn column in Columns)
                row[column.Key] = GetValue(model, column.Key);
        }

        private List<Decimal> TryParseDecimals(IList<String> values)
        {
            List<Decimal> numbers = new List<Decimal>();
            foreach (String value in values)
                if (Decimal.TryParse(value, out Decimal number))
                    numbers.Add(number);

            return numbers;
        }
        private String GetValue(T model, String propertyName)
        {
            PropertyInfo property = typeof(T).GetProperty(propertyName);
            if (property == null) return null;

            LookupColumnAttribute column = property.GetCustomAttribute<LookupColumnAttribute>(false);
            if (column?.Format != null) return String.Format(column.Format, property.GetValue(model));

            return property.GetValue(model)?.ToString();
        }
        private Boolean IsNumeric(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }
    }
}
