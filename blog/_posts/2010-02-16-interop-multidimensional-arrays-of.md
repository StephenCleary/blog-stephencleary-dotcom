---
layout: post
title: "Interop: Multidimensional Arrays of Characters in a Structure"
---
Yesterday an interesting problem was brought up on the MSDN forums. An unmanaged structure had a form like this:

    struct MyStruct
    {
      int id;
      char names[6][25];
    };

Each structure has 6 strings of up to 25 characters each. Marshaling a single "flattened" string in a structure is not difficult (UnmanagedType.ByValTStr with SizeConst), and marshaling a "flattened" array of simple types in a structure is likewise not difficult (UnmanagedType.ByValArray with SizeConst and optionally ArraySubType). However, marshaling a flattened array of flattened strings is not exactly straightforward (there is no "ArraySubTypeSizeConst" option).

The answer is to split off the "25 character string" type into its own structure (containing a single flattened string), and define a flattened array of those structures in the parent structure, as such:

{% highlight csharp %}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct MyStruct
{
    public void Init()
    {
        this.names = new StringSizeConst25AsString[6];
    }

    public int id;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    public StringSizeConst25AsString[] names;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct StringSizeConst25AsString
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 25)]
        private string Value;

        public static implicit operator string(StringSizeConst25AsString source)
        {
            return source.Value;
        }

        public static implicit operator StringSizeConst25AsString(string source)
        {
            // Note that longer strings would be silently truncated
            //  if we didn't explicitly check this.
            if (source.Length >= 25)
                throw new Exception("String too large for field: " + source);

            return new StringSizeConst25AsString { Value = source };
        }
    }
}
{% endhighlight %}

(The implicit conversions on the inner structure are for convenience; note that the default marshaling will silently truncate string values that are more than 24 characters).

The inner structure "StringSizeConst25AsString" marshals its string as a 25-character array, and the outer structure "MyStruct" marshals an array of the inner structures. Both of them end up getting flattened correctly into a single multidimensional unmanaged character array.

If we have an unmanaged function as such:

    // ByValArrayOfStrings.h:
    extern "C" __declspec(dllexport) void AddMultipleNames(const MyStruct* DSNames);
    
    // ByValArrayOfStrings.cpp:
    #include <string>
    #include <sstream>
    
    __declspec(dllexport) void AddMultipleNames(const MyStruct* DSNames)
    {
     {
      std::ostringstream out;
      out << "DSNames->id: " << DSNames->id;
      OutputDebugStringA(out.str().c_str());
     }
    
     for (int i = 0; i != 6; ++i)
     {
      std::ostringstream out;
      out << "DSNames->names[" << i << "]: " << DSNames->names[i];
      OutputDebugStringA(out.str().c_str());
     }
    }

Then we can use the managed interop definitions above like this:

{% highlight csharp %}

[DllImport("ByValArrayOfStrings.dll", CharSet = CharSet.Ansi)]
static extern void AddMultipleNames(ref MyStruct DSNames);

private void button1_Click(object sender, EventArgs e)
{
    try
    {
        MyStruct tmp = new MyStruct();
        tmp.Init();
        tmp.id = 17;
        tmp.names[0] = "Hi";
        tmp.names[2] = "There";
        tmp.names[3] = "123456789012345678901234";

        // The following assignment would throw
        //tmp.names[4] = "1234567890123456789012345";
        tmp.names[5] = "x";

        AddMultipleNames(ref tmp);
    }
    catch (Exception ex)
    {
        MessageBox.Show("[" + ex.GetType().Name + "] " + ex.Message);
    }
}
{% endhighlight %}

And this would cause the unmanaged DLL to send to its debug output:

    DSNames->id: 17
    DSNames->names[0]: Hi
    DSNames->names[1]: 
    DSNames->names[2]: There
    DSNames->names[3]: 123456789012345678901234
    DSNames->names[4]: 
    DSNames->names[5]: x

## Non-Null-Terminated Strings

The above solution works well if each of the strings in the unmanaged structure are null-terminated. There are some APIs, however, which work with implicitly-terminated strings. It is possible that an unmanaged function may treat these strings as having an implicit length of 25 characters.

In this case, string marshaling cannot be used in the managed code. The above solution can be modified to marshal an array of characters instead:

{% highlight csharp %}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct MyStruct
{
    public void Init()
    {
        this.names = new StringSizeConst25AsCharArray[6];
    }

    public int id;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    public StringSizeConst25AsCharArray[] names;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct StringSizeConst25AsCharArray
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
        private char[] Value;

        public static implicit operator string(StringSizeConst25AsCharArray source)
        {
            return new string(source.Value);
        }

        public static implicit operator StringSizeConst25AsCharArray(string source)
        {
            if (source.Length > 25)
                throw new Exception("String too large for field: " + source);

            var ret = new StringSizeConst25AsCharArray() { Value = new char[25] };
            Array.Copy(source.ToCharArray(), ret.Value, source.Length);
            return ret;
        }
    }
}
{% endhighlight %}

This solution allows sending a 25-character, non-null-terminated string as a member of the unmanaged string array:

{% highlight csharp %}

private void button1_Click(object sender, EventArgs e)
{
    try
    {
        MyStruct tmp = new MyStruct();
        tmp.Init();
        tmp.id = 17;
        tmp.names[0] = "Hi";
        tmp.names[2] = "There";
        tmp.names[3] = "123456789012345678901234";

        // The following assignment would throw
        //tmp.names[4] = "12345678901234567890123456";
        tmp.names[4] = "1234567890123456789012345";
        tmp.names[5] = "x";

        AddMultipleNames(ref tmp);
    }
    catch (Exception ex)
    {
        MessageBox.Show("[" + ex.GetType().Name + "] " + ex.Message);
    }
}
{% endhighlight %}

Which produces this debug output:

    DSNames->id: 17
    DSNames->names[0]: Hi
    DSNames->names[1]: 
    DSNames->names[2]: There
    DSNames->names[3]: 123456789012345678901234
    DSNames->names[4]: 1234567890123456789012345x
    DSNames->names[5]: x

Note that our unmanaged function is still interpreting the strings as null-terminated, and we're marshaling them as implicitly-terminated. This is why entry [4] above "spills over" and picks up entry [5]. If the unmanaged function actually interpreted the strings as having a length of 25 (or a maximum length of 25), then this "spill over" would not happen.

