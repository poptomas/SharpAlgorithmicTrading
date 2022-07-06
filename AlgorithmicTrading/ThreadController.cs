using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    struct ThreadController {
        private readonly object mutex;
        public ThreadController() {
            mutex = new object();
        }

        public bool WaitFor(TimeSpan time) {
            lock(mutex) {
                return !Monitor.Wait(mutex, time);
            }
        }

        public void ReleaseAll() {
            lock(mutex) {
                Monitor.PulseAll(mutex);
            }
        }
    }
}
