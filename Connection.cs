using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;



namespace AlgorithmicTrading {
    using TaskList = List<Task<List<double>>>;

    interface IConnection {
        public void ReceiveCurrentData();
        public void PrepareDatasets(string[] input);
        public Dictionary<string, double> Cryptocurrencies { get; }
        public Dictionary<string, Cryptocurrency> Watchlist { get; }
        public DataAnalyzer Analyzer { get; }
        public ServiceNumerics Numerics { get; }
    }

    sealed internal class Connection<Service> : IConnection
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
            if(Watchlist.ContainsKey(inCryptocurrency)) {
                Analyzer.Remove(inCryptocurrency);
                Watchlist.Remove(inCryptocurrency);
            }
        }

        internal void TryAdd(string inCryptocurrency) {
            if (Cryptocurrencies.ContainsKey(inCryptocurrency) 
            && !Watchlist.ContainsKey(inCryptocurrency)) {
                var cryptocurrencyAction = new Cryptocurrency(
                    inAction: State.Default,
                    inPrice: Cryptocurrencies[inCryptocurrency]
                );
                Watchlist[inCryptocurrency] = cryptocurrencyAction;
            }
            else {
                Console.WriteLine("Already in the list/invalid cryptocurrency");
            }
        }

        internal void TryDeposit(double total) {
            throw new NotImplementedException();
        }

        internal void DisplayMarket() {
            foreach (var (name, info) in Watchlist) {
                Console.WriteLine("[{0}: {1}]", name, info.Price);
            }
        }

        internal void DisplayIndicators() {
            throw new NotImplementedException();
        }

        internal void DisplayCurrent() {
            foreach(var (name, info) in Watchlist) {
                Console.WriteLine("[{0}: {1}]", name, info.Price);
            }
        }

        internal void DisplayTransactions() {
            throw new NotImplementedException();
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
            Analyzer = new DataAnalyzer();
            Numerics = new ServiceNumerics(
                inTradingFee: 0.001,
                inWithdrawalFee: 0.01,
                inMinDeposit: 15
            );
            Watchlist = new Dictionary<string, Cryptocurrency>();
        }


        public async void ReceiveCurrentData() {
            /*
            Stopwatch sw = new Stopwatch();
            sw.Start();
            /**/
            try {
                string endpoint = GetCurrentDataEndpoint();
                var response = await Client.GetAsync(endpoint).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine("Request #{0} - Time: {1}", ++responseCounter, DateTime.Now);

                    // binance api - a long list of {"symbol" : "example", "price" : "0.0001"}
                Cryptocurrencies = JsonSerializer
                    .Deserialize<List<BinanceAPICryptocurrencyInfo>>(content, JsonOptions)
                    .ToDictionary(member => member.Symbol, member => member.Price);

                foreach(var (symbol, info) in Watchlist) {
                    Watchlist[symbol] = new Cryptocurrency(info.Action, Cryptocurrencies[symbol]);
                }
            }
            catch(HttpRequestException exc){
                Console.WriteLine(exc.Message);
            }
            Analyzer.ProcessData(
                data: Watchlist, 
                shallAddRow: false
            );

            /*
            sw.Stop();
            foreach (var v in Cryptocurrencies) {
                if (v.Key.EndsWith("USDT")) {
                    Console.WriteLine("[{0} : {1}]", v.Key, v.Value);
                }
            }
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
            foreach(var symbol in inSymbols) {
                if (Cryptocurrencies.ContainsKey(symbol)) {
                    AddNewCryptocurrency(symbol);
                    // give the opportunity process on the go
                    yield return symbol;
                }
                else {
                    Console.WriteLine($"{symbol} unavailable...");
                }
            }
        }

        public async void PrepareDatasets(string[] inSymbols) {
            var filteredSymbols = FilterSetPreferences(inSymbols);
            try {
                // slow part
                TaskList tasks = new TaskList();
                foreach(var symbol in filteredSymbols) {
                    tasks.Add(Task.Run(() => ReceiveDataset(symbol)));
                }
                await Task.WhenAll(tasks);
                // end of the slow part
                // easier tasks can run in serial
                // (only 500 records per cryptocurrency symbol)

                /*
                var sw = new Stopwatch();
                sw.Start();
                /**/

                var zipped = filteredSymbols.Zip(tasks);
                foreach (var (symbol, task) in zipped) {
                    Analyzer.PrepareSymbol(symbol, task.Result);
                }

                /*
                sw.Stop();
                Console.WriteLine("{0} ms", sw.ElapsedMilliseconds);
                /**/
                // dbg
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
