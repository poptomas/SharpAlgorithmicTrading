﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    class InputProcessor {
        enum Options { Add, Assets, Deposit, Help, Indicators, Market, Remove, Transactions, Withdraw };
        private readonly Dictionary<Options, Command> enumMap;
        private readonly Dictionary<Options, Action> simpleFuncMap;
        private readonly Dictionary<Options, Action<string>> paramFuncMap;
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
                { Options.Remove, new Command("remove <symbol>", "removes a cryptocurrency symbol from your watchlist") },
                // ...
            };

            simpleFuncMap = new Dictionary<Options, Action>() {
                { Options.Help, ShowHelp },
                { Options.Withdraw, CallWithdraw },
                { Options.Transactions, CallTransactions },
                { Options.Assets, CallAsssets },
                { Options.Indicators, CallIndicators },
                { Options.Market, CallMarket },
                // ...
            };

            paramFuncMap = new Dictionary<Options, Action<string>>() {
               { Options.Deposit, TryDeposit },
               { Options.Add, TryAdd },
               { Options.Remove, TryRemove },
               // ...
            };

            slash = '/';
            delimiter = ' ';
        }

        public string[] Process() {
            Printer.DisplayHeader();
            return ProcessCinArguments();
        }

        public void ShowInitialHelp() {
            Printer.DisplaySeparator();
            ShowHelp();
            Printer.DisplaySeparator();
        }

        public void ShowHelp() {
            Printer.DisplayHelpHeader();
            foreach(var (option, command) in enumMap) {
                Console.WriteLine(command.GetLine());
            }
        }

        public void CallTransactions() {
            service.CallTransactions();
        }

        public void CallIndicators() {
            service.CallIndicators();
        }

        public void CallAsssets() {
            service.CallAssets();
        }

        public void CallMarket() {
            service.CallMarket();
        }

        public void CallWithdraw() {
            service.CallWithdraw();
        }

        public void TryDeposit(string amount) {
            if(double.TryParse(amount, out double total) && total > 0) {
                service.TryDeposit(total);
            }
            else {
                Printer.WarnInvalidAmount();
            }
        }

        public void TryAdd(string inCryptocurrency) {
            service.TryAdd(inCryptocurrency);
        }

        public void TryRemove(string inCryptocurrency) {
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
            Printer.PrintCommandsCommon(inCommand, wasFound, this);
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
            Printer.PrintCommandsCommon(string.Join(" ", tokens), wasFound, this);
        }

        private bool ReadInputInternal(ThreadController controller, string line) {
            var lower = line.ToLower();
            var tokens = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 1
            && lower == enumMap[Options.Withdraw].Name) {
                ProcessSimpleCommand(lower);
                controller.ReleaseAll();
                return false;
            }
            else if (tokens.Length == 1) {
                ProcessSimpleCommand(lower);
            }
            else if (tokens.Length == 2) {
                ProcessParamCommand(tokens);
            }
            else {
                Printer.WarnUnknownAction(line);
                ShowHelp();
            }
            return true;
        }

        public void ReadInput(ThreadController controller) {
            while (true) {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }
                Printer.DisplaySeparator();
                Printer.DisplayTime();
                if (!ReadInputInternal(controller, line)) {
                    break;
                }
                Printer.DisplaySeparator();
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
