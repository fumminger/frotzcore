using Frotz.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Frotz;

public static class DebugState
{
    internal static bool IsActive { get; private set; }
    public static void StartState(string stateFileToLoad)
    {
        if (stateFileToLoad != null)
        {
            using var good = new StreamReader(stateFileToLoad);
            string? line;
            while ((line = good.ReadLine()) != null)
            {
                if (!line.StartsWith('#'))
                {
                    StateLines.Add(line);
                }
            }
        }
        IsActive = true;
    }

    public static List<string> StateLines { get; } = new();
    public static List<string> OutputLines { get; } = new();

    private static int CurrentState = 0;

    internal static string LastCallMade = "";

    public static void SaveZMachine(string fileToSaveTo)
    {
        if (IsActive)
        {
            using var fs = new FileStream(fileToSaveTo, FileMode.Create);
            fs.Write(FastMem.ZMData);
        }
    }

    private static int Seed = 0;
    internal static int RandomSeed() => Seed++;
}
