/*
 * dinput.c - Dumb interface, input functions
 *
 * This file is ported from Frotz.
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
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 * Or visit http://www.fsf.org/
 */
using Frotz.Constants;

using static Frotz.Constants.CharCodes;
using static Frotz.Constants.FileTypes;
using static Frotz.Constants.General;
using static Frotz.Constants.ZMachine;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using zword = System.UInt16;

namespace Frotz
{

    public static partial class OS
    {
        private const int FILENAME_MAX = 1024;
        private const char PATH_SEPARATOR = '\\';

        private const string runtime_usage =
        "DUMB-FROTZ runtime help:\n" +
        "  General Commands:\n" +
        "    \\help    Show this message.\n" +
        "    \\set     Show the current values of runtime settings.\n" +
        "    \\s       Show the current contents of the whole screen.\n" +
        "    \\d       Discard the part of the input before the cursor.\n" +
        "    \\wN      Advance clock N/10 seconds, possibly causing the current\n" +
        "                and subsequent inputs to timeout.\n" +
        "    \\w       Advance clock by the amount of real time since this input\n" +
        "                started (times the current speed factor).\n" +
        "    \\t       Advance clock just enough to timeout the current input\n" +
        "  Reverse-Video Display Method Settings:\n" +
        "    \\rn   none    \\rc   CAPS    \\rd   doublestrike    \\ru   underline\n" +
        "    \\rbC  show rv blanks as char C (orthogonal to above modes)\n" +
        "  Output Compression Settings:\n" +
        "    \\cn      none: show whole screen before every input.\n" +
        "    \\cm      max: show only lines that have new nonblank characters.\n" +
        "    \\cs      spans: like max, but emit a blank line between each span of\n" +
        "                screen lines shown.\n" +
        "    \\chN     Hide top N lines (orthogonal to above modes).\n" +
        "  Misc Settings:\n" +
        "    \\sfX     Set speed factor to X.  (0 = never timeout automatically).\n" +
        "    \\mp      Toggle use of MORE prompts\n" +
        "    \\ln      Toggle display of line numbers.\n" +
        "    \\lt      Toggle display of the line type identification chars.\n" +
        "    \\vb      Toggle visual bell.\n" +
        "    \\pb      Toggle display of picture outline boxes.\n" +
        "    (Toggle commands can be followed by a 1 or 0 to set value ON or OFF.)\n" +
        "  Character Escapes:\n" +
        "    \\\\  backslash    \\#  backspace    \\[  escape    \\_  return\n" +
        "    \\< \\> \\^ \\.  cursor motion        \\1 ..\\0  f1..f10\n" +
        "    \\D ..\\X   Standard Frotz hotkeys.  Use \\H (help) to see the list.\n" +
        "  Line Type Identification Characters:\n" +
        "    Input lines:\n" +
        "      untimed  timed\n" +
        "      >        T      A regular line-oriented input\n" +
        "      )        t      A single-character input\n" +
        "      }        D      A line input with some input before the cursor.\n" +
        "                         (Use \\d to discard it.)\n" +
        "    Output lines:\n" +
        "      ]     Output line that contains the cursor.\n" +
        "      .     A blank line emitted as part of span compression.\n" +
        "            (blank) Any other output line.\n"
    ;

        static float speed = 1;
        private enum input_type
        {
            INPUT_CHAR = 0,
            INPUT_LINE = 1,
            INPUT_LINE_CONTINUED = 2,
        };

        /* get a character.  Exit with no fuss on EOF.  */
        static ushort xgetchar()
        {
            int c = System.Console.Read();
            if (c == -1) // EOF
            {
                /*
                            if (feof(stdin))
                            {
                                fprintf(stderr, "\nEOT\n");
                                os_quit(EXIT_SUCCESS);
                            }
                */
                /*
                  #ifdef TOPS20
                            // On TOPS-20 only, the very first getchar() may return EOF,
                            // even thought feof(stdin) is false.  No idea why, but...
                            //
                            if (!spurious_getchar)
                            {
                                spurious_getchar = true;
                                return xgetchar();
                            }
                            else
                            {
                                os_fatal(strerror(errno));
                            }
                #else
                */
                Fatal("End Of File");
                //#endif
            }
            return (ushort)c;
        }


