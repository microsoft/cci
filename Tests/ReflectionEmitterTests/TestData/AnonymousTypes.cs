using System;
using System.Linq;

namespace ReflectionEmitterTests.TestData
{
    public class AnonymousTypes
    {
        public void Run()
        {
            var sum = new[]
            {
                new {Id = 10, Name ="a"},
                new {Id = 20, Name ="asdfasdf"},
                new {Id = 30, Name =Guid.NewGuid().ToString()},
            }.Sum(_ => _.Id + _.Name.Length);
        }
    }
}