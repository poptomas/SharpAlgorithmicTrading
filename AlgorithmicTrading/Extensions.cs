﻿using System.Diagnostics;
using System.Collections.Concurrent;

namespace AlgorithmicTrading {
    using DataMap = ConcurrentDictionary<string, Queue<Dictionary<string, double>>>;

    static class IEnumerableExtensions {
        public static IEnumerable<double> GetDifferences(this IEnumerable<double> storage) {
            return storage.Zip(
                storage.Skip(1), (first, second) => { 
                    return second - first; 
                }
            );
        }
    }

    static class ConcurrentDictionaryExtensions {
        public static void Increment<T>(this ConcurrentDictionary<T, int> dictionary, T key) {
            dictionary.AddOrUpdate(key, 1, (key, count) => count + 1);
        }

        public static void Add<T>(this ConcurrentDictionary<T, double> dictionary, T key, double amount) {
            dictionary.AddOrUpdate(key, amount, (key, count) => count + amount);
        }

        public static void Subtract<T>(this ConcurrentDictionary<T, double> dictionary, T key, double amount) {
            dictionary.AddOrUpdate(key, amount, (key, count) => count - amount);
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
        public static void Print(this ConcurrentDictionary<string, Cryptocurrency> watchList) {
            foreach (var (name, cryptocurrency) in watchList.OrderBy(member => member.Key)) {
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
        public static void Print(this ConcurrentDictionary<string, double> assets) {
            foreach (var (symbol, amount) in assets.OrderBy(member => member.Key)) {
                Console.WriteLine("[{0}: {1:0.#####}]", symbol, amount);
            }
        }
    }

    static class IndicatorsExtensions {
        public static void Print(this ConcurrentDictionary<string, Dictionary<string, double>> records) {
            foreach (var (symbol, indicators) in records.OrderBy(member => member.Key)) {
                Console.WriteLine(" {0}", symbol);
                foreach(var (name, value) in indicators) {
                    Console.WriteLine("     {0} : {1:0.#####}", name, value);
                }
            }
        }
    }

    static class TransactionExtensions {
        public static void Print(this ConcurrentQueue<Transaction> records) {
            int lineNum = 1;

            foreach (var item in records) {
                Console.WriteLine("{0}. {1}", lineNum, item);
                ++lineNum;
            }
        }
    }
}
