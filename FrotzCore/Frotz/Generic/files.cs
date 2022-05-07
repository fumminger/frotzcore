/* files.c - Transscription, recording and playback
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
using System.IO;

using zword = System.UInt16;

namespace Frotz.Generic
{

    internal static class Files
    {
        internal static string ScriptName = General.DEFAULT_SCRIPT_NAME;
        internal static string CommandName = General.DEFAULT_COMMAND_NAME;
        private static int ScriptWidth = 0;
//        private static StreamWriter? Sfp = null;
//        private static System.IO.StreamWriter? Rfp = null;
//        private static System.IO.FileStream? Pfp = null;

#if true
        /*
         * script_open
         *
         * Open the transscript file. 'AMFV' makes this more complicated as it
         * turns transscription on/off several times to exclude some text from
         * the transscription file. This wasn't a problem for the original V4
         * interpreters which always sent transscription to the printer, but it
         * means a problem to modern interpreters that offer to open a new file
         * every time transscription is turned on. Our solution is to append to
         * the old transscription file in V1 to V4, and to ask for a new file
         * name in V5+.
         *
         */

        private static bool ScriptValid = false;

        internal static void ScriptOpen()
        {
          
        }/* script_open */

        /*
         * script_close
         *
         * Stop transscription.
         *
         */

        internal static void ScriptClose()
        {
        }/* script_close */

        /*
         * script_new_line
         *
         * Write a newline to the transscript file.
         *
         */

        internal static void ScriptNewLine()
        {
            //Sfp?.WriteLine();

            ScriptWidth = 0;
        }/* script_new_line */

        /*
         * script_char
         *
         * Write a single character to the transscript file.
         *
         */

        internal static void ScriptChar(zword c)
        {
            if (c == CharCodes.ZC_INDENT && ScriptWidth != 0)
                c = ' ';

            if (c == CharCodes.ZC_INDENT)
            {
                ScriptChar(' '); ScriptChar(' '); ScriptChar(' ');
                return;
            }
            if (c == CharCodes.ZC_GAP)
            {
                ScriptChar(' '); ScriptChar(' ');
                return;
            }
            if (c > 0xff)
            {
                ScriptChar('?');
                return;
            }

            //Sfp?.Write((char)c);
            ScriptWidth++;
        }/* script_char */

        /*
         * script_word
         *
         * Write a string to the transscript file.
         *
         */

        internal static void ScriptWord(ReadOnlySpan<zword> s)
        {
        }/* script_word */

        /*
         * script_write_input
         *
         * Send an input line to the transscript file.
         *
         */

        internal static void ScriptWriteInput(ReadOnlySpan<zword> buf, zword key)
        {
        }/* script_write_input */

        /*
         * script_erase_input
         *
         * Remove an input line from the transscript file.
         *
         */

        internal static void ScriptEraseInput(ReadOnlySpan<zword> buf)
        {
            int width;
            int i;

            for (i = 0, width = 0; buf[i] != 0; i++)
                width++;

            //Sfp?.BaseStream.SetLength(Sfp.BaseStream.Length - width);
            ScriptWidth -= width;
        }/* script_erase_input */

        /*
         * script_mssg_on
         *
         * Start sending a "debugging" message to the transscript file.
         *
         */

        internal static void ScriptMssgOn()
        {

            if (Files.ScriptWidth != 0)
                ScriptNewLine();

            ScriptChar(CharCodes.ZC_INDENT);

        }/* script_mssg_on */

        /*
         * script_mssg_off
         *
         * Stop writing a "debugging" message.
         *
         */

        internal static void ScriptMssgOff() => ScriptNewLine();/* script_mssg_off */
