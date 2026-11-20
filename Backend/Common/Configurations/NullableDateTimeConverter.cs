using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Common.Configurations
{
    public class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringVal = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringVal))
                {
                    return null;
                }

                if (DateTime.TryParse(stringVal, out var dt))
                {
                    return dt;
                }
            }

            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
