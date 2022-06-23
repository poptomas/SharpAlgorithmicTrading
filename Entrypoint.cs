
global using Service = AlgorithmicTrading.BinanceConnection; // CoinbaseConnection, DummyConnection, etc.
global using DataAnalyzer = AlgorithmicTrading.TechnicalIndicatorsAnalyzer;

namespace AlgorithmicTrading {
    internal struct Entrypoint {
        private readonly TimeSpan delay;

        public Entrypoint() {
            delay = TimeSpan.FromSeconds(10);
        }

        internal void Run() {
            var conn = new Connection<Service>();
            InputProcessor proc = new InputProcessor(conn);
            string[] userInput = proc.Process();
            conn.ReceiveCurrentData();
            conn.PrepareDatasets(userInput);
            proc.ShowInitialHelp();
            RunLoop(conn, proc);
        }

        private void RunLoop(Connection<Service> conn, InputProcessor proc) {
            DateTime currentTime = DateTime.Now;
            ThreadController controller = new ThreadController();
            
            ThreadStart cinDelegate = () => {
                proc.ReadInput(controller);
            };
            
            Thread cinThread = new Thread(cinDelegate);
            cinThread.Start();
            while (cinThread.IsAlive) {
                if (controller.WaitFor(delay)) {
                    conn.ReceiveCurrentData();
                }
                DateTime updatedTime = DateTime.Now;
                TimeSpan elapsed = updatedTime - currentTime;
                if (elapsed >= TimeSpan.FromMinutes(1)) {
                    // get the latest info possible before update
                    conn.UpdateDataset();
                    Console.WriteLine("Dataset update zzz....");
                    currentTime = updatedTime;
                }
            }
        }
    }

    internal class Program {
        static void Main(string[] args) {
            var entrypoint = new Entrypoint();
            entrypoint.Run();
        }
    }
}