namespace Game
{
    public class Order
    {
        public Block block;

        public int order;

        public int value;

        public Order(Block b, int o, int v)
        {
            block = b;
            order = o;
            value = v;
        }
    }
}