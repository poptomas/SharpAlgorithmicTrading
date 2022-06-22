using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {
    internal struct ThreadController {
        object mutex;
        public ThreadController() {
            mutex = new object();
        }

        internal bool WaitFor(TimeSpan time) {
            lock(mutex) {
                return !Monitor.Wait(mutex, time);
            }
        }

        internal void Release() {
            lock(mutex) {
                Monitor.Pulse(mutex);
            }
        }

        internal void ReleaseAll() {
            lock(mutex) {

                Monitor.PulseAll(mutex);
            }
        }
    }
}
