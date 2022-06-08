using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal struct Command {
        public Command(string inName, string inDescription) {
            Name = inName;
            Description = inDescription;
        }
        public string Name { get; init; }
        public string Description { get; init; }
    }
}
