using System.Diagnostics;
namespace AlgorithmicTrading {
    using DataMap = Dictionary<string, Queue<Dictionary<string, double>>>;

    static class IEnumerableExtensions {
        public static IEnumerable<double> GetDifferences(this IEnumerable<double> storage) {
            return storage.Zip(
                storage.Skip(1), (first, second) => { 
                    return second - first; 
                }
            );
        }
    }

    static class StringExtensions {
        public static string Filter(this string input, char removedCharacter) {
            return string.Join("", input.Split(removedCharacter));
        }
    }

   
    static class StopwatchExtensions {
        public static void ShowMs(this Stopwatch stopwatch) {
            Console.WriteLine("Elapsed: {0} ms", stopwatch.ElapsedMilliseconds);
        }

        public static void ShowMs(this Stopwatch stopwatch, string message) {
            Console.WriteLine("{0}: {1} ms", message, stopwatch.ElapsedMilliseconds);
        }
    }

    static class WatchlistExtensions {
        public static void Print(this SortedDictionary<string, Cryptocurrency> watchList) {
            foreach (var (name, cryptocurrency) in watchList.ToList()) {
                Console.WriteLine("[{0}: {1} USD]", name, cryptocurrency.Price);
            }
        }
    }

    static class DataMapExtensions {
        public static void Print(this DataMap dataset) {
            foreach (var (symbol, matrix) in dataset) {
                Console.WriteLine("Cryptocurrency: {0}", symbol);
                foreach (var dict in matrix) {
                    foreach (var (indicator, value) in dict) {
                        Console.Write("{0}: {1:0.#####} ", indicator, value);
                    }
                    Console.WriteLine();
                }
            }
        }
    }

    static class AssetsExtensions {
        public static void Print(this SortedDictionary<string, double> assets) {
            foreach (var (symbol, amount) in assets) {
                Console.WriteLine("[{0}: {1:0.#####}]", symbol, amount);
            }
        }
    }

    static class IndicatorsExtensions {
        public static void Print(this SortedDictionary<string, Dictionary<string, double>> records) {
            foreach (var (symbol, indicators) in records) {
                Console.WriteLine(" {0}", symbol);
                foreach(var (name, value) in indicators) {
                    Console.WriteLine("     {0} : {1:0.#####}", name, value);
                }
            }
        }
    }

    static class TransactionExtensions {
        public static void Print(this Queue<Transaction> records) {
            int lineNum = 1;
            foreach (var item in records) {
                Console.WriteLine("{0}. {1}", lineNum, item);
                ++lineNum;
            }
        }
    }
}
