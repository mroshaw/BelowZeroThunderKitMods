using System.Collections.Generic;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Implements a simple PriorityQueue
    /// </summary>
    internal class NavPriorityQueue<T>
    {
        private readonly List<KeyValuePair<T, float>> _elements = new List<KeyValuePair<T, float>>();

        public int Count => _elements.Count;

        public void Enqueue(T item, float priority)
        {
            _elements.Add(new KeyValuePair<T, float>(item, priority));
            HeapifyUp(_elements.Count - 1);
        }

        public T Dequeue()
        {
            var bestItem = _elements[0].Key;
            var last = _elements[_elements.Count - 1];
            _elements[0] = last;
            _elements.RemoveAt(_elements.Count - 1);
            HeapifyDown(0);
            return bestItem;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_elements[index].Value >= _elements[parent].Value) break;
                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int lastIndex = _elements.Count - 1;
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left <= lastIndex && _elements[left].Value < _elements[smallest].Value)
                    smallest = left;
                if (right <= lastIndex && _elements[right].Value < _elements[smallest].Value)
                    smallest = right;
                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j)
        {
            (_elements[i], _elements[j]) = (_elements[j], _elements[i]);
        }
    }
}