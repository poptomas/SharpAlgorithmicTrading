
global using Service = AlgorithmicTrading.BinanceConnection; // CoinbaseConnection, DummyConnection, etc.
global using DataAnalyzer = AlgorithmicTrading.TechnicalIndicatorsAnalyzer;

namespace AlgorithmicTrading {
    struct Entrypoint {
        private readonly TimeSpan delay;

        public Entrypoint() {
            delay = TimeSpan.FromSeconds(1);
        }

        public void Run() {
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
            bool isInitial = true;

            ThreadController controller = new ThreadController();
            ThreadStart cinDelegate = () => {
                proc.ReadInput(controller);
            };
            Thread cinThread = new Thread(cinDelegate);
            cinThread.Start();

            while (cinThread.IsAlive) {
                if (isInitial || controller.WaitFor(delay)) {
                    conn.ReceiveCurrentData();
                    isInitial = false;
                }
                DateTime updatedTime = DateTime.Now;
                TimeSpan elapsed = updatedTime - currentTime;
                if (elapsed >= TimeSpan.FromMinutes(1)) {
                    conn.UpdateDataset();
                    currentTime = updatedTime;
                }
            }
        }
    }

    class Program {
        static void Main(string[] args) {
            var entrypoint = new Entrypoint();
            entrypoint.Run();
        }
    }
}