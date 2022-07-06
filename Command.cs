using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    struct Command {
        public Command(string inName, string inDescription) {
            Name = inName;
            Description = inDescription;
        }
        public string Name { get; init; }
        public string Description { get; init; }
        public string GetLine() {
            string hyphenation = " - ";
            int nameLength = Name.Length;
            int nameOffset = 20;
            StringBuilder sb = new StringBuilder();
            sb.Append(Name)
              .Append(hyphenation);
            if(nameLength < nameOffset) {
                int dotCount = nameOffset - nameLength;
                sb.Append(new string('.', dotCount));
            }
            sb.Append(hyphenation)
              .Append(Description);
            return sb.ToString();
        }
    }
}
