using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
	static class Printer {
		internal static void Print(string input) {
			Console.Write(input);
		}

		internal static void Println(string input) {
            Console.WriteLine(input);
        }

		internal static void PrintSeparator() {
			int lineLength = 40;
			Print(new string('-', lineLength));
		}

		internal static void PrintHeader() {
			Println("ToTheMoon (Cryptocurrency Trading Bot)");
			Println("For cryptocurrency symbols see https://coinmarketcap.com/exchanges/binance");
			// slashes e.g. BTC/USDT can be included - when parsing the input it is removed anyway
			Println("- an example to use: BTCUSDT ETHUSDT SOLUSDT ADAUSDT (case insensitive, slash can be included)");
			Print("Add cryptocurrency symbols to your watchlist: ");
		}

	}
}
