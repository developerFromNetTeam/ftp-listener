using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace windows_service_logic.HashtableExtentions
{
    public static class HashtableExtentions
    {
        public static List<T> ToCollection<T>(this Hashtable hashtable, string field, Func<Hashtable, T> converter)
        {
            return ((IEnumerable)hashtable[field]).Cast<Hashtable>().Select(converter).ToList();
        }
    }
}
