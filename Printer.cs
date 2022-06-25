using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {

	static class Printer {
		internal static void ShowBuySignal(string symbol, double price) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("[BUY SIGNAL]: {0} at {1} USD", symbol, price);
			Console.Clear();
		}

		internal static void ShowForcedSell(string symbol, double price) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("{0} was forced to be sold at {1} USD", symbol, price);
			Console.Clear();
		}

		internal static void ShowSellSignal(string symbol, double price) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[SELL SIGNAL]: {0} at {1} USD", symbol, price);
			Console.Clear();
		}

		internal static void ShowAddedSuccessfully(string inCryptocurrency) {
            Console.WriteLine("{0} added successfully", inCryptocurrency);
		}

		internal static void ShowRemovedSuccessfully(string inCryptocurrency) {
			Console.WriteLine("{0} removed successfully", inCryptocurrency);
		}

		internal static void ShowAlreadyExists(string inSymbol) {
			Console.WriteLine("{0} is already in your watchlist", inSymbol);
		}

		internal static void ShowNotFound(string inSymbol) {
			Console.WriteLine("{0} was not found", inSymbol);
		}

		internal static void ShowIsUnavailable(string inSymbol) {
			Console.WriteLine("{0} is not available", inSymbol);
		}

		internal static void ShowUnknownAction(string userInput) {
			Console.WriteLine("Unknown action: \"{0}\"", userInput);
		}

		internal static void ShowDepositSuccessful(double depositValue, double fee) {
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
