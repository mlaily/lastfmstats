using Microsoft.FSharp.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LastFmStatsServer
{
    public class FSharpUnionConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return FSharpType.IsUnion(typeToConvert, Microsoft.FSharp.Core.FSharpOption<System.Reflection.BindingFlags>.None);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(FSharpUnionConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }
    public class FSharpUnionConverter<T> : JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var unionCaseValue = FSharpValue.GetUnionFields(value, typeof(T), Microsoft.FSharp.Core.FSharpOption<System.Reflection.BindingFlags>.None);
            writer.WriteStringValue(unionCaseValue.Item1.Name);
        }
    }
}
