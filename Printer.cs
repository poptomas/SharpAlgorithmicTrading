using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
	static class Printer {

		internal static void ShowTotal(double finalBalance, string currency, double fee) {
            Console.WriteLine("You ended up with {0} {1} (withdrawal fee: {2} %)", finalBalance, currency, fee);
        }

		internal static void ShowCantAdd() {
			Console.WriteLine("Already in the list/invalid cryptocurrency");
		}

		internal static void ShowHelpHeader() {
            Console.WriteLine("Supported commands (case insensitive, without <>): ");
        }

		internal static void ShowSeparator() {
			int lineLength = 40;
			Console.WriteLine(new string('-', lineLength));
		}

		private static void PrintUnknownAction(string inCommand) {
			Console.WriteLine("Unknown action: \"{0}\"", inCommand);
		}

		internal static void PrintCommandsCommon(string inCommand, bool wasFound) {
			if(!wasFound) {
				PrintUnknownAction(inCommand);
				ShowSeparator();
			}
        }

		internal static void ShowHeader() {
			Console.WriteLine("ToTheMoon (Cryptocurrency Trading Bot)");
			Console.WriteLine("For cryptocurrency symbols see https://coinmarketcap.com/exchanges/binance");
			// slashes e.g. BTC/USDT can be included - when parsing the input it is removed anyway
			Console.WriteLine("- an example to use: BTCUSDT ETHUSDT SOLUSDT ADAUSDT (case insensitive, slash can be included)");
			Console.Write("Add cryptocurrency symbols to your watchlist: ");
		}

	}
}