        /* Read one line, including the newline, into s.  Safely avoids buffer
         * overruns (but that's kind of pointless because there are several
         * other places where I'm not so careful).  */
        private static void dumb_getline(ref zword[] s)
        {
            int c;
            int p = 0;
            while (p < INPUT_BUFFER_SIZE - 1)
            {
                if ((s[p++] = xgetchar()) == '\n')
                {
                    s[p] = '\0';
                    return;
                }
            }
            s[p - 1] = '\n';
            s[p] = '\0';
            while ((c = xgetchar()) != '\n')
                ;
            Console.Out.Write("Line too long, truncated to {0}\n", s);
        }



        /* Translate in place all the escape characters in s.  */
        static void translate_special_chars(ref zword[] s)
        {
            ref zword[] src = ref s;
            ref zword[] dest = ref s;
            int si = 0;
            int di = 0;
            while (src[si] != 0)
                switch (src[si++])
                {
                    default: dest[di++] = src[si - 1]; break;
                    case '\n': dest[di++] = ZC_RETURN; break;
                    case '\\':
                        switch (src[si++])
                        {
                            case '\n': dest[di++] = ZC_RETURN; break;
                            case '\\': dest[di++] = '\\'; break;
                            case '?': dest[di++] = ZC_BACKSPACE; break;
                            case '[': dest[di++] = ZC_ESCAPE; break;
                            case '_': dest[di++] = ZC_RETURN; break;
                            case '^': dest[di++] = ZC_ARROW_UP; break;
                            case '.': dest[di++] = ZC_ARROW_DOWN; break;
                            case '<': dest[di++] = ZC_ARROW_LEFT; break;
                            case '>': dest[di++] = ZC_ARROW_RIGHT; break;
                            case 'R': dest[di++] = ZC_HKEY_RECORD; break;
                            case 'P': dest[di++] = ZC_HKEY_PLAYBACK; break;
                            case 'S': dest[di++] = ZC_HKEY_SEED; break;
                            case 'U': dest[di++] = ZC_HKEY_UNDO; break;
                            case 'N': dest[di++] = ZC_HKEY_RESTART; break;
                            case 'X': dest[di++] = ZC_HKEY_QUIT; break;
                            case 'D': dest[di++] = ZC_HKEY_DEBUG; break;
                            case 'H': dest[di++] = ZC_HKEY_HELP; break;
                            case '1': dest[di++] = ZC_FKEY_F1; break;
                            case '2': dest[di++] = ZC_FKEY_F2; break;
                            case '3': dest[di++] = ZC_FKEY_F3; break;
                            case '4': dest[di++] = ZC_FKEY_F4; break;
                            case '5': dest[di++] = ZC_FKEY_F5; break;
                            case '6': dest[di++] = ZC_FKEY_F6; break;
                            case '7': dest[di++] = ZC_FKEY_F7; break;
                            case '8': dest[di++] = ZC_FKEY_F8; break;
                            case '9': dest[di++] = ZC_FKEY_F9; break;
                            /* # ifdef TOPS20
                                                    case '0': dest[di++] = (ZC_FKEY_F10 & 0xff); break;
                            #else */
                            case '0': dest[di++] = ZC_FKEY_F10; break;
                            //#endif
                            default:
                                Console.Error.Write("DUMB-FROTZ: unknown escape char: {0}\n", src[si - 1]);
                                Console.Error.Write("Enter \\help to see the list\n");
                                break;
                        }
                        break;
                }
            dest[di] = '\0';
        }


        /* The time in tenths of seconds that the user is ahead of z time.  */
        private static TimeSpan time_ahead = TimeSpan.Zero;

