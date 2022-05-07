/* process.c - Interpreter loop and program control
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

    internal static class Process
    {
        internal static readonly zword[] zargs = new zword[8];
        internal static int zargc;

        internal static int finished = 0;

        public delegate void ZInstruction();

        internal static readonly ZInstruction[] op0_opcodes = new ZInstruction[]
        {
            new(ZRTrue),
            new(ZRFalse),
            new(Text.ZPrint),
            new(Text.ZPrintRet),
            new(ZNoop),
            new(FastMem.ZSave),
            new(FastMem.ZRestore),
            new(FastMem.ZRestart),
            new(ZRetPopped),
            new(ZCatch),
            new(ZQuit),
            new(Text.ZNewLine),
            new(Screen.ZShowStatus),
            new(FastMem.ZVerify), // Not Tested or Implemented
            new(__extended__),
        };

        internal static readonly ZInstruction[] op1_opcodes = new ZInstruction[]
        {
            new(Text.ZPrintAddr),
            new(ZCallS),
            new(Text.ZPrintObj),
            new(ZRet),
            new(ZJump),
            new(Text.ZPrintPaddr),
            new(ZCallN),
        };

        internal static readonly ZInstruction[] var_opcodes = new ZInstruction[]
        {
            new(__illegal__),
            new(ZCallS),
            new(ZCallN),
            new(Screen.ZSetColor),
            new(ZThrow),
            new(__illegal__),
            new(__illegal__),
            new(__illegal__),
            new(ZCallS),
            new(Input.ZRead),
            new(Text.ZPrintChar),
            new(Text.ZPrintNum),
            new(Screen.ZSplitWindow),
            new(Screen.ZSetWindow),
            new(ZCallS),
            new(Screen.ZEraseWindow),
            new(Screen.ZEraseLine),
            new(Screen.ZSetCursor),
            new(Screen.ZGetCursor),
            new(Screen.ZSetTextStyle),
            new(Screen.ZBufferMode),
            new(Stream.ZOutputStream),
            new(Stream.ZIputStream),
            new(Input.ZReadChar),
            new(ZCallN),
            new(ZCallN),
            new(Text.ZTokenise),
            new(Text.ZEncodeText),
            new(Screen.ZPrintTable),
            new(ZCheckArgCount),
        };

        internal static readonly ZInstruction[] ext_opcodes = new ZInstruction[]
        {
            new(FastMem.ZSave),
            new(FastMem.ZRestore),
            new(Screen.ZSetFont),
            new(Screen.ZDrawPicture),
            new(Screen.ZPictureData),
            new(Screen.ZErasePicture),
            new(Screen.ZSetMargins),
            new(FastMem.ZSaveUndo),
            new(FastMem.ZRestoreUndo),//    z_restore_undo, // 10
            new(Text.ZPrintUnicode),
            new(Text.ZCheckUnicode),
            new(Screen.ZSetTrueColor),	/* spec 1.1 */
            new(__illegal__),
            new(__illegal__),
            new(Screen.ZMoveWindow),
            new(Screen.ZWindowSize),
            new(Screen.ZWindowStyle),
            new(Screen.ZGetWindProp),
            new(Screen.ZScrollWindow), // 20
            new(Input.ZReadMouse),//    z_read_mouse,
            new(Screen.ZMouseWindow),
            new(Screen.ZPutWindProp),
            new(Text.ZPrintForm),//    z_print_form,
            new(Input.ZMakeMenu),//    z_make_menu,
            new(Screen.ZPictureTable),
            new(Screen.ZBufferScreen),   /* spec 1.1 */
        };
        private static int invokeCount = 0;
        private static void PrivateInvoke(ZInstruction instruction, string array, int index, int opcode)
        {
            instruction.Invoke();
            invokeCount++;
        }

        /*
         * init_process
         *
         * Initialize process variables.
         *
         */

        internal static void InitProcess() => finished = 0;

        /*
         * load_operand
         *
         * Load an operand, either a variable or a constant.
         *
         */

        private static void LoadOperand(zbyte type)
        {
            zword value;

            if ((type & 2) > 0)
            {           /* variable */

                FastMem.CodeByte(out zbyte variable);


            }
            else if ((type & 1) > 0)
            {       /* small constant */

                FastMem.CodeByte(out zbyte bvalue);
                value = bvalue;
            }
            else
            {
                FastMem.CodeWord(out value);      /* large constant */
            }


        }/* load_operand */

        /*
         * load_all_operands
         *
         * Given the operand specifier byte, load all (up to four) operands
         * for a VAR or EXT opcode.
         *
         */

        internal static void LoadAllOperands(zbyte specifier)
        {
            int i;

            for (i = 6; i >= 0; i -= 2)
            {

                zbyte type = (zbyte)((specifier >> i) & 0x03); // TODO Check this conversion

                if (type == 3)
                    break;

                LoadOperand(type);

            }

        }/* load_all_operands */

        /*
         * interpret
         *
         * Z-code interpreter main loop
         *
         */

        internal static void Interpret()
        {
            do
            {
                FastMem.CodeByte(out zbyte opcode);


                zargc = 0;
                if (opcode < 0x80)
                {           /* 2OP opcodes */
                    LoadOperand((zbyte)((opcode & 0x40) > 0 ? 2 : 1));
                    LoadOperand((zbyte)((opcode & 0x20) > 0 ? 2 : 1));

                    PrivateInvoke(var_opcodes[opcode & 0x1f], "2OP", (opcode & 0x1f), opcode);
                }
                else if (opcode < 0xb0)
                {   /* 1OP opcodes */
                    LoadOperand((zbyte)(opcode >> 4));
                    PrivateInvoke(op1_opcodes[opcode & 0x0f], "1OP", (opcode & 0x0f), opcode);
                }
                else if (opcode < 0xc0)
                {   /* 0OP opcodes */
                    PrivateInvoke(op0_opcodes[opcode - 0xb0], "0OP", (opcode - 0xb0), opcode);
                }
                else
                {   /* VAR opcodes */
                    zbyte specifier1;

                    if (opcode is 0xec or 0xfa)
                    {   /* opcodes 0xec */
                        FastMem.CodeByte(out specifier1);                  /* and 0xfa are */
                        FastMem.CodeByte(out zbyte specifier2);                  /* call opcodes */
                        LoadAllOperands(specifier1);        /* with up to 8 */
                        LoadAllOperands(specifier2);         /* arguments    */
                    }
                    else
                    {
                        FastMem.CodeByte(out specifier1);
                        LoadAllOperands(specifier1);
                    }

                    PrivateInvoke(var_opcodes[opcode - 0xc0], "VAR", (opcode - 0xc0), opcode);
                }

                OS.Tick();
            } while (finished == 0);

            finished--;
        }/* interpret */

        /*
         * call
         *
         * Call a subroutine. Save PC and FP then load new PC and initialise
         * new stack frame. Note that the caller may legally provide less or
         * more arguments than the function actually has. The call type "ct"
         * can be 0 (z_call_s), 1 (z_call_n) or 2 (direct call).
         *
         */
        internal static void Call(zword routine, int argc, int args_offset, int ct)
        {
        
        }/* call */

        /*
         * ret
         *
         * Return from the current subroutine and restore the previous stack
         * frame. The result may be stored (0), thrown away (1) or pushed on
         * the stack (2). In the latter case a direct call has been finished
         * and we must exit the interpreter loop.
         *
         */

        internal static void Ret(zword value)
        {
         

        }/* ret */

        /*
         * branch
         *
         * Take a jump after an instruction based on the flag, either true or
         * false. The branch can be short or long; it is encoded in one or two
         * bytes respectively. When bit 7 of the first byte is set, the jump
         * takes place if the flag is true; otherwise it is taken if the flag
         * is false. When bit 6 of the first byte is set, the branch is short;
         * otherwise it is long. The offset occupies the bottom 6 bits of the
         * first byte plus all the bits in the second byte for long branches.
         * Uniquely, an offset of 0 means return false, and an offset of 1 is
         * return true.
         *
         */
        internal static void Branch(bool flag)
        {
            FastMem.CodeByte(out zbyte specifier);

            zbyte off1 = (zbyte)(specifier & 0x3f);

            if (!flag)
                specifier ^= 0x80;

            zword offset;
            if ((specifier & 0x40) == 0)
            { // if (!(specifier & 0x40)) {		/* it's a long branch */

                if ((off1 & 0x20) > 0)      /* propagate sign bit */
                    off1 |= 0xc0;

                FastMem.CodeByte(out zbyte off2);

                offset = (zword)((off1 << 8) | off2);
            }
            else
            {
                offset = off1;        /* it's a short branch */
            }

            if ((specifier & 0x80) > 0)
            {

                if (offset > 1)
                {       /* normal branch */
                    FastMem.GetPc(out long pc);
                    pc += (short)offset - 2;
                    FastMem.SetPc(pc);
                }
                else
                {
                    Ret(offset);      /* special case, return 0 or 1 */
                }
            }
        }/* branch */

        /*
         * store
         *
         * Store an operand, either as a variable or pushed on the stack.
         *
         */
        internal static void Store(zword value)
        {


        }/* store */

        /*
         * direct_call
         *
         * Call the interpreter loop directly. This is necessary when
         *
         * - a sound effect has been finished
         * - a read instruction has timed out
         * - a newline countdown has hit zero
         *
         * The interpreter returns the result value on the stack.
         *
         */
        internal static int DirectCall(zword addr)
        {
            Span<zword> saved_zargs = stackalloc zword[8];
            int saved_zargc;
            int i;

            /* Calls to address 0 return false */

            if (addr == 0)
                return 0;

            /* Save operands and operand count */

            for (i = 0; i < 8; i++)
                saved_zargs[i] = zargs[i];

            saved_zargc = zargc;

            /* Call routine directly */

            Call(addr, 0, 0, 2);

            /* Restore operands and operand count */

            for (i = 0; i < 8; i++)
                zargs[i] = saved_zargs[i];

            zargc = saved_zargc;

            /* Resulting value lies on top of the stack */

            return 0;

        }/* direct_call */

        /*
         * __extended__
         *
         * Load and execute an extended opcode.
         *
         */

        private static void __extended__()
        {
            FastMem.CodeByte(out zbyte opcode);
            FastMem.CodeByte(out zbyte specifier);

            LoadAllOperands(specifier);

            if (opcode < 0x1e)          /* extended opcodes from 0x1e on */
                // ext_opcodes[opcode] ();		/* are reserved for future spec' */
                PrivateInvoke(ext_opcodes[opcode], "Extended", opcode, opcode);

        }/* __extended__ */

        /*
         * __illegal__
         *
         * Exit game because an unknown opcode has been hit.
         *
         */

        private static void __illegal__() => Err.RuntimeError(ErrorCodes.ERR_ILL_OPCODE);/* __illegal__ */

        /*
         * z_catch, store the current stack frame for later use with z_throw.
         *
         *	no zargs used
         *
         */

        internal static void ZCatch() { }
        /*
         * z_throw, go back to the given stack frame and return the given value.
         *
         *	zargs[0] = value to return
         *	zargs[1] = stack frame
         *
         */

        internal static void ZThrow()
        {
     
            Ret(zargs[0]);

        }/* z_throw */

        /*
         * z_call_n, call a subroutine and discard its result.
         *
         * 	zargs[0] = packed address of subroutine
         *	zargs[1] = first argument (optional)
         *	...
         *	zargs[7] = seventh argument (optional)
         *
         */

        internal static void ZCallN()
        {

            if (Process.zargs[0] != 0)
                Process.Call(zargs[0], zargc - 1, 1, 1);

        }/* z_call_n */

        /*
         * z_call_s, call a subroutine and store its result.
         *
         * 	zargs[0] = packed address of subroutine
         *	zargs[1] = first argument (optional)
         *	...
         *	zargs[7] = seventh argument (optional)
         *
         */

        internal static void ZCallS()
        {

            if (zargs[0] != 0)
                Call(zargs[0], zargc - 1, 1, 0); // TODO Was "call (zargs[0], zargc - 1, zargs + 1, 0);"
            else
                Store(0);

        }/* z_call_s */

        /*
         * z_check_arg_count, branch if subroutine was called with >= n arg's.
         *
         * 	zargs[0] = number of arguments
         *
         */

        internal static void ZCheckArgCount()
        {

 
        }/* z_check_arg_count */

        /*
         * z_jump, jump unconditionally to the given address.
         *
         *	zargs[0] = PC relative address
         *
         */

        internal static void ZJump()
        {


        }/* z_jump */

        /*
         * z_nop, no operation.
         *
         *	no zargs used
         *
         */

        internal static void ZNoop()
        {

            /* Do nothing */

        }/* z_nop */

        /*
         * z_quit, stop game and exit interpreter.
         *
         *	no zargs used
         *
         */

        internal static void ZQuit() => finished = 9999;/* z_quit */

        /*
         * z_ret, return from a subroutine with the given value.
         *
         *	zargs[0] = value to return
         *
         */

        internal static void ZRet() => Ret(zargs[0]);/* z_ret */

        /*
         * z_ret_popped, return from a subroutine with a value popped off the stack.
         *
         *	no zargs used
         *
         */

        internal static void ZRetPopped() {}// ret (*sp++);/* z_ret_popped */

        /*
         * z_rfalse, return from a subroutine with false (0).
         *
         * 	no zargs used
         *
         */

        internal static void ZRFalse() => Ret(0);/* z_rfalse */

        /*
         * z_rtrue, return from a subroutine with true (1).
         *
         * 	no zargs used
         *
         */

        internal static void ZRTrue() => Ret(1);/* z_rtrue */
    }
}