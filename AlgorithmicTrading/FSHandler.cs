
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    class FSHandler {
        private readonly string directory;
        private readonly string file;

        public FSHandler(string path) {
            directory = Path.GetDirectoryName(path);
            file = Path.GetFullPath(path);
            try {
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
                else if (File.Exists(file)) {
                    File.Delete(file);
                }
                using StreamWriter writer = new StreamWriter(file);
            }
            catch (IOException) {
                Printer.WarnFileOpen();
            }
        }

        public void Save(string row) {
            try {
                using StreamWriter writer = File.AppendText(file);
                writer.WriteLine(row);
            }
            catch (IOException) {
                Printer.WarnFileOpen();
            }
        }
    }
}
