using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    using Matrix = Queue<List<double>>;
    using DataMap = Dictionary<string, Queue<List<double>>>;

    internal interface IAnalyzer {
        public void AddToDataset(string inCryptocurrency);
        public void RemoveFromDataset(string inCryptocurrency);
        public Dictionary<string, double> Assets { get; }
    }

    internal struct TechnicalIndicatorsAnalyzer : IAnalyzer {
        private readonly string currency;
        private byte rsiPeriod = 13; // +1 (the latest received value via API)
        private byte bbPeriod = 20;  // ...
        private DataMap dataset;

        private readonly BollingerBands bb;
        private readonly RelativeStrengthIndex rsi;

        // sorted for the convenience
        private SortedDictionary<string, List<double>> lastRecords;
        public ServiceNumerics Numerics { get; init; }

        public Dictionary<string, double> Assets { get; private set; }

        public TechnicalIndicatorsAnalyzer(ServiceNumerics inNumerics) {
            dataset = new DataMap();
            lastRecords = new SortedDictionary<string, List<double>>();
            // indicators
            bb = new BollingerBands();
            rsi = new RelativeStrengthIndex();
            currency = "USD";
            Assets = new Dictionary<string, double>() { { currency, 0 } };
            Numerics = inNumerics;
        }

        public void AddToDataset(string inCryptocurrency) {
            
        }

        public void RemoveFromDataset(string inCryptocurrency) {
            throw new NotImplementedException();
        }

        public void Deposit(double depositValue) {
            double afterFee = depositValue - depositValue * Numerics.DepositFee;

        }

        internal void PrepareSymbol(string symbol, List<double> previousPrices) {
            int iteration = 0;
            dataset.Add(symbol, new Matrix());
            foreach (double price in previousPrices) {
                List<double> rowCells = new List<double>();
                if(iteration > rsi.LookBackPeriod) {
                    double v = rsi.GetIndicatorValue(dataset[symbol], price);
                    rowCells.Add(v);
                }
                else {
                    rowCells.Add(0);
                }

                if(iteration > bb.LookBackPeriod) {
                    var (lowerBand, upperBand) = bb.GetBands(dataset[symbol], price);
                    rowCells.Add(lowerBand); 
                    rowCells.Add(upperBand);
                }
                else {
                    rowCells.Add(0);
                    rowCells.Add(0);
                }

                // the max constant for the technical indicators lookback
                if (dataset[symbol].Count > bbPeriod) { 
                    dataset[symbol].Dequeue();
                }

                rowCells.Add(price);
                dataset[symbol].Enqueue(rowCells);
                ++iteration;
            }
        }

        private List<double> GetNextRow(string symbol, double price) {

            var rsiValue = rsi.GetIndicatorValue(dataset[symbol], price);
            var (lowerBand, upperBand) = bb.GetBands(dataset[symbol], price);

            List<double> rowCells = new List<double>() { 
                rsiValue, lowerBand, upperBand, price 
            };
            lastRecords[symbol] = rowCells;
            return rowCells;
        }

        internal void ShowTransactions() {
            throw new NotImplementedException();
        }

        internal void ShowAssets() {
            Console.WriteLine("Assets:");
            foreach (var (v, w) in Assets) {
                Console.WriteLine($"{v}: {w}");
            }
        }

        internal void ShowIndicators() {
            Console.WriteLine("Indicators");
            
            foreach (var(symbol, indicators) in lastRecords) {
                Console.WriteLine("{0}: RSI: {1:0.#####} Lower: {2:0.#####} Upper: {3:0.#####} Price: {4:0.#####}",
                symbol, indicators[0], indicators[1], indicators[2], indicators[3]);
            }
        }

        private void DecideSignal(string symbol, List<double> newRow) {
            Console.WriteLine("Buy of course xd");
        }

        internal void ProcessData(Dictionary<string, Cryptocurrency> data, bool shallAddRow) {
            foreach (var (symbol, cryptocurrency) in data) {
                double price = cryptocurrency.Price;
                var newRow = GetNextRow(symbol, price);
                if (shallAddRow) {
                    dataset[symbol].Dequeue();
                    dataset[symbol].Enqueue(newRow);
                }
                DecideSignal(symbol, newRow);
            }

        }

        internal void ShowDataset() {
            foreach(var (symbol, matrix) in dataset) {
                Console.WriteLine("Cryptocurrency: {0}", symbol);
                foreach(var v in matrix) {
                    Console.WriteLine(string.Join(" ", v));
                }
            }
        }

        public void Remove(string symbol) {
            if (Assets[symbol] > 0) {
                double lastPrice = lastRecords[symbol].Last();
                // force sell
            }
            dataset.Remove(symbol);
            Assets.Remove(symbol);
            lastRecords.Remove(symbol);

        }

        interface Indicator {
            public int LookBackPeriod { get; init; }
        }

        private struct BollingerBands : Indicator {
            public int LookBackPeriod { get; init; }
            public BollingerBands() {
                LookBackPeriod = 21;
            }

            private double GetStandardDeviation(List<double> prices, double mean) {
                double variance = 0;
                foreach(var val in prices) {
                    variance = variance + (val - mean) * (val - mean);
                }
                variance = Math.Sqrt(variance / prices.Count);
                return variance;
            }

            public (double, double) GetBands(Matrix inMatrix, double price) {
                List<double> prices = new List<double>();
                var lastValues = inMatrix.TakeLast(LookBackPeriod);
                foreach (var val in lastValues) {
                    prices.Add(val.Last());
                }
                prices.Add(price);
                double mean = prices.Average();
                double stdDeviation = GetStandardDeviation(prices, mean);
                double lowerBand = mean - 2 * stdDeviation;
                double upperBand = mean + 2 * stdDeviation;
                return (lowerBand, upperBand);
            }
        }

        private struct RelativeStrengthIndex : Indicator {
            public int LookBackPeriod { get; init; }
            private readonly int sellSignalPercentage;
            private readonly int buySignalPercentage;
            public RelativeStrengthIndex() {
                LookBackPeriod = 14;
                sellSignalPercentage = 70;
                buySignalPercentage = 30;
            }

            public double GetIndicatorValue(Matrix inMatrix, double price) {
                List<double> prices = new List<double>();
                var lastValues = inMatrix.TakeLast(LookBackPeriod);
                foreach(var val in lastValues) {
                    prices.Add(val.Last());
                }
                // latest
                prices.Add(price);
                var differences = prices.GetDifferences();
                double averageUp = GetAbsMovingAverage(
                    storage: differences,
                    isPositive: true
                );
                double averageDown = GetAbsMovingAverage(
                    storage: differences,
                    isPositive: false
                );
                double relStrength = 0;
                if(averageDown != 0) {
                    relStrength = averageUp / averageDown;
                }
                return GetRelativeStrengthIndex(relStrength);
            }

            private double GetAbsMovingAverage(IEnumerable<double> storage, bool isPositive) {
                List<double> newValues = new List<double>();
                foreach (var value in storage) {
                    if ((isPositive && value < 0)
                    || (!isPositive && value > 0)) {
                        newValues.Add(0);
                    }
                    else if (value < 0) {
                        newValues.Add(-1 * value);
                    }
                    else {
                        newValues.Add(value);
                    }
                }
                return newValues.Average();
            }
            private double GetRelativeStrengthIndex(double relStrength) {
                int percentage = 100;
                return percentage - (percentage / (1 + relStrength));
            }

        }
    }
}
