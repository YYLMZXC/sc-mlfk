namespace Game
{
    public class TStack
    {
        public int top = -1;

        public int maxSize = 64;

        public object[] array = new object[64];

        public void Push(object val)
        {
            if (top != maxSize - 1)
            {
                array[++top] = val;
            }
        }

        public object Pop()
        {
            if (top == -1)
            {
                return null;
            }

            return array[top--];
        }

        public object GetTop()
        {
            if (top == -1)
            {
                return null;
            }

            return array[top];
        }

        public bool IsEmpty()
        {
            if (top == -1)
            {
                return true;
            }

            return false;
        }
    }
}