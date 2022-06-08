using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal interface IAnalyzer {
        virtual void AddToDataset(Dictionary<string, Cryptocurrency> currentWatchlist) {
            
        }
    }

    internal class DummyAnalyzer : IAnalyzer {
    
    }

    internal class TechnicalIndicatorsAnalyzer : IAnalyzer {

    }
}
