using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Netptune.Core.Reflection
{
    public static class PropertiesHelper
    {
        public static List<string> GetPropertyNameList(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public);

            return props.Select(prop => prop.Name).ToList();
        }
    }
}
