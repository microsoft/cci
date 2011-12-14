namespace TestILGarbageCollector.SourceInputs {
    class DeadMethod
    {
        public void DeadMethodFoo() { }
        public void LiveMethodFoo() { }

        static void Main(string[] argv)
        {
            var x = new DeadMethod();
            x.LiveMethodFoo();

            A();
        }

        // A calls B calls C calls D, which calls C
        // Unreachable1 calls Unreachable2 and A

        static void B()
        {
            C();
        }

        static void A()
        {
            B();
        }


        static void C()
        {
            D();
        }

        static void D()
        {
            C();
        }

        static void Unreachable2() {

        }

        static void Unreachable1() {
            Unreachable2();
            A();
        }
    }
}
