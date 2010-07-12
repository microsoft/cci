using System;

namespace Demo {
  public class Program {
    public static string NeverReturnsNull() {
      // the following post condition should be automatically injected:
      // Contract.Ensures(Contract.Result<string>() != null);
      return null;
    }

    static void Main(string[] args) {
      Console.WriteLine(NeverReturnsNull());
    }
  }
}
