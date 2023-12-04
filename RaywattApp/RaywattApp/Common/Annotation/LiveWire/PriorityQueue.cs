using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaywattApp.Common.Annotation.LiveWire
{
    public class PriorityQueue<T>
    {
        private List<T> data;
        private Func<T, T, int> comparison;

        public PriorityQueue()
        {
            data = new List<T>();
            comparison = Comparer<T>.Default.Compare;
        }

        public PriorityQueue(Func<T, T, int> comparison)
        {
            data = new List<T>();
            this.comparison = comparison;
        }

        public void Enqueue(T item)
        {
            data.Add(item);
            int childIndex = data.Count - 1;
            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;
                if (comparison(data[childIndex], data[parentIndex]) >= 
                    0)
                {
                    break;
                }
                T tmp = data[childIndex];
                data[childIndex] = data[parentIndex];
                data[parentIndex] = tmp;
                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            int lastIndex = data.Count - 1;
            T frontItem = data[0];
            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);

            --lastIndex;
            int parentIndex = 0;
            while (true)
            {
                int childIndex = parentIndex * 2 + 1;
                if (childIndex > lastIndex)
                {
                    break;
                }
                int rightChild = childIndex + 1;
                if (rightChild <= lastIndex && comparison(data[rightChild], data[childIndex]) < 0)
                {
                    childIndex = rightChild;
                }
                if (comparison(data[parentIndex], data[childIndex]) <= 0)
                {
                    break;
                }
                T tmp = data[parentIndex];
                data[parentIndex] = data[childIndex];
                data[childIndex] = tmp;
                parentIndex = childIndex;
            }
            return frontItem;
        }

        public T Peek()
        {
            if (data.Count == 0)
            {
                return default(T);
            }
            return data[0];
        }

        public int Count
        {
            get { return data.Count; }
        }
    }
}
