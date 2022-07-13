#nullable enable

using System.Net.Http;

namespace RetroCoreFit
{

    public delegate T BuilderDelegate<T>(T input);

    public class Builder<T>
        where T: class
    {
        private BuilderDelegate<T> Handler;

        protected Builder()
        {
        }

        internal static BT Factory<BT>(BuilderDelegate<T> d)
            where BT : Builder<T>, new()
        {
            return new BT() {
                Handler = d
            };
        }

        public T Build()
        {
            return Handler(default!);
        }
    }

    public static class BuilderExtensions
    {
        public static BT Append<T,BT>(this BT @this, BuilderDelegate<T> handler)
            where BT: Builder<T>, new ()
            where T: class
        {
            return Builder<T>.Factory<BT>((input) => handler(@this.Build()));
        }        

    }
}
