using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
	static class Printer {

		internal static void ShowUnknownAction(string userInput) {
			Console.WriteLine("Unknown action: \"{0}\"", userInput);
		}

		internal static void ShowDepositSuccesful(double depositValue, double fee) {
            Console.WriteLine("{0} USD added (deposit fee: {1:P})", depositValue, fee);
        }

		internal static void ShowMinDepositRequired(double minDeposit) {
			Console.WriteLine($"add at least {0} USD", minDeposit);
		}

		internal static void ShowTotal(double finalBalance, string currency, double fee) {
            Console.WriteLine("You ended up with {0} {1} (withdrawal fee: {2:P})", finalBalance, currency, fee);
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

		internal static void PrintCommandsCommon(string inCommand, bool wasFound) {
			if(!wasFound) {
				ShowUnknownAction(inCommand);
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
