using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    struct Transaction {
        public Transaction(string inSymbol, double inAmount, double inExchangeRate, string inAction) {
            timestamp = DateTime.Now;
            symbol = inSymbol;
            amount = inAmount;
            exchangeRate = inExchangeRate;
            action = inAction;
        }

        public override string ToString() {
            return string.Format(
                "{0} Symbol: {1} Price: {2:0.#####} Amount: {3:0.#####} Action: {4}",
                timestamp, symbol.PadRight(10), amount, exchangeRate, action
            );
        }
        
        public string GetCSVLine() {
            return string.Format(
                "{0},{1},{2:0.#####},{3:0.#####},{4}", 
                timestamp, symbol, amount, exchangeRate, action
            );
        }

        private readonly DateTime timestamp;
        private readonly string symbol;
        private readonly double amount;
        private readonly double exchangeRate;
        private readonly string action;
    }
}
