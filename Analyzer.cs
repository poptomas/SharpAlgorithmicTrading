using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    using Matrix = Deque<List<double>>;
    using DataMap = Dictionary<string, Deque<List<double>>>;

    internal interface IAnalyzer {
        public void AddToDataset(Dictionary<string, Cryptocurrency> currentWatchlist);
        public void RemoveFromDataset(Dictionary<string, Cryptocurrency> currentWatchlist);
    }

    internal struct TechnicalIndicatorsAnalyzer : IAnalyzer {

        private byte rsiPeriod = 13; // +1 (the latest received value via API)
        private byte bbPeriod = 20;  // ...
        private DataMap dataset;

        public TechnicalIndicatorsAnalyzer() {
            dataset = new DataMap();
        }

        public void AddToDataset(Dictionary<string, Cryptocurrency> currentWatchlist) {
            throw new NotImplementedException();
        }

        public void RemoveFromDataset(Dictionary<string, Cryptocurrency> currentWatchlist) {
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
                    dataset[symbol].PopFront();
                }

                rowCells.Add(price);
                dataset[symbol].Add(rowCells);
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
    }
}
