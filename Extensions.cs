using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    static class ListExtensions {
        public static bool IsEmpty<T>(this ICollection<T> storage) { 
            return storage.Count == 0; 
        }
    }

    static class StringExtensions {
        public static string Filter(this string input, char removedCharacter) {
            return input.Replace(removedCharacter, '\0');
        }
    }
}
