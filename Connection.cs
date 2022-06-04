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
    internal interface IConnection {
        public void ReceiveCurrentData(bool addToDataset);
        public void PrepareDatasets(string[] input);
        public Dictionary<string, double> Cryptocurrencies { get; }
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
        }

        public Dictionary<string, double> Cryptocurrencies{
            get {
                return service.Cryptocurrencies;
            }
        }

        private readonly Service service;
    }

    sealed internal class BinanceConnection : IConnection {
        internal class Cryptocurrency {
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
            CryptocurrencyTicker = new Dictionary<string, double>();
        }

        public Dictionary<string, double> Cryptocurrencies {
            get { return CryptocurrencyTicker; } 
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
                CryptocurrencyTicker = JsonSerializer
                    .Deserialize<List<Cryptocurrency>>(content, JsonOptions)
                    .ToDictionary(v => v.Symbol, v => v.Price);
            }
            catch(HttpRequestException ex){
                Console.WriteLine(ex.Message);
            }
            sw.Stop();
            Console.WriteLine(CryptocurrencyTicker.Count);
            Console.WriteLine("{0} ms", sw.ElapsedMilliseconds);
        }

        public void PrepareDatasets(string[] input) {

        }

        //TODO: delete later - make sure we fit into API limits
        int responseCounter = 0;
        private Uri Endpoint { get; init; }
        private HttpClient Client { get; }
        private JsonSerializerOptions JsonOptions { get; }
        private Dictionary<string, double> CryptocurrencyTicker { get; set; }
    }

    sealed internal class DummyConnection : IConnection {
        public void PrepareDatasets(string[] input) {
            throw new NotImplementedException();
        }

        public void ReceiveCurrentData(bool addToDataset) {
            throw new NotImplementedException();
        }
        public Dictionary<string, double> Cryptocurrencies { get; }
    }
}
