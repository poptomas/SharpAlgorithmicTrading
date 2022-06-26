
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal class FSHandler {
        private readonly string directory;
        private readonly string file;
        private TextWriter writer;

        public FSHandler(string path) {
            directory = Path.GetDirectoryName(path);
            file = Path.GetFileName(path);
        }

        public void TryFlushPreviousRun() {
            try {
                if(Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
                else if(File.Exists(file)) {
                    File.Delete(file);
                }
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
            }
            writer = new StreamWriter(file);
        }

        public void Save(string row) {
            try {
                writer.WriteLine(row);
            }
            catch(IOException exc) {
                Console.WriteLine(exc.Message);
            }
        }
    }
}
