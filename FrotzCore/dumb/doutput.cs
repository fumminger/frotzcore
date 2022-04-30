using Frotz.Generic;
using Frotz.Other;
using static Frotz.Constants.CharCodes;
using static Frotz.Constants.ZFont;
using static Frotz.Constants.ZMachine;
using static Frotz.Constants.ZStyles;
using System.Buffers;

namespace Frotz;

public static partial class OS
{
  
    private static bool show_line_numbers = false;
    private static bool show_line_types = false;
    private static bool show_pictures = true;
    private static bool visual_bell = true;
    private static bool plain_ascii = false;

    static string latin1_to_ascii =
    "    !   c   L   >o< Y   |   S   ''  C   a   <<  not -   R   _   " +
	"^0  +/- ^2  ^3  '   my  P   .   ,   ^1  o   >>  1/4 1/2 3/4 ?   " +
	"A   A   A   A   Ae  A   AE  C   E   E   E   E   I   I   I   I   " +
	"Th  N   O   O   O   O   Oe  *   O   U   U   U   Ue  Y   Th  ss  " +
	"a   a   a   a   ae  a   ae  c   e   e   e   e   i   i   i   i   " +
	"th  n   o   o   o   o   oe  :   o   u   u   u   ue  y   th  y   "
    ;
    private const byte DEFAULT_DUMB_COLOUR = (byte) 31;

    private static byte[] frotz_to_dumb = new byte[256];

    /* z_header.screen_rows * z_header.screen_cols */
    private static int screen_cells;

    /* The in-memory state of the screen.  */
    /* Each cell contains a style in the lower byte and a zchar above. */

    private struct cell_t
    {
        public int style;
        public short fg;
        public short bg;
        public zword c;
    };

    private static cell_t[]? screen_data; // WARNING: Not very safe

    private static int current_style = 0;
    private static byte current_fg = DEFAULT_DUMB_COLOUR;
    private static byte current_bg = DEFAULT_DUMB_COLOUR;

    /* Which cells have changed (1 byte per cell).  */
    private static byte[]? screen_changes; // WARNING: Not very safe

    private static int cursor_row = 0;
    private static int cursor_col = 0;

    /* Compression styles.  */
    enum compression_mode_type
    {
        COMPRESSION_NONE = 0,
        COMPRESSION_SPANS = 1,
        COMPRESSION_MAX = 2,
    };
    private static compression_mode_type compression_mode = compression_mode_type.COMPRESSION_SPANS;
    private static string[] compression_names = { "NONE", "SPANS", "MAX" };
    static int hide_lines = 0;

    /* Reverse-video display styles.  */
    enum rv_type {
        RV_NONE = 0,
        RV_DOUBLESTRIKE = 1,
        RV_UNDERLINE = 2,
        RV_CAPS = 3,
    }
    private static rv_type rv_mode = rv_type.RV_NONE;
    private static string[] rv_names = { "NONE", "DOUBLESTRIKE", "UNDERLINE", "CAPS" };
    static zword[] rv_blank_str = { ' ', 0, 0, 0, 0 };


    private static void putchar(zword x)
    {
        Console.Out.Write((char) x);
    }

    /* # ifdef USE_UTF8
        static void zputchar(zchar);
        void zputchar(zchar c)
        {
            if (c > 0x7ff)
            {
                putchar(0xe0 | ((c >> 12) & 0xf));
                putchar(0x80 | ((c >> 6) & 0x3f));
                putchar(0x80 | (c & 0x3f));
            }
            else if (c > 0x7f)
            {
                putchar(0xc0 | ((c >> 6) & 0x1f));
                putchar(0x80 | (c & 0x3f));
            }
            else
            {
                putchar(c);
            }
        }
    #else */
    private static void zputchar(zword x) {
        putchar(x);
    }
    //#endif
    /* Print a cell to stdout without using formatting codes.  */
    private static void show_cell_normal(cell_t cel)
    {
        switch (cel.style)
        {
            case NORMAL_STYLE:
            case FIXED_WIDTH_STYLE: /* NORMAL_STYLE falls through to here */
                zputchar(cel.c);
                break;
            case PICTURE_STYLE:
                zputchar(show_pictures ? cel.c : ' ');
                break;
            case REVERSE_STYLE:
                if (cel.c == ' ')
                    Console.Out.Write("{0}", rv_blank_str);
                else
                {
                    switch (rv_mode)
                    {
                        case rv_type.RV_CAPS:
                            if (cel.c <= 0x7f)
                            {
                                zputchar(Char.ToUpper((char) cel.c));
                            }
                            else
                            {
                                zputchar(cel.c);
                            }
                            break;
                        case rv_type.RV_NONE:
                            zputchar(cel.c);
                            break;
                        case rv_type.RV_UNDERLINE:
                            putchar('_');
                            putchar('\b');
                            zputchar(cel.c);
                            break;
                        case rv_type.RV_DOUBLESTRIKE:
                            zputchar(cel.c);
                            putchar('\b');
                            zputchar(cel.c);
                            break;
                    }
                }
                break;
        }
    }


