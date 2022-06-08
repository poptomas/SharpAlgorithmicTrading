using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmicTrading {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    public interface IDeque<T> : IList<T> {
        void PushFront(T item);
        void PushBack(T item);
        void PopFront();
        void PopBack();
        T Front();
        T Back();
    }

    public class Deque<T> : IDeque<T> {
        struct Storage {
            public T[] InternalArray { get; init; }
            public Storage() {
                InternalArray = new T[blockSize];
            }
        }
        private Storage[] blocks;
        public Deque() {
            Count = 0;
            Offset = 0;
            Version = 0;
            blocks = new Storage[1];
        }

        public Deque(int inCapacity) {
            Count = 0;
            Offset = 0;
            Version = 0;
            blocks = new Storage[
                Math.Max(inCapacity, blockSize) / blockSize
            ];
        }

        const int blockSize = 32;
        const int extensionMultiplier = 2;

        public int Count { get; private set; }
        public int Offset { get; private set; }
        public int Version { get; set; }
        public bool IsReadOnly { get { return false; } }
        public bool IsFull { get { return Count == blocks.Length * blockSize; } }
        public bool IsModified { get { return Version > 0; } }
        public bool IsEmpty { get { return Count == 0; } }

        public void PushFront(T item) {
            ModificationProtectionCheck();
            ExtensionCheck();
            PushFrontInternal(item);
        }

        public void PushBack(T item) {
            ModificationProtectionCheck();
            ExtensionCheck();
            PushBackInternal(item);
        }

        public void PopFront() {
            ModificationProtectionCheck();
            RemovalFromEmptyCheck();
            PopFrontInternal();
        }

        public void PopBack() {
            RemoveAt(Count - 1);
        }

        public T Front() {
            RemovalFromEmptyCheck();
            return this[0];
        }

        public T Back() {
            RemovalFromEmptyCheck();
            return this[Count - 1];
        }

        public T this[int index] {
            get {
                OutOfRangeCheck(index);
                index = (index + Offset) % (blocks.Length * blockSize);
                int blockIndex = index / blockSize;
                int arrIndex = index % blockSize;
                CreateNewBlockCheck(blockIndex);
                return blocks[blockIndex].InternalArray[arrIndex];
            }
            set {
                ModificationProtectionCheck();
                OutOfRangeCheck(index);
                index = (index + Offset) % (blocks.Length * blockSize);
                int blockIndex = index / blockSize;
                int arrIndex = index % blockSize;
                CreateNewBlockCheck(blockIndex);
                blocks[blockIndex].InternalArray[arrIndex] = value;
            }
        }

        public void Add(T item) {
            PushBack(item);
        }

        public void Clear() {
            ModificationProtectionCheck();
            Offset = 0;
            Count = 0;
            blocks = new Storage[1];
        }

        public bool Contains(T item) {
            return IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int index) {
            CopyingConditionsCheck(array, index);
            for (int iter = 0; iter < Count; ++iter) {
                array[iter + index] = this[iter];
            }
        }

        public int IndexOf(T item) {
            var comparer = EqualityComparer<T>.Default;
            for (int index = 0; index < Count; ++index) {
                if (comparer.Equals(this[index], item)) {
                    return index;
                }
            }
            return -1;
        }

        public void Insert(int index, T item) {
            if (index == 0) {
                PushFront(item);
            }
            else {
                ModificationProtectionCheck();
                CompletelyOutOfRangeCheck(index);
                ExtensionCheck();
                InsertInternal(index, item);
            }
        }

        public bool Remove(T item) {
            int index = IndexOf(item);
            if (index == -1) {
                ModificationProtectionCheck();
                return false;
            }
            else {
                RemoveAt(index);
                return true;
            }
        }

        public void RemoveAt(int index) {
            if (index == 0) {
                PopFront();
            }
            else {
                ModificationProtectionCheck();
                OutOfRangeCheck(index);
                RemovalFromEmptyCheck();
                RemoveAtInternal(index);
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return new Enumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        private void PushFrontInternal(T item) {
            AdditionToEmptyCheck();
            --Offset;
            if (Offset == -1) {
                Offset = blockSize * blocks.Length - 1;
            }
            this[0] = item;
        }

        private void PushBackInternal(T item) {
            AdditionToEmptyCheck();
            this[Count - 1] = item;
        }

        private void PopFrontInternal() {
            this[0] = default;
            --Count;
            Offset = (1 + Offset) % (blockSize * blocks.Length);
        }

        private void InsertInternal(int index, T item) {
            AdditionToEmptyCheck();
            for (int it = Count - 1; it > index; --it) {
                this[it] = this[it - 1];
            }
            this[index] = item;
        }

        private void RemoveAtInternal(int index) {
            for (int idx = index; idx < Count - 1; ++idx) {
                this[idx] = this[idx + 1];
            }
            this[Count - 1] = default;
            --Count;
        }

        private void Extend() {
            Storage[] newBlocks = new Storage[blocks.Length * extensionMultiplier];
            for (int index = 0; index < Count; ++index) {
                int blockIndex = index / blockSize;
                int arrIndex = index % blockSize;
                if (newBlocks[blockIndex].InternalArray == null) {
                    newBlocks[blockIndex] = new Storage();
                }
                newBlocks[blockIndex].InternalArray[arrIndex] = this[index];
            }
            blocks = newBlocks;
            Offset = 0;
        }

        private void OutOfRangeCheck(int index) {
            if (index < 0 || index >= Count) {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void CompletelyOutOfRangeCheck(int index) {
            if (index < 0 || index > Count) {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void ExtensionCheck() {
            if (IsFull) {
                Extend();
            }
        }

        private void CreateNewBlockCheck(int index) {
            if (blocks[index].InternalArray == null) {
                blocks[index] = new Storage();
            }
        }

        private void ModificationProtectionCheck() {
            if (IsModified) {
                throw new InvalidOperationException();
            }
        }

        private void AdditionToEmptyCheck() {
            if (IsEmpty) {
                Count = 1;
                Offset = 0;
            }
            else {
                ++Count;
            }
        }

        private void RemovalFromEmptyCheck() {
            if (IsEmpty) {
                throw new ArgumentOutOfRangeException();
            }
        }

        public void CopyingConditionsCheck(T[] array, int index) {
            if (index < 0) {
                throw new ArgumentOutOfRangeException();
            }
            else if (array == null) {
                throw new ArgumentNullException();
            }
            else if (array.Length - index < Count) {
                throw new ArgumentException();
            }
        }
    }

    public struct Enumerator<T> : IEnumerator<T> {
        private Deque<T> deque;
        private int currentPosition;
        public Enumerator(Deque<T> inDeque) {
            ++inDeque.Version;
            deque = inDeque;
            currentPosition = -1;
        }

        public T Current {
            get {
                return deque[currentPosition];
            }
        }
        object IEnumerator.Current {
            get {
                return Current;
            }
        }

        public void Dispose() {
            --deque.Version;
        }

        public bool MoveNext() {
            ++currentPosition;
            return currentPosition < deque.Count;
        }

        public void Reset() {
            currentPosition = -1;
        }
    }
}
