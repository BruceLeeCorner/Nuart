namespace Nuart.Modbus.TestApp
{
    public class RegisterValue<T> : BindableBase where T : struct, IConvertible
    {
        private int addr;
        private T value;

        public int Addr
        {
            get { return addr; }
            set { SetProperty(ref addr, value); }
        }

        public T Value
        {
            get { return value; }
            set { SetProperty(ref value, value); }
        }
    }
}