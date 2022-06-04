using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal struct ThreadController {
        Mutex mutex;
        public ThreadController() {
            mutex = new Mutex();
        }

        internal bool WaitFor(TimeSpan time) {
            lock(mutex) {
                return !Monitor.Wait(mutex, time);
            }
        }

        internal void GiveChance() {
            lock(mutex) {
                Monitor.Pulse(mutex);
            }
        }

        internal void Kill() {
            lock(mutex) {
                Monitor.PulseAll(mutex);
            }
        }
    }
}