    private static void show_cell(cell_t cel)
    {
        /*
# ifndef DISABLE_FORMATS
        if (f_setup.format == FORMAT_IRC)
            show_cell_irc(cel);
        else if (f_setup.format == FORMAT_ANSI)
            show_cell_ansi(cel);
        else if (f_setup.format == FORMAT_BBCODE)
            show_cell_bbcode(cel);
        else
#endif
        */
            show_cell_normal(cel);
    }

    private static bool will_print_blank(cell_t c)
    {
/* # ifndef DISABLE_FORMATS
        if (f_setup.format != FORMAT_NORMAL)
            return false;
#endif */
        return (((c.style == PICTURE_STYLE) && !show_pictures)
            || ((c.c == ' ')
            && ((c.style != REVERSE_STYLE)
            || (rv_blank_str[0] == ' '))));
    }

    static cell_t make_cell(int style, short fg, short bg, zword c)
    {
        cell_t cel;

        cel.style = style;
        cel.c = c;
        cel.bg = 0; // WARNING : Correct?
        cel.fg = 0; // WARNING : Correct?

        /* WARNING : What is the appropriate way to port f_setup_format?
         Should probably be option_format in main.cs
        if (f_setup.format != FORMAT_NORMAL)
        {
            cel.bg = bg;
            cel.fg = fg;
        }
        */
        return cel;
    }


    private static void show_line_prefix(int row, char c)
    {
        if (show_line_numbers)
        {
            if (row == -1)
            {
                show_cell(make_cell(0, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, '.'));
                show_cell(make_cell(0, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, '.'));
            }
            else
            {
                string s = ((row + 1) % 100).ToString();
                show_cell(make_cell(0, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, s[0]));
                show_cell(make_cell(0, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, s[1]));
            }
        }
        if (show_line_types)
            show_cell(make_cell(0, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, c));

        /* Add a separator char (unless there's nothing to separate).  */
        if (show_line_numbers || show_line_types)
            show_cell(make_cell(0, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, ' '));
    }


    static ref cell_t dumb_row(int r, int c)
    {
        // WARNING : Do not check bounds
        return ref screen_data[r * Main.h_screen_cols+c];
    }


    /* Print a row to stdout.  */
    private static void show_row(int r)
    {
        if (r == -1)
        {
            show_line_prefix(-1, '.');
        }
        else
        {
            int c, last;
            show_line_prefix(r, (r == cursor_row) ? ']' : ' ');
            /* Don't print spaces at end of line.  */
            /* (Saves bandwidth and printhead wear.)  */
            /* TODO: compress spaces to tabs.  */
            for (last = Main.h_screen_cols - 1; last >= 0; last--)
            {
                if (!will_print_blank(dumb_row(r,last)))
                    break;
            }

            for (c = 0; c <= last; c++)
                show_cell(dumb_row(r,c));
        }
        show_cell(make_cell(0, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, '\n'));
    }

    static ref byte dumb_changes_row(int r, int c)
    {
        // WARNING : Do not check bounds
        return ref screen_changes[r * Main.h_screen_cols+c];
    }

