#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Globalization;
using Exceptionless.Json.Utilities;

namespace Exceptionless.Json.Converters
{
    /// <summary>
    /// Converts a <see cref="DateTime"/> to and from a JavaScript date constructor (e.g. new Date(52231943)).
    /// </summary>
    internal class JavaScriptDateTimeConverter : DateTimeConverterBase
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            long ticks;

            if (value is DateTime)
            {
                DateTime dateTime = (DateTime)value;
                DateTime utcDateTime = dateTime.ToUniversalTime();
                ticks = DateTimeUtils.ConvertDateTimeToJavaScriptTicks(utcDateTime);
            }
#if !NET20
            else if (value is DateTimeOffset)
            {
                DateTimeOffset dateTimeOffset = (DateTimeOffset)value;
                DateTimeOffset utcDateTimeOffset = dateTimeOffset.ToUniversalTime();
                ticks = DateTimeUtils.ConvertDateTimeToJavaScriptTicks(utcDateTimeOffset.UtcDateTime);
            }
#endif
            else
            {
                throw new JsonSerializationException("Expected date object value.");
            }

            writer.WriteStartConstructor("Date");
            writer.WriteValue(ticks);
            writer.WriteEndConstructor();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing property value of the JSON that is being converted.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                if (!ReflectionUtils.IsNullable(objectType))
                {
                    throw JsonSerializationException.Create(reader, "Cannot convert null value to {0}.".FormatWith(CultureInfo.InvariantCulture, objectType));
                }

                return null;
            }

            if (reader.TokenType != JsonToken.StartConstructor || !string.Equals(reader.Value.ToString(), "Date", StringComparison.Ordinal))
            {
                throw JsonSerializationException.Create(reader, "Unexpected token or value when parsing date. Token: {0}, Value: {1}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType, reader.Value));
            }

            reader.Read();

            if (reader.TokenType != JsonToken.Integer)
            {
                throw JsonSerializationException.Create(reader, "Unexpected token parsing date. Expected Integer, got {0}.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
            }

            long ticks = (long)reader.Value;

            DateTime d = DateTimeUtils.ConvertJavaScriptTicksToDateTime(ticks);

            reader.Read();

            if (reader.TokenType != JsonToken.EndConstructor)
            {
                throw JsonSerializationException.Create(reader, "Unexpected token parsing date. Expected EndConstructor, got {0}.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
            }

#if !NET20
            Type t = (ReflectionUtils.IsNullableType(objectType))
                ? Nullable.GetUnderlyingType(objectType)
                : objectType;
            if (t == typeof(DateTimeOffset))
            {
                return new DateTimeOffset(d);
            }
#endif

            return d;
        }
    }
}
