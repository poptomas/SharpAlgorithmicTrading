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
    }

    public struct TechnicalIndicatorsAnalyzer : IAnalyzer {

        private byte rsiPeriod = 13; // +1 (the latest received value via API)
        private byte bbPeriod = 20;  // ...
        private DataMap dataset;

        private readonly BollingerBands bb;
        private readonly RelativeStrengthIndex rsi;

        // sorted for the convenience
        private SortedDictionary<string, List<double>> lastRecords;

        public TechnicalIndicatorsAnalyzer() {
            dataset = new DataMap();
            lastRecords = new SortedDictionary<string, List<double>>();

            // indicators
            bb = new BollingerBands();
            rsi = new RelativeStrengthIndex();
        }

        public void AddToDataset(string inCryptocurrency) {
            throw new NotImplementedException();
        }

        public void RemoveFromDataset(string inCryptocurrency) {
            throw new NotImplementedException();
        }

        private double ComputeBB(string symbol, double price) {
            return 654321;
        }

        private double ComputeRSI(string symbol, double price) {
            return 123456;
        }

        internal void PrepareSymbol(string symbol, List<double> previousPrices) {
            int iteration = 0;
            dataset.Add(symbol, new Matrix());
            foreach (double price in previousPrices) {
                List<double> rowCells = new List<double>();
                if(iteration > rsiPeriod) {
                    double rsi = ComputeRSI(symbol, price);
                    rowCells.Add(rsi);
                }
                else {
                    rowCells.Add(0);
                }

                if(iteration > bbPeriod) {
                    double bollingerBands = ComputeBB(symbol, price);
                    rowCells.Add(bollingerBands);
                }
                else {
                    rowCells.Add(0);
                    rowCells.Add(0);
                }

                if (dataset[symbol].Count > bbPeriod) { // the max constant for the technical indicators lookback
                    dataset[symbol].Dequeue();
                }

                rowCells.Add(price);
                dataset[symbol].Enqueue(rowCells);
            }
        }

        private List<double> GetNextRow(string symbol, double price) {
            List<double> rowCells = new List<double>();

            var rsiValue = rsi.GetIndicatorValue(dataset[symbol], price);
            var(lowerBand, upperBand) = bb.GetBands(dataset[symbol], price);

            return rowCells;
        }

        private void DecideSignal(string symbol, List<double> newRow) {

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

        public void Remove(string cryptocurrencyName) {
            throw new NotImplementedException();
        }

        interface Indicator {
            public int LookBackPeriod { get; init; }
        }

        private struct BollingerBands : Indicator {
            public int LookBackPeriod { get; init; }
            public BollingerBands() {
                LookBackPeriod = 20;
            }
            public (double, double) GetBands(Matrix inMatrix, double price) {
                return (1.0, 2.0);
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
                    prices.Add(val[val.Count - 1]);
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
