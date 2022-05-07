
using System;

class FrotCoreConsole
{
    static void Main(string[] args)
    {
        Console.WriteLine("Begin Frotzing!");

        string[] string_list = new string[] { "ZORK1.dat" };
        ReadOnlySpan<string> string_span = new ReadOnlySpan<string>(string_list);

    }
}