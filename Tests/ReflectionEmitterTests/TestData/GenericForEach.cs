using System.Collections.Generic;

namespace ReflectionEmitterTests.TestData
{
    public class GenericForEach
    {
        public void Run()
        {
            var result = 0;
            foreach (var item in new List<int> { 1,2,3})
            {
                result += item;
            }
        }
    }
}