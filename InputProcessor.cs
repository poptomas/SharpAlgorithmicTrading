using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal class InputProcessor {
        enum Options { Add, Current, Deposit, Help, Indicators, Market, Remove, Transactions, Withdraw };
        private Dictionary<Options, Command> enumMap;
        private Dictionary<Options, Action> simpleFuncMap;
        private Dictionary<Options, Action<string>> paramFuncMap;

        public InputProcessor() {
            slash = '/';
            delimiter = ' ';

            enumMap = new Dictionary<Options, Command>() {
                { Options.Help, new Command("help", "prints this help") },
                { Options.Deposit, new Command("deposit <value>", "adds amount of cash to your account") },
                { Options.Withdraw, new Command("withdraw", "withdraw all your currently possessed cryptocurrencies and end the session") },
                { Options.Current, new Command("current", "shows the amount of your currently possessed cryptocurrencies including cash currency") },
                { Options.Transactions, new Command("transactions", "shows your recently accomplished transactions") },
                { Options.Market, new Command("market", "shows current cryptocurrency market prices from your watchlist") },
                { Options.Indicators, new Command("indicators", "shows indicators concerning your cryptocurrency watchlist") },
                { Options.Add, new Command("add <symbol>", "adds a cryptocurrency symbol to your watchlist") },
                { Options.Remove, new Command("remove <symbol>", "removes a cryptocurrency symbol from your watchlist") }
            };

            simpleFuncMap = new Dictionary<Options, Action>() {
                { Options.Help, ShowInitialHelp },
                { Options.Transactions, ShowTransactions },
                { Options.Withdraw, Withdraw },
                { Options.Current, ShowCurrent },
                { Options.Indicators, ShowIndicators },
                { Options.Market, ShowMarket },
                //...
            };

            paramFuncMap = new Dictionary<Options, Action<string>>() {
               { Options.Deposit, TryDepositCash },
               { Options.Add, TryAddCryptocurrency },
               { Options.Remove, TryRemoveCryptocurrency },
               //...
            };
        }

        internal string[] Process(string[] arguments) {
            if (arguments.IsEmpty()) {
                Printer.PrintHeader();
                return ProcessCinArguments();
            }
            else {
                return ProcessCmdlineArguments(arguments);
            }
        }

        internal void ShowInitialHelp() {

        }

        internal void ShowTransactions() {

        }

        internal void ShowIndicators() {

        }

        internal void ShowCurrent() {

        }

        internal void Withdraw() {

        }

        internal void ShowMarket() {

        }

        internal void TryDepositCash(string amount) {

        }

        internal void TryAddCryptocurrency(string inCryptocurrency) {

        }

        internal void TryRemoveCryptocurrency(string inCryptocurrency) {

        }


        private void ProcessSimpleCommand(string command) {
            Console.WriteLine("Ten jednoduchej: {0}", command);
        }

        private void ProcessParamCommand(string[] tokens) {
            Console.WriteLine("Ten s parametrem: {0} -> {1}", tokens[0], tokens[1]);
        }

        internal void ReadInput(ThreadController c) {
            while(true) {
                var line = Console.ReadLine();
                if(line == null) {
                    continue;
                }
                var lower = line.ToLower();
                var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if(tokens.Length == 1 && lower == "quit") {
                    ProcessSimpleCommand(lower);
                    c.Kill();
                    break;
                }
                else if(tokens.Length == 1) {
                    ProcessSimpleCommand(lower);
                }
                else if(tokens.Length == 2) {
                    ProcessParamCommand(tokens);
                }
                else {
                    Console.WriteLine("Bad decision");
                }
            }
        }

        private string[] GetFiltered(string[] inStorage) {
            int length = inStorage.Length;
            for (int idx = 0; idx < length; ++idx) {
                inStorage[idx] = inStorage[idx].Filter(slash).ToUpper();
            }
            return inStorage;
        }

        private string[] ProcessCmdlineArguments(string[] arguments) {
            return GetFiltered(arguments);
        }

        private string[] ProcessCinArguments() {
            var userInput = Console.ReadLine();
            var entries = userInput.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            return GetFiltered(entries);
        }

        private char slash;
        private char delimiter;
    }
}