    static void dumb_set_cell(int row, int col, cell_t c)
    {
        cell_t test;
        bool result = false;

        test = dumb_row(row, col);

        if (c.style == test.style &&
            c.fg == test.fg &&
            c.bg == test.bg &&
            c.c == test.c)
        {
            result = true;
        }

        dumb_changes_row(row,col) = Convert.ToByte(!result);
        dumb_row(row,col) = c;
    }
    public static void dumb_display_char(zword c)
    {
        dumb_set_cell(cursor_row, cursor_col, make_cell(current_style, current_fg, current_bg, c));
        if (++cursor_col == Main.h_screen_cols)
        {
            if (cursor_row == Main.h_screen_rows - 1)
                cursor_col--;
            else
            {
                cursor_row++;
                cursor_col = 0;
            }
        }
    }

    static void mark_all_unchanged()
    {
        Array.Fill<byte>(screen_changes, 0, 0, screen_cells);
    }

    /* Check if a cell is a blank or will display as one.
 * (Used to help decide if contents are worth printing.)  */
    private static bool is_blank(cell_t c)
    {
        return ((c.c == ' ')
            || ((c.style == PICTURE_STYLE) && !show_pictures));
    }

    static void dumb_copy_cell(int dest_row, int dest_col,
            int src_row, int src_col)
    {
        dumb_row(dest_row,dest_col) = dumb_row(src_row,src_col);
        dumb_changes_row(dest_row,dest_col) = dumb_changes_row(src_row,src_col);
    }

    private static void dumb_init_output()
    {
        /*
   #ifndef DISABLE_FORMATS
                if (f_setup.format == FORMAT_IRC)
                {
                    // WARNING : No C# equivalent?
                    //setvbuf(stdout, 0, _IONBF, 0);
                    //setvbuf(stderr, 0, _IONBF, 0);

                    Main.h_config |= CONFIG_COLOUR | CONFIG_BOLDFACE | CONFIG_EMPHASIS;

                    Array.Fill<byte>(frotz_to_dumb, DEFAULT_DUMB_COLOUR, 0, 256);
                    frotz_to_dumb[BLACK_COLOUR] = 1;
                    frotz_to_dumb[RED_COLOUR] = 4;
                    frotz_to_dumb[GREEN_COLOUR] = 3;
                    frotz_to_dumb[YELLOW_COLOUR] = 8;
                    frotz_to_dumb[BLUE_COLOUR] = 12;
                    frotz_to_dumb[MAGENTA_COLOUR] = 6;
                    frotz_to_dumb[CYAN_COLOUR] = 11;
                    frotz_to_dumb[WHITE_COLOUR] = 0;
                    frotz_to_dumb[GREY_COLOUR] = 14;

                    Main.h_default_foreground = WHITE_COLOUR;
                    Main.h_default_background = BLACK_COLOUR;
                }
                else if (f_setup.format == FORMAT_ANSI)
        {
            // WARNING : No C# equivalent?
            //setvbuf(stdout, 0, _IONBF, 0);
            //setvbuf(stderr, 0, _IONBF, 0);

            Main.h_config |= CONFIG_COLOUR | CONFIG_BOLDFACE | CONFIG_EMPHASIS;

            Array.Fill<byte>(frotz_to_dumb, DEFAULT_DUMB_COLOUR, 0, 256);
            frotz_to_dumb[BLACK_COLOUR] = 0;
            frotz_to_dumb[RED_COLOUR] = 1;
            frotz_to_dumb[GREEN_COLOUR] = 2;
            frotz_to_dumb[YELLOW_COLOUR] = 3;
            frotz_to_dumb[BLUE_COLOUR] = 4;
            frotz_to_dumb[MAGENTA_COLOUR] = 5;
            frotz_to_dumb[CYAN_COLOUR] = 6;
            frotz_to_dumb[WHITE_COLOUR] = 7;
            frotz_to_dumb[LIGHTGREY_COLOUR] = 17;
            frotz_to_dumb[MEDIUMGREY_COLOUR] = 13;
            frotz_to_dumb[DARKGREY_COLOUR] = 8;

            Main.h_default_foreground = WHITE_COLOUR;
            Main.h_default_background = BLACK_COLOUR;
        }
        /*
        else if (f_setup.format == FORMAT_BBCODE)
        {
            // WARNING : No C# equivalent?
            //setvbuf(stdout, 0, _IONBF, 0);
            //setvbuf(stderr, 0, _IONBF, 0);

            Main.h_config |= CONFIG_COLOUR | CONFIG_BOLDFACE | CONFIG_EMPHASIS;

            Array.Fill<byte>(frotz_to_dumb, DEFAULT_DUMB_COLOUR, 0, 256);
            frotz_to_dumb[BLACK_COLOUR] = 0;
            frotz_to_dumb[RED_COLOUR] = 1;
            frotz_to_dumb[GREEN_COLOUR] = 2;
            frotz_to_dumb[YELLOW_COLOUR] = 3;
            frotz_to_dumb[BLUE_COLOUR] = 4;
            frotz_to_dumb[MAGENTA_COLOUR] = 5;
            frotz_to_dumb[CYAN_COLOUR] = 6;
            frotz_to_dumb[WHITE_COLOUR] = 7;
            frotz_to_dumb[LIGHTGREY_COLOUR] = 8;
            frotz_to_dumb[MEDIUMGREY_COLOUR] = 9;
            frotz_to_dumb[DARKGREY_COLOUR] = 10;

            Main.h_default_foreground = BLACK_COLOUR;
            Main.h_default_background = WHITE_COLOUR;
        }
        */
        if (Main.h_version == V3)
        {
            Main.h_config |= CONFIG_SPLITSCREEN;
            Main.h_flags &= (ushort) (~OLD_SOUND_FLAG & 0xffff);
        }

        if (Main.h_version >= V5)
        {
            Main.h_flags &= (ushort) (~SOUND_FLAG & 0xffff);
        }

        Main.h_screen_height = Main.h_screen_rows;
        Main.h_screen_width = Main.h_screen_cols;
        screen_cells = Main.h_screen_rows * Main.h_screen_cols;

        Main.h_font_width = 1; Main.h_font_height = 1;

        screen_data = new cell_t[screen_cells];
        screen_changes = new byte[screen_cells];
        EraseArea(1, 1, Main.h_screen_rows, Main.h_screen_cols, -2);
        Array.Fill<byte>(screen_changes, 0, 0, screen_cells);
    }

