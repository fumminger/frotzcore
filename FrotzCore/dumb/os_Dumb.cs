using Frotz.Generic;
using Frotz.Other;
using Frotz.Screen;
using static Frotz.Constants.CharCodes;
using static Frotz.Constants.General;
using static Frotz.Constants.ZColor;
using static Frotz.Constants.ZMachine;
using static Frotz.Constants.ZStyles;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Frotz;

public static partial class OS
{
    public static Blorb.Blorb? BlorbFile = null; // TODO Make this static again, or something
    private const int MaxStack = 0xff;

    // Helpers
    //

    public static int strlen(char[] c)
    {
        int length = 0;
        while (c[length] != 0) length++;
        return length;
    }

    public static int strlen(zword[] c)
    {
        int length = 0;
        while (c[length] != 0) length++;
        return length;
    }

    public static int strlen(Span<zword> c)
    {
        int length = 0;
        while (c[length] != 0) length++;
        return length;
    }

    public static ref char[] strncat(ref char[] s1, in char[] s2, int n)
    {
        int i = strlen(s1);

        while (s2[i] != '\0' && n-- != 0)
        {
            s1[i] = s2[i];
            i++;
        }

        s1[i] = '\0';

        return ref s1;
    }

    public static ref zword[] strncat(ref zword[] s1, in zword[] s2, int n)
    {
        int i = strlen(s1);

        while (s2[i] != '\0' && n-- != 0)
        {
            s1[i] = s2[i];
            i++;
        }

        s1[i] = '\0';

        return ref s1;
    }

    public static ref Span<zword> strncat(ref Span<zword> s1, in zword[] s2, int n)
    {
        int i = strlen(s1);

        while (s2[i] != '\0' && n-- != 0)
        {
            s1[i] = s2[i];
            i++;
        }

        s1[i] = '\0';

        return ref s1;
    }

    private static void strncpy(zword[] dst, in zword[] src, int n )
    {
       int i = 0;
       while(i != n && (dst[i] = src[i]) != 0) i++;
    }

    private static zword[] strdup(zword[] src)
    {
        int len = 0;

        while (src[len] != 0)
            len++;
        zword[] str = new zword[len + 1];
        int i = 0;
        while (src[i] != 0)
        {
            str[i++] = src[i];
            i++;
        }
        str[i] = '\0';
        return str;
    }

    private static string ConvertToString( ushort[] uSpan)
    {
        byte[] bytes = new byte[sizeof(ushort) * uSpan.Length];

        for (int i = 0; i < uSpan.Length; i++)
        {
            Unsafe.As<byte, ushort>(ref bytes[i * 2]) = uSpan[i];
        }

        return Encoding.Unicode.GetString(bytes);
    }

    private static ushort[] ConvertToZWords(string s)
    {
        ushort[] uSpan = new ushort[s.Length+1];

        for (int i = 0; i < s.Length; i++)
        {
            uSpan[i] = s[i];
        }
        uSpan[s.Length + 1] = 0;

        return uSpan;
    }

    public static void Fail(string message) => Fatal(message);

    private static bool IsValidChar(zword c)
    {
        if (c is >= CharCodes.ZC_ASCII_MIN and <= CharCodes.ZC_ASCII_MAX)
            return true;
        if (c is >= CharCodes.ZC_LATIN1_MIN and <= CharCodes.ZC_LATIN1_MAX)
            return true;
        return c >= 0x100;
    }

    public static (string FileName, MemoryOwner<byte> FileData)? SelectGameFile()
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


    /////////////////////////////////////////////////////////////////////////////
    // Interface to the Frotz core
    /////////////////////////////////////////////////////////////////////////////


    /*
     * os_menu
     *
     * Add to or remove a menu item. Action can be:
     *     MENU_NEW    - Add a new menu with the given title
     *     MENU_ADD    - Add a new menu item with the given text
     *     MENU_REMOVE - Remove the menu at the given index
     *
     */
    public static void Menu(int action, int menu, zword[] text) => Fail("os_menu not yet handled");

    /*
     * os_peek_color
     *
     * Return the color of the screen unit below the cursor. (If the
     * interface uses a text mode, it may return the background colour
     * of the character at the cursor position instead.) This is used
     * when text is printed on top of pictures. Note that this coulor
     * need not be in the standard set of Z-machine colours. To handle
     * this situation, Frotz entends the colour scheme: Colours above
     * 15 (and below 256) may be used by the interface to refer to non
     * standard colours. Of course, os_set_colour must be able to deal
     * with these colours.
     *
     */
    public static zword PeekColor()
    {
        return 0;
    }

    /*
     * os_picture_data
     *
     * Return true if the given picture is available. If so, store the
     * picture width and height in the appropriate variables. Picture
     * number 0 is a special case: Write the highest legal picture number
     * and the picture file release number into the height and width
     * variables respectively when this picture number is asked for.
     *
     */
    public static bool PictureData(int picture, out int height, out int width)
    {
        height = 0;
        width = 0;
        return false;
    }

    /*
     * os_draw_picture
     *
     * Display a picture at the given coordinates.
     *
     */
    public static void DrawPicture(int picture, int y, int x)
    {
    }



    /*
     * os_path_open
     *
     * Open a file in the current directory.
     * -- Szurgot: Changed this to return a Memory stream, and also has Blorb Logic.. May need to refine
     * -- Changed this again to take a byte[] to allow the data to be loaded further up the chain
     */
    public static System.IO.Stream PathOpen(MemoryOwner<byte> story_data)
    {
        // WARNING : May break with blorb files
        return story_data.AsStream();
    }

    /*
     * os_scrollback_char
     *
     * Write a character to the scrollback buffer.
     *
     */
    public static void ScrollbackChar(zword c)
    {
        // TODO Implement scrollback
    }

    /*
     * os_scrollback_erase
     *
     * Remove characters from the scrollback buffer.
     *
     */
    public static void ScrollbackErase(int erase)
    {
        // TODO Implement scrollback
    }



    /*
     * os_buffer_screen
     *
     * Set the screen buffering mode, and return the previous mode.
     * Possible values for mode are:
     *
     *     0 - update the display to reflect changes when possible
     *     1 - do not update the display
     *    -1 - redraw the screen, do not change the mode
     *
     */
    public static int BufferScreen(int mode)
    {
        Fail("os_buffer_screen is not yet implemented");
        return 0;
    }

    /*
     * os_wrap_window
     *
     * Return non-zero if the window should have text wrapped.
     *
     */
    public static int WrapWindow(int win)
    {
        return 1;
    }

    /*
     * os_window_height
     *
     * Called when the height of a window is changed.
     *
     */
    public static void SetWindowSize(int win, ZWindow wp)
    {
    }

    /*
     * set_active_window
     * Called to set the output window (I hope)
     * 
     */
    public static void SetActiveWindow(int win)
    {
    }

}
