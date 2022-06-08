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

    using Analyzer = TechnicalIndicatorsAnalyzer;

    internal interface IConnection {
        public void ReceiveCurrentData(bool addToDataset);
        public void PrepareDatasets(string[] input);
        public Dictionary<string, double> Cryptocurrencies { get; }
        public Dictionary<string, Cryptocurrency> Watchlist { get; }
    }

    sealed internal class Connection<Service> : IConnection
        where Service : IConnection, new() {
        public Connection() {
            service = new Service();
            analyzer = new Analyzer();
        }

        public void PrepareDatasets(string[] input) {
            service.PrepareDatasets(input);
        }

        public void ReceiveCurrentData(bool addToDataset) {
            service.ReceiveCurrentData(addToDataset);
            if(addToDataset) {
                analyzer.AddToDataset(Watchlist);
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

        private readonly IAnalyzer analyzer;
        private readonly Service service;
    }

    sealed internal class BinanceConnection : IConnection {
        private struct BinanceAPICryptocurrencyInfo {
            public string Symbol { get; init; }
            public double Price { get; init; }
        }

        public BinanceConnection() {
            Endpoint = new Uri("https://api.binance.com/api/v3/ticker/price");
            Client = new HttpClient();
            JsonOptions = new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            Cryptocurrencies = new Dictionary<string, double>();
        }


        public async void ReceiveCurrentData(bool addToDataset) {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try {
                var response = await Client.GetAsync(Endpoint).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine("Request #{0} - Time: {1}", ++responseCounter, DateTime.Now);

                    // binance api - a long list of {"symbol" : "example", "price" : "0.0001"}
                    Cryptocurrencies = JsonSerializer
                    .Deserialize<List<BinanceAPICryptocurrencyInfo>>(content, JsonOptions)
                    .ToDictionary(member => member.Symbol, member => member.Price);
            }
            catch(HttpRequestException ex){
                Console.WriteLine(ex.Message);
            }
            sw.Stop();
            foreach (var v in Cryptocurrencies) {
                if (v.Key.EndsWith("USDT")) {
                    Console.WriteLine("[{0} : {1}]", v.Key, v.Value);
                }
            }
            Console.WriteLine("{0} ms", sw.ElapsedMilliseconds);
        }

        public void PrepareDatasets(string[] input) {

        }

        public Dictionary<string, double> Cryptocurrencies { get; private set; }
        public Dictionary<string, Cryptocurrency> Watchlist { get; private set; }

        //TODO: delete later - make sure we fit into API limits
        int responseCounter = 0;
        private Uri Endpoint { get; init; }
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
    }
}
