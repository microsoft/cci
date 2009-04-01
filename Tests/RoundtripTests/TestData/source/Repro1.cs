using System;

public class Repro1 {

    public int codePage = 10;

    public bool IsAlwaysNormalized() {
        switch (codePage) {
            case 0x4e4:
            case 0x4e6:
            case 0x4e8:
            case 0x6fb7:
            case 0x6fbb:
            case 0x6fbd:
                return true;
        }

        return false;
    }
}
