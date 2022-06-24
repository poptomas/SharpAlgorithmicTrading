using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;



namespace AlgorithmicTrading {
    interface IConnection {
        public void ReceiveCurrentData();
        public void PrepareDatasets(string[] input);
        public Dictionary<string, double> Cryptocurrencies { get; }
        public Dictionary<string, Cryptocurrency> Watchlist { get; }
        public DataAnalyzer Analyzer { get; }
        public ServiceNumerics Numerics { get; }
    }

    sealed class Connection<Service> : IConnection
        where Service : IConnection, new() {
        public Connection() {
            service = new Service();
        }

        public void PrepareDatasets(string[] input) {
            service.PrepareDatasets(input);
        }

        public void UpdateDataset() {
            Analyzer.ProcessData(
                data: Watchlist,
                shallAddRow: true
            );
        }

        public void ReceiveCurrentData() {
            service.ReceiveCurrentData();
        }

        public Dictionary<string, double> Cryptocurrencies {
            get {
                return service.Cryptocurrencies;
            }
        }

        public Dictionary<string, Cryptocurrency> Watchlist {
            get {
                return service.Watchlist;
            }
        }

        DataAnalyzer IConnection.Analyzer {
            get {
                return service.Analyzer;
            }
        }

        private DataAnalyzer Analyzer {
            get {
                return service.Analyzer;
            }
        }

        public ServiceNumerics Numerics {
            get {
                return service.Numerics;
            }
        }

        private readonly Service service;

        internal void TryRemove(string inCryptocurrency) {
            if (Watchlist.ContainsKey(inCryptocurrency)) {
                Analyzer.Remove(inCryptocurrency);
                Watchlist.Remove(inCryptocurrency);
            }
        }

        internal void TryAdd(string inCryptocurrency) {
            if (Cryptocurrencies.ContainsKey(inCryptocurrency)
            && !Watchlist.ContainsKey(inCryptocurrency)) {
                service.PrepareDatasets(new string[1] { inCryptocurrency });
                var cryptocurrencyAction = new Cryptocurrency(
                    inAction: State.Default,
                    inPrice: Cryptocurrencies[inCryptocurrency]
                );
                Watchlist[inCryptocurrency] = cryptocurrencyAction;
            }
            else {
                Printer.ShowCantAdd();
            }
        }

        internal void TryDeposit(double inDepositValue) {
            if (inDepositValue < Numerics.MinimumDeposit) {
                Printer.ShowMinDepositRequired(Numerics.MinimumDeposit);
            }
            else {
                Analyzer.Deposit(inDepositValue);
            }
        }

        internal void CallMarket() {
            // accessible from connection
            Watchlist.Print();
        }

        internal void CallIndicators() {
            Analyzer.ShowIndicators();
        }

        internal void CallAssets() {
            Analyzer.ShowAssets();
        }

        internal void CallWithdraw() {
            Analyzer.Withdraw(Watchlist);
        }

        internal void CallTransactions() {
            Analyzer.ShowTransactions();
        }
    }

    sealed internal class BinanceConnection : IConnection {
        private struct BinanceAPICryptocurrencyInfo {
            public string Symbol { get; init; }
            public double Price { get; init; }
        }

        private string GetDatasetEndpoint(string inSymbol) {
            return string.Format("{0}/api/v3/klines?symbol={1}&interval=1m", BaseUrl, inSymbol);
        }

        private string GetCurrentDataEndpoint() {
            return string.Format("{0}/api/v3/ticker/price", BaseUrl);
        }

        public BinanceConnection() {
            BaseUrl = "https://api.binance.com";
            Client = new HttpClient();
            JsonOptions = new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            Cryptocurrencies = new Dictionary<string, double>();
            Numerics = new ServiceNumerics(
                inTradingFee: 0.001,
                inDepositFee: 0.01,
                inWithdrawalFee: 0.01,
                inMinDeposit: 15
            );
            Analyzer = new DataAnalyzer(Numerics);
            Watchlist = new Dictionary<string, Cryptocurrency>();
        }

