using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DOL.Util
{
    public class ThreadSafeList<T> : IList<T>
    {
        protected List<T> _interalList = new List<T>();

        public IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        protected static object _lock = new object();

        public int Count { get { return Clone().Count; } }

        public bool IsReadOnly
        { get { return false; } }

        public T this[int index]
        {
            get { IList<T> t = Clone(); return t[index]; }
            set { lock (_lock) { _interalList[index] = value; } }
        }

        public List<T> Clone()
        {
            List<T> newList = new List<T>();

            lock (_lock)
            {
                _interalList.ForEach(x => newList.Add(x));
            }

            return newList;
        }

        public int IndexOf(T item) { return Clone().IndexOf(item); }

        public void Insert(int index, T item)
        { lock (_lock) { _interalList.Insert(index, item); } }

        public void RemoveAt(int index) { lock (_lock) { _interalList.RemoveAt(index); } }

        public void Add(T item) { lock (_lock) { _interalList.Add(item); } }

        public void Clear() { lock (_lock) { _interalList.Clear(); } }

        public bool Contains(T item) { return Clone().Contains(item); }

        public void CopyTo(T[] array, int arrayIndex) { Clone().CopyTo(array, arrayIndex); }

        public bool Remove(T item) { lock (_lock) { return _interalList.Remove(item); } }
    }
}