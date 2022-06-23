using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal class InputProcessor {
        enum Options { Add, Assets, Deposit, Help, Indicators, Market, Remove, Transactions, Withdraw };
        private Dictionary<Options, Command> enumMap;
        private Dictionary<Options, Action> simpleFuncMap;
        private Dictionary<Options, Action<string>> paramFuncMap;
        private readonly Connection<Service> service;

        public InputProcessor(Connection<Service> inConnection) {
            service = inConnection;

            // to ensure simple mapping between an enum mapped to a concrete string command and its description
            enumMap = new Dictionary<Options, Command>() {
                { Options.Help, new Command("help", "prints this help") },
                { Options.Deposit, new Command("deposit <value>", "adds amount of cash to your account") },
                { Options.Withdraw, new Command("withdraw", "withdraw all your currently possessed cryptocurrencies and end the session") },
                { Options.Assets, new Command("assets", "shows the amount of your currently possessed cryptocurrencies including cash currency") },
                { Options.Transactions, new Command("transactions", "shows your recently accomplished transactions") },
                { Options.Market, new Command("market", "shows current cryptocurrency market prices from your watchlist") },
                { Options.Indicators, new Command("indicators", "shows indicators concerning your cryptocurrency watchlist") },
                { Options.Add, new Command("add <symbol>", "adds a cryptocurrency symbol to your watchlist") },
                { Options.Remove, new Command("remove <symbol>", "removes a cryptocurrency symbol from your watchlist") }
            };

            simpleFuncMap = new Dictionary<Options, Action>() {
                { Options.Help, ShowHelp },
                { Options.Withdraw, Withdraw },
                { Options.Transactions, CallTransactions },
                { Options.Assets, CallAsssets },
                { Options.Indicators, CallIndicators },
                { Options.Market, CallMarket },
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

        internal string[] Process() {
            Printer.ShowHeader();
            return ProcessCinArguments();
        }

        internal void ShowInitialHelp() {
            Printer.ShowSeparator();
            ShowHelp();
            Printer.ShowSeparator();
        }

        internal void ShowHelp() {
            Printer.ShowHelpHeader();
            foreach(var (option, command) in enumMap) {
                Console.WriteLine(command.GetLine());
            }
        }

        internal void CallTransactions() {
            Console.WriteLine("Transactions done.");
            service.CallTransactions();
        }

        internal void CallIndicators() {
            service.CallIndicators();
        }

        internal void CallAsssets() {
            Console.WriteLine("Assets done.");
            service.CallAssets();
        }

        internal void CallMarket() {
            Console.WriteLine("Market done.");
            service.CallMarket();
        }

        internal void Withdraw() {
            Console.WriteLine("Withdrawal done.");
        }

        internal void TryDepositCash(string amount) {
            if(double.TryParse(amount, out double total) && total > 0) {
                service.TryDeposit(total);
            }
            else {
                Console.WriteLine("Problem.");
            }
        }

        internal void TryAddCryptocurrency(string inCryptocurrency) {
            service.TryAdd(inCryptocurrency);
        }

        internal void TryRemoveCryptocurrency(string inCryptocurrency) {
            service.TryRemove(inCryptocurrency);
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
                if (command.Name.StartsWith(tokens[0])) {
                    var symbol = tokens[1].Filter(slash).ToUpper();
                    functionCall(symbol);
                    wasFound = true;
                    break;
                }
            }
            Printer.PrintCommandsCommon(
                string.Join(" ", tokens), 
                wasFound
            );
        }

        internal void ReadInput(ThreadController controller) {
            while (true) {
                var line = Console.ReadLine();
                if (line == null) {
                    continue;
                }
                var lower = line.ToLower();
                var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if(tokens.Length == 1 
                && lower == enumMap[Options.Withdraw].Name) {
                    ProcessSimpleCommand(lower);
                    controller.ReleaseAll();
                    break;
                }
                else if(tokens.Length == 1) {
                    ProcessSimpleCommand(lower);
                }
                else if(tokens.Length == 2) {
                    ProcessParamCommand(tokens);
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Red;
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

        private string[] ProcessCinArguments() {
            var userInput = Console.ReadLine();
            var entries = userInput.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            return GetFiltered(entries);
        }

        private readonly char slash;
        private readonly char delimiter;
    }
}
