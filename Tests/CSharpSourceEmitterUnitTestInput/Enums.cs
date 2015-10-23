class Enums {
  private Enums() {
  }

  private const Enums.SimpleEnum val1 = Enums.SimpleEnum.Two;

  private const Enums.SimpleEnum val2 = unchecked((Enums.SimpleEnum)0x4);

  private const Enums.FlagsEnum val3 = Enums.FlagsEnum.Bit1 | Enums.FlagsEnum.Bit2;

  private const Enums.FlagsEnum val4 = Enums.FlagsEnum.Bit3 | unchecked((Enums.FlagsEnum)0x10);

  private const Enums.FlagsEnum val5 = ~(Enums.FlagsEnum.Bit2 | Enums.FlagsEnum.Bit4);

  private const Enums.LongEnum val6 = Enums.LongEnum.Big;

  private const Enums.LongEnum val7 = Enums.LongEnum.Negative;

  public enum SimpleEnum {
    One = 1,
    Two = 2,
    Three = 3,
  }

  [System.FlagsAttribute]
  public enum FlagsEnum : ushort {
    Bit1 = 0x1,
    Bit2 = 0x2,
    Bit3 = 0x4,
    Bit4 = 0x8,
  }

  public enum LongEnum : long {
    Negative = -2,
    Zero = 0,
    Big = long.MaxValue,
  }

  [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
  private class EnumAttr : System.Attribute {
    public EnumAttr(Enums.FlagsEnum val) {
    }
  }

  [Enums.EnumAttr(Enums.FlagsEnum.Bit3)]
  private class TestAttr {
    private TestAttr() {
    }
  }
}
