---
layout: post
title: "INI File Reader in C#"
tags: ["P/Invoke", ".NET", "Sample code"]
---


Most .NET applications do not need access to the old INI file format, so Microsoft decided not to include it in the .NET framework. Multiple other options are available, from .config files to the Registry. However, there are a handful of situations where an old INI file must be read.





I wrote this class while doing some test development on an ini2reg-style program (a program that would read an existing INI file and then write appropriate Registry entries so that the information is read out of the registry instead; see [MSDN: GetPrivateProfileString](http://msdn.microsoft.com/en-us/library/ms724353(VS.85).aspx), [MSDN: INF Ini2Reg](http://msdn.microsoft.com/en-us/library/ms794363.aspx), and [MSDN: NT Resource Kit, Chapter 26](http://technet.microsoft.com/en-us/library/cc722567.aspx)).





Note that this is not a particularly well-designed class; I wrote it quickly. It's posted here as an example of moderately difficult interop; specifically, how to read a multi-string value, where "multi-string" means a single buffer that is double-null-terminated and may contain embedded (single) nulls.



{% highlight csharp %}public sealed class IniReader
{
    [DllImport("kernel32.dll", EntryPoint="GetPrivateProfileStringW", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true), SuppressUnmanagedCodeSecurity]
    private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=4)] char[] lpReturnedString, int nSize, string lpFileName);
 
    private static string GetPrivateProfileString(string fileName, string sectionName, string keyName)
    {
        char[] ret = new char[256];
 
        while (true)
        {
            int length = GetPrivateProfileString(sectionName, keyName, null, ret, ret.Length, fileName);
            if (length == 0)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
 
            // This function behaves differently if both sectionName and keyName are null
            if (sectionName != null && keyName != null)
            {
                if (length == ret.Length - 1)
                {
                    // Double the buffer size and call again
                    ret = new char[ret.Length * 2];
                }
                else
                {
                    // Return simple string
                    return new string(ret, 0, length);
                }
            }
            else
            {
                if (length == ret.Length - 2)
                {
                    // Double the buffer size and call again
                    ret = new char[ret.Length * 2];
                }
                else
                {
                    // Return multistring
                    return new string(ret, 0, length - 1);
                }
            }
        }
    }
 
    public static string[] SectionNames(string fileName)
    {
        return GetPrivateProfileString(fileName, null, null).Split('\0');
    }
 
    public static string[] KeyNames(string fileName, string sectionName)
    {
        return GetPrivateProfileString(fileName, sectionName, null).Split('\0');
    }
 
    public static string Value(string fileName, string sectionName, string keyName)
    {
        return GetPrivateProfileString(fileName, sectionName, keyName);
    }
}
{% endhighlight %}