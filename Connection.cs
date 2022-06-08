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

    using DataAnalyzer = TechnicalIndicatorsAnalyzer;

    internal interface IConnection {
        public void ReceiveCurrentData(bool addToDataset);
        public void PrepareDatasets(string[] input);
        public Dictionary<string, double> Cryptocurrencies { get; }
        public Dictionary<string, Cryptocurrency> Watchlist { get; }
        public DataAnalyzer Analyzer { get; }
    }

    sealed internal class Connection<Service> : IConnection
        where Service : IConnection, new() {
        public Connection() {
            service = new Service();
        }



        public void PrepareDatasets(string[] input) {
            service.PrepareDatasets(input);
        }

        public void ReceiveCurrentData(bool addToDataset) {
            service.ReceiveCurrentData(addToDataset);
            if(addToDataset) {
                Analyzer.AddToDataset(Watchlist);
            }
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

        public DataAnalyzer Analyzer {
            get {
                return service.Analyzer;
            }
        }

        private readonly Service service;
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
        }

        public async void ReceiveCurrentData(bool addToDataset) {
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
            }
            catch(HttpRequestException exc){
                Console.WriteLine(exc.Message);
            }
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

        public async void PrepareDatasets(string[] inSymbols) {
            try {
                int length = inSymbols.Length;
                // slow part
                var tasks = new Task<List<double>>[inSymbols.Length];
                for (int index = 0; index < length; ++index) {
                    tasks[index] = ReceiveDataset(inSymbols[index]);
                }
                await Task.WhenAll(tasks);
                // end of the slow part
                // easier tasks can run in serial
                // (only 500 records per cryptocurrency symbol)
                
                /*
                var sw = new Stopwatch();
                sw.Start();
                /**/
                for (int index = 0; index < tasks.Length; ++index) {
                    Analyzer.PrepareSymbol(inSymbols[index], tasks[index].Result);
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
        public DataAnalyzer Analyzer { get; }

        //TODO: delete later - make sure we fit into API limits
        int responseCounter = 0;
        private string BaseUrl { get; }
        private HttpClient Client { get; }
        private JsonSerializerOptions JsonOptions { get; }
    }

    sealed internal class DummyConnection : IConnection {
        public void PrepareDatasets(string[] input) {
            throw new NotImplementedException();
        }

        public void ReceiveCurrentData(bool addToDataset) {
            throw new NotImplementedException();
        }
        public Dictionary<string, double> Cryptocurrencies { get; }

        public Dictionary<string, Cryptocurrency> Watchlist { get; }
        public DataAnalyzer Analyzer { get; init; }
    }
}
