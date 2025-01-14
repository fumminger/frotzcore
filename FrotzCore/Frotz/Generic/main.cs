/* main.c - Frotz V2.40 main function
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

/*
 * This is an interpreter for Infocom V1 to V6 games. It also supports
 * the recently defined V7 and V8 games. Please report bugs to
 *
 *    s.jokisch@avu.de
 *
 */

namespace Frotz.Generic;

public static class Main
{
    public static string? StoryName { get; internal set; } = null;

    internal static MemoryOwner<byte>? StoryData = null;

    internal static Story StoryId = Story.UNKNOWN;
    internal static long StorySize = 0;

    ///* Story file header data */

    internal static zbyte h_version = 0;
    internal static zbyte h_config = 0;
    internal static zword h_release = 0;
    internal static zword h_resident_size = 0;
    internal static zword h_start_pc = 0;
    internal static zword h_dictionary = 0;
    internal static zword h_objects = 0;
    internal static zword h_globals = 0;
    internal static zword h_dynamic_size = 0;
    internal static zword h_flags = 0;
    internal static readonly zbyte[] h_serial = new zbyte[6];
    internal static zword h_abbreviations = 0;
    internal static zword h_file_size = 0;
    internal static zword h_checksum = 0;
    internal static zbyte h_interpreter_number = 0;
    internal static zbyte h_interpreter_version = 0;
    internal static zbyte h_screen_rows = 0;
    internal static zbyte h_screen_cols = 0;
    internal static zword h_screen_width = 0;
    internal static zword h_screen_height = 0;
    internal static zbyte h_font_height = 1;
    internal static zbyte h_font_width = 1;
    internal static zword h_functions_offset = 0;
    internal static zword h_strings_offset = 0;
    internal static zbyte h_default_background = 0;
    internal static zbyte h_default_foreground = 0;
    internal static zword h_terminating_keys = 0;
    internal static zword h_line_width = 0;
    internal static zbyte h_standard_high = 1;
    internal static zbyte h_standard_low = 1;
    internal static zword h_alphabet = 0;
    internal static zword h_extension_table = 0;
    internal static readonly zbyte[] h_user_name = new zbyte[8];

    internal static zword hx_table_size = 0;
    internal static zword hx_mouse_x = 0;
    internal static zword hx_mouse_y = 0;
    internal static zword hx_unicode_table = 0;
    internal static zword hx_flags = 0;
    internal static zword hx_fore_colour = 0;
    internal static zword hx_back_colour = 0;

    /* Stack data */

    internal static zword[] Stack = new zword[General.STACK_SIZE];
    internal static int sp = 0;
    internal static int fp = 0;
    internal static zword frame_count = 0;

    /* IO streams */

    internal static bool ostream_screen = true;
    internal static bool ostream_script = false;
    internal static bool ostream_memory = false;
    internal static bool ostream_record = false;
    internal static bool istream_replay = false;
    internal static bool message = false;

    /* Current window and mouse data */

    internal static zword cwin = 0;
    internal static int mwin = 0;

    internal static zword MouseY = 0;
    internal static zword MouseX = 0;
    internal static zword menu_selected = 0;

    /* Window attributes */

    internal static bool enable_wrapping = false;
    internal static bool enable_scripting = false;
    internal static bool enable_scrolling = false;
    internal static bool enable_buffering = false;

    /* User options */

    public static bool option_attribute_assignment = false;
    public static bool option_attribute_testing = false;
    public static zword option_context_lines = 0;
    public static bool option_object_locating = false;
    public static bool option_object_movement = false;
    public static zword option_left_margin = 0;
    public static zword option_right_margin = 0;
    public static bool option_ignore_errors = false;
    public static bool option_piracy = false;
    public static int option_undo_slots = General.MAX_UNDO_SLOTS;
    public static bool option_expand_abbreviations = false;
    public static int option_script_cols = 80;
    public static bool option_save_quetzal = true;
    public static bool option_sound = true;
    //' internal static char *option_zcode_path;

    // TODO This is to allow the game to "cleanly" exit instead of Thread.Abort
    public static bool AbortGameLoop = false;

    /* Size of memory to reserve (in bytes) */

    internal static long reserve_mem = 0;

    /*
     * z_piracy, branch if the story file is a legal copy.
     *
     *	no zargs used
     *
     */

    internal static void ZPiracy()
    {
        OS.Fail("hit piracy message");

        Process.Branch(option_piracy == false);
    }/* z_piracy */

    /*
     * main
     *
     * Prepare and run the game.
     *
     */

    public static int MainFunc(ReadOnlySpan<string> args)
    {
        if (OS.ProcessArguments(args))
        {
            Buffer.InitBuffer();

            Err.InitErr();

            FastMem.InitMemory();

            Process.InitProcess();

            Sound.InitSound();

            Text.InitText();

            OS.InitScreen();

            FastMem.InitUndo();

            FastMem.ZRestart();

            OS.GameStarted(); // New function; Called to allow the screen to know info about the game

            Process.Interpret();

            FastMem.ResetMemory();

            //OS.ResetScreen();
        }
        return 0;

    }/* main */
}
