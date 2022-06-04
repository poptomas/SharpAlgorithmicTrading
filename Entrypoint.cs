namespace AlgorithmicTrading {

    // set accordingly
    using Service = BinanceConnection; // CoinbaseConnection, DummyConnection, etc.

    internal struct Entrypoint {
        TimeSpan delay;
        DateTime currentTime;

        public Entrypoint() {
            delay = TimeSpan.FromSeconds(5);
            currentTime = DateTime.Now;
        }

        internal void Run(string[] args) {
            IConnection conn = new Connection<Service>();
            InputProcessor proc = new InputProcessor();
            string[] userInput = proc.Process(args);
            conn.ReceiveCurrentData(addToDataset: false);
            conn.PrepareDatasets(userInput);
            proc.ShowInitialHelp();
            RunLoop(conn, proc);
        }

        private void RunLoop(IConnection conn, InputProcessor proc) {
            bool addToDataset = false;
            ThreadController c = new ThreadController();
            ThreadStart cinDelegate = () => {
                proc.ReadInput(c);
            };
            ThreadStart workerDelegate = () => {
                conn.ReceiveCurrentData(addToDataset);
            };
            Thread cinThread = new Thread(cinDelegate);
            var workerThread = new Thread(workerDelegate);
            cinThread.Start();
            while (cinThread.IsAlive) {
                if (c.WaitFor(delay)) {
                    workerThread = new Thread(workerDelegate);
                    workerThread.Start();
                    workerThread.Join();
                }
                DateTime updatedTime = DateTime.Now;
                TimeSpan elapsed = updatedTime - currentTime;
                addToDataset = false;
                if (elapsed >= TimeSpan.FromMinutes(1)) {
                    addToDataset = true;
                    currentTime = updatedTime;
                }
                c.GiveChance();
            }
            cinThread.Join();
        }
    }

    internal class Program {
        static void Main(string[] args) {
            var entrypoint = new Entrypoint();
            entrypoint.Run(args);
        }
    }
}