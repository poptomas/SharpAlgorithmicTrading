using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {

	static class Printer {
		private static void Warn(this string message) {
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("[WARNING] {0}", message);
			Console.ResetColor();
		}

		private static void Success(this string message) {
			Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
			Console.ResetColor();
		}

		#region OnBuy, OnSell
		internal static void ShowBuySignal(string symbol, double price) {
			DisplayTime(); // "non-event" driven call - needs to be added explicitly
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("[BUY SIGNAL]: {0} at {1} USD", symbol, price);
            Console.WriteLine();
			Console.ResetColor();
		}

		internal static void DisplaySellForced(string symbol, double price) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("{0} was forced to be sold at {1} USD", symbol, price);
			Console.WriteLine();
			Console.ResetColor();
		}

		internal static void DisplaySellSignal(string symbol, double price) {
			DisplayTime(); // "non-event" driven call - needs to be added explicitly
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[SELL SIGNAL]: {0} at {1} USD", symbol, price);
			Console.WriteLine();
			Console.ResetColor();
		}
		#endregion

		#region OnWarnings

		internal static void WarnFileOpen() {
			string message = "Transactions results cannot be written to the file, it is used by another process";
			message.Warn();
		}

		internal static void WarnCantBuy(string symbol, double price) {
			string message = string.Format(
				"BUY SIGNAL: {0} (at price {1:0.#####} USD) cannot be bought - deposit to increase funds", 
				symbol, price
			);
			message.Warn();
		}

		internal static void WarnCantSell(string symbol, double price) {
			string message = string.Format(
				"SELL SIGNAL: {0} (at price {1:0.#####} USD) cannot be sold - you do not possess any", 
				symbol, price
			);
			message.Warn();
		}

		internal static void WarnEmptyWatchlist() {
			string message = "Make sure to use add <symbol> command, otherwise your watchlist remains empty";
			message.Warn();
		}

		internal static void WarnAlreadyInWatchlist(string inSymbol) {
			string message = string.Format("{0} is already in your watchlist", inSymbol);
			message.Warn();
		}

		internal static void WarnNotFound(string inSymbol) {
			string message = string.Format("{0} was not found", inSymbol);
			message.Warn();
		}

		internal static void WarnIsUnavailable(string inSymbol) {
			string message = string.Format("{0} is not available", inSymbol);
			message.Warn();
		}

		internal static void WarnUnknownAction(string userInput) {
			string message = string.Format("Unknown action: \"{0}\"", userInput);
			message.Warn();
		}

		internal static void WarnMinDepositRequired(double minDeposit) {
			string message = string.Format("Deposit at least {0} USD", minDeposit);
			message.Warn();
		}

		internal static void WarnInvalidAmount() {
			string message = "Invalid amount";
			message.Warn();
		}

		internal static void WarnConnectionLost(string endpoint) {
			string message = string.Format("Connection to {0} was lost", endpoint);
			message.Warn();
		}


		#endregion

		#region OnSuccess
		internal static void DisplayDepositSuccess(double depositValue, double fee, double currentBalance) {
			string message = string.Format("{0} USD added (deposit fee: {1:P}), current balance: {2:0.###} USD", depositValue, fee, currentBalance);
			message.Success();
        }

		internal static void DisplayAddedSuccess(string inCryptocurrency) {
			string message = string.Format("{0} added successfully", inCryptocurrency);
			message.Success();
		}

		internal static void DisplayRemovedSuccess(string inCryptocurrency) {
			string message = string.Format("{0} removed successfully", inCryptocurrency);
			message.Success();
		}
        #endregion

        #region Neutral
        internal static void DisplayTotalBalance(double finalBalance, string currency, double fee) {
			string message = string.Format("You ended up with {0} {1} (withdrawal fee: {2:P})", finalBalance, currency, fee);
			Console.WriteLine(message);
		}

		internal static void DisplayHelpHeader() {
			string message = "Supported commands (case insensitive, without <>): ";
			Console.WriteLine(message);
        }

		internal static void DisplaySeparator() {
			int lineLength = 40;
			Console.WriteLine(new string('-', lineLength));
		}

		internal static void PrintCommandsCommon(string inCommand, bool wasFound, InputProcessor proc) {
			if(!wasFound) {
				WarnUnknownAction(inCommand);
				proc.ShowHelp();
			}
		}

		internal static void DisplayEstimatedWithdrawal(double estBalance, string currency, double fee) {
			string message = string.Format("Estimated withdrawal: {0:0.#####} {1} (after {2:P} fee)", estBalance, currency, fee);
			Console.WriteLine(message);
		}

		internal static void DisplayNoTransactionsYet() {
			string message = "No transactions have been accomplished yet.";
			Console.WriteLine(message);
		}

		internal static void DisplayHeader() {
			Console.WriteLine("Algorithmic Trading Bot for Cryptocurrency");
			Console.WriteLine("	For cryptocurrency symbols see https://coinmarketcap.com/exchanges/binance");
			// slashes e.g. BTC/USDT can be included - when parsing the input it is removed anyway
			Console.WriteLine("	- an example to use: BTCUSDT ETHUSDT SOLUSDT ADAUSDT (case insensitive, slash can be included)");
			Console.Write("Add cryptocurrency symbols to your watchlist: ");
		}

        internal static void DisplayWatchlistEmpty() {
			string message = "Your watchlist is empty";
			Console.WriteLine(message);
		}

		internal static void DisplayTime() {
			Console.WriteLine(DateTime.Now);
		}

        #endregion
    }
}
