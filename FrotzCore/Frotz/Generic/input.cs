/* input.c - High level input functions
 *	Copyright (c) 1995-1997 Stefan Jokisch
 *
 * This file is part of Frotz.
 *
 * Frotz is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * Frotz is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA
 */
using Frotz.Constants;

using System;

using zbyte = System.Byte;
using zword = System.UInt16;

namespace Frotz.Generic
{

    internal static class Input
    {

        //zword unicode_tolower (zword);

        /*
         * is_terminator
         *
         * Check if the given key is an input terminator.
         *
         */
        internal static bool IsTerminator(zword key)
        {
            if (key == CharCodes.ZC_TIME_OUT)
                return true;
            if (key == CharCodes.ZC_RETURN)
                return true;
            if (key is >= CharCodes.ZC_HKEY_MIN and <= CharCodes.ZC_HKEY_MAX)
                return true;

            return false;

        }/* is_terminator */

        /*
         * z_make_menu, add or remove a menu and branch if successful.
         *
         *	zargs[0] = number of menu
         *	zargs[1] = table of menu entries or 0 to remove menu
         *
         */
        private static readonly zword[] menu = new zword[32];

        internal static void ZMakeMenu()
        {
    

        }/* z_make_menu */

        /*
         * read_yes_or_no
         *
         * Ask the user a question; return true if the answer is yes.
         *
         */
        internal static bool ReadYesOrNo(string s)
        {
            zword key;

            Text.PrintString(s);
            Text.PrintString("? (y/n) >");

            key = Stream.StreamReadKey(0, 0, false);

            if (key is 'y' or 'Y')
            {
                Text.PrintString("y\n");
                return true;
            }
            else
            {
                Text.PrintString("n\n");
                return false;
            }

        }/* read_yes_or_no */

        /*
         * read_string
         *
         * Read a string from the current input stream.
         *
         */

        internal static void ReadString(int max, Span<zword> buffer)
        {
            zword key;
            buffer[0] = 0;

            do
            {
                key = Stream.StreamReadInput(max, buffer, 0, 0, false, false);
            } while (key != CharCodes.ZC_RETURN);

        }/* read_string */

        /*
         * read_number
         *
         * Ask the user to type in a number and return it.
         *
         */

        internal static int ReadNumber()
        {
            Span<zword> buffer = stackalloc zword[6];
            int value = 0;
            int i;


            return value;

        }/* read_number */

        /*
         * z_read, read a line of input and (in V5+) store the terminating key.
         *
         *	zargs[0] = address of text buffer
         *	zargs[1] = address of token buffer
         *	zargs[2] = timeout in tenths of a second (optional)
         *	zargs[3] = packed address of routine to be called on timeout
         *
         */
        internal static void ZRead()
        {
            var pooled = new zword[General.INPUT_BUFFER_SIZE];
            var buffer = pooled;
            zword addr;
            zword key;
            zbyte size;
            int i;

            /* Supply default arguments */

            if (Process.zargc < 3)
                Process.zargs[2] = 0;

            /* Get maximum input size */

            addr = Process.zargs[0];



        }/* z_read */

        /*
         * z_read_char, read and store a key.
         *
         *	zargs[0] = input device (must be 1)
         *	zargs[1] = timeout in tenths of a second (optional)
         *	zargs[2] = packed address of routine to be called on timeout
         *
         */

        internal static void ZReadChar()
        {
            zword key;

            /* Supply default arguments */

            if (Process.zargc < 2)
                Process.zargs[1] = 0;

            /* Read input from the current input stream */

            key = Stream.StreamReadKey(
                Process.zargs[1],   /* timeout value   */
                Process.zargs[2],   /* timeout routine */
                true);      /* enable hot keys */

            if (key == CharCodes.ZC_BAD)
                return;

            /* Store key */

            Process.Store(Text.TranslateToZscii(key));

        }/* z_read_char */

        /*
         * z_read_mouse, write the current mouse status into a table.
         *
         *	zargs[0] = address of table
         *
         */

        internal static void ZReadMouse()
        {
            /* Read the mouse position, the last menu click
               and which buttons are down */

            zword btn = OS.ReadMouse();


        }/* z_read_mouse */
    }
}