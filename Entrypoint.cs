namespace AlgorithmicTrading {

    // set accordingly
    using Service = BinanceConnection; // CoinbaseConnection, DummyConnection, etc.

    internal struct Entrypoint {
        private readonly TimeSpan delay;

        public Entrypoint() {
            delay = TimeSpan.FromSeconds(5);
        }

        internal void Run(string[] args) {
            IConnection conn = new Connection<Service>();
            InputProcessor proc = new InputProcessor(conn);
            string[] userInput = proc.Process(args);
            conn.ReceiveCurrentData(addToDataset: false);
            conn.PrepareDatasets(userInput);
            proc.ShowInitialHelp();
            RunLoop(conn, proc);
        }

        private void RunLoop(IConnection conn, InputProcessor proc) {
            bool addToDataset = false;
            DateTime currentTime = DateTime.Now;
            ThreadController controller = new ThreadController();
            ThreadStart cinDelegate = () => {
                proc.ReadInput(controller);
            };
            ThreadStart workerDelegate = () => {
                conn.ReceiveCurrentData(addToDataset);
            };
            Thread cinThread = new Thread(cinDelegate);

            cinThread.Start();
            while (cinThread.IsAlive) {
                if (controller.WaitFor(delay)) {
                    var workerThread = new Thread(workerDelegate);
                    workerThread.Start();
                    workerThread.Join();
                }
                DateTime updatedTime = DateTime.Now;
                TimeSpan elapsed = updatedTime - currentTime;
                addToDataset = false;
                if (elapsed >= TimeSpan.FromMinutes(1)) {
                    Console.WriteLine("i am good do not worry mate");
                    addToDataset = true;
                    currentTime = updatedTime;
                }
                controller.GiveChance();
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