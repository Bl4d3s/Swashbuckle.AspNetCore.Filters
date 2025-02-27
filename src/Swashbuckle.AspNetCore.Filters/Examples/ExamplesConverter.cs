using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Swashbuckle.AspNetCore.Filters
{
    internal class ExamplesConverter
    {
        private static readonly MediaTypeHeaderValue ApplicationXml = MediaTypeHeaderValue.Parse("application/xml; charset=utf-8");
        private static readonly MediaTypeHeaderValue ApplicationJson = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
        private static readonly MediaTypeHeaderValue TextCsv = MediaTypeHeaderValue.Parse("text/csv");

        private readonly MvcOutputFormatter mvcOutputFormatter;

        public ExamplesConverter(MvcOutputFormatter mvcOutputFormatter)
        {
            this.mvcOutputFormatter = mvcOutputFormatter;
        }

        public IOpenApiAny SerializeExampleCsv(object value)
        {
            var type = value.GetType();
            if (type.IsPrimitive || type.IsValueType || type == typeof(string))
            {
                return new OpenApiString(value.ToString());
            }

            try
            {
                return new OpenApiString(mvcOutputFormatter.Serialize(value, TextCsv));
            }
            catch (MvcOutputFormatter.FormatterNotFoundException)
            {
                return new OpenApiString("No formatter found");
            }
        }

        public IOpenApiAny SerializeExampleXml(object value)
        {
            return new OpenApiString(mvcOutputFormatter.Serialize(value, ApplicationXml).FormatXml());
        }

        public IOpenApiAny SerializeExampleJson(object value)
        {
            return new OpenApiString(mvcOutputFormatter.Serialize(value, ApplicationJson), false, true);
        }

        public IDictionary<string, OpenApiExample> ToOpenApiExamplesDictionaryXml(
            IEnumerable<ISwaggerExample<object>> examples)
        {
            return ToOpenApiExamplesDictionary(examples, SerializeExampleXml);
        }

        public IDictionary<string, OpenApiExample> ToOpenApiExamplesDictionaryCsv(
            IEnumerable<ISwaggerExample<object>> examples)
        {
            return ToOpenApiExamplesDictionary(examples, SerializeExampleCsv);
        }

        public IDictionary<string, OpenApiExample> ToOpenApiExamplesDictionaryJson(
            IEnumerable<ISwaggerExample<object>> examples)
        {
            return ToOpenApiExamplesDictionary(examples, SerializeExampleJson);
        }

        private static IDictionary<string, OpenApiExample> ToOpenApiExamplesDictionary(
            IEnumerable<ISwaggerExample<object>> examples,
            Func<object, IOpenApiAny> exampleConverter)
        {
            var groupedExamples = examples.GroupBy(
                ex => ex.Name,
                ex => new OpenApiExample
                {
                    Summary = ex.Summary,
                    Description = ex.Description,
                    Value = exampleConverter(ex.Value)
                });

            // If names are duplicated, only the first one is taken
            var examplesDict = groupedExamples.ToDictionary(
                grouping => grouping.Key,
                grouping => grouping.First());

            return examplesDict;
        }
    }
}