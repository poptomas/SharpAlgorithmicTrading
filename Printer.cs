using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {

	static class Printer {
		private static void Warn(this string message) {
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine("[WARNING] {0}", message);
			Console.ResetColor();
		}

		private static void Error(this string message) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ResetColor();
			Environment.Exit(Environment.ExitCode);
		}

		private static void Success(this string message) {
			Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
			Console.ResetColor();
		}

		#region OnBuy, OnSell
		public static void ShowBuySignal(string symbol, double price) {
			DisplayTime(); // "non-event" driven call - needs to be added explicitly
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("[BUY SIGNAL]: {0} at {1} USD", symbol, price);
            Console.WriteLine();
			Console.ResetColor();
		}

		public static void DisplaySellSignal(string symbol, double price) {
			DisplayTime(); // "non-event" driven call - needs to be added explicitly
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[SELL SIGNAL]: {0} at {1} USD", symbol, price);
			Console.WriteLine();
			Console.ResetColor();
		}

		public static void DisplaySellForced(string symbol, double price) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("{0} was forced to be sold at {1} USD", symbol, price);
			Console.WriteLine();
			Console.ResetColor();
		}

		#endregion // OnBuy, OnSell

		#region OnWarnings

		public static void WarnFileOpen() {
			string message = "Transactions results cannot be written to the file, it is used by another process";
			message.Warn();
		}

		public static void WarnCantBuy(string symbol, double price) {
			string message = string.Format(
				"BUY SIGNAL: {0} (at price {1:0.#####} USD) cannot be bought - deposit to increase funds", 
				symbol, price
			);
			message.Warn();
            Console.WriteLine();
		}

		public static void WarnCantSell(string symbol, double price) {
			DisplayTime(); // "non-event" driven call - needs to be added explicitly
			string message = string.Format(
				"SELL SIGNAL: {0} (at price {1:0.#####} USD) cannot be sold - you do not possess any", 
				symbol, price
			);
			message.Warn();
			Console.WriteLine();
		}

		public static void WarnEmptyWatchlist() {
			string message = "Make sure to use add <symbol> command, otherwise your watchlist remains empty";
			message.Warn();
		}

		public static void WarnAlreadyInWatchlist(string inSymbol) {
			string message = string.Format("{0} is already in your watchlist", inSymbol);
			message.Warn();
		}

		public static void WarnNotFound(string inSymbol) {
			string message = string.Format("{0} was not found", inSymbol);
			message.Warn();
		}

		public static void WarnIsUnavailable(string inSymbol) {
			string message = string.Format("{0} is not available", inSymbol);
			message.Warn();
		}

		public static void WarnUnknownAction(string userInput) {
			string message = string.Format("Unknown action: \"{0}\"", userInput);
			message.Warn();
		}

		public static void WarnMinDepositRequired(double minDeposit) {
			string message = string.Format("Deposit at least {0} USD", minDeposit);
			message.Warn();
		}

		public static void WarnInvalidAmount() {
			string message = "Invalid amount";
			message.Warn();
		}

		public static void WarnConnectionLost(string endpoint) {
			string message = string.Format("Connection to {0} was lost", endpoint);
			message.Warn();
		}

		#endregion // OnWarnings

		#region OnError

		public static void ErrorConnectionLost() {
			string message = "Connection lost: Make sure that you are connected to the internet";
			message.Error();
		}

        #endregion // OnError

        #region OnSuccess

        public static void DisplayDepositSuccess(double depositValue, double fee, double currentBalance) {
			string message = string.Format("{0} USD added (deposit fee: {1:P}), current balance: {2:0.###} USD", depositValue, fee, currentBalance);
			message.Success();
        }

		public static void DisplayAddedSuccess(string inCryptocurrency) {
			string message = string.Format("{0} added successfully", inCryptocurrency);
			message.Success();
		}

		public static void DisplayRemovedSuccess(string inCryptocurrency) {
			string message = string.Format("{0} removed successfully", inCryptocurrency);
			message.Success();
		}
        #endregion // OnSuccess

        public static void DisplayTotalBalance(double finalBalance, string currency, double fee) {
			string message = string.Format("You ended up with {0:0.##} {1} (withdrawal fee: {2:P})", finalBalance, currency, fee);
			Console.WriteLine(message);
		}

		public static void DisplayHelpHeader() {
			string message = "Supported commands (case insensitive, without <>): ";
			Console.WriteLine(message);
        }

		public static void DisplaySeparator() {
			int lineLength = 40;
			Console.WriteLine(new string('-', lineLength));
		}

		public static void PrintCommandsCommon(string inCommand, bool wasFound, InputProcessor proc) {
			if(!wasFound) {
				WarnUnknownAction(inCommand);
				proc.ShowHelp();
			}
		}

		public static void DisplayEstimatedWithdrawal(double estBalance, string currency, double fee) {
			string message = string.Format("Estimated withdrawal: {0:0.#####} {1} (after {2:P} fee)", estBalance, currency, fee);
			Console.WriteLine(message);
		}

		public static void DisplayNoTransactionsYet() {
			string message = "No transactions have been accomplished yet.";
			Console.WriteLine(message);
		}

		public static void DisplayHeader() {
			Console.WriteLine("Cryptocurrency Algorithmic Trading Bot");
			Console.WriteLine("	For cryptocurrency symbols see https://coinmarketcap.com/exchanges/binance");
			// slashes e.g. BTC/USDT can be included - when parsing the input it is removed anyway
			Console.WriteLine("	- an example to use: BTCUSDT ETHUSDT SOLUSDT ADAUSDT (case insensitive, slash can be included)");
			Console.Write("Add cryptocurrency symbols to your watchlist: ");
		}

        public static void DisplayWatchlistEmpty() {
			string message = "Your watchlist is empty";
			Console.WriteLine(message);
		}

		public static void DisplayTime() {
			Console.WriteLine(DateTime.Now);
		}
    }
}
