using Game;
namespace LibPixz2
{
	public class ArraySlice<T>
	{
		public readonly T[,] arr;

		public int firstDimension;

		public T this[int index]
		{
			get
			{
				return arr[firstDimension, index];
			}
			set
			{
				arr[firstDimension, index] = value;
			}
		}

		public ArraySlice(T[,] arr)
		{
			this.arr = arr;
		}

		public ArraySlice<T> GetSlice(int firstDimension)
		{
			this.firstDimension = firstDimension;
			return this;
		}
	}
}