    private static void dumb_display_user_input(zword[] s)
    {
        /* copy to screen without marking it as a change.  */
        int i = 0;
        while (s[i] != 0)
            dumb_row(cursor_row,cursor_col++) = make_cell(0, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, s[i++]);
    }

    private static void dumb_discard_old_input(int num_chars)
    {
        /* Weird discard stuff.  Grep spec for 'pain in my butt'.  */
        /* The old characters should be on the screen just before the cursor.
         * Erase them.  */
        cursor_col -= num_chars;

        if (cursor_col < 0)
            cursor_col = 0;
        EraseArea(cursor_row + 1, cursor_col + 1,
        cursor_row + 1, cursor_col + num_chars, -1);
    }



    /* Print the part of the cursor row before the cursor.  */
    private static void dumb_show_prompt(bool show_cursor, char line_type)
    {
        int i;
        show_line_prefix(show_cursor ? cursor_row : -1, line_type);
        if (show_cursor)
        {
            for (i = 0; i < cursor_col; i++)
                show_cell(dumb_row(cursor_row,i));
        }
    }

    /* Show the current screen contents, or what's changed since the last
     * call.
     *
     * If compressing, and show_cursor is true, and the cursor is past the
     * last nonblank character on the last line that would be shown, then
     * don't show that line (because it will be redundant with the prompt
     * line just below it).
     */
    private static void dumb_show_screen(bool show_cursor)
    {
        int r, c, first, last;
        bool[] changed_rows = new bool[0x100];

        /* Easy case */
        if (compression_mode == compression_mode_type.COMPRESSION_NONE)
        {
            for (r = hide_lines; r < Main.h_screen_rows; r++)
                show_row(r);
            mark_all_unchanged();
            return;
        }

        /* Check which rows changed, and where the first and last change is. */
        first = last = -1;
        Array.Fill<bool>(changed_rows, false, 0, Main.h_screen_rows);
        for (r = hide_lines; r < Main.h_screen_rows; r++)
        {
            for (c = 0; c < Main.h_screen_cols; c++)
            {
                if (dumb_changes_row(r,c) != 0 && !is_blank(dumb_row(r,c)))
                    break;
            }

            changed_rows[r] = (c != Main.h_screen_cols);
            if (changed_rows[r])
            {
                first = (first != -1) ? first : r;
                last = r;
            }
        }

        if (first == -1)
            return;

        /* The show_cursor rule described above */
        if (show_cursor && (cursor_row == last))
        {
            for (c = cursor_col; c < Main.h_screen_cols; c++)
            {
                if (!is_blank(dumb_row(last,c)))
                    break;
            }
            if (c == Main.h_screen_cols)
                last--;
        }

        /* Display the appropriate rows.  */
        if (compression_mode == compression_mode_type.COMPRESSION_MAX)
        {
            for (r = first; r <= last; r++)
            {
                if (changed_rows[r])
                    show_row(r);
            }
        }
        else
        {
            /* COMPRESSION_SPANS */
            for (r = first; r <= last; r++)
            {
                if (changed_rows[r] || changed_rows[r + 1])
                    show_row(r);
                else
                {
                    while (!changed_rows[r + 1])
                        r++;
                    show_row(-1);
                }
            }
            if (show_cursor && (cursor_row > last + 1))
                show_row((cursor_row == last + 2) ? (last + 1) : -1);
        }
        mark_all_unchanged();
    }

