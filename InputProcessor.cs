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
        private readonly IConnection service;

        public InputProcessor(IConnection inConnection) {
            service = inConnection;

            // to ensure simple mapping between an enum mapped to a concrete string command and its description
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
                { Options.Help, ShowHelp },
                { Options.Withdraw, Withdraw },
                { Options.Transactions, BypassTransactions },
                { Options.Current, BypassCurrent },
                { Options.Indicators, BypassIndicators },
                { Options.Market, BypassMarket },
                //...
            };

            paramFuncMap = new Dictionary<Options, Action<string>>() {
               { Options.Deposit, TryDepositCash },
               { Options.Add, TryAddCryptocurrency },
               { Options.Remove, TryRemoveCryptocurrency },
               //...
            };

            slash = '/';
            delimiter = ' ';
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
            Printer.PrintSeparator();
            ShowHelp();
            Printer.PrintSeparator();
        }

        internal void ShowHelp() {
            Printer.PrintHelpHeader();
            foreach(var (option, command) in enumMap) {
                Console.WriteLine(command.GetLine());
            }
        }

        internal void BypassTransactions() {

        }

        internal void BypassIndicators() {

        }

        internal void BypassCurrent() {

        }

        internal void Withdraw() {

        }

        internal void BypassMarket() {

        }

        internal void TryDepositCash(string amount) {

        }

        internal void TryAddCryptocurrency(string inCryptocurrency) {

        }

        internal void TryRemoveCryptocurrency(string inCryptocurrency) {

        }

        private void ProcessSimpleCommand(string inCommand) {
            bool wasFound = false;
            foreach(var (key, functionCall) in simpleFuncMap) {
                var command = enumMap[key];
                if(command.Name == inCommand) {
                    functionCall();
                    wasFound = true;
                    break;
                }
            }
            Printer.PrintCommandsCommon(inCommand, wasFound);
        }

        private void ProcessParamCommand(string[] tokens) {
            bool wasFound = false;
            foreach (var (key, functionCall) in paramFuncMap) {
                var command = enumMap[key];
                if (command.Name == tokens[0]) {
                    functionCall(tokens[1]);
                    wasFound = true;
                    break;
                }
            }
            Printer.PrintCommandsCommon(
                string.Join(" ", tokens), 
                wasFound
            );
        }

        internal void ReadInput(ThreadController c) {
            while(true) {
                var line = Console.ReadLine();
                if (line == null) {
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

        private readonly char slash;
        private readonly char delimiter;
    }
}
