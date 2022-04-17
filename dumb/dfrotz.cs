/*
 * dfrotz.h
 *
 * Frotz os functions for a standard C library and a dumb terminal.
 * Now you can finally play Zork Zero on your Teletype.
 *
 * Copyright 1997, 1998 Alembic Petrofsky <alembic@petrofsky.berkeley.ca.us>.
 * Any use permitted provided this notice stays intact.
 * 
 * Modified for C#
 * Copyright 2022 Frederick Umminger
 */

using Frotz.Generic;

//extern f_setup_t f_setup;

//extern bool do_more_prompts;
//extern bool quiet_mode;

public static class Dumb
{
    /* Handle input-related user settings and call dumb_output_handle_setting.  */

    public static bool dumb_handle_setting(string setting, bool show_cursor, bool startup)
{
	if (!strncmp(setting, "sf", 2)) {
		speed = atof(&setting[2]);
    printf("Speed Factor %g\n", speed);
} else if (!strncmp(setting, "mp", 2))
{
    toggle(&do_more_prompts, setting[2]);
    printf("More prompts %s\n", do_more_prompts ? "ON" : "OFF");
}
else
{
    if (!strcmp(setting, "set"))
    {
        printf("Speed Factor %g\n", speed);
        printf("More Prompts %s\n",
            do_more_prompts ? "ON" : "OFF");
    }
    return dumb_output_handle_setting(setting, show_cursor, startup);
}
return true;
}

    public static void dumb_init_input()
    {
        if ((z_header.version >= V4) && (speed != 0))
            z_header.config |= CONFIG_TIMEDINPUT;

        if (z_header.version >= V5)
            z_header.flags &= ~(MOUSE_FLAG | MENU_FLAG);
    }

    /* dumb-output.c */
    public static void dumb_init_output()
    {

        if (f_setup.format == FORMAT_IRC)
        {
            setvbuf(stdout, 0, _IONBF, 0);
            setvbuf(stderr, 0, _IONBF, 0);

            z_header.config |= CONFIG_COLOUR | CONFIG_BOLDFACE | CONFIG_EMPHASIS;

            memset(frotz_to_dumb, 256, DEFAULT_DUMB_COLOUR);
            frotz_to_dumb[BLACK_COLOUR] = 1;
            frotz_to_dumb[RED_COLOUR] = 4;
            frotz_to_dumb[GREEN_COLOUR] = 3;
            frotz_to_dumb[YELLOW_COLOUR] = 8;
            frotz_to_dumb[BLUE_COLOUR] = 12;
            frotz_to_dumb[MAGENTA_COLOUR] = 6;
            frotz_to_dumb[CYAN_COLOUR] = 11;
            frotz_to_dumb[WHITE_COLOUR] = 0;
            frotz_to_dumb[GREY_COLOUR] = 14;

            z_header.default_foreground = WHITE_COLOUR;
            z_header.default_background = BLACK_COLOUR;
        }
        else if (f_setup.format == FORMAT_ANSI)
        {
            setvbuf(stdout, 0, _IONBF, 0);
            setvbuf(stderr, 0, _IONBF, 0);

            z_header.config |= CONFIG_COLOUR | CONFIG_BOLDFACE | CONFIG_EMPHASIS;

            memset(frotz_to_dumb, 256, DEFAULT_DUMB_COLOUR);
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

            z_header.default_foreground = WHITE_COLOUR;
            z_header.default_background = BLACK_COLOUR;
        }
        else if (f_setup.format == FORMAT_BBCODE)
        {
            setvbuf(stdout, 0, _IONBF, 0);
            setvbuf(stderr, 0, _IONBF, 0);

            z_header.config |= CONFIG_COLOUR | CONFIG_BOLDFACE | CONFIG_EMPHASIS;

            memset(frotz_to_dumb, 256, DEFAULT_DUMB_COLOUR);
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

            z_header.default_foreground = BLACK_COLOUR;
            z_header.default_background = WHITE_COLOUR;
        }

        if (z_header.version == V3)
        {
            z_header.config |= CONFIG_SPLITSCREEN;
            z_header.flags &= ~OLD_SOUND_FLAG;
        }

        if (z_header.version >= V5)
        {
            z_header.flags &= ~SOUND_FLAG;
        }

        z_header.screen_height = z_header.screen_rows;
        z_header.screen_width = z_header.screen_cols;
        screen_cells = z_header.screen_rows * z_header.screen_cols;

        z_header.font_width = 1; z_header.font_height = 1;

        screen_data = malloc(screen_cells * sizeof(cell_t));
        screen_changes = malloc(screen_cells);
        os_erase_area(1, 1, z_header.screen_rows, z_header.screen_cols, -2);
        memset(screen_changes, 0, screen_cells);
    }
    
