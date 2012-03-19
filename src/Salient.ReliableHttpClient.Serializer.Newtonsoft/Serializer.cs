﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Salient.ReliableHttpClient.Serialization.Newtonsoft
{
    public class Serializer : IJsonSerializer
    {


        public string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);


        }

        public T DeserializeObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
