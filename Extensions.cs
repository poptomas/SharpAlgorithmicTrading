namespace AlgorithmicTrading {

    static class IEnumerableExtensions {
        public static IEnumerable<double> GetDifferences(this IEnumerable<double> storage) {
            return storage.Zip(storage.Skip(1), (first, second) => { return second - first; });
        }
    }

    static class CollectionExtensions {
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
