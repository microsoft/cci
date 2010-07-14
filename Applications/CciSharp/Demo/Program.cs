using System;

namespace Demo {
  
  [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple =false, Inherited = false)]
  public sealed class NotNullAttribute : Attribute{}
  [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
  public sealed class MaybeNullAttribute : Attribute { }

  [NotNull]
  public class Program {
    public static string NeverReturnsNull() {
      // the following post condition should be automatically injected:
      // Contract.Ensures(Contract.Result<string>() != null);
      return null;
    }
    [return:MaybeNull]
    public static string TakesNonNullParameter([NotNull] string s) {
      return s != null ? null : "bad";
    }

    static void Main(string[] args) {
      var x = TakesNonNullParameter("a");
      Console.WriteLine(x);
      //Console.WriteLine(NeverReturnsNull());
    }
  }
}
