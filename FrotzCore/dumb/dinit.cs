
using Frotz.Constants;

using System;
using System.IO;

namespace Frotz
{

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
            var r = new System.Random();
            return r.Next() & 32767;
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
}