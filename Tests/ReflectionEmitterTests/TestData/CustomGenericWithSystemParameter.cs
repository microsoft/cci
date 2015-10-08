namespace ReflectionEmitterTests.TestData
{
    public class CustomGenericWithSystemParameter
    {
        public void Run()
        {
            var a = new Foo<int>().Bar();
        }
    }

    public class Foo<T>
    {
        public T Bar()
        {
            return default(T);
        }
    }
}