        public void ReceiveCurrentData() {
            /**/
            Stopwatch sw = new Stopwatch();
            sw.Start();
            /**/
            try {
                string endpoint = GetCurrentDataEndpoint();
                var response = Client.GetAsync(endpoint).ConfigureAwait(false).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                Console.WriteLine("Request #{0} - Time: {1}", ++responseCounter, DateTime.Now);

                // binance api - a long list of {"symbol" : "example", "price" : "0.0001"}
                Cryptocurrencies = JsonSerializer
                    .Deserialize<List<BinanceAPICryptocurrencyInfo>>(content, JsonOptions)
                    .ToDictionary(member => member.Symbol, member => member.Price);

                foreach (var (symbol, info) in Watchlist) {
                    Watchlist[symbol] = new Cryptocurrency(info.Action, Cryptocurrencies[symbol]);
                }
            }

            catch (HttpRequestException exc) {
                Console.WriteLine(exc.Message);
            }
            Analyzer.ProcessData(
                data: Watchlist,
                shallAddRow: false
            );

            sw.Stop();
            Console.WriteLine("{0} ms", sw.ElapsedMilliseconds);
            /**/
        }

        public async Task<List<double>> ReceiveDataset(string name) {
            // https://binance-docs.github.io/apidocs/spot/en/#kline-candlestick-data
            int closingPriceIndex = 4;
            string endpoint = GetDatasetEndpoint(name);
            var response = await Client.GetAsync(endpoint).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine("Request #{0} - Time: {1}", ++responseCounter, DateTime.Now);
            List<double> result = new List<double>();
            var storage = JsonSerializer.Deserialize<double[][]>(content, JsonOptions);
            foreach (var v in storage) {
                result.Add(v[closingPriceIndex]);
            }
            return result;
        }

        private void AddNewCryptocurrency(string symbol) {
            var cryptocurrency = new Cryptocurrency(
                inAction: State.Default,
                inPrice: Cryptocurrencies[symbol]
            );
            Watchlist[symbol] = cryptocurrency;
        }

        private IEnumerable<string> FilterSetPreferences(string[] inSymbols) {
            foreach (var symbol in inSymbols) {
                if (Cryptocurrencies.ContainsKey(symbol)
                && !Watchlist.ContainsKey(symbol)) {
                    AddNewCryptocurrency(symbol);
                    // give the opportunity process on the go
                    yield return symbol;
                }
                else {
                    Console.WriteLine("{0} is not available", symbol);
                }
            }
        }

        public void PrepareDatasets(string[] inSymbols) {
            try {
                var sw = new Stopwatch();
                sw.Start();
                var filteredSymbols = FilterSetPreferences(inSymbols);
                var query = filteredSymbols.AsParallel().AsOrdered().Select(symbol => {
                    return (
                        Symbol: symbol,
                        Dataset: ReceiveDataset(symbol)
                     );
                });
                query.ForAll(part => {
                    Analyzer.Add(part.Symbol, part.Dataset.Result);
                });
                sw.Stop();
                Console.WriteLine("{0} ms", sw.ElapsedMilliseconds);

                Analyzer.ShowDataset();
            }
            catch (HttpRequestException exc) {
                Console.WriteLine(exc.Message);
            }
        }

        public Dictionary<string, double> Cryptocurrencies { get; private set; }
        public Dictionary<string, Cryptocurrency> Watchlist { get; private set; }
        public DataAnalyzer Analyzer { get; init; }

        //TODO: delete later - make sure we fit into API limits
        int responseCounter = 0;
        private string BaseUrl { get; }
        private HttpClient Client { get; }
        private JsonSerializerOptions JsonOptions { get; }
        public ServiceNumerics Numerics { get; }
    }

    sealed internal class DummyConnection : IConnection {
        public DummyConnection() {
            // optionally if needed - initialize service connected numerics
            // trading fee, withdrawal fee, minimum deposit
        }

        public void PrepareDatasets(string[] input) {
            throw new NotImplementedException();
        }

        public void ReceiveCurrentData() {
            throw new NotImplementedException();
        }
        public Dictionary<string, double> Cryptocurrencies { get; }
        public Dictionary<string, Cryptocurrency> Watchlist { get; }
        public DataAnalyzer Analyzer { get; }
        public ServiceNumerics Numerics { get; }
    }
}
