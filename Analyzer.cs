using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal interface IAnalyzer {
        public void AddToDataset(Dictionary<string, Cryptocurrency> currentWatchlist);
        public void RemoveFromDataset(Dictionary<string, Cryptocurrency> currentWatchlist);
    }

    internal struct TechnicalIndicatorsAnalyzer : IAnalyzer {
        public void AddToDataset(Dictionary<string, Cryptocurrency> currentWatchlist) {
            throw new NotImplementedException();
        }

        public void RemoveFromDataset(Dictionary<string, Cryptocurrency> currentWatchlist) {
            throw new NotImplementedException();
        }

        internal void PrepareSymbol(string symbol, List<double> result) {
            Console.WriteLine("Cryptocurrency: {0}", symbol);
            foreach(var v in result) {
                Console.Write("{0} ", v);
            }
        }
    }
}
