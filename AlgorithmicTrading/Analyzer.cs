using System.Collections.Concurrent;

namespace AlgorithmicTrading {
    using DataMap = ConcurrentDictionary<string, Queue<Dictionary<string, double>>>;
    using Matrix = Queue<Dictionary<string, double>>;

    struct TechnicalIndicatorsAnalyzer {
        public TechnicalIndicatorsAnalyzer(ServiceInfo inInfo) {
            dataset = new DataMap();
            lastRecords = new ConcurrentDictionary<string, Dictionary<string, double>>();
            ServiceInfo = inInfo;
            Assets = new ConcurrentDictionary<string, double>();
            Assets[ServiceInfo.Currency] = 0;
            // indicators
            signalCounterMap = new ConcurrentDictionary<string, int>();
            bb = new BollingerBands();
            rsi = new RelativeStrengthIndex();
            indicators = new List<IIndicator>() { bb, rsi };
            maxLookback = indicators.Select(c => c.LookBackPeriod).Max();
            // transactions
            transactions = new ConcurrentQueue<Transaction>();
            fsHandler = new FSHandler("transactions/results.csv");
        }

        public void ProcessData(ConcurrentDictionary<string, Cryptocurrency> data, bool shallAddRow) {
            foreach (var (symbol, cryptocurrency) in data.OrderBy(member => member.Key)) {
                if (dataset.ContainsKey(symbol)) {
                    var newRow = GetNextRow(symbol, cryptocurrency.Price);
                    if (shallAddRow) {
                        dataset[symbol].Dequeue();
                        dataset[symbol].Enqueue(newRow);
                    }
                    DecideSignal(symbol, newRow);
                }
            }
        }

        public void Withdraw(ConcurrentDictionary<string, Cryptocurrency> inValues) {
            double withdrawalVal = GetWithdrawalValue(inValues);
            string currency = ServiceInfo.Currency;
            double fee = ServiceInfo.WithdrawalFee;
            foreach (var (symbol, cryptocurrency) in inValues) {
                if (Assets[symbol] > 0) {
                    ProcessSellSignalInternal(symbol, cryptocurrency.Price, wasForced: true);
                    Assets[symbol] = 0;
                }
            }
            Assets[currency] = 0;
            Printer.DisplayTotalBalance(withdrawalVal, currency, fee);
        }

        public void Deposit(double depositValue) {
            string currency = ServiceInfo.Currency;
            double afterFee = depositValue - depositValue * ServiceInfo.DepositFee;
            Assets.Add(currency, afterFee);
            Printer.DisplayDepositSuccess(afterFee, ServiceInfo.DepositFee, Assets[currency]);
        }

