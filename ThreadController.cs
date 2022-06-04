using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal class ThreadController {
        Mutex mutex;
        bool shallStop;
        public ThreadController() {
            mutex = new Mutex();
            shallStop = false;
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
                shallStop = true;
                Monitor.PulseAll(mutex);
            }
        }
    }
}
