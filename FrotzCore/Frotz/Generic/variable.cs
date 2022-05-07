/* variable.c - Variable and stack related opcodes
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

using zword = System.UInt16;

namespace Frotz.Generic
{

    internal static class Variable
    {
        /*
         * z_dec, decrement a variable.
         *
         * 	zargs[0] = variable to decrement
         *
         */

        internal static void ZDec()
        {
            if (Process.zargs[0] == 0)
            {
                Main.Stack[Main.sp]--;
            }
            else if (Process.zargs[0] < 16)
            {
                Main.Stack[Main.fp - Process.zargs[0]]--;
            }
            else
            {
                zword addr = (zword)(Main.h_globals + 2 * (Process.zargs[0] - 16));
            }

        }/* z_dec */

        /*
         * z_dec_chk, decrement a variable and branch if now less than value.
         *
         * 	zargs[0] = variable to decrement
         * 	zargs[1] = value to check variable against
         *
         */

        internal static void ZDecChk()
        {
            zword value;

            if (Process.zargs[0] == 0)
            {
                value = --Main.Stack[Main.sp];
            }
            else if (Process.zargs[0] < 16)
            {
                value = --Main.Stack[Main.fp - Process.zargs[0]];
            }
            else
            {
                zword addr = (zword)(Main.h_globals + 2 * (Process.zargs[0] - 16));
                value = 0;
            }

            Process.Branch((short)value < (short)Process.zargs[1]);

        }/* z_dec_chk */

        /*
         * z_inc, increment a variable.
         *
         * 	zargs[0] = variable to increment
         *
         */

        internal static void ZInc()
        {
            if (Process.zargs[0] == 0)
            {
                Main.Stack[Main.sp]++; // (*sp)++;
            }
            else if (Process.zargs[0] < 16)
            {
                (Main.Stack[Main.fp - Process.zargs[0]])++;
            }
            else
            {
                zword addr = (zword)(Main.h_globals + 2 * (Process.zargs[0] - 16));
            }

        }/* z_inc */

        /*
         * z_inc_chk, increment a variable and branch if now greater than value.
         *
         * 	zargs[0] = variable to increment
         * 	zargs[1] = value to check variable against
         *
         */

        internal static void ZIncChk()
        {
            zword value;

            if (Process.zargs[0] == 0)
            {
                value = ++(Main.Stack[Main.sp]);
            }
            else if (Process.zargs[0] < 16)
            {
                value = ++(Main.Stack[Main.fp - Process.zargs[0]]);
            }
            else
            {
                zword addr = (zword)(Main.h_globals + 2 * (Process.zargs[0] - 16));
                value = 0;
            }

            Process.Branch((short)value > (short)Process.zargs[1]);

        }/* z_inc_chk */

        /*
         * z_load, store the value of a variable.
         *
         *	zargs[0] = variable to store
         *
         */

        internal static void ZLoad()
        {
            zword value = 0;

            if (Process.zargs[0] == 0)
            {
                value = Main.Stack[Main.sp];
            }
            else if (Process.zargs[0] < 16)
            {
                value = Main.Stack[Main.fp - Process.zargs[0]];
            }
            else
            {
                zword addr = (zword)(Main.h_globals + 2 * (Process.zargs[0] - 16));
            }

            Process.Store(value);

        }/* z_load */

        /*
         * z_pop, pop a value off the game stack and discard it.
         *
         *	no zargs used
         *
         */

        internal static void ZPop() => Main.sp++; /* z_pop */

        /*
         * z_pop_stack, pop n values off the game or user stack and discard them.
         *
         *	zargs[0] = number of values to discard
         *	zargs[1] = address of user stack (optional)
         *
         */

        internal static void ZPopStack()
        {

            if (Process.zargc == 2)
            {       /* it's a user stack */

                zword addr = Process.zargs[1];


            }
            else
            {
                Main.sp += Process.zargs[0];    /* it's the game stack */
            }
        }/* z_pop_stack */

        /*
         * z_pull, pop a value off...
         *
         * a) ...the game or a user stack and store it (V6)
         *
         *	zargs[0] = address of user stack (optional)
         *
         * b) ...the game stack and write it to a variable (other than V6)
         *
         *	zargs[0] = variable to write value to
         *
         */

        internal static void ZPull()
        {
            zword value;

            if (Main.h_version != ZMachine.V6)
            {   /* not a V6 game, pop stack and write */
                value = Main.Stack[Main.sp++];

                if (Process.zargs[0] == 0)
                {
                    Main.Stack[Main.sp] = value;
                }
                else if (Process.zargs[0] < 16)
                {
                    // *(fp - Process.zargs[0]) = value;
                    Main.Stack[Main.fp - Process.zargs[0]] = value;
                }
                else
                {

                }
            }
            else
            {   /* it's V6, but is there a user stack? */
                if (Process.zargc == 1)
                {   /* it's a user stack */
                    value = 0;
               
                }
                else
                {
                    value = Main.Stack[Main.sp++];// value = *sp++;	/* it's the game stack */
                }

                Process.Store(value);

            }

        }/* z_pull */

        /*
         * z_push, push a value onto the game stack.
         *
         *	zargs[0] = value to push onto the stack
         *
         */

        internal static void ZPush()
        {
            // *--sp = zargs[0];
            Main.Stack[--Main.sp] = Process.zargs[0];
        }/* z_push */

        /*
         * z_push_stack, push a value onto a user stack then branch if successful.
         *
         *	zargs[0] = value to push onto the stack
         *	zargs[1] = address of user stack
         *
         */

        internal static void ZPushStack()
        {
            zword addr = Process.zargs[1];



        }/* z_push_stack */

        /*
         * z_store, write a value to a variable.
         *
         * 	zargs[0] = variable to be written to
         *      zargs[1] = value to write
         *
         */

        internal static void ZStore()
        {
            zword value = Process.zargs[1];

            if (Process.zargs[0] == 0)
            {
                Main.Stack[Main.sp] = value;
            }
            else if (Process.zargs[0] < 16)
            {
                Main.Stack[Main.fp - Process.zargs[0]] = value;
            }
            else
            {
                zword addr = (zword)(Main.h_globals + 2 * (Process.zargs[0] - 16));

            }

        }/* z_store */
    }
}