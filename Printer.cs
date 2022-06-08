using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
	static class Printer {

		internal static void PrintHelpHeader() {
            Console.WriteLine("Supported commands (case insensitive, without <>): ");
        }

		internal static void PrintSeparator() {
			int lineLength = 40;
			Console.WriteLine(new string('-', lineLength));
		}

		private static void PrintUnknownAction(string inCommand) {
			Console.WriteLine("Unknown action: \"{0}\"", inCommand);
		}

		internal static void PrintCommandsCommon(string inCommand, bool wasFound) {
			if(!wasFound) {
				PrintUnknownAction(inCommand);
				PrintSeparator();
			}
        }

		internal static void PrintHeader() {
			Console.WriteLine("ToTheMoon (Cryptocurrency Trading Bot)");
			Console.WriteLine("For cryptocurrency symbols see https://coinmarketcap.com/exchanges/binance");
			// slashes e.g. BTC/USDT can be included - when parsing the input it is removed anyway
			Console.WriteLine("- an example to use: BTCUSDT ETHUSDT SOLUSDT ADAUSDT (case insensitive, slash can be included)");
			Console.Write("Add cryptocurrency symbols to your watchlist: ");
		}

	}
}
