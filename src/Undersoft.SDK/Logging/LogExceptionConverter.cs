﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace Undersoft.SDK.Logging
{
    public class LogExceptionConverter : JsonConverter<Exception>
    {
        public override Exception Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            throw new NotImplementedException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            Exception value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteString("Message", value.Message);
            writer.WriteString("HelpLink", value.HelpLink);
            writer.WriteString("HResult", value.HResult.ToString());
            writer.WriteString("Host", value.Source);
            writer.WriteString("StackTrace", value.StackTrace);
            writer.WriteEndObject();
        }
    }
}
