using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Salient.JsonSchemaUtilities
{
    public class Converter
    {
        private const string DATE_TIME_FORMAT = "wcf-date";


        static Type GetEnumerableType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            return (from intType in type.GetInterfaces()
                    where intType.IsGenericType && intType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    select intType.GetGenericArguments()[0]).FirstOrDefault();
        }

        /// <summary>
        /// does not support nested array/lists (yet)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="target"></param>

        public void AssignSchemaType(Type type, ref JObject target)
        {
            var constraints = new Dictionary<string, JToken>();
            bool isArray = false;


            // is it a list type?
            if (type != typeof(String))
            {
                Type elementType = GetEnumerableType(type);

                if (elementType != null)
                {
                    isArray = true;
                    type = elementType;
                }
            }


            TypeCode typecode;
            string schemaType = "";
            bool nullable = false;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
                typecode = Type.GetTypeCode(type);
                nullable = true;
            }
            else
            {
                typecode = Type.GetTypeCode(type);
            }
            if (type.IsEnum)
            {
                target["type"] = "number";
                if (type.GetCustomAttributes(typeof(FlagsAttribute), true).FirstOrDefault() != null)
                {
                    target["format"] = "bitmask";
                }
                Array enumValues = Enum.GetValues(type);
                target["enum"] = new JArray(enumValues);
      
                var options = new JArray();
                target["options"] = options;
                foreach (var item in enumValues)
                {
                    var option = new JObject(new JProperty("value", item), new JProperty("label", Enum.GetName(type, item)));
                    options.Add(option);
                }
            }
            else
            {
                switch (typecode)
                {
                    // odd ducks first

                    case TypeCode.DateTime:
                        // there is no concept of date in json
                        // so this description is very subjective
                        // this converter should allow the caller to
                        // specify how a date is going to be represented
                        // by their service. in this case it is hard coded
                        schemaType = "string";
                        constraints.Add("format", DATE_TIME_FORMAT);
                        break;

                    case TypeCode.Empty:
                        // VOID?
                        schemaType = "null";
                        break;

                    case TypeCode.Boolean:
                        schemaType = "boolean";
                        break;
                    case TypeCode.Byte:
                        schemaType = "number";
                        constraints.Add("format", "integer");
                        constraints.Add("minimum", (int)Byte.MinValue);
                        constraints.Add("maximum", (int)Byte.MaxValue);
                        break;
                    case TypeCode.Char:
                        schemaType = "string";
                        constraints.Add("minLength", 0);
                        constraints.Add("maxLength", 1);
                        break;


                    case TypeCode.Decimal:
                        schemaType = "number";
                        constraints.Add("format", "decimal");
                        constraints.Add("minimum", Decimal.MinValue);
                        constraints.Add("maximum", Decimal.MaxValue);
                        break;
                    case TypeCode.Double:
                        schemaType = "number";
                        constraints.Add("minimum", Double.MinValue);
                        constraints.Add("maximum", Double.MaxValue);
                        break;

                    case TypeCode.Int16:
                        schemaType = "number";
                        constraints.Add("format", "integer");
                        constraints.Add("minimum", Int16.MinValue);
                        constraints.Add("maximum", Int16.MaxValue);
                        break;
                    case TypeCode.Int32:
                        schemaType = "number";
                        constraints.Add("format", "integer");
                        constraints.Add("minimum", Int32.MinValue);
                        constraints.Add("maximum", Int32.MaxValue);
                        break;
                    case TypeCode.Int64:
                        schemaType = "number";
                        constraints.Add("format", "integer");
                        constraints.Add("minimum", Int64.MinValue);
                        constraints.Add("maximum", Int64.MaxValue);
                        break;
                    case TypeCode.SByte:
                        schemaType = "number";
                        constraints.Add("format", "integer");
                        constraints.Add("minimum", SByte.MinValue);
                        constraints.Add("maximum", SByte.MaxValue);
                        break;
                    case TypeCode.Single:
                        schemaType = "number";
                        constraints.Add("minimum", Single.MinValue);
                        constraints.Add("maximum", Single.MaxValue);
                        break;
                    case TypeCode.String:
                        schemaType = "string";
                        break;
                    case TypeCode.UInt16:
                        schemaType = "number";
                        constraints.Add("format", "integer");
                        constraints.Add("minimum", UInt16.MinValue);
                        constraints.Add("maximum", UInt16.MaxValue);
                        break;
                    case TypeCode.UInt32:
                        schemaType = "number";
                        constraints.Add("format", "integer");
                        constraints.Add("minimum", UInt32.MinValue);
                        constraints.Add("maximum", UInt32.MaxValue);
                        break;
                    case TypeCode.UInt64:
                        throw new NotImplementedException("json.net chokes on uint64 maxvalue");
                        schemaType = "number";
                        constraints.Add("format", "integer");
                        constraints.Add("minimum", UInt64.MinValue);
                        constraints.Add("maximum", UInt64.MaxValue);
                        break;


                    case TypeCode.Object:
                        // determine if is a framework type?
                        if (type == typeof(DateTimeOffset))
                        {
                            // no typecode for DateTimeOffset
                            // there is no concept of date in json
                            // so this description is very subjective
                            // this converter should allow the caller to
                            // specify how a date is going to be represented
                            // by their service. in this case it is hard coded
                            schemaType = "string";
                            constraints.Add("format", DATE_TIME_FORMAT);
                        }
                        else
                        {
                            schemaType = "#/" + type.Name;
                        }

                        break;

                    case TypeCode.DBNull:
                        throw new NotImplementedException();
                    default:
                        throw new NotImplementedException();
                }

                var typeTarget = target;
                if (isArray)
                {
                    target["type"] = "array";

                    typeTarget = new JObject();
                    var items = new JArray();

                    target["items"] = typeTarget;
                }
                if (nullable)
                {
                    typeTarget["type"] = new JArray("null", schemaType);
                }
                else
                {
                    typeTarget["type"] = schemaType;
                }
                if (constraints.Count > 0)
                {
                    foreach (var constraint in constraints)
                    {
                        typeTarget[constraint.Key] = constraint.Value;
                    }
                }
            }
            
        }
    }
}
