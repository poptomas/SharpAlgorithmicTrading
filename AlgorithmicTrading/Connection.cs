using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace AlgorithmicTrading {
    interface IConnection {
        public void ReceiveCurrentData();
        public void PrepareDatasets(string[] input);
        public Dictionary<string, double> Cryptocurrencies { get; }
        public ConcurrentDictionary<string, Cryptocurrency> Watchlist { get; }
        public DataAnalyzer Analyzer { get; }
        public ServiceInfo ServiceInfo { get; }
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

        public ConcurrentDictionary<string, Cryptocurrency> Watchlist {
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

        public ServiceInfo ServiceInfo {
            get {
                return service.ServiceInfo;
            }
        }

        private readonly Service service;

        public void TryRemove(string inCryptocurrency) {
            if (Watchlist.ContainsKey(inCryptocurrency)) {
                Analyzer.Remove(inCryptocurrency);
                Watchlist.Remove(inCryptocurrency, out _);
                Printer.DisplayRemovedSuccess(inCryptocurrency);
            }
            else {
                Printer.WarnNotFound(inCryptocurrency);
            }
        }

        public void TryAdd(string inCryptocurrency) {
            if (IsValidInput(inCryptocurrency)) {
                service.PrepareDatasets(new string[1] { inCryptocurrency });
                var cryptocurrencyAction = new Cryptocurrency(
                    inAction: State.Default,
                    inPrice: Cryptocurrencies[inCryptocurrency]
                );
                Watchlist[inCryptocurrency] = cryptocurrencyAction;
                Printer.DisplayAddedSuccess(inCryptocurrency);
            }
            else if(Watchlist.ContainsKey(inCryptocurrency)) {
                Printer.WarnAlreadyInWatchlist(inCryptocurrency);
            }
            else {
                Printer.WarnNotFound(inCryptocurrency);
            }
        }

        public void TryDeposit(double inDepositValue) {
            if (inDepositValue < ServiceInfo.MinimumDeposit) {
                Printer.WarnMinDepositRequired(ServiceInfo.MinimumDeposit);
            }
            else {
                Analyzer.Deposit(inDepositValue);
            }
        }

        public void CallMarket() {
            // accessible from connection
            if (Watchlist.Count == 0) {
                Printer.DisplayWatchlistEmpty();
            }
            else {
                Watchlist.Print();
            }
        }

        public void CallIndicators() {
            Analyzer.ShowIndicators();
        }

        public void CallAssets() {
            Analyzer.ShowAssets(Watchlist);
        }

        public void CallWithdraw() {
            Analyzer.Withdraw(Watchlist);
        }

        public void CallTransactions() {
            Analyzer.ShowTransactions();
        }

        private bool IsValidInput(string symbol) {
            return symbol.Contains(ServiceInfo.Currency)
                && Cryptocurrencies.ContainsKey(symbol)
                && !Watchlist.ContainsKey(symbol);
        }
    }

    sealed class BinanceConnection : IConnection {
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
            ServiceInfo = new ServiceInfo(
                inTradingFee: 0.001,
                inDepositFee: 0.01,
                inWithdrawalFee: 0.01,
                inMinDeposit: 15,
                inCurrency: "USD"
            );
            Analyzer = new DataAnalyzer(ServiceInfo);
            Watchlist = new ConcurrentDictionary<string, Cryptocurrency>();
        }
        public Dictionary<string, double> Cryptocurrencies { get; private set; }
        public ConcurrentDictionary<string, Cryptocurrency> Watchlist { get; private set; }
        public DataAnalyzer Analyzer { get; init; }
        public ServiceInfo ServiceInfo { get; }

        public void ReceiveCurrentData() {
            if (firstTime) {
                ReceiveCurrentDataSync();
                firstTime = false;
            }
            else {
                //ReceiveCurrentDataAsyncWithParallelism();
                ReceiveCurrentDataAsync();
            }
        }

        public void PrepareDatasets(string[] inSymbols) {
            if (inSymbols.Length == 0) {
                Printer.WarnEmptyWatchlist();
            }
            var filteredSymbols = FilterSetPreferences(inSymbols);
            var query = filteredSymbols
                .AsParallel()
                .AsUnordered()
                .Select(symbol => {
                    return (
                        Symbol: symbol,
                        Dataset: ReceiveDataset(symbol)
                        );
                });
            query.ForAll(part => {
                Analyzer.Add(part.Symbol, part.Dataset.Result);
            });
        }

        private async Task<List<double>> ReceiveDataset(string name) {
            // https://binance-docs.github.io/apidocs/spot/en/#kline-candlestick-data
            int closingPriceIndex = 4;
            string endpoint = GetDatasetEndpoint(name);
            List<double> result = new List<double>();
            try {
                var response = await Client
                    .GetStreamAsync(endpoint);
                var storage = await JsonSerializer
                    .DeserializeAsync<double[][]>(response, JsonOptions);
                foreach (var arr in storage) {
                    result.Add(arr[closingPriceIndex]);
                }
            }
            catch (HttpRequestException) {
                Printer.WarnConnectionLost(endpoint);
            }
            return result;
        }

        private void FormWatchlist(List<BinanceAPICryptocurrencyInfo> parsedValues) {
            Cryptocurrencies = parsedValues.ToDictionary(member => member.Symbol, member => member.Price);
            lock (Watchlist) {
                foreach (var (symbol, info) in Watchlist.ToList()) {
                    Watchlist[symbol] = new Cryptocurrency(info.Action, Cryptocurrencies[symbol]);
                }
            }
        }

        private async Task<List<BinanceAPICryptocurrencyInfo>> Deserialize(Stream response) {
            return await JsonSerializer
                .DeserializeAsync<List<BinanceAPICryptocurrencyInfo>>(response, JsonOptions)
                .ConfigureAwait(false);
        }

        private async Task<List<BinanceAPICryptocurrencyInfo>> GetJsonAsync(string endpoint, SemaphoreSlim sem) {
            await sem.WaitAsync();
            var response = await Client.GetStreamAsync(endpoint).ConfigureAwait(false);
            var result = await Deserialize(response);
            return result;
        }

        private async void ReceiveCurrentDataAsyncWithParallelism() {
            int requestMultiplier = 4; // launches multiple requests and the fastest one is taken further
                                       // Environment.ProcessorCount may cause way too many requests -> temporary suspension
            string endpoint = GetCurrentDataEndpoint();
            var urls = Enumerable.Range(1, requestMultiplier).Select(v => endpoint);
            try {
                // Deserialization of Binance API
                // - a single long list, such as [ {"symbol" : "example", "price" : "0.0001"}, ..., {...} ]
                var semaphore = new SemaphoreSlim(requestMultiplier);
                var tasks = urls.Select(url => GetJsonAsync(url, semaphore));
                var fastestTask = await Task.WhenAny(tasks);
                FormWatchlist(fastestTask.Result);
            }
            catch (HttpRequestException) {
                Printer.WarnConnectionLost(endpoint);
            }
            Analyzer.ProcessData(
                data: Watchlist,
                shallAddRow: false
            );
        }

        private async void ReceiveCurrentDataAsync() {
            string endpoint = GetCurrentDataEndpoint();
            try {
                // Deserialization of Binance API
                // - a single long list, such as [ {"symbol" : "example", "price" : "0.0001"}, ..., {...} ]
                var response = await Client.GetStreamAsync(endpoint).ConfigureAwait(false);
                var result = await Deserialize(response);
                FormWatchlist(result);
            }
            catch (HttpRequestException) {
                Printer.WarnConnectionLost(endpoint);
            }
            Analyzer.ProcessData(
                data: Watchlist,
                shallAddRow: false
            );
        }

        private void ReceiveCurrentDataSync() {
            string endpoint = GetCurrentDataEndpoint();
            try {
                var response = Client.GetStreamAsync(endpoint).GetAwaiter().GetResult();
                var result = JsonSerializer
                    .Deserialize<List<BinanceAPICryptocurrencyInfo>>(response, JsonOptions);
                ;
                FormWatchlist(result);
            }
            catch (HttpRequestException) {
                Printer.ErrorConnectionLost();
            }
            Analyzer.ProcessData(
                data: Watchlist,
                shallAddRow: false
            );
        }

        private void AddNewCryptocurrency(string symbol) {
            var cryptocurrency = new Cryptocurrency(
                inAction: State.Default,
                inPrice: Cryptocurrencies[symbol]
            );
            Watchlist[symbol] = cryptocurrency;
        }

        private bool IsValidInput(string symbol) {
            return symbol.Contains(ServiceInfo.Currency)
                && Cryptocurrencies.ContainsKey(symbol)
                && !Watchlist.ContainsKey(symbol);
        }

        private IEnumerable<string> FilterSetPreferences(string[] inSymbols) {
            foreach (var symbol in inSymbols) {
                if (IsValidInput(symbol)) {
                    AddNewCryptocurrency(symbol);
                    yield return symbol; // give the opportunity to process gradually
                }
                else if(Watchlist.ContainsKey(symbol)) {
                    Printer.WarnAlreadyInWatchlist(symbol);
                }
                else {
                    Printer.WarnIsUnavailable(symbol);
                }
            }
        }

        private string GetDatasetEndpoint(string inSymbol) {
            return string.Format("{0}/api/v3/klines?symbol={1}&interval=1m", BaseUrl, inSymbol);
        }

        private string GetCurrentDataEndpoint() {
            return string.Format("{0}/api/v3/ticker/price", BaseUrl);
        }

        // force first synchronous data retrieval, then async only
        private bool firstTime = true;
        private string BaseUrl { get; }
        private HttpClient Client { get; }
        private JsonSerializerOptions JsonOptions { get; }
    }

    sealed class DummyConnection : IConnection {
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
        public ConcurrentDictionary<string, Cryptocurrency> Watchlist { get; }
        public DataAnalyzer Analyzer { get; }
        public ServiceInfo ServiceInfo { get; }
    }
}
