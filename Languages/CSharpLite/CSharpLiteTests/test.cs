class A {
  public static int Main() {
    int result = 0;

    int[][] arr1 = new int[3][] { new int[] { 1, 2 }, new int[] { 3, 4 }, new int[] { 8, 9 } };
    if (arr1.Rank != 1)
      result = 1;

    int[][][][] arr2 = new int[3][][][] { new int[][][] {  new int[][] {  new int[] {5,6}  }, new int[][] {  new int[] {4,4}  }  },
                        new int[][][] {  new int[][] {  new int[] {2,3}  }, new int[][] {  new int[] {8,7}  }  },
                        new int[][][] {  new int[][] {  new int[] {4,4}  }, new int[][] {  new int[] {2,1}  }  }  };
    if (arr2.Rank != 1)
      result = 2;

    int[][] arr3 = new int[3][] { new int[] { 4, 3 }, new int[] { 40, 34 }, new int[] { 93, 29 } };
    if (arr3.Rank != 1)
      result = 3;

    return result;
  }
}