using System;
using System.IO;
using System.Reflection;

public class Logger {

  static FileStream traceFile;
  static byte[] buffer;

  static Logger(){
    var myAssembly = Assembly.GetExecutingAssembly();
    var traceFileName = myAssembly.Location;
    traceFile = File.Create(traceFileName.Replace(".instrumented.exe", ".profile"));
    buffer = new byte[4];
  }

  public static void LogEdgeCount(uint count) {
    buffer[0] = (byte)(count & 0xFF);
    count >>= 8;
    buffer[1] = (byte)(count & 0xFF);
    count >>= 8;
    buffer[2] = (byte)(count & 0xFF);
    count >>= 8;
    buffer[3] = (byte)(count & 0xFF);

    traceFile.Write(buffer, 0, 4);
  }

}

