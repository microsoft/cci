//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  /// <summary>
  /// 
  /// </summary>
  public class FrameworkOptions {
    /// <summary>
    /// 
    /// </summary>
    public bool CheckedArithmetic;
    /// <summary>
    /// 
    /// </summary>
    public int? CodePage = null;
    /// <summary>
    /// 
    /// </summary>
    public bool DisplayCommandLineHelp;
    /// <summary>
    /// 
    /// </summary>
    public bool DisplayVersion;
    /// <summary>
    /// 
    /// </summary>
    public List<string> FileNames = new List<string>();
    /// <summary>
    /// 
    /// </summary>
    public string/*?*/ OutputFileName;
    /// <summary>
    /// 
    /// </summary>
    public List<string> ReferencedAssemblies = new List<string>();
  }

  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="Options"></typeparam>
  public abstract class OptionParser<Options> where Options : FrameworkOptions, new() {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hostEnvironment"></param>
    protected OptionParser(MetadataHostEnvironment hostEnvironment) {
      this.hostEnvironment = hostEnvironment;
      Options options = new Options();
      //^ assume options != null;
      this.options = options;
    }

    /// <summary>
    /// 
    /// </summary>
    protected Dictionary<string, bool>/*?*/ alreadySeenResponseFiles;
    /// <summary>
    /// 
    /// </summary>
    protected MetadataHostEnvironment hostEnvironment;
    /// <summary>
    /// 
    /// </summary>
    protected Options/*!*/ options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="oneOrMoreSourceFilesExpected"></param>
    protected void ParseCommandLineArguments(IEnumerable<string> arguments, bool oneOrMoreSourceFilesExpected) {
      bool gaveFileNotFoundError = false;
      foreach (string arg in arguments) {
        if (arg == null || arg.Length == 0) continue;
        char ch = arg[0];
        if (ch == '@') {
          this.ParseOptionBatch(arg);
        } else if (ch == '/' || ch == '-') {
          if (!this.ParseCompilerOption(arg))
            this.ReportError(Error.InvalidCompilerOption, arg);
        } else {
          // allow URL syntax
          string s = arg.Replace('/', '\\');
          // allow wildcards
          try {
            string path = (s.IndexOf('\\') < 0) ? ".\\" : Path.GetDirectoryName(s);
            string pattern = Path.GetFileName(s);
            string extension = Path.HasExtension(pattern) ? Path.GetExtension(pattern) : "";
            bool notAFile = true;
            if (path != null && Directory.Exists(path)) {
              foreach (string file in Directory.GetFiles(path, pattern)) {
                string ext = Path.HasExtension(file) ? Path.GetExtension(file) : "";
                if (string.Compare(extension, ext, true, System.Globalization.CultureInfo.InvariantCulture) != 0) continue;
                this.options.FileNames.Add(Path.GetFullPath(file));
                notAFile = false;
              }
            }

            if (notAFile && this.DirectoryIsOk(path, pattern, extension))
              continue;
            if (notAFile && oneOrMoreSourceFilesExpected) {
              this.ReportError(Error.SourceFileNotRead, arg, this.LocalizedNoSuchFile(arg));
              gaveFileNotFoundError = true;
            }
          } catch (ArgumentException exc) {
            this.ReportError(Error.InvalidFileOrPath, s, exc.Message);
            gaveFileNotFoundError = true;
          } catch (System.IO.IOException exc) {
            this.ReportError(Error.InvalidFileOrPath, s, exc.Message);
            gaveFileNotFoundError = true;
          } catch (NotSupportedException exc) {
            this.ReportError(Error.InvalidFileOrPath, s, exc.Message);
            gaveFileNotFoundError = true;
          } catch (System.Security.SecurityException exc) {
            this.ReportError(Error.InvalidFileOrPath, s, exc.Message);
            gaveFileNotFoundError = true;
          } catch (UnauthorizedAccessException exc) {
            this.ReportError(Error.InvalidFileOrPath, s, exc.Message);
            gaveFileNotFoundError = true;
          }
        }
      }
      if (oneOrMoreSourceFilesExpected && this.options.FileNames.Count == 0 && !gaveFileNotFoundError && !this.options.DisplayCommandLineHelp && !this.options.DisplayVersion)
        this.ReportError(Error.NoSourceFiles);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="pattern"></param>
    /// <param name="extension"></param>
    /// <returns></returns>
    protected virtual bool DirectoryIsOk(string path, string pattern, string extension) {
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    protected abstract bool ParseCompilerOption(string arg);

    /// <summary>
    /// 
    /// </summary>
    protected static char[] spaceNewLineTabQuote = { ' ', (char)0x0A, (char)0x0D, '\t', '"' };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arg"></param>
    protected void ParseOptionBatch(string arg) {
      if (arg.Length < 2) {
        this.ReportError(Error.InvalidCompilerOption, arg);
        return;
      }
      try {
        string batchFileName = Path.GetFullPath(arg.Substring(1));
        if (this.alreadySeenResponseFiles == null) this.alreadySeenResponseFiles = new Dictionary<string, bool>();
        if (this.alreadySeenResponseFiles.ContainsKey(batchFileName)) {
          this.ReportError(Error.DuplicateResponseFile, batchFileName);
          return;
        }
        this.alreadySeenResponseFiles.Add(batchFileName, true);
        string optionBatch = this.ReadSourceText(batchFileName);
        List<string> opts = new List<string>();
        bool insideQuotedString = false;
        bool insideComment = false;
        for (int i = 0, j = 0, n = optionBatch.Length; j < n; j++) {
          switch (optionBatch[j]) {
            case (char)0x0A:
            case (char)0x0D:
              insideQuotedString = false;
              if (insideComment) {
                insideComment = false;
                i = j+1;
                break;
              }
              goto case ' ';
            case ' ':
            case '\t':
              if (insideQuotedString || insideComment) break;
              if (i < j)
                opts.Add(optionBatch.Substring(i, j-i));
              i = j+1;
              break;
            case '"':
              if (insideQuotedString) {
                if (!insideComment)
                  opts.Add(optionBatch.Substring(i, j-i));
                insideQuotedString = false;
              } else
                insideQuotedString = true;
              i = j+1;
              break;
            case '#':
              insideComment = true;
              break;
            default:
              if (j == n-1 && i < j)
                opts.Add(optionBatch.Substring(i, n-i));
              break;
          }
        }
        this.ParseCommandLineArguments(opts, false);
        return;
      } catch (ArgumentException) {
      } catch (System.IO.IOException) {
      } catch (NotSupportedException) {
      } catch (System.Security.SecurityException) {
      } catch (UnauthorizedAccessException) {
      }
      this.ReportError(Error.BatchFileNotRead, arg.Substring(1), this.LocalizedNoSuchFile(arg.Substring(1)));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arg"></param>
    /// <param name="name"></param>
    /// <param name="shortName"></param>
    /// <returns></returns>
    protected bool ParseName(string arg, string name, string shortName) {
      arg = arg.ToLower();
      int n = arg.Length;
      int j = name.Length;
      int k = shortName.Length;
      int i = 0;
      if (n > j) i = arg.IndexOf(name, 1, j, StringComparison.Ordinal);
      if (i < 1 && j > k && n > k) {
        i = arg.IndexOf(shortName, 1, k, StringComparison.Ordinal);
        j = k;
      }
      if (i < 1) return false;
      if (++j >= n) return true;
      char ch = arg[j];
      if (ch == ' ' || ch == '/' || ch == (char)9 || ch == (char)10 || ch == (char)13) return true;
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arg"></param>
    /// <param name="name"></param>
    /// <param name="shortName"></param>
    /// <returns></returns>
    protected bool? ParseNamedBoolean(string arg, string name, string shortName) {
      arg = arg.ToLower();
      int n = arg.Length;
      int j = name.Length;
      int k = shortName.Length;
      int i = 0;
      if (n > j) i = arg.IndexOf(name, 1, j, StringComparison.Ordinal);
      if (i < 1 && j > k && n > k) {
        i = arg.IndexOf(shortName, 1, k, StringComparison.Ordinal);
        j = k;
      }
      if (i < 1) return null;
      if (++j >= n) return true;
      char ch = arg[j];
      if (ch == '+') return true;
      if (ch == '-') return false;
      if (ch == ' ' || ch == '/' || ch == (char)9 || ch == (char)10 || ch == (char)13) return true;
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arg"></param>
    /// <param name="name"></param>
    /// <param name="shortName"></param>
    /// <returns></returns>
    protected string/*?*/ ParseNamedArgument(string arg, string name, string shortName) {
      string arg1 = arg.ToLower();
      int n = arg.Length;
      int j = name.Length;
      int k = shortName.Length;
      int i = 0;
      if (n > j) i = arg1.IndexOf(name, 1, j, StringComparison.Ordinal);
      if (i < 1 && j > k && n > k) {
        i = arg1.IndexOf(shortName, 1, k, StringComparison.Ordinal);
        j = k;
      }
      if (i < 1) return null;
      if (++j >= n) return null;
      if (arg[j] != ':') return null;
      return arg.Substring(j+1);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arg"></param>
    /// <param name="name"></param>
    /// <param name="shortName"></param>
    /// <returns></returns>
    protected List<string>/*?*/ ParseNamedArgumentList(string arg, string name, string shortName) {
      string/*?*/ argList = this.ParseNamedArgument(arg, name, shortName);
      if (argList == null || argList.Length == 0) return null;
      List<string> result = new List<string>();
      int i = 0;
      for (int n = argList.Length; i < n; ) {
        int separatorIndex = this.GetArgumentSeparatorIndex(argList, i);
        if (separatorIndex > i) {
          result.Add(argList.Substring(i, separatorIndex-i));
          i = separatorIndex+1;
          continue;
        }
        result.Add(argList.Substring(i));
        break;
      }
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="argList"></param>
    /// <param name="startIndex"></param>
    /// <returns></returns>
    protected int GetArgumentSeparatorIndex(string argList, int startIndex) {
      int commaIndex = argList.IndexOf(",", startIndex);
      int semicolonIndex = argList.IndexOf(";", startIndex);
      if (commaIndex == -1) return semicolonIndex;
      if (semicolonIndex == -1) return commaIndex;
      if (commaIndex < semicolonIndex) return commaIndex;
      return semicolonIndex;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    protected string ReadSourceText(string fileName) {
      try {
        using (FileStream inputStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          // get the file size
          long size = inputStream.Seek(0, SeekOrigin.End);
          if (size > int.MaxValue) {
            this.ReportError(Error.SourceFileTooLarge, fileName);
            return "";
          }
          inputStream.Seek(0, SeekOrigin.Begin);
          int b1 = inputStream.ReadByte();
          int b2 = inputStream.ReadByte();
          if (b1 == 'M' && b2 == 'Z') {
            this.ReportError(Error.IsBinaryFile, Path.GetFullPath(fileName));
            return "";
          }

          inputStream.Seek(0, SeekOrigin.Begin);
          Encoding encoding = Encoding.Default;
          if (this.options.CodePage != null) {
            try {
              encoding = Encoding.GetEncoding((int)this.options.CodePage);
            } catch (ArgumentOutOfRangeException) {
              this.ReportError(Error.InvalidCodePage, this.options.CodePage.ToString());
              return "";
            } catch (ArgumentException) {
              this.ReportError(Error.InvalidCodePage, this.options.CodePage.ToString());
              return "";
            } catch (NotSupportedException) {
              this.ReportError(Error.InvalidCodePage, this.options.CodePage.ToString());
              return "";
            }
          }
          StreamReader reader = new StreamReader(inputStream, encoding, true); //last param allows markers to override encoding

          //Read the contents of the file into an array of char and return as a string
          char[] sourceText = new char[(int)size];
          int length = reader.Read(sourceText, 0, (int)size);
          return new String(sourceText, 0, length);
        }
      } catch (Exception e) {
        string/*?*/ message = e.Message;
        if (message != null)
          this.ReportError(Error.SourceFileNotRead, fileName, e.Message);
        return "";
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="error"></param>
    /// <param name="messageArguments"></param>
    protected void ReportError(Error error, params string[] messageArguments) {
      DummyExpression dummyExpression = new DummyExpression(SourceDummy.SourceLocation);
      this.hostEnvironment.ReportError(new AstErrorMessage(dummyExpression, error, messageArguments));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    protected string LocalizedNoSuchFile(string fileName) {
      DummyExpression dummyExpression = new DummyExpression(SourceDummy.SourceLocation);
      return new AstErrorMessage(dummyExpression, Error.NoSuchFile, fileName).Message;
    }

  }
}