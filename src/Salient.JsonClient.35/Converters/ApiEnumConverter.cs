﻿using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Salient.JsonClient.Converters
{
    /// <summary>
    /// Converts an <see cref="Enum"/> to and from its name string value.
    /// </summary>
    public class ApiEnumConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var s = new StringWriter();
            var w = new JsonTextWriter(s);
            base.WriteJson(w, value, serializer);
            writer.WriteValue(s.ToString().ToLower().Trim('"'));
        }
    }
}