using System.Collections.Generic;

namespace ReflectionEmitterTests.TestData
{
    public class SystemGenericWithCustomParameter
    {
        public void Run()
        {
            var list = new List<Foo>();
            list.Add(new Foo());
            list.Add(new Foo());
            list.Add(new Foo());
            var item0 = list[0];
            var length = item0.ToString().Length;
        }
    }

    public class Foo
    {
        
    }
}