        public void Add(string inSymbol, List<double> inPreviousPrices) {
            lock (mutex) { // protect critical section from para linq threads
                int iteration = 0;
                dataset[inSymbol] = new Matrix();
                int prevPricesCount = inPreviousPrices.Count;
                foreach (double price in inPreviousPrices) {
                    var rowRecords = new Dictionary<string, double>();

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
                signalCounterMap[inSymbol] = 0;
            }
        }

        public void Remove(string symbol) {
            if (Assets[symbol] > 0) {
                double lastPrice = lastRecords[symbol][priceKey];
                ProcessSellSignalInternal(symbol, lastPrice, wasForced: true);
            }
            dataset.TryRemove(symbol, out _);
            Assets.TryRemove(symbol, out _);
            lastRecords.TryRemove(symbol, out _);
        }

        public void ShowAssets(ConcurrentDictionary<string, Cryptocurrency> inValues) {
            var value = GetWithdrawalValue(inValues);
            Assets.Print();
            Printer.DisplayEstimatedWithdrawal(value, ServiceInfo.Currency, ServiceInfo.WithdrawalFee);
        }

        public void ShowIndicators() {
            lastRecords.Print();
        }

        public void ShowTransactions() {
            if (transactions.Count == 0) {
                Printer.DisplayNoTransactionsYet();
            }
            else {
                transactions.Print();
            }
        }

        public void ShowDataset() {
            dataset.Print();
        }

        public ServiceInfo ServiceInfo { get; init; }
        public ConcurrentDictionary<string, double> Assets { get; private set; }

        private double GetWithdrawalValue(ConcurrentDictionary<string, Cryptocurrency> inValues) {
            string currency = ServiceInfo.Currency;
            double withdrawValue = Assets[currency];
            foreach (var (symbol, amount) in Assets) {
                if (symbol != currency) {
                    double cryptocurrencyValue = inValues[symbol].Price;
                    double value = cryptocurrencyValue * amount;
                    withdrawValue += value;
                }
            }
            double withdrawalAfterFee = withdrawValue - withdrawValue * ServiceInfo.WithdrawalFee;
            return withdrawalAfterFee;
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

        private void CreateTransaction(string symbol, double price, double cryptoAmount, State action) {
            var transaction = new Transaction(symbol, price, cryptoAmount, Enum.GetName(action));
            var line = transaction.GetCSVLine();
            if (transactions.Count >= maxTransactions) {
                transactions.TryDequeue(out _);
            }
            transactions.Enqueue(transaction);
            fsHandler.Save(line);
        }

        private void ProcessSellSignalInternal(string symbol, double price, bool wasForced) {
            if (wasForced) {
                Printer.DisplaySellForced(symbol, price);
            }
            else {
                Printer.DisplaySellSignal(symbol, price);
            }
            double cryptoAmount = Assets[symbol];
            double cryptoInFiat = cryptoAmount * price;
            double afterTradingFee = cryptoInFiat - cryptoInFiat * ServiceInfo.TradingFee;
            Assets[symbol] = 0;
            Assets.Add(ServiceInfo.Currency, afterTradingFee);
            signalCounterMap[symbol] = 0; // start over for another signal to come
            CreateTransaction(symbol, price, cryptoAmount, State.Sell);
        }

        private void ProcessSellSignal(string symbol, double price) {
            signalCounterMap.Increment(symbol);
            var cryptoAmount = Assets[symbol];
            if (cryptoAmount > 0 && signalCounterMap[symbol] >= signalThreshold) {
                ProcessSellSignalInternal(symbol, price, wasForced: false);
            }
            else if (cryptoAmount == 0 && signalCounterMap[symbol] >= signalThreshold) {
                Printer.WarnCantSell(symbol, price);
                signalCounterMap[symbol] = 0;
            }
            else {
                // "preparing for the signal"
                // Console.WriteLine("Prepare for the [SELL] signal with {0} ({1}x)", symbol, signalCounterMap[symbol]);
            }
        }

        private void BuySignalInternal(string symbol, double price) {
            Printer.ShowBuySignal(symbol, price);
            string currency = ServiceInfo.Currency;
            double investedValue = Assets[currency] / investmentSplit;
            double afterTradingFee = investedValue - investedValue * ServiceInfo.TradingFee;
            double cryptoAmount = afterTradingFee / price;
            Assets.Subtract(currency, investedValue);
            Assets.Add(symbol, cryptoAmount);
            signalCounterMap[symbol] = 0; // start over for another signal to come
            CreateTransaction(symbol, price, cryptoAmount, State.Buy);
        }

        private void ProcessBuySignal(string symbol, double price) {
            ++signalCounterMap[symbol];
            var currencyAmount = Assets[ServiceInfo.Currency];
            if (currencyAmount > 1 && signalCounterMap[symbol] >= signalThreshold) {
                // it could be (and probably is) different across multiple cryptocurrency exchanges
                // to ensure that the bot is not just working with infinitely low amounts
                BuySignalInternal(symbol, price);
            }
            else if (currencyAmount <= 1 && signalCounterMap[symbol] >= signalThreshold) {
                Printer.WarnCantBuy(symbol, price);
                signalCounterMap[symbol] = 0;
            }
            else {
                // "preparing for the signal"
                // Console.WriteLine("Prepare for the [BUY] signal with {0} ({1}x)", symbol, signalCounterMap[symbol]);
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
                signalCounterMap[symbol] = 0;
            }
        }

        private const string priceKey = "price";
        private const int maxTransactions = 20; // do not keep all transactions in memory
                                                // - just a queue of all couple of them, the full history will be stored in a file
        private const int investmentSplit = 20;
        private const int signalThreshold = 10;
        private readonly int maxLookback;

        private FSHandler fsHandler;
        private DataMap dataset;
        private ConcurrentQueue<Transaction> transactions;
        private readonly BollingerBands bb;
        private readonly RelativeStrengthIndex rsi;
        private List<IIndicator> indicators;
        private ConcurrentDictionary<string, Dictionary<string, double>> lastRecords;
        private ConcurrentDictionary<string, int> signalCounterMap;
        private object mutex = new object();

        interface IIndicator {
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

            public void FillCells(ref Dictionary<string, double> rowCells,
                int iteration, Matrix matrix, double price) {
                if (iteration > LookBackPeriod) {
                    var (lower, upper) = GetBands(matrix, price);
                    rowCells[LIndicatorName] = lower;
                    rowCells[UIndicatorName] = upper;
                }
                else {
                    rowCells[LIndicatorName] = 0;
                    rowCells[UIndicatorName] = 0;
                }
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
            private double GetStandardDeviation(List<double> prices, double mean) {
                double variance = 0;
                foreach (var val in prices) {
                    variance = variance + (val - mean) * (val - mean);
                }
                variance = Math.Sqrt(variance / prices.Count);
                return variance;
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
                foreach (var val in lastValues) {
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
                if (averageDown != 0) {
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
                if (rsi > sellSignalPercentage) {
                    return State.Sell;
                }
                else if (rsi < buySignalPercentage) {
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