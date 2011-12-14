namespace TestILGarbageCollector.SourceInputs {
  class DeadField {
    public object liveField;
    public object deadField;

    static void Main(string[] argv) {
      var x = new DeadField().liveField;
    }
  }
}