    public static bool dumb_output_handle_setting(string setting, bool show_cursor,
				bool startup)
        {

    char* p;
    int i;

	if (!strncmp(setting, "pb", 2)) {
		toggle(&show_pictures, setting[2]);
    printf("Picture outlines display %s\n", show_pictures? "ON" : "OFF");
		if (startup)
			return true;
		for (i = 0; i<screen_cells; i++)
			screen_changes[i] = (screen_data[i].style == PICTURE_STYLE);
		dumb_show_screen(show_cursor);
} else if (!strncmp(setting, "vb", 2))
{
    toggle(&visual_bell, setting[2]);
    printf("Visual bell %s\n", visual_bell ? "ON" : "OFF");
    os_beep(1); os_beep(2);
}
else if (!strncmp(setting, "ln", 2))
{
    toggle(&show_line_numbers, setting[2]);
    printf("Line numbering %s\n", show_line_numbers ? "ON" : "OFF");
}
else if (!strncmp(setting, "lt", 2))
{
    toggle(&show_line_types, setting[2]);
    printf("Line-type display %s\n", show_line_types ? "ON" : "OFF");
}
else if (*setting == 'c')
{
    switch (setting[1])
    {
        case 'm': compression_mode = COMPRESSION_MAX; break;
        case 's': compression_mode = COMPRESSION_SPANS; break;
        case 'n': compression_mode = COMPRESSION_NONE; break;
        case 'h': hide_lines = atoi(&setting[2]); break;
        default: return false;
    }
    printf("Compression mode %s, hiding top %d lines\n",
    compression_names[compression_mode], hide_lines);
}
else if (*setting == 'r')
{
    switch (setting[1])
    {
        case 'n': rv_mode = RV_NONE; break;
        case 'o': rv_mode = RV_DOUBLESTRIKE; break;
        case 'u': rv_mode = RV_UNDERLINE; break;
        case 'c': rv_mode = RV_CAPS; break;
        case 'b': strncpy(rv_blank_str, setting[2] ? &setting[2] : " ", 4); break;
        default: return false;
    }

    rv_blank_str[1] = 0;

    printf("Reverse-video mode %s, blanks reverse to '%s': ",
        rv_names[rv_mode], rv_blank_str);

    for (p = "sample reverse text"; *p; p++)
        show_cell(make_cell(REVERSE_STYLE, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, *p));
    putchar('\n');
    for (i = 0; i < screen_cells; i++)
        screen_changes[i] = (screen_data[i].style == REVERSE_STYLE);
    dumb_show_screen(show_cursor);
}
else if (!strcmp(setting, "set"))
{
    printf("Compression Mode %s, hiding top %d lines\n",
        compression_names[compression_mode], hide_lines);
    printf("Picture Boxes display %s\n", show_pictures ? "ON" : "OFF");
    printf("Visual Bell %s\n", visual_bell ? "ON" : "OFF");
    os_beep(1); os_beep(2);
    printf("Line Numbering %s\n", show_line_numbers ? "ON" : "OFF");
    printf("Line-Type display %s\n", show_line_types ? "ON" : "OFF");
    printf("Reverse-Video mode %s, Blanks reverse to '%s': ",
        rv_names[rv_mode], rv_blank_str);
    for (p = "sample reverse text"; *p; p++)
        show_cell(make_cell(REVERSE_STYLE, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, *p));
    putchar('\n');
}
else
    return false;
return true;
}

    /* Show the current screen contents, or what's changed since the last
     * call.
     *
     * If compressing, and show_cursor is true, and the cursor is past the
     * last nonblank character on the last line that would be shown, then
     * don't show that line (because it will be redundant with the prompt
     * line just below it).
     */
    public static void dumb_show_screen(bool show_cursor)
    {
        int r, c, first, last;
        char[] changed_rows = new char[0x100];

        /* Easy case */
        if (compression_mode == COMPRESSION_NONE)
        {
            for (r = hide_lines; r < z_header.screen_rows; r++)
                show_row(r);
            mark_all_unchanged();
            return;
        }

        /* Check which rows changed, and where the first and last change is. */
        first = last = -1;
        memset(changed_rows, 0, Main.h_screen_rows);
        for (r = hide_lines; r < z_header.screen_rows; r++)
        {
            for (c = 0; c < z_header.screen_cols; c++)
            {
                if (dumb_changes_row(r)[c] && !is_blank(dumb_row(r)[c]))
                    break;
            }

            changed_rows[r] = (c != z_header.screen_cols);
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
            for (c = cursor_col; c < z_header.screen_cols; c++)
            {
                if (!is_blank(dumb_row(last)[c]))
                    break;
            }
            if (c == z_header.screen_cols)
                last--;
        }

        /* Display the appropriate rows.  */
        if (compression_mode == COMPRESSION_MAX)
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

    /* Print the part of the cursor row before the cursor.  */
    public static void dumb_show_prompt(bool show_cursor, char line_type)
    {
        int i;
        show_line_prefix(show_cursor ? cursor_row : -1, line_type);
        if (show_cursor)
        {
            for (i = 0; i < cursor_col; i++)
                show_cell(dumb_row(cursor_row)[i]);
        }
    }

    /* Unconditionally show whole screen.  For \s user command.  */
    public static void dumb_dump_screen()
    {
        int r;
        for (r = 0; r < z_header.screen_height; r++)
            show_row(r);
    }

    public static void dumb_display_user_input(string s)
    {
        /* copy to screen without marking it as a change.  */
        while (*s)
            dumb_row(cursor_row)[cursor_col++] = make_cell(0, DEFAULT_DUMB_COLOUR, DEFAULT_DUMB_COLOUR, *s++);
    }
    public static void dumb_discard_old_input(int num_chars)
    {
        /* Weird discard stuff.  Grep spec for 'pain in my butt'.  */
        /* The old characters should be on the screen just before the cursor.
         * Erase them.  */
        cursor_col -= num_chars;

        if (cursor_col < 0)
            cursor_col = 0;
        os_erase_area(cursor_row + 1, cursor_col + 1,
        cursor_row + 1, cursor_col + num_chars, -1);
    }

    /* Called when it's time for a more prompt but user has them turned off.  */
    public static void dumb_elide_more_prompt()
    {
        dumb_show_screen(false);
        if (compression_mode == COMPRESSION_SPANS && hide_lines == 0)
        {
            show_row(-1);
        }
    }


    /*
 * Public functions just for the Dumb interface.
 */
    public static void dumb_set_picture_cell(int row, int col, zchar c)
    {
	    dumb_set_cell(row, col, make_cell(current_style | PICTURE_STYLE, current_fg, current_bg, c));
    }
    /* dumb-pic.c */
    public static bool dumb_init_pictures()
    {
        return false;
    }
}
