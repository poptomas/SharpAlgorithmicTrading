using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    using Matrix = Queue<List<double>>;
    using DataMap = Dictionary<string, Queue<List<double>>>;

    internal interface IAnalyzer {
        public Dictionary<string, double> Assets { get; }
    }

    internal struct TechnicalIndicatorsAnalyzer : IAnalyzer {
        private readonly string currency;
        private DataMap dataset;

        private readonly BollingerBands bb;
        private readonly RelativeStrengthIndex rsi;

        private readonly int investmentSplit;

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
            investmentSplit = 20;
        }

        private double GetWithdrawalValue(Dictionary<string, Cryptocurrency> inValues) {
            double withdrawValue = Assets[currency];
            foreach (var (symbol, amount) in Assets) {
                if (symbol != currency) {
                    double cryptocurrencyValue = inValues[symbol].Price;
                    double value = cryptocurrencyValue * amount;
                    withdrawValue += value;
                }
            }
            double withdrawalAfterFee = withdrawValue - withdrawValue * Numerics.WithdrawalFee;
            return withdrawalAfterFee;
        }

        public void Withdraw(Dictionary<string, Cryptocurrency> inValues) {
            double withdrawalVal = GetWithdrawalValue(inValues);
            foreach(var (symbol, cryptocurrency) in inValues) {
                if (Assets[symbol] > 0) {
                    ProcessSellSignalInternal(symbol, cryptocurrency.Price, wasForced: true);
                    Assets[symbol] = 0;
                }
            }
            Assets[currency] = 0;
            Printer.ShowTotal(withdrawalVal, currency, Numerics.WithdrawalFee);
        }

        public void Deposit(double depositValue) {
            double afterFee = depositValue - depositValue * Numerics.DepositFee;
            Assets[currency] += afterFee;
        }

        internal void Add(string inSymbol, List<double> inPreviousPrices) {
            int iteration = 0;
            dataset.Add(inSymbol, new Matrix());
            int prevPricesCount = inPreviousPrices.Count;
            foreach (double price in inPreviousPrices) {
                List<double> rowCells = new List<double>();
                if (iteration > rsi.LookBackPeriod) {
                    double v = rsi.GetIndicatorValue(dataset[inSymbol], price);
                    rowCells.Add(v);
                }
                else {
                    rowCells.Add(0);
                }

                if (iteration > bb.LookBackPeriod) {
                    var (lowerBand, upperBand) = bb.GetBands(dataset[inSymbol], price);
                    rowCells.Add(lowerBand);
                    rowCells.Add(upperBand);
                }
                else {
                    rowCells.Add(0);
                    rowCells.Add(0);
                }

                // the max constant for the technical indicators lookback
                if (dataset[inSymbol].Count > bb.LookBackPeriod) {
                    dataset[inSymbol].Dequeue();
                }

                rowCells.Add(price);
                dataset[inSymbol].Enqueue(rowCells);
                ++iteration;
                if (iteration == prevPricesCount) {
                    lastRecords[inSymbol] = rowCells;
                }
            }
            Assets[inSymbol] = 0;
        }

        private List<double> GetNextRow(string symbol, double price) {

            var rsiValue = rsi.GetIndicatorValue(dataset[symbol], price);
            var (lowerBand, upperBand) = bb.GetBands(dataset[symbol], price);

            Console.WriteLine("{0} {1} {2} {3}", rsiValue, lowerBand, upperBand, price);

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

        private void ProcessSellSignalInternal(string symbol, double price, bool wasForced) {
            if(wasForced) {
                Console.WriteLine("Normal sell signal");
            }
            else {
                Console.WriteLine("Force sell");
            }
            double cryptoAmount = Assets[symbol];
            double cryptoInFiat = cryptoAmount * price;
            double afterTradingFee = cryptoInFiat - cryptoInFiat * Numerics.TradingFee;
            // TODO CREATE TRANSACTION
            Assets[symbol] = 0;
            Assets[currency] += afterTradingFee;
        }

        private void ProcessSellSignal(string symbol, double price) {
            var cryptoAmount = Assets[symbol];
            if(cryptoAmount > 0) {
                ProcessSellSignalInternal(symbol, price, wasForced: false);
            }
            else {
                Console.WriteLine("Cant sell - I dont have any");
            }
        }

        private void BuySignalInternal(string symbol, double price) {
            Console.WriteLine("BUY SIGNAL");
            double investedValue = Assets[currency] / investmentSplit;
            double afterTradingFee = investedValue - investedValue * Numerics.TradingFee;
            double cryptoAmount = afterTradingFee / price;
            Assets[currency] -= investedValue;
            // TODO CREATE TRANSACTION
            Assets[symbol] += cryptoAmount;
        }

        private void ProcessBuySignal(string symbol, double price) {
            var currencyAmount = Assets[currency];
            if (currencyAmount > 1) {
                BuySignalInternal(symbol, price);
            }
            else  {
                Console.WriteLine("Cant buy - no money");
            }
        }

        private void DecideSignal(string symbol, List<double> newRow) {
            State bbSignal = bb.GetDecision(newRow);
            State rsiSignal = rsi.GetDecision(newRow);
            double closePrice = newRow[^1];

            if (bbSignal == State.Buy || rsiSignal == State.Buy) {
                ProcessBuySignal(symbol, closePrice);
            }
            else if (bbSignal == State.Sell || rsiSignal == State.Sell) {
                ProcessSellSignal(symbol, closePrice);
            }
            else {
                Console.WriteLine("nothing ");
            }

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
                double lastPrice = lastRecords[symbol][^1];
                // force sell
            }
            dataset.Remove(symbol);
            Assets.Remove(symbol);
            lastRecords.Remove(symbol);

        }

        interface Indicator {
            public int LookBackPeriod { get; init; }
            public State GetDecision(List<double> row);
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
                    prices.Add(val[^1]);
                }
                prices.Add(price);
                double mean = prices.Average();
                double stdDeviation = GetStandardDeviation(prices, mean);
                double lowerBand = mean - 2 * stdDeviation;
                double upperBand = mean + 2 * stdDeviation;
                return (lowerBand, upperBand);
            }

            public State GetDecision(List<double> row) {
                double closingPrice = row[^1];
                double lower = row[1];
                double upper = row[2];
                if (closingPrice > upper) {
                    return State.Sell;
                }
                else if (closingPrice < lower) {
                    return State.Buy;
                }
                else {
                    return State.Hold;
                }
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
                    prices.Add(val[^1]);
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

            public State GetDecision(List<double> row) {
                double rsi = row[0];
                if(rsi > sellSignalPercentage) {
                    return State.Sell;
                }
                else if(rsi < buySignalPercentage) {
                    return State.Buy;
                }
                else {
                    return State.Hold;
                }
            }
        }
    }
}