    /* Unconditionally show whole screen.  For \s user command.  */
    private static void dumb_dump_screen()
    {
        int r;
        for (r = 0; r < Main.h_screen_height; r++)
            show_row(r);
    }

    /* Called when it's time for a more prompt but user has them turned off.  */
    private static void dumb_elide_more_prompt()
    {
        dumb_show_screen(false);
        if (compression_mode == compression_mode_type.COMPRESSION_SPANS && hide_lines == 0)
        {
            show_row(-1);
        }
    }
    private static bool dumb_output_handle_setting(in zword[] setting, bool show_cursor, bool startup)
    {
        int i;
        /*# ifdef USE_UTF8
            unsigned char* q;
        #endif*/

        if (setting.ToString() == "pb") {
            toggle(ref show_pictures, setting[2]);
            Console.Out.Write("Picture outlines display {0}\n", show_pictures ? "ON" : "OFF");
            if (startup)
                return true;
            for (i = 0; i < screen_cells; i++)
                screen_changes[i] = (screen_data[i].style == PICTURE_STYLE).ToByte();
	        dumb_show_screen(show_cursor);
        } else if (setting.ToString() == "vb") {
	        toggle(ref visual_bell, setting[2]);
            Console.Out.Write("Visual bell {0}\n", visual_bell? "ON" : "OFF");
            Beep(1); Beep(2);
	    } else if (setting.ToString() == "ln")
        {
            toggle(ref show_line_numbers, setting[2]);
            Console.Out.Write("Line numbering {0}\n", show_line_numbers ? "ON" : "OFF");
        }
        else if (setting.ToString() == "lt")
        {
            toggle(ref show_line_types, setting[2]);
            Console.Out.Write("Line-type display {0}\n", show_line_types ? "ON" : "OFF");
        }
        else if (setting[0] == 'c')
        {
            switch (setting[1])
            {
                case 'm': compression_mode = compression_mode_type.COMPRESSION_MAX; break;
                case 's': compression_mode = compression_mode_type.COMPRESSION_SPANS; break;
                case 'n': compression_mode = compression_mode_type.COMPRESSION_NONE; break;
                case 'h': hide_lines = int.Parse(ConvertToString(setting).Substring(2)); break;
                default: return false;
            }
            Console.Out.Write("Compression mode %s, hiding top %d lines\n",
            compression_names[(int) compression_mode], hide_lines);
        }
        else if (setting[0] == 'r')
        {
            switch (setting[1])
            {
                case 'n': rv_mode = rv_type.RV_NONE; break;
                case 'o': rv_mode = rv_type.RV_DOUBLESTRIKE; break;
                case 'u': rv_mode = rv_type.RV_UNDERLINE; break;
                case 'c': rv_mode = rv_type.RV_CAPS; break;
                case 'b':
                    if (setting[2] != 0)
                    {
                        rv_blank_str[0] = setting[2];
                        if (setting[3] != 0)
                        {
                            rv_blank_str[1] = setting[3];
                            if (setting[4] != 0)
                            {
                                rv_blank_str[2] = setting[4];
                                if (setting[5] != 0)
                                {
                                    rv_blank_str[3] = setting[5];
                                }
                            }
                        }
                    }
                    else
                    {
                        rv_blank_str[0] = ' ';
                        rv_blank_str[1] = (char) 0;
                        rv_blank_str[2] = (char) 0;
                        rv_blank_str[3] = (char) 0;
                    }
                    break;
                default: return false;
            }
        /*# ifdef USE_UTF8
            for (q = (unsigned char *) &rv_blank_str[1]; *q; q++)
			        if (*q < 0x80 || *q >= 0xc0 || (unsigned char)rv_blank_str[0] < 0x80)
				        *q = 0;
        #else */
            rv_blank_str[1] = 0;
            //#endif
            Console.Out.Write("Reverse-video mode %s, blanks reverse to '%s': ",
                rv_names[(int) rv_mode], rv_blank_str);

            string p = "sample reverse text";
            for (int j = 0; p[j] != 0; j++)
                show_cell(make_cell(REVERSE_STYLE, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, p[j]));
            putchar('\n');
            for (i = 0; i < screen_cells; i++)
                screen_changes[i] = (screen_data[i].style == REVERSE_STYLE).ToByte();
            dumb_show_screen(show_cursor);
        }
        else if (setting.ToString() == "set")
        {
            Console.Out.Write("Compression Mode {0}, hiding top {1} lines\n",
                compression_names[(int) compression_mode], hide_lines);
            Console.Out.Write("Picture Boxes display %s\n", show_pictures ? "ON" : "OFF");
            Console.Out.Write("Visual Bell %s\n", visual_bell ? "ON" : "OFF");
            Beep(1); Beep(2);
            Console.Out.Write("Line Numbering %s\n", show_line_numbers ? "ON" : "OFF");
            Console.Out.Write("Line-Type display %s\n", show_line_types ? "ON" : "OFF");
            Console.Out.Write("Reverse-Video mode %s, Blanks reverse to '%s': ",
                rv_names[(int) rv_mode], rv_blank_str);
            string p = "sample reverse text";
            for (int j = 0; p[j] != 0; j++)
                show_cell(make_cell(REVERSE_STYLE, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, p[j]));
            putchar('\n');
        }
        else
            return false;
        return true;
    }


