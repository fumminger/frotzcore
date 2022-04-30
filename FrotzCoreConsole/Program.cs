using Frotz;
using FrotzCoreConsole;

Console.WriteLine("Begin Frotzing!");

string[] string_list = new string[] { "ZORK1.dat" };
ReadOnlySpan<string> string_span = new ReadOnlySpan<string>(string_list);

FrotzCoreConsole.ConsoleScreen consoleScreen = new ConsoleScreen();

Frotz.Generic.Main.MainFunc(string_span);



