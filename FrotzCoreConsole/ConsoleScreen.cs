namespace FrotzCoreConsole;

using Frotz.Blorb;
using Frotz.Screen;

using Microsoft.Toolkit.HighPerformance.Buffers;

using System.Diagnostics.CodeAnalysis;
using System.Text;

using zword = System.UInt16;

internal class ConsoleScreen : IZScreen
{
    public ConsoleScreen()
    {
        /*
        _parent = Parent;
        Margin = new Thickness(0);

        _parent = Parent;

        _cursorCanvas = new Canvas
        {
            Background = ZColorCheck.ZColorToBrush(1, ColorType.Foreground),
            Visibility = Visibility.Hidden
        };
        cnvsTop.Children.Add(_cursorCanvas);

        _sound = new FrotzSound();
        LayoutRoot.Children.Add(_sound);

        fColor = 1;
        bColor = 1;

        Background = ZColorCheck.ZColorToBrush(1, ColorType.Background);

        _substituion = new NumberSubstitution();

        SetFontInfo();

        Unloaded += (s, e) =>
        {
            _regularLines?.Dispose();
            _fixedWidthLines?.Dispose();
        };
        */
    }

    public ScreenMetrics Metrics { get; private set; }

    private int _cursorX = 0;
    private int _cursorY = 0;
    private StringBuilder _inputText = null;
    public void SetCharsAndLines()
    {/*
        double height = ActualHeight;
        double width = ActualWidth;

        var fixedFt = BuildFormattedText("A", _fixedFont, true, null, null);
        var propFt = BuildFormattedText("A", _regularFont, true, null, null);

        //double w = fixedFt.Width;
        //double h = fixedFt.Height;

        charHeight = Math.Max(fixedFt.Height, propFt.Height);
        charWidth = fixedFt.Width;

        // Account for the margin of the Rich Text Box
        // TODO Find a way to determine what this should be, or to remove the margin
        double screenWidth = width - 20;
        double screenHeight = height - 20;

        if (OS.BlorbFile != null)
        {
            var standard = OS.BlorbFile.StandardSize;
            if (standard.Height > 0 && standard.Width > 0)
            {
                int maxW = (int)Math.Floor(width / standard.Width);
                int maxH = (int)Math.Floor(height / standard.Height);

                scale = Math.Min(maxW, maxH);
                // scale = 2; // Ok, so the rest of things are at the right scale, but we've pulled back the images to 1x

                screenWidth = standard.Width * scale;
                screenHeight = standard.Height * scale;
            }
        }
        else
        {
            scale = 1;
        }

        _actualCharSize = new Size(propFt.Width, propFt.Height);

        _chars = Convert.ToInt32(Math.Floor(screenWidth / charWidth)); // Determine chars based only on fixed width chars since proportional fonts are accounted for as they are written
        _lines = Convert.ToInt32(Math.Floor(screenHeight / charHeight)); // Use the largest character height

        Metrics = new ScreenMetrics(
            new ZSize(charHeight, charWidth),// new ZSize(h, w),
            new ZSize(_lines * charHeight, _chars * charWidth), // The ZMachine wouldn't take screenHeight as round it down, so this takes care of that
            _lines, _chars, scale);

        Conversion.Metrics = Metrics;

        _regularLines = new ScreenLines(Metrics.Rows, Metrics.Columns);
        _fixedWidthLines = new ScreenLines(Metrics.Rows, Metrics.Columns);

        _cursorCanvas.MinHeight = 2;
        _cursorCanvas.MinWidth = charWidth;

        ztc.SetMetrics(Metrics);
        */

        int charHeight = 12;
        int charWidth = 8;
        int lines = 40;
        int chars = 80;
        int scale = 1;

        Metrics = new ScreenMetrics(
            new ZSize(charHeight, charWidth),// new ZSize(h, w),
            new ZSize(lines * charHeight, chars * charWidth), // The ZMachine wouldn't take screenHeight as round it down, so this takes care of that
            lines, chars, scale);
    }


    [DoesNotReturn]
    public void HandleFatalError(string message)
    {
        Console.WriteLine("Fatal Error");
        Console.WriteLine(message);
        throw new Exception(message);
    }
    public ScreenMetrics GetScreenMetrics()
    {
        return Metrics;
    }
    public void DisplayChar(char c)
    {
        Console.Write(c.ToString());
    }
    public void RefreshScreen()
    { } // TODO Need to make this a little different

    public void SetCursorPosition(int x, int y) {
        _cursorX = x;
        _cursorY = y;
    }
    public void ScrollLines(int top, int height, int lines) { }
    public event EventHandler<ZKeyPressEventArgs> KeyPressed;
    public void SetTextStyle(int new_style) { }
    public void Clear() { }
    public void ClearArea(int top, int left, int bottom, int right) { }

    public string OpenExistingFile(string defaultName, string title, string filter)
    {
        Console.WriteLine("Enter filename:");
        string filename = Console.ReadLine();

        return filename;
    }
    public string OpenNewOrExistingFile(string defaultName, string title, string filter, string defaultExtension)
    {
        Console.WriteLine("Enter filename:");
        string filename = Console.ReadLine();

        return filename;
    }

    public (string FileName, MemoryOwner<byte> FileData)? SelectGameFile()
    {
        Console.WriteLine("Enter filename:");
        string? filename = Console.ReadLine();

        MemoryOwner<byte>? buffer = null;

        if (filename is not null)
        {
            using (FileStream fs = File.Open(filename, FileMode.Open))
            {
                buffer = MemoryOwner<byte>.Allocate((int)fs.Length);
                fs.Read(buffer.Span);
            }

            if (buffer is not null)
            {
                return (filename, buffer);
            }
        }
        return null;
    }

    public ZSize GetImageInfo(byte[] image) { return new ZSize(1, 1); }

    public void ScrollArea(int top, int bottom, int left, int right, int units) { }
    public void DrawPicture(int picture, byte[] image, int y, int x) { }

    public void SetFont(int font) { }

    public void DisplayMessage(string message, string caption)
    {
        Console.WriteLine("{" + caption + "}");
        Console.WriteLine(message);
    }

    public int GetStringWidth(string s, CharDisplayInfo font) { return 8; }

    public void RemoveChars(int count) { }

    public bool GetFontData(int font, ref zword height, ref zword width) { return false; }

    public void GetColor(out int foreground, out int background) 
    { 
        foreground = 0;
        background = 0;
    }
    public void SetColor(int new_foreground, int new_background) { }

    public zword PeekColor() { return 0; }

    public void FinishWithSample(int number) { }
    public void PrepareSample(int number) { }
    public void StartSample(int number, int volume, int repeats, zword eos) { }
    public void StopSample(int number) { }

    private bool _inInputMode = false;
    public void SetInputMode(bool inputMode, bool cursorVisibility)
    {
        if (_inInputMode != inputMode)
        {
            _inInputMode = inputMode;

            if (_inInputMode == true)
            {
                _inputText = new StringBuilder();
            }
            else
            {
                _inputText = null;
            }
        }
    }

    public void SetInputColor() { }
    public void AddInputChar(char c)
    {
        _inputText?.Append(c);
    }

    public void StoryStarted(string storyName, Blorb? blorbFile)
    {
        Console.WriteLine(storyName);
    }

    public ZPoint GetCursorPosition() { return new ZPoint(0,0); }

    public void SetActiveWindow(int win) { }
    public void SetWindowSize(int win, int top, int left, int height, int width) { }

    public bool ShouldWrap() { return true; }

}
