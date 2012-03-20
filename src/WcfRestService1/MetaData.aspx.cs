using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Web;
using System.Web.Routing;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WcfRestService1
{
    public partial class MetaData : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var smd = new JObject();
            smd["version"] = 1.0;
            JObject services = new JObject();
            smd["services"] = services;
            foreach (var route in (List<RouteInfo>)Application["routes"])
            {
                var service = new JObject();
                service["target"] = route.RoutePrefix;
                services[route.RoutePrefix] = service;


                foreach (var method in route.ServiceType.GetMethods())
                {
                    bool publishMethod = false;
                    string httpMethod = "";
                    string uriTemplate = "";
                    var get = (WebGetAttribute)method.GetCustomAttributes(typeof(WebGetAttribute), true).FirstOrDefault();
                    if (get != null)
                    {
                        httpMethod = "GET";
                        uriTemplate = get.UriTemplate;
                        publishMethod = true;

                    }
                    else
                    {
                        // #question: would one put both a get and and invoke on the same method
                        var invoke = (WebInvokeAttribute)method.GetCustomAttributes(typeof(WebInvokeAttribute), true).FirstOrDefault();
                        if (invoke != null)
                        {
                            httpMethod = invoke.Method;
                            uriTemplate = invoke.UriTemplate;
                            publishMethod = true;
                        }
                    }
                    
                    if (publishMethod)
                    {

                        var methodObj = new JObject();
                        methodObj["transport"] = httpMethod;
                        methodObj["uriTemplate"] = uriTemplate;
                        if (method.ReturnType != typeof(void))
                        {
                            var rt = GetSchemaType(method.ReturnType);
                            
                            // check to see if is a primitive and translate, and otherwise assert that it will be represented in the schema document
                            methodObj["returns"] = new JObject(new JProperty("$ref", rt));
                        }

                        service[method.Name] = methodObj;
                    }

                }


                
            }

            Response.Write("<pre>" + smd.ToString(Formatting.Indented) + "</pre>");
        }


        /// <summary>
        /// Assumes that you have already unwrapped generic and list types
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public JObject GetSchemaType(Type type)
        {
            JObject result = new JObject();
            TypeCode typecode;
            string schemaType = "";
            bool nullable = false;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {

                typecode = Type.GetTypeCode(type.GetGenericArguments()[0]);
                nullable = true;
            }
            else
            {
                typecode = Type.GetTypeCode(type);
            }

            switch (typecode)
            {
                case TypeCode.Boolean:
                    schemaType = "boolean";
                    break;
                case TypeCode.Byte:
                    schemaType = "number";
                    break;
                case TypeCode.Char:
                    schemaType = "string";
                    break;
                case TypeCode.DateTime:
                    schemaType = "string";
                    // set format
                    break;
                case TypeCode.Decimal:
                    schemaType = "number";
                    break;
                case TypeCode.Double:
                    schemaType = "number";
                    break;
                case TypeCode.Empty:
                    schemaType = "null";
                    break;
                case TypeCode.Int16:
                    schemaType = "number";
                    break;
                case TypeCode.Int32:
                    schemaType = "number";
                    break;
                case TypeCode.Int64:
                    schemaType = "number";
                    break;
                case TypeCode.SByte:
                    schemaType = "number";
                    break;
                case TypeCode.Single:
                    schemaType = "number";
                    break;
                case TypeCode.String:
                    schemaType = "string";
                    break;
                case TypeCode.UInt16:
                    schemaType = "number";
                    break;
                case TypeCode.UInt32:
                    schemaType = "number";
                    break;
                case TypeCode.UInt64:
                    schemaType = "number";
                    break;


                case TypeCode.Object:
                    // determine if is a framework type?
                    schemaType = "#/" + type.Name;
                    break;

                case TypeCode.DBNull:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
            if (nullable)
            {
                result["type"] = new JArray("null", schemaType);
            }
            else
            {
                result["type"] = schemaType;
            }
            return result;
        }
    }
}