#endif
#if true
        /*
         * record_open
         *
         * Open a file to record the player's input.
         *
         */
        internal static void RecordOpen()
        {

            if (OS.ReadFileName(out string? new_name, CommandName, FileTypes.FILE_RECORD))
            {
                CommandName = new_name;
                /*
                if ((Rfp = new System.IO.StreamWriter(CommandName, false)) != null)
                {

                    Rfp.AutoFlush = true;
                }
                else
                {
                    Text.PrintString("Cannot open file\n");
                }
                */
            }
        }/* record_open */

        /*
         * record_close
         *
         * Stop recording the player's input.
         *
         */

        internal static void RecordClose()
        {


        }/* record_close */

        ///*
        // * record_code
        // *
        // * Helper function for record_char.
        // *
        // */

        private static void RecordCode(int c, bool force_encoding)
        {
            //if (Rfp is null)
            //    throw new InvalidOperationException("Rfp not initialized.");

            if (force_encoding || (c is '[' or < 0x20 or > 0x7e))
            {
                int i;

                //Rfp.Write('[');

                for (i = 10000; i != 0; i /= 10)
                {
                    //if (c >= i || i == 1)
                        //Rfp.Write((char)('0' + (c / i) % 10));
                }

                //Rfp.Write(']');

            }
            else
            {
                //Rfp.Write((char)c);
            }
        }/* record_code */

        /*
         * record_char
         *
         * Write a character to the command file.
         *
         */

        private static void RecordChar(zword c)
        {

        }/* record_char */

        /*
         * record_write_key
         *
         * Copy a keystroke to the command file.
         *
         */

        internal static void RecordWriteKey(zword key)
        {
            RecordChar(key);

            //Rfp?.Write('\n');

        }/* record_write_key */

        /*
         * record_write_input
         *
         * Copy a line of input to a command file.
         *
         */

        internal static void RecordWriteInput(ReadOnlySpan<zword> buf, zword key)
        {
            //zword c;

            for (int i = 0; i < buf.Length && buf[i] != 0; i++)
            {
                RecordChar(buf[i]);
            }

            RecordChar(key);

            //Rfp?.Write('\n');

        }/* record_write_input */
#endif
#if true
        /*
         * replay_open
         *
         * Open a file of commands for playback.
         *
         */

        internal static void ReplayOpen()
        {




        }/* replay_open */

        /*
         * replay_close
         *
         * Stop playback of commands.
         *
         */

        internal static void ReplayClose()
        {
            Screen.SetMorePrompts(true);

//            Pfp?.Close();


        }/* replay_close */

        /*
         * replay_code
         *
         * Helper function for replay_key and replay_line.
         *
         */

        private static int ReplayCode()
        {
            //if (Pfp is null)
            //    throw new InvalidOperationException("Pfp not initialized");

            int c = 0;

            //if ((c = Pfp.ReadByte()) == '[')
            //{
            //    int c2;
            //    c = 0;
            //    while ((c2 = Pfp.ReadByte()) is not -1 and >= '0' and <= '9')
            //        c = 10 * c + c2 - '0';
//
 //               return (c2 == ']') ? c : -1;
 //           }
 //           else
            {
                return c;
            }
        }/* replay_code */

        /*
         * replay_char
         *
         * Read a character from the command file.
         *
         */

        private static zword ReplayChar()
        {
//            if (Pfp is null)
//                throw new InvalidOperationException("Pfp not initialized.");

            int c;
            if ((c = ReplayCode()) != -1)
            {
                if (c != '\n')
                {
                    if (c < 1000)
                    {
                        c = Text.TranslateFromZscii((byte)c);



                        return (zword)c;
                    }
                    else
                    {
                        return (zword)(CharCodes.ZC_HKEY_MIN + c - 1000);
                    }
                }

//                Pfp.Position--;
//                Pfp.WriteByte((byte)'\n');

                return CharCodes.ZC_RETURN;
            }
            else
            {
                return CharCodes.ZC_BAD;
            }
        }/* replay_char */

        /*
         * replay_read_key
         *
         * Read a keystroke from a command file.
         *
         */

        internal static zword ReplayReadKey()
        {
            zword key = ReplayChar();

//            if (Pfp?.ReadByte() != '\n')
            {

                ReplayClose();
                return CharCodes.ZC_BAD;

            }
//            else
            {
                return key;
            }
        }/* replay_read_key */

        /*
         * replay_read_input
         *
         * Read a line of input from a command file.
         *
         */

        internal static zword ReplayReadInput(Span<zword> buf)
        {
            zword c;

            int pos = 0;

            for (; ; )
            {

                c = ReplayChar();

                if (c == CharCodes.ZC_BAD || Input.IsTerminator(c))
                    break;

                buf[pos++] = c;
            }

            //pos = 0;

            //if ( pfp.ReadByte() != '\n') {
            //    replay_close();
            //    return CharCodes.ZC_BAD;

            //} else return c;

            return c;

        }/* replay_read_input */
#endif
    }
}