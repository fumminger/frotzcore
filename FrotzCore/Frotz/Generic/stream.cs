/* stream.c - IO stream implementation
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

using zword = System.UInt16;

namespace Frotz.Generic
{

    internal static class Stream
    {
        /*
         * scrollback_char
         *
         * Write a single character to the scrollback buffer.
         *
         */

        internal static void ScrollBackChar(zword c)
        {

            if (c == CharCodes.ZC_INDENT) { ScrollBackChar(' '); ScrollBackChar(' '); ScrollBackChar(' '); return; }
            if (c == CharCodes.ZC_GAP) { ScrollBackChar(' '); ScrollBackChar(' '); return; }

            OS.ScrollbackChar(c);
        }/* scrollback_char */

        /*
         * scrollback_word
         *
         * Write a string to the scrollback buffer.
         *
         */

        internal static void ScrollBackWord(ReadOnlySpan<zword> s)
        {
            for (int i = 0; i < s.Length && s[i] != 0; i++)
            {
                if (s[i] is CharCodes.ZC_NEW_FONT or CharCodes.ZC_NEW_STYLE)
                    i++;
                else
                    ScrollBackChar(s[i]);
            }
        }/* scrollback_word */

        /*
         * scrollback_write_input
         *
         * Send an input line to the scrollback buffer.
         *
         */

        internal static void ScrollBackWriteInput(ReadOnlySpan<zword> buf, zword key)
        {
            int i;

            for (i = 0; i < buf.Length && buf[i] != 0; i++)
                ScrollBackChar(buf[i]);

            if (key == CharCodes.ZC_RETURN)
                ScrollBackChar('\n');
        }/* scrollback_write_input */

        /*
         * scrollback_erase_input
         *
         * Remove an input line from the scrollback buffer.
         *
         */

        internal static void ScrollbackEraseInput(ReadOnlySpan<zword> buf)
        {
            int width;
            int i;

            for (i = 0, width = 0; i < buf.Length && buf[i] != 0; i++)
                width++;

            OS.ScrollbackErase(width);
        }/* scrollback_erase_input */

        /*
         * stream_mssg_on
         *
         * Start printing a "debugging" message.
         *
         */

        internal static void StreamMssgOn()
        {


        }/* stream_mssg_on */

        /*
         * stream_mssg_off
         *
         * Stop printing a "debugging" message.
         *
         */

        internal static void StreamMssgOff()
        {


        }/* stream_mssg_off */

        /*
         * z_output_stream, open or close an output stream.
         *
         *	zargs[0] = stream to open (positive) or close (negative)
         *	zargs[1] = address to redirect output to (stream 3 only)
         *	zargs[2] = width of redirected output (stream 3 only, optional)
         *
         */

        internal static void ZOutputStream()
        {
            Buffer.FlushBuffer();



        }/* z_output_stream */

        /*
         * stream_char
         *
         * Send a single character to the output stream.
         *
         */

        internal static void StreamChar(zword c)
        {

        }/* stream_char */

        /*
         * stream_word
         *
         * Send a string of characters to the output streams.
         *
         */

        internal static void StreamWord(ReadOnlySpan<zword> s)
        {
        }/* stream_word */

        /*
         * stream_new_line
         *
         * Send a newline to the output streams.
         *
         */

        internal static void NewLine()
        {
        }/* stream_new_line */

        /*
         * z_input_stream, select an input stream.
         *
         *	zargs[0] = input stream to be selected
         *
         */

        internal static void ZIputStream()
        {

            Buffer.FlushBuffer();


        }/* z_input_stream */

        /*
         * stream_read_key
         *
         * Read a single keystroke from the current input stream.
         *
         */

        internal static zword StreamReadKey(zword timeout, zword routine, bool hot_keys)
        {
            zword key = CharCodes.ZC_BAD;
            Buffer.FlushBuffer();

        /* Read key from current input stream */

        continue_input:


            /* Verify mouse clicks */

            if (key is CharCodes.ZC_SINGLE_CLICK or CharCodes.ZC_DOUBLE_CLICK)
            {
                if (!Screen.ValidateClick())
                    goto continue_input;
            }



            /* Handle timeouts */

            if (key == CharCodes.ZC_TIME_OUT)
            {
                if (Process.DirectCall(routine) == 0)
                    goto continue_input;
            }

            /* Handle hot keys */

            if (hot_keys && key is >= CharCodes.ZC_HKEY_MIN and <= CharCodes.ZC_HKEY_MAX)
            {

            }

            /* Return key */
            return key;
        }/* stream_read_key */

        /*
         * stream_read_input
         *
         * Read a line of input from the current input stream.
         *
         */

        internal static zword StreamReadInput(int max, Span<zword> buf, zword timeout, zword routine, bool hot_keys, bool no_scripting)
        {
            zword key = CharCodes.ZC_BAD;
            bool no_scrollback = no_scripting;


            Buffer.FlushBuffer();


            /* Read input line from current input stream */

            continue_input:


            /* Verify mouse clicks */

            if (key is CharCodes.ZC_SINGLE_CLICK or CharCodes.ZC_DOUBLE_CLICK)
            {
                if (!Screen.ValidateClick())
                    goto continue_input;
            }

            /* Copy input line to the command file */

            /* Handle timeouts */

            if (key == CharCodes.ZC_TIME_OUT)
            {
                if (Process.DirectCall(routine) == 0)
                    goto continue_input;
            }

            /* Handle hot keys */

            if (hot_keys && key is >= CharCodes.ZC_HKEY_MIN and <= CharCodes.ZC_HKEY_MAX)
            {

                return CharCodes.ZC_BAD;
            }

            /* Return terminating key */

            return key;

        }/* stream_read_input */
    }
}