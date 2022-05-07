using Frotz.Constants;
using Frotz.Other;

using System;
using System.IO;
using System.Text;

using zword = System.UInt16;

namespace Frotz
{

    public static partial class OS
    {
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

        private static void strncpy(zword[] dst, in zword[] src, int n)
        {
            int i = 0;
            while (i != n && (dst[i] = src[i]) != 0) i++;
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
                str[i] = src[i];
                i++;
            }
            str[i] = '\0';
            return str;
        }

        private static string ConvertToString(ushort[] uSpan)
        {
            byte[] bytes = new byte[sizeof(ushort) * uSpan.Length];

            for (int i = 0; i < uSpan.Length; i++)
            {
                bytes[2 * i] = (byte)(uSpan[i] & 0xff);
                bytes[2 * i + 1] = (byte)((uSpan[i] & 0xff00) >> 8);
            }

            return Encoding.Unicode.GetString(bytes);
        }

        private static ushort[] ConvertToZWords(string s)
        {
            ushort[] uSpan = new ushort[s.Length + 1];

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

        public static (string FileName, byte[])? SelectGameFile()
        {
            Console.Write("Enter filename:");
            string? filename = Console.ReadLine();

            byte[]? buffer = null;

            if (filename is not null)
            {
                using (FileStream fs = File.Open(filename, FileMode.Open))
                {
                    buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, (int)fs.Length);
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
         * os_path_open
         *
         * Open a file in the current directory.
         * -- Szurgot: Changed this to return a Memory stream, and also has Blorb Logic.. May need to refine
         * -- Changed this again to take a byte[] to allow the data to be loaded further up the chain
         */
        public static System.IO.Stream PathOpen(byte[] story_data)
        {
            // WARNING : May break with blorb files
            return new MemoryStream(story_data);
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
}