        /* Called from os_read_key and os_read_line if they have input from
         * a previous call to dumb_read_line.
         * Returns true if we should timeout rather than use the read-ahead.
         * (because the user is further ahead than the timeout).  */
        static bool check_timeout(TimeSpan timeout)
        {
            if ((timeout == TimeSpan.Zero) || (timeout > time_ahead))
                time_ahead = TimeSpan.Zero;
            else
                time_ahead -= timeout;
            return time_ahead != TimeSpan.Zero;
        }


        /* If val is '0' or '1', set *var accordingly, otherwise toggle it.  */
        private static void toggle(ref bool var, zword val)
        {
            var = (val == '1') || (val != '0' && !var);
        }


        /* Handle input-related user settings and call dumb_output_handle_setting.  */
        private static bool dumb_handle_setting(in zword[] zsetting, bool show_cursor, bool startup)
        {
            string setting = ConvertToString(zsetting);

            if (setting.Substring(0, 2) == "sf")
            {
                speed = float.Parse(setting.Substring(2));
                Console.Out.Write("Speed Factor {0}\n", speed);
            }
            else if (setting.Substring(0, 2) == "mp")
            {
                toggle(ref do_more_prompts, setting[2]);
                Console.Out.Write("More prompts %s\n", do_more_prompts ? "ON" : "OFF");
            }
            else
            {
                if (setting == "set")
                {
                    Console.Out.Write("Speed Factor {0}\n", speed);
                    Console.Out.Write("More Prompts {0}\n",
                        do_more_prompts ? "ON" : "OFF");
                }
                return dumb_output_handle_setting(zsetting, show_cursor, startup);
            }
            return true;
        }


        /* Read a line, processing commands (lines that start with a backslash
         * (that isn't the start of a special character)), and write the
         * first non-command to s.
         * Return true if timed-out.  */
        private static bool dumb_read_line(zword[] s, string? prompt, bool show_cursor,
                       TimeSpan timeout, input_type type,
                   zword[]? continued_line_chars)
        {
            return true;
        }


        /* Read a line that is not part of z-machine input (more prompts and
         * filename requests).  */
        static void dumb_read_misc_line(zword[] s, string prompt)
        {
            dumb_read_line(s, prompt, false, TimeSpan.Zero, 0, null);
            /* Remove terminating newline */
            s[strlen(s) - 1] = '\0';
        }


        /* For allowing the user to input in a single line keys to be returned
         * for several consecutive calls to read_char, with no screen update
         * in between.  Useful for traversing menus.  */
        private static zword[] read_key_buffer = new zword[INPUT_BUFFER_SIZE];

        /* Similar.  Useful for using function key abbreviations.  */
        private static zword[] read_line_buffer = new zword[INPUT_BUFFER_SIZE];



        /////////////////////////////////////////////////////////////////////////////
        // Interface to the Frotz core
        /////////////////////////////////////////////////////////////////////////////



        /*
         * os_read_key
         *
         * Read a single character from the keyboard (or a mouse click) and
         * return it. Input aborts after timeout/10 seconds.
         *
         */
        public static zword ReadKey(int timeout, bool show_cursor)
        {
            zword c;
            bool timed_out;
            int idx = 1;

            /* Discard any keys read for line input.  */
            read_line_buffer[0] = '\0';

            if (read_key_buffer[0] == '\0')
            {
                timed_out = dumb_read_line(read_key_buffer, null,
                    show_cursor, TimeSpan.FromSeconds(timeout), input_type.INPUT_CHAR, null);
                /* An empty input line is reported as a single CR.
                 * If there's anything else in the line, we report
                 * only the line's contents and not the terminating CR.  */
                if (strlen(read_key_buffer) > 1)
                    read_key_buffer[strlen(read_key_buffer) - 1] = '\0';
            }
            else
                timed_out = check_timeout(TimeSpan.FromSeconds(timeout));

            if (timed_out)
                return ZC_TIME_OUT;

#if USE_UTF8 == false
            c = read_key_buffer[0];
#else
        idx = utf8_to_zchar(&c, read_key_buffer, 0);
#endif
            Array.Copy(read_key_buffer, idx, read_key_buffer, 0,
                strlen(read_key_buffer) - idx + 1);

            /* TODO: error messages for invalid special chars.  */

            return c;
        }


