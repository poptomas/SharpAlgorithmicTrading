using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlgorithmicTrading {
    interface IConnection {
        public void ReceiveCurrentData();
        public void PrepareDatasets(string[] input);
        public Dictionary<string, double> Cryptocurrencies { get; }
        public SortedDictionary<string, Cryptocurrency> Watchlist { get; }
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

        public SortedDictionary<string, Cryptocurrency> Watchlist {
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
                Printer.ShowRemovedSuccessfully(inCryptocurrency);
            }
            else {
                Printer.ShowNotFound(inCryptocurrency);
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
                Printer.ShowAddedSuccessfully(inCryptocurrency);
            }
            else if(Watchlist.ContainsKey(inCryptocurrency)) {
                Printer.ShowAlreadyExists(inCryptocurrency);
            }
            else {
                Printer.ShowNotFound(inCryptocurrency);
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
            if (Watchlist.Count == 0) {
                Printer.ShowWatchlistEmpty();
            }
            else {
                Watchlist.Print();
            }
        }

        internal void CallIndicators() {
            Analyzer.ShowIndicators();
        }

        internal void CallAssets() {
            Analyzer.ShowAssets(Watchlist);
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
            Watchlist = new SortedDictionary<string, Cryptocurrency>();
        }
        public Dictionary<string, double> Cryptocurrencies { get; private set; }
        public SortedDictionary<string, Cryptocurrency> Watchlist { get; private set; }
        public DataAnalyzer Analyzer { get; init; }

        public void ReceiveCurrentData() {
            if (firstTime) {
                ReceiveCurrentDataSync();
                firstTime = false;
            }
            else {
                ReceiveCurrentDataAsync();
            }
        }

        public async Task<List<double>> ReceiveDataset(string name) {
            /**/
            // parallelization tryout
            Stopwatch sw = new Stopwatch();
            sw.Start();
            /**/

            // https://binance-docs.github.io/apidocs/spot/en/#kline-candlestick-data
            int closingPriceIndex = 4;
            string endpoint = GetDatasetEndpoint(name);
            List<double> result = new List<double>();
            try {
                var response = await Client
                    .GetStreamAsync(endpoint);
                //Console.WriteLine("Request #{0} - Time: {1}", ++responseCounter, DateTime.Now);
                var storage = await JsonSerializer
                    .DeserializeAsync<double[][]>(response, JsonOptions);
                foreach (var arr in storage) {
                    result.Add(arr[closingPriceIndex]);
                }
            }
            catch (HttpRequestException exc) {
                Console.WriteLine(exc.Message);
            }
            /*
            sw.Stop();
            sw.ShowMs(string.Format("ReceiveDataset({0})", name));
            /**/
            return result;
        }

        private void FormWatchlist(List<BinanceAPICryptocurrencyInfo> parsedValues) {
            Cryptocurrencies = parsedValues.ToDictionary(member => member.Symbol, member => member.Price);
            foreach (var (symbol, info) in Watchlist.ToList()) {
                Watchlist[symbol] = new Cryptocurrency(info.Action, Cryptocurrencies[symbol]);
            }
        }

        private async void ReceiveCurrentDataAsync() {
            /*
            Stopwatch sw = new Stopwatch();
            sw.Start();
            /**/

            try {
                string endpoint = GetCurrentDataEndpoint();

                // for deserialization:
                // binance api - a long list [ {"symbol" : "example", "price" : "0.0001"}, ..., {...} ]

                /**/
                //variant 1)
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var response = await Client.GetStreamAsync(endpoint);
                sw.Stop();  
                sw.ShowMs("Part 1");
                sw.Restart();
                var result = await JsonSerializer
                    .DeserializeAsync<List<BinanceAPICryptocurrencyInfo>>(response, JsonOptions);
                sw.Stop();
                sw.ShowMs("Part 2");
                /**/

                /*
                //variant 2)
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var response = await Client.GetStringAsync(endpoint);
                var result = JsonSerializer.Deserialize<List<BinanceAPICryptocurrencyInfo>>(response, JsonOptions);
                /**/

                /*
                //variant 3)
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var response = await Client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                sw.Stop();  
                sw.ShowMs("Part 1");
                sw.Restart();
                var content = await response.Content.ReadAsStreamAsync();
                sw.Stop();
                sw.ShowMs("Part 2");
                sw.Restart();
                var result = await JsonSerializer.DeserializeAsync<List<BinanceAPICryptocurrencyInfo>>(content, JsonOptions);
                sw.Stop();
                sw.ShowMs("Part 3");
                /**/


                //Console.WriteLine("Request #{0} - Time: {1}", ++responseCounter, DateTime.Now);

                FormWatchlist(result);
            }
            catch (TaskCanceledException) { // time exceeded
                return;
            }
            catch (HttpRequestException exc) {
                Console.WriteLine(exc.Message);
            }
            Analyzer.ProcessData(
                data: Watchlist,
                shallAddRow: false
            );

            /*
            sw.Stop();
            sw.ShowMs("ReceiveCurrentDataAsync()");
            /**/
        }


        private void ReceiveCurrentDataSync() {
            /*
            Stopwatch sw = new Stopwatch();
            sw.Start();
            /**/
            try {
                string endpoint = GetCurrentDataEndpoint();
                var response = Client.GetStreamAsync(endpoint).GetAwaiter().GetResult();
                //Console.WriteLine("Request #{0} - Time: {1}", ++responseCounter, DateTime.Now);

                var result = JsonSerializer
                    .Deserialize<List<BinanceAPICryptocurrencyInfo>>(response, JsonOptions);
                ;
                FormWatchlist(result);
            }
            catch (HttpRequestException exc) {
                Console.WriteLine(exc.Message);
            }
            Analyzer.ProcessData(
                data: Watchlist,
                shallAddRow: false
            );
            /*
            sw.Stop();
            sw.ShowMs("ReceiveCurrentDataSync()");
            /**/
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
                    // give the opportunity to process on the go
                    yield return symbol;
                }
                else if(Watchlist.ContainsKey(symbol)) {
                    Printer.ShowAlreadyExists(symbol);
                }
                else {
                    Printer.ShowIsUnavailable(symbol);
                }
            }
        }

        public void PrepareDatasets(string[] inSymbols) {
            /**/
            var sw = new Stopwatch();
            sw.Start();
            /**/
            var filteredSymbols = FilterSetPreferences(inSymbols);

            try {
                var query = filteredSymbols
                    .AsParallel()
                    .AsUnordered()
                    // testing the difference -> .WithDegreeOfParallelism(2)
                    .Select(symbol => {
                        return (
                            Symbol: symbol,
                            Dataset: ReceiveDataset(symbol)
                         );
                    });
                query.ForAll(part => {
                    Analyzer.Add(part.Symbol, part.Dataset.Result);
                });
                //Analyzer.ShowDataset();
            }
            catch (Exception exc) {
                Console.WriteLine(exc.Message);
            }

            /**/
            sw.Stop();
            sw.ShowMs("PrepareDatasets(string[] inSymbols)");
            /**/
        }

        private string GetDatasetEndpoint(string inSymbol) {
            return string.Format("{0}/api/v3/klines?symbol={1}&interval=1m", BaseUrl, inSymbol);
        }

        private string GetCurrentDataEndpoint() {
            return string.Format("{0}/api/v3/ticker/price", BaseUrl);
        }

        // force first synchronous data retrieval, then async only
        private bool firstTime = true;
        //TODO: delete later - make sure we fit into API limits
        private int responseCounter = 0;
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
        public SortedDictionary<string, Cryptocurrency> Watchlist { get; }
        public DataAnalyzer Analyzer { get; }
        public ServiceNumerics Numerics { get; }
    }
}
