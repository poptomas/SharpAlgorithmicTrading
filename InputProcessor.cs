using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal class InputProcessor {
        public InputProcessor() {
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

        }

        private void ProcessSimpleCommand(string command) {
            Console.WriteLine(command);
        }

        private void ProcessParamCommand(string[] tokens) {
            Console.WriteLine("{0}: {1}", tokens[0], tokens[1]);
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
