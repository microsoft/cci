using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace CciSharp.Test
{
    public class EnsuresNotNullTest
    {
        public static object ReturnsNull()
        {
            return null;
        }

        [Fact]
        public static void ReturnsNullThrows()
        {
            try
            {
                ReturnsNull();
                Assert.True(false);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