    /////////////////////////////////////////////////////////////////////////////
    // Interface to the Frotz core
    /////////////////////////////////////////////////////////////////////////////

    /*
     * os_display_char
     *
     * Display a character of the current font using the current colours and
     * text style. The cursor moves to the next position. Printable codes are
     * all ASCII values from 32 to 126, ISO Latin-1 characters from 160 to
     * 255, ZC_GAP (gap between two sentences) and ZC_INDENT (paragraph
     * indentation), and Unicode characters above 255. The screen should not
     * be scrolled after printing to the bottom right corner.
     *
     */
    // OK
    // HACK : Ignore c >= ZC_LATIN1_MIN
    public static void DisplayChar(zword c)
    {
        if (c == CharCodes.ZC_INDENT)
        {
            dumb_display_char(' ');
            dumb_display_char(' ');
            dumb_display_char(' ');
        }
        else if (c == CharCodes.ZC_GAP)
        {
            dumb_display_char(' ');
            dumb_display_char(' ');
        }
        else if (IsValidChar(c))
        {
            dumb_display_char(c);
        }
    }

    /*
     * os_display_string
     *
     * Pass a string of characters to os_display_char.
     *
     */
    public static void DisplayString(ReadOnlySpan<zword> chars)
    {
        zword c;

        for (int i = 0; i < chars.Length && chars[i] != 0; i++)
        {
            c = chars[i];
            if (c is CharCodes.ZC_NEW_FONT or CharCodes.ZC_NEW_STYLE)
            {
                int arg = chars[++i];
                if (c == CharCodes.ZC_NEW_FONT)
                {
                    SetFont(arg);
                }
                else if (c == CharCodes.ZC_NEW_STYLE)
                {
                    SetTextStyle(arg);
                }
            }
            else
            {
                DisplayChar(c);
            }
        }
    }