        /*
         * os_read_line
         *
         * Read a line of input from the keyboard into a buffer. The buffer
         * may already be primed with some text. In this case, the "initial"
         * text is already displayed on the screen. After the input action
         * is complete, the function returns with the terminating key value.
         * The length of the input should not exceed "max" characters plus
         * an extra 0 terminator.
         *
         * Terminating keys are the return key (13) and all function keys
         * (see the Specification of the Z-machine) which are accepted by
         * the is_terminator function. Mouse clicks behave like function
         * keys except that the mouse position is stored in global variables
         * "mouse_x" and "mouse_y" (top left coordinates are (1,1)).
         *
         * Furthermore, Frotz introduces some special terminating keys:
         *
         *     ZC_HKEY_PLAYBACK (Alt-P)
         *     ZC_HKEY_RECORD (Alt-R)
         *     ZC_HKEY_SEED (Alt-S)
         *     ZC_HKEY_UNDO (Alt-U)
         *     ZC_HKEY_RESTART (Alt-N, "new game")
         *     ZC_HKEY_QUIT (Alt-X, "exit game")
         *     ZC_HKEY_DEBUG (Alt-D)
         *     ZC_HKEY_HELP (Alt-H)
         *
         * If the timeout argument is not zero, the input gets interrupted
         * after timeout/10 seconds (and the return value is 0).
         *
         * The complete input line including the cursor must fit in "width"
         * screen units.
         *
         * The function may be called once again to continue after timeouts,
         * misplaced mouse clicks or hot keys. In this case the "continued"
         * flag will be set. This information can be useful if the interface
         * implements input line history.
         *
         * The screen is not scrolled after the return key was pressed. The
         * cursor is at the end of the input line when the function returns.
         *
         * Since Frotz 2.2 the helper function "completion" can be called
         * to implement word completion (similar to tcsh under Unix).
         *
         */
        private static bool timed_out_last_time;
        public static zword ReadLine(int max, Span<zword> buf, int timeout, int width, bool continued)
        {
             return 0;
        }


        /*
         * os_read_file_name
         *
         * Return the name of a file. Flag can be one of:
         *
         *    FILE_SAVE     - Save game file
         *    FILE_RESTORE  - Restore game file
         *    FILE_SCRIPT   - Transcript file
         *    FILE_RECORD   - Command file for recording
         *    FILE_PLAYBACK - Command file for playback
         *    FILE_SAVE_AUX - Save auxiliary ("preferred settings") file
         *    FILE_LOAD_AUX - Load auxiliary ("preferred settings") file
         *
         * The length of the file name is limited by MAX_FILE_NAME. Ideally
         * an interpreter should open a file requester to ask for the file
         * name. If it is unable to do that then this function should call
         * print_string and read_string to ask for a file name.
         *
         */
        public static bool ReadFileName(out string? out_file_name, string default_name, FileTypes flag)
        {
            out_file_name = "";
            return true;
        }

        /*
         * os_more_prompt
         *
         * Display a MORE prompt, wait for a keypress and remove the MORE
         * prompt from the screen.
         *
         */
        public static void MorePrompt()
        {
            if (do_more_prompts)
            {
                zword[] buf = new zword[INPUT_BUFFER_SIZE];
                dumb_read_misc_line(buf, "***MORE***");
            }
            else
                dumb_elide_more_prompt();
        }

        private static void dumb_init_input()
        {
        }

        /*
         * os_read_mouse
         *
         * Store the mouse position in the global variables "mouse_x" and
         * "mouse_y", the code of the last clicked menu in "menu_selected"
         * and return the mouse buttons currently pressed.
         *
         */
        internal static zword ReadMouse()
        {
            /* NOT IMPLEMENTED */
            return 0;
        }

        /*
         * os_tick
         *
         * Called after each opcode.
         *
         */
        private static int osTickCount = 0;
        public static void Tick()
        {
            /* Nothing here yet */
        }
    }
}