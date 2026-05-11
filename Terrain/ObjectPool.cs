using System.Collections.Generic;
using UnityEngine;

namespace BinGa.Pool
{
	public class ObjectPool<T>
	{
		int _maxPoolSize;
		Stack<T> _poolStack;

		public int Count => _poolStack.Count;

		public ObjectPool(int maxPoolSize)
		{
			_maxPoolSize = maxPoolSize;
			_poolStack = new Stack<T>(maxPoolSize);
		}

		public bool TryAdd(T obj)
		{
			if (_poolStack.Count >= _maxPoolSize)
				return false;

			_poolStack.Push(obj);
			return true;
		}

		public bool TryGet(out T obj)
		{
			//return _poolStack.Count > 0 ? _poolStack.Pop() : System.Activator.CreateInstance<T>();
			if (_poolStack.Count > 0)
            {
				obj = _poolStack.Pop();
				return true;
            }

			obj = default(T);
			return false;
		}
	}
}