    public static void DisplayString(ReadOnlySpan<char> s)
    {
        zword[]? pooled = null;
        int len = s.Length;
        Span<zword> word = len <= MaxStack ? stackalloc zword[len] : (pooled = ArrayPool<zword>.Shared.Rent(len));
        try
        {
            for (int i = 0; i < len; i++)
            {
                word[i] = s[i];
            }
            DisplayString(word[..len]);
        }
        finally
        {
            if (pooled is not null)
                ArrayPool<zword>.Shared.Return(pooled);
        }
    }

    /*
     * os_erase_area
     *
     * Fill a rectangular area of the screen with the current background
     * colour. Top left coordinates are (1,1). The cursor does not move.
     *
     * The final argument gives the window being changed, -1 if only a
     * portion of a window is being erased, or -2 if the whole screen is
     * being erased.
     *
     */
    public static void EraseArea(int top, int left, int bottom, int right, int win)
    {
        int row, col;
        top--; left--; bottom--; right--;
        for (row = top; row <= bottom; row++)
        {
            for (col = left; col <= right; col++)
                dumb_set_cell(row, col, make_cell(current_style, current_fg, current_bg, ' '));
        }
    }


    /*
     * os_scroll_area
     *
     * Scroll a rectangular area of the screen up (units > 0) or down
     * (units < 0) and fill the empty space with the current background
     * colour. Top left coordinates are (1,1). The cursor stays put.
     *
     */
    public static void ScrollArea(int top, int left, int bottom, int right, int units)
    {
        int row, col;

        top--; left--; bottom--; right--;

        if (units > 0)
        {
            for (row = top; row <= bottom - units; row++)
            {
                for (col = left; col <= right; col++)
                    dumb_copy_cell(row, col, row + units, col);
            }
            EraseArea(bottom - units + 2, left + 1,
                bottom + 1, right + 1, -1);
        }
        else if (units < 0)
        {
            for (row = bottom; row >= top - units; row--)
            {
                for (col = left; col <= right; col++)
                    dumb_copy_cell(row, col, row + units, col);
            }
            EraseArea(top + 1, left + 1, top - units, right + 1, -1);
        }
    }

    /*
      * os_font_data
      *
      * Return true if the given font is available. The font can be
      *
      *    TEXT_FONT
      *    PICTURE_FONT
      *    GRAPHICS_FONT
      *    FIXED_WIDTH_FONT
      *
      * The font size should be stored in "height" and "width". If
      * the given font is unavailable then these values must _not_
      * be changed.
      *
      */
    public static bool FontData(int font, ref zword height, ref zword width)
    {
        if (font == TEXT_FONT)
        {
            height = 1;
            width = 1;
            return true;
        }
        return false;
    }

    /*
     * os_set_colour
     *
     * Set the foreground and background colours which can be:
     *
     *     1
     *     BLACK_COLOUR
     *     RED_COLOUR
     *     GREEN_COLOUR
     *     YELLOW_COLOUR
     *     BLUE_COLOUR
     *     MAGENTA_COLOUR
     *     CYAN_COLOUR
     *     WHITE_COLOUR
     *     TRANSPARENT_COLOUR
     *
     *     Amiga only:
     *
     *     LIGHTGREY_COLOUR
     *     MEDIUMGREY_COLOUR
     *     DARKGREY_COLOUR
     *
     * There may be more colours in the range from 16 to 255; see the
     * remarks about os_peek_colour.
     *
     */
    public static void SetColor(int newfg, int newbg)
    {
        current_fg = frotz_to_dumb[newfg];
        current_bg = frotz_to_dumb[newbg];
    }

    /*
     * os_reset_screen
     *
     * Reset the screen before the program ends.
     *
     */
    public static void ResetScreen()
    {
        dumb_show_screen(false);
    }
    /*
     * os_beep
     *
     * Play a beep sound. Ideally, the sound should be high- (number == 1)
     * or low-pitched (number == 2).
     *
     */
    public static void Beep(int volume)
    {
        if (visual_bell)
        {
            Console.Out.Write("[{0}-PITCHED BEEP]\n", (volume == 1) ? "HIGH" : "LOW");
        }
        else
        {
            if (OperatingSystem.IsWindows())
            {
                if (volume == 1)
                {
                    Console.Beep(800, 350);
                }
                else
                {
                    Console.Beep(392, 350);
                }
            }
            else
            {
                putchar('\a'); /* so much for dumb.  */
            }
        }
    }

