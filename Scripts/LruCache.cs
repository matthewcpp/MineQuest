using System.Collections.Generic;
using System.Net.Sockets;

namespace MineQuest
{
    public class LruCache<K, V>
    {
        public int Capacity { get; }
        public int Count
        {
            get { return nodeMap.Count; }
        }

        public bool IsEmpty { get { return Count == 0; } }

        public delegate void LruEvictionFunc(K key, V value);

        public event LruEvictionFunc OnEvict;

        private readonly Dictionary<K,LinkedListNode<KeyValuePair<K,V>>> nodeMap = new Dictionary<K, LinkedListNode<KeyValuePair<K,V>>>();
        private readonly LinkedList<KeyValuePair<K, V>> usageList = new LinkedList<KeyValuePair<K,V>>();

        public LruCache(int capacity)
        {
            Capacity = capacity;
        }

        public void Add(K key, V value)
        {
            LinkedListNode<KeyValuePair<K,V>> node = null;
            if (nodeMap.TryGetValue(key, out node))
            {
                node.Value = new KeyValuePair<K, V>(key, value);
            }
            else
            {
                if (Count == Capacity)
                {
                    node = usageList.Last;
                    OnEvict?.Invoke(node.Value.Key, node.Value.Value);
                    nodeMap.Remove(node.Value.Key);
                    node.Value = new KeyValuePair<K, V>(key, value);
                }
                else
                {
                    node = new LinkedListNode<KeyValuePair<K,V>>(new KeyValuePair<K, V>(key, value));
                    usageList.AddLast(node);
                }

                nodeMap[key] = node;
            }
        }

        public bool TryGetValue(K key, out V value)
        {
            LinkedListNode<KeyValuePair<K,V>> node = null;
            
            if (nodeMap.TryGetValue(key, out node))
            {
                usageList.Remove(node);
                usageList.AddFirst(node);
                value = node.Value.Value;
                
                return true;
            }
            else
            {
                value = default(V);
                return false;
            }
        }

        void Clear()
        {
            usageList.Clear();
            nodeMap.Clear();
        }
    }
}