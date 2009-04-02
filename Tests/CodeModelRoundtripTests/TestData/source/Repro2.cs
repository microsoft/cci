using System;

public class Repro2 {

    static object GetEncodingRare(int codepage) {
        int num = codepage;

        if (num <= 0xcadc) {
            switch (num) {
                case 0x2ee0:
                    return null;

                case 0x2ee1:
                    return null;

                case 0x2718:
                    return null;

                case 0xcadc:
                    return null;

                case 0x96c6:
                    return null;
            }
            goto Label_01B4;
        }

    Label_01B4:
        return null;
    }

} // class