    /*
     * os_set_font
     *
     * Set the font for text output. The interpreter takes care not to
     * choose fonts which aren't supported by the interface.
     *
     */
    public static void SetFont(int newFont)
    {
    }

    /*
     * os_prepare_sample
     *
     * Load the given sample from the disk.
     *
     */
    public static void PrepareSample(int number)
    {
    }

    /*
     * os_finish_with_sample
     *
     * Remove the current sample from memory (if any).
     *
     */
    public static void FinishWithSample(int number)
    {
    }

    /*
     * os_start_sample
     *
     * Play the given sample at the given volume (ranging from 1 to 8 and
     * 255 meaning a default volume). The sound is played once or several
     * times in the background (255 meaning forever). The end_of_sound
     * function is called as soon as the sound finishes, passing in the
     * eos argument.
     *
     */
    public static void StartSample(int number, int volume, int repeats, zword eos)
    {
    }

    /*
     * os_stop_sample
     *
     * Turn off the current sample.
     *
     */
    public static void StopSample(int number)
    {
    }

    /*
     * os_check_unicode
     *
     * Return with bit 0 set if the Unicode character can be
     * displayed, and bit 1 if it can be input.
     * 
     *
     */
    public static zword CheckUnicode(int font, zword c)
    {
        /* Only UTF-8 output, no input yet.  */
        return 1;
    }

    /*
     * os_char_width
     *
     * Return the length of the character in screen units.
     *
     */
    public static int CharWidth(zword z)
    {
        if (plain_ascii && z >= ZC_LATIN1_MIN)
        {
            int pb = 4 * (z - ZC_LATIN1_MIN);
            int pe = latin1_to_ascii.IndexOf(' ', pb);
            return pe - pb;
        }
        return 1;
    }

    /*
     * os_string_width
     *
     * Calculate the length of a word in screen units. Apart from letters,
     * the word may contain special codes:
     *
     *    ZC_NEW_STYLE - next character is a new text style
     *    ZC_NEW_FONT  - next character is a new font
     *
     */
    public static int StringWidth(ReadOnlySpan<zword> s)
    {
        int width = 0;
        zword c;
        int i = 0;
        while ((c = s[i++]) != 0)
        {
            if (c == ZC_NEW_STYLE || c == ZC_NEW_FONT)
                i++;
            else
                width += CharWidth(c);
        }
        return width;
    }

    /*
     * os_set_cursor
     *
     * Place the text cursor at the given coordinates. Top left is (1,1).
     *
     */
    public static void SetCursor(int row, int col)
    {
        cursor_row = row - 1; cursor_col = col - 1;
        if (cursor_row >= Main.h_screen_rows)
            cursor_row = Main.h_screen_rows - 1;
    }

    /*
     bool os_repaint_window(int UNUSED(win), int UNUSED(ypos_old),
			int UNUSED(ypos_new), int UNUSED(xpos),
			int UNUSED(ysize), int UNUSED(xsize))
    {
	    return false;
    }
     */

    /*
     * os_set_text_style
     *
     * Set the current text style. Following flags can be set:
     *
     *     REVERSE_STYLE
     *     BOLDFACE_STYLE
     *     EMPHASIS_STYLE (aka underline aka italics)
     *     FIXED_WIDTH_STYLE
     *
     */
    public static void SetTextStyle(int newStyle)
    {
        current_style = newStyle;
    }

    /*
     * os_from_true_culour
     *
     * Given a true colour, return an appropriate colour index.
     *
     */
    public static zword FromTrueColor(zword colour) => TrueColorStuff.GetColourIndex(TrueColorStuff.RGB5ToTrue(colour));

    /*
     * os_to_true_colour
     *
     * Given a colour index, return the appropriate true colour.
     *
     */
    public static zword ToTrueColor(int index) => TrueColorStuff.TrueToRGB5(TrueColorStuff.GetColor(index));

}
