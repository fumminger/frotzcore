using Frotz.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Frotz;

public static partial class OS
{
    private static bool do_more_prompts = true; // WARNING : Set to true, not read from settings or config

    /////////////////////////////////////////////////////////////////////////////
    // Interface to the Frotz core
    /////////////////////////////////////////////////////////////////////////////


    /*
     * os_init_screen
     *
     * Initialise the IO interface. Prepare screen and other devices
     * (mouse, sound card). Set various OS depending story file header
     * entries:
     *
     *     h_config (aka flags 1)
     *     h_flags (aka flags 2)
     *     h_screen_cols (aka screen width in characters)
     *     h_screen_rows (aka screen height in lines)
     *     h_screen_width
     *     h_screen_height
     *     h_font_height (defaults to 1)
     *     h_font_width (defaults to 1)
     *     h_default_foreground
     *     h_default_background
     *     h_interpreter_number
     *     h_interpreter_version
     *     h_user_name (optional; not used by any game)
     *
     * Finally, set reserve_mem to the amount of memory (in bytes) that
     * should not be used for multiple undo and reserved for later use.
     *
     */
    public static void InitScreen()
    {
        // TODO Really need to clean this up

        Main.h_interpreter_number = 4;

        // Set the configuration
        if (Main.h_version == ZMachine.V3)
        {
            Main.h_config |= ZMachine.CONFIG_SPLITSCREEN;
            Main.h_config |= ZMachine.CONFIG_PROPORTIONAL;
            // TODO Set Tandy bit here if appropriate
        }
        if (Main.h_version >= ZMachine.V4)
        {
            Main.h_config |= ZMachine.CONFIG_BOLDFACE;
            Main.h_config |= ZMachine.CONFIG_EMPHASIS;
            Main.h_config |= ZMachine.CONFIG_FIXED;
            Main.h_config |= ZMachine.CONFIG_TIMEDINPUT;
        }
        if (Main.h_version >= ZMachine.V5)
        {
            Main.h_config |= ZMachine.CONFIG_COLOUR;
        }
        if (Main.h_version == ZMachine.V6)
        {
            if (BlorbFile != null)
            {
                Main.h_config |= ZMachine.CONFIG_PICTURES;
                Main.h_config |= ZMachine.CONFIG_SOUND;
            }
        }
        //theApp.CopyUsername();

        Main.h_interpreter_version = (byte)'F';
        if (Main.h_version == ZMachine.V6)
        {
            Main.h_default_background = ZColor.BLACK_COLOUR;
            Main.h_default_foreground = ZColor.WHITE_COLOUR;
            // TODO Get the defaults from the application itself
        }
        else
        {
            Main.h_default_foreground = 1;
            Main.h_default_background = 1;
        }

        // TODO Clear out the input queue incase a quit left characters

        // TODO Set font to be default fixed width font

        // TODO Make these numbers not be totally made up

        // BEGIN WARNING May be obsolete from dumb_init_*() functions
        Main.h_screen_width = 80*8;
        Main.h_screen_height = 25*8;

        Main.h_screen_cols = 80;
        Main.h_screen_rows = 25;

        Main.h_font_width = 8;
        Main.h_font_height = 8;
        // END WARNING 

        // Check for sound
        if ((Main.h_version == ZMachine.V3) && ((Main.h_flags & ZMachine.OLD_SOUND_FLAG) != 0))
        {
            // TODO Config sound here if appropriate
        }
        else if ((Main.h_version >= ZMachine.V4) && ((Main.h_flags & ZMachine.SOUND_FLAG) != 0))
        {
            // TODO Config sound here if appropriate
        }

        if (Main.h_version >= ZMachine.V5)
        {
            ushort mask = 0;
            if (Main.h_version == ZMachine.V6) mask |= ZMachine.TRANSPARENT_FLAG;

            // Mask out any unsupported bits in the extended flags
            Main.hx_flags &= mask;

            // TODO Set fore & back color here if apporpriate
            //  hx_fore_colour = 
            //  hx_back_colour = 
        }


        string name = Main.StoryName ?? "UNKNOWN";
        // Set default filenames

        FastMem.SaveName = $"{name}.sav";
        Files.ScriptName = $"{name}.log";
        Files.CommandName = $"{name}.rec";
        FastMem.AuxilaryName = $"{name}.aux";

        dumb_init_input();
        dumb_init_output();
        dumb_init_pictures();
    }

    /*
     * os_process_arguments
     *
     * Handle command line switches. Some variables may be set to activate
     * special features of Frotz:
     *
     *     option_attribute_assignment
     *     option_attribute_testing
     *     option_context_lines
     *     option_object_locating
     *     option_object_movement
     *     option_left_margin
     *     option_right_margin
     *     option_ignore_errors
     *     option_piracy
     *     option_undo_slots
     *     option_expand_abbreviations
     *     option_script_cols
     *
     * The global pointer "story_name" is set to the story file name.
     *
     */
    public static bool ProcessArguments(ReadOnlySpan<string> args)
    {
        // WARNING : Have not ported the Frotz code, most arguments are not handled

        Main.StoryData = null;

        if (args.Length == 0)
        {
            var file = SelectGameFile();
            if (!file.HasValue)
                return false;

            (Main.StoryName, Main.StoryData) = file.GetValueOrDefault();
        }
        else
        {
            Main.StoryName = args[0];
            using var fs = new FileStream(args[0], FileMode.Open);
            var data = new byte[fs.Length];
            fs.Read(data);
            Main.StoryData = data;
        }

        Err.ErrorReportMode = ErrorCodes.ERR_REPORT_NEVER;
        return true;
    }

    /*
     * os_random_seed
     *
     * Return an appropriate random seed value in the range from 0 to
     * 32767, possibly by using the current system time.
     *
     */
    public static int RandomSeed()
    {
        if (DebugState.IsActive)
        {
            return DebugState.RandomSeed();
        }
        else
        {
            var r = new System.Random();
            return r.Next() & 32767;
        }
    }

    /*
     * os_restart_game
     *
     * This routine allows the interface to interfere with the process of
     * restarting a game at various stages:
     *
     *     RESTART_BEGIN - restart has just begun
     *     RESTART_WPROP_SET - window properties have been initialised
     *     RESTART_END - restart is complete
     *
     */
    public static void RestartGame(int stage)
    {
        // Show Beyond Zork's title screen
        if ((stage == ZMachine.RESTART_BEGIN) && (Main.StoryId == Story.BEYOND_ZORK))
        {
            if (OS.PictureData(1, out int _, out int _))
            {
                OS.DrawPicture(1, 1, 1);
                OS.ReadKey(0, false);
            }
        }
    }


    /*
     * os_fatal
     *
     * Display error message and stop interpreter.
     *
     */
#pragma warning disable CS8763 // A method marked [DoesNotReturn] should not return.
    public static void Fatal(string message)
    {
        Console.WriteLine("Fatal Error");
        Console.WriteLine(message);
        throw new Exception(message);
    }
#pragma warning restore CS8763 // A method marked [DoesNotReturn] should not return.



}
