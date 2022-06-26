using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AlgorithmicTrading {
    using Matrix = Queue<Dictionary<string, double>>;
    using DataMap = Dictionary<string, Queue<Dictionary<string, double>>>;

    internal interface IAnalyzer {
        public SortedDictionary<string, double> Assets { get; }
    }

    internal struct TechnicalIndicatorsAnalyzer : IAnalyzer {
        private const string currency = "USD";
        private const string priceKey = "price";
        private const int maxTransactions = 20; // do not keep all transactions in memory
                                                // - just a queue of all couple of them, the full history will be stored in a file
        private const int investmentSplit = 20;
        private readonly int maxLookback;

        private FSHandler fsHandler;
        private DataMap dataset;
        private Queue<Transaction> transactions;
        private readonly BollingerBands bb;
        private readonly RelativeStrengthIndex rsi;
        private List<IIndicator> indicators;
        private SortedDictionary<string, Dictionary<string, double>> lastRecords;
        private object mutex = new object();
        public ServiceNumerics Numerics { get; init; }

        public SortedDictionary<string, double> Assets { get; private set; }

        public TechnicalIndicatorsAnalyzer(ServiceNumerics inNumerics) {
            dataset = new DataMap();
            lastRecords = new SortedDictionary<string, Dictionary<string, double>>();
            Assets = new SortedDictionary<string, double>() { { currency, 0 } };
            Numerics = inNumerics;
            // indicators
            bb = new BollingerBands();
            rsi = new RelativeStrengthIndex();
            indicators = new List<IIndicator>() { bb, rsi };
            maxLookback = indicators.Select(c => c.LookBackPeriod).Max();
            // transactions
            transactions = new Queue<Transaction>();
            fsHandler = new FSHandler("transactions/results.csv");
            fsHandler.TryFlushPreviousRun();
        }

        private double GetWithdrawalValue(SortedDictionary<string, Cryptocurrency> inValues) {
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

        public void Withdraw(SortedDictionary<string, Cryptocurrency> inValues) {
            double withdrawalVal = GetWithdrawalValue(inValues);
            foreach (var (symbol, cryptocurrency) in inValues) {
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
            Printer.ShowDepositSuccessful(afterFee, Numerics.DepositFee);
        }

        internal void Add(string inSymbol, List<double> inPreviousPrices) {
            lock (mutex) { // protect critical section from para linq threads
                int iteration = 0;
                dataset.Add(inSymbol, new Matrix());
                int prevPricesCount = inPreviousPrices.Count;
                foreach (double price in inPreviousPrices) {
                    Dictionary<string, double> rowRecords = new Dictionary<string, double>();

                    foreach (var member in indicators) {
                        member.FillCells(ref rowRecords, iteration, dataset[inSymbol], price);
                    }

                    if (dataset[inSymbol].Count > maxLookback) {
                        dataset[inSymbol].Dequeue();
                    }

                    rowRecords[priceKey] = price;
                    dataset[inSymbol].Enqueue(rowRecords);
                    ++iteration;
                    if (iteration == prevPricesCount) {
                        lastRecords[inSymbol] = rowRecords;
                    }
                }
                Assets[inSymbol] = 0;
            }
        }

        private Dictionary<string, double> GetNextRow(string symbol, double price) {
            var rsiValue = rsi.GetIndicatorValue(dataset[symbol], price);
            var (lowerBand, upperBand) = bb.GetBands(dataset[symbol], price);

            var rowCells = new Dictionary<string, double>() {
                { rsi.IndicatorName, rsiValue },
                { bb.LIndicatorName, lowerBand },
                { bb.UIndicatorName, upperBand },
                { priceKey, price } 
            };

            lastRecords[symbol] = rowCells;
            return rowCells;
        }

        internal void ShowTransactions() {
            if(transactions.Count == 0) {
                Printer.ShowNoTransactionsAccomplished();
            }
            else {
                transactions.Print();
            }
        }

        internal void ShowAssets(SortedDictionary<string, Cryptocurrency> inValues) {
            var value = GetWithdrawalValue(inValues);
            Assets.Print();
            Printer.ShowEstimatedWithdrawal(value, currency, Numerics.WithdrawalFee);
        }

        internal void ShowIndicators() {
            lastRecords.Print();
        }

        private void CreateTransaction(string symbol, double price, double cryptoAmount, State action) {
            var transaction = new Transaction(symbol, price, cryptoAmount, Enum.GetName(action));
            if(transactions.Count >= maxTransactions) {
                transactions.Dequeue();
            }
            transactions.Enqueue(transaction);
            var line = transaction.GetCSVLine();
            fsHandler.Save(line);
        }

        private void ProcessSellSignalInternal(string symbol, double price, bool wasForced) {
            if(wasForced) {
                Printer.ShowForcedSell(symbol, price);
            }
            else {
                Printer.ShowSellSignal(symbol, price);
            }
            double cryptoAmount = Assets[symbol];
            double cryptoInFiat = cryptoAmount * price;
            double afterTradingFee = cryptoInFiat - cryptoInFiat * Numerics.TradingFee;

            Assets[symbol] = 0;
            Assets[currency] += afterTradingFee;
            CreateTransaction(symbol, price, cryptoAmount, State.Sell);
        }

        private void ProcessSellSignal(string symbol, double price) {
            var cryptoAmount = Assets[symbol];
            if(cryptoAmount > 0) {
                ProcessSellSignalInternal(symbol, price, wasForced: false);
            }
            else {
                Printer.ShowCantSell(symbol);
            }
        }

        private void BuySignalInternal(string symbol, double price) {
            Printer.ShowBuySignal(symbol, price);
            double investedValue = Assets[currency] / investmentSplit;
            double afterTradingFee = investedValue - investedValue * Numerics.TradingFee;
            double cryptoAmount = afterTradingFee / price;
            Assets[currency] -= investedValue;
            Assets[symbol] += cryptoAmount;
            CreateTransaction(symbol, price, cryptoAmount, State.Buy);
        }

        private void ProcessBuySignal(string symbol, double price) {
            var currencyAmount = Assets[currency];
            if (currencyAmount > 1) { 
                // it could be (and probably is) different across multiple cryptocurrency exchanges
                // to ensure that the bot is not just working with infinitely low amounts
                BuySignalInternal(symbol, price);
            }
            else  {
                Printer.ShowCantBuy(symbol);
            }
        }

        private void DecideSignal(string symbol, Dictionary<string, double> newRow) {
            State bbSignal = bb.GetDecision(newRow);
            State rsiSignal = rsi.GetDecision(newRow);
            double closePrice = newRow[priceKey];

            if (bbSignal == State.Buy && rsiSignal == State.Buy) {
                ProcessBuySignal(symbol, closePrice);
            }
            else if (bbSignal == State.Sell && rsiSignal == State.Sell) {
                ProcessSellSignal(symbol, closePrice);
            }
            else {
                // TODO remove in the future, way too much spam.
                //Console.WriteLine("For {0}: nothing ", symbol);
            }
        }

        internal void ProcessData(SortedDictionary<string, Cryptocurrency> data, bool shallAddRow) {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var (symbol, cryptocurrency) in data) {
                var newRow = GetNextRow(symbol, cryptocurrency.Price);
                if (shallAddRow) {
                    dataset[symbol].Dequeue();
                    dataset[symbol].Enqueue(newRow);
                }
                DecideSignal(symbol, newRow);
            }
            sw.Stop();
            sw.ShowMs();
        }

        internal void ShowDataset() {
            dataset.Print();
        }

        public void Remove(string symbol) {
            if (Assets[symbol] > 0) {
                double lastPrice = lastRecords[symbol][priceKey];
                ProcessSellSignalInternal(symbol, lastPrice, wasForced: true);
            }
            dataset.Remove(symbol);
            Assets.Remove(symbol);
            lastRecords.Remove(symbol);
        }

        public interface IIndicator {
            public int LookBackPeriod { get; init; }

            public State GetDecision(Dictionary<string, double> row);
            public void FillCells(ref Dictionary<string, double> rowCells, int iteration, Matrix queue, double price);
        }

        private struct BollingerBands : IIndicator {
            public int LookBackPeriod { get; init; }
            public string LIndicatorName { get; init; }
            public string UIndicatorName { get; init; }
            public BollingerBands() {
                LookBackPeriod = 21;
                LIndicatorName = "Lower band";
                UIndicatorName = "Upper band";
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
                    prices.Add(val[priceKey]);
                }
                prices.Add(price);
                double mean = prices.Average();
                double stdDeviation = GetStandardDeviation(prices, mean);
                double lowerBand = mean - 2 * stdDeviation;
                double upperBand = mean + 2 * stdDeviation;
                return (lowerBand, upperBand);
            }

            public State GetDecision(Dictionary<string, double> row) {
                double closingPrice = row[priceKey];
                double lower = row[LIndicatorName];
                double upper = row[UIndicatorName];
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

            public void FillCells(
                ref Dictionary<string, double> rowCells, int iteration, Matrix matrix, double price
            ) {
                if (iteration > LookBackPeriod) {
                    var(lower, upper) = GetBands(matrix, price);
                    rowCells[LIndicatorName] = lower;
                    rowCells[UIndicatorName] = upper;
                }
                else {
                    rowCells[LIndicatorName] = 0;
                    rowCells[UIndicatorName] = 0;
                }
            }
        }

        private struct RelativeStrengthIndex : IIndicator {
            public int LookBackPeriod { get; init; }
            private readonly int sellSignalPercentage;
            private readonly int buySignalPercentage;
            public string IndicatorName { get; init; }
            public RelativeStrengthIndex() {
                LookBackPeriod = 14;
                sellSignalPercentage = 70;
                buySignalPercentage = 30;
                IndicatorName = "RSI";
            }

            public double GetIndicatorValue(Matrix inMatrix, double price) {
                List<double> prices = new List<double>();
                var lastValues = inMatrix.TakeLast(LookBackPeriod);
                foreach(var val in lastValues) {
                    prices.Add(val[priceKey]);
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

            public State GetDecision(Dictionary<string, double> row) {
                double rsi = row[IndicatorName];
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

            public void FillCells(
                ref Dictionary<string, double> rowCells, int iteration, Matrix matrix, double price
            ) {
                string keyword = "rsi";
                if (iteration > LookBackPeriod) {
                    double v = GetIndicatorValue(matrix, price);
                    rowCells[keyword] = v;
                }
                else {
                    rowCells[keyword] = 0;
                }
            }
        }
    }
}
