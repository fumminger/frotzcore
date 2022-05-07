/* fastmem.c - Memory related functions (fast version without virtual memory)
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
 * New undo mechanism added by Jim Dunleavy <jim.dunleavy@erha.ie>
 */

using Frotz.Constants;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using zbyte = System.Byte;
using zword = System.UInt16;

namespace Frotz.Generic
{

    internal struct RecordStruct
    {
        public Story StoryId;
        public zword Release;
        public string Serial;

        public RecordStruct(Story storyId, zword release, string serial)
        {
            StoryId = storyId;
            Release = release;
            Serial = serial;
        }
    }
    internal static class FastMem
    {
        private static readonly RecordStruct[] Records = {
        new(Story.SHERLOCK,  97, "871026"),
        new(Story.SHERLOCK,  21, "871214"),
        new(Story.SHERLOCK,  22, "880112"),
        new(Story.SHERLOCK,  26, "880127"),
        new(Story.SHERLOCK,   4, "880324"),
        new(Story.BEYOND_ZORK,   1, "870412"),
        new(Story.BEYOND_ZORK,   1, "870715"),
        new(Story.BEYOND_ZORK,  47, "870915"),
        new(Story.BEYOND_ZORK,  49, "870917"),
        new(Story.BEYOND_ZORK,  51, "870923"),
        new(Story.BEYOND_ZORK,  57, "871221"),
        new(Story.BEYOND_ZORK,  60, "880610"),
        new(Story.ZORK_ZERO,   0, "870831"),
        new(Story.ZORK_ZERO,  96, "880224"),
        new(Story.ZORK_ZERO, 153, "880510"),
        new(Story.ZORK_ZERO, 242, "880830"),
        new(Story.ZORK_ZERO, 242, "880901"),
        new(Story.ZORK_ZERO, 296, "881019"),
        new(Story.ZORK_ZERO, 366, "890323"),
        new(Story.ZORK_ZERO, 383, "890602"),
        new(Story.ZORK_ZERO, 387, "890612"),
        new(Story.ZORK_ZERO, 392, "890714"),
        new(Story.ZORK_ZERO, 393, "890714"),
        new(Story.SHOGUN, 295, "890321"),
        new(Story.SHOGUN, 292, "890314"),
        new(Story.SHOGUN, 311, "890510"),
        new(Story.SHOGUN, 320, "890627"),
        new(Story.SHOGUN, 321, "890629"),
        new(Story.SHOGUN, 322, "890706"),
        new(Story.ARTHUR,  40, "890502"),
        new(Story.ARTHUR,  41, "890504"),
        new(Story.ARTHUR,  54, "890606"),
        new(Story.ARTHUR,  63, "890622"),
        new(Story.ARTHUR,  74, "890714"),
        new(Story.JOURNEY,  46, "880603"),
        new(Story.JOURNEY,   2, "890303"),
        new(Story.JOURNEY,  26, "890316"),
        new(Story.JOURNEY,  30, "890322"),
        new(Story.JOURNEY,  51, "890522"),
        new(Story.JOURNEY,  54, "890526"),
        new(Story.JOURNEY,  77, "890616"),
        new(Story.JOURNEY,  79, "890627"),
        new(Story.JOURNEY,  83, "890706"),
        new(Story.LURKING_HORROR, 203, "870506"),
        new(Story.LURKING_HORROR, 219, "870912"),
        new(Story.LURKING_HORROR, 221, "870918"),
        new(Story.AMFV,  47, "850313"),
        new(Story.UNKNOWN,   0, "------")
    };

        internal static string SaveName = General.DEFAULT_SAVE_NAME;
        internal static string AuxilaryName = General.DEFAULT_AUXILARY_NAME;

        internal static zbyte[] ZMData = Array.Empty<zbyte>();
        internal static zword ZMData_checksum = 0;

        internal static long Zmp = 0;
        internal static long Pcp = 0;

        private static System.IO.Stream? StoryFp = null;
        private static bool FirstRestart = true;
        private static long InitFpPos = 0;

        #region zmp & pcp

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte Lo(zword v) => (byte)(v & 0xff);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte Hi(zword v) => (byte)(v >> 8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetWord(int addr, zword v)
        {
            BinaryPrimitives.WriteUInt16BigEndian(ZMData.AsSpan(addr, 2), v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LowWord(int addr, out zword v)
            => v = BinaryPrimitives.ReadUInt16BigEndian(ZMData.AsSpan(addr, 2));

        // TODO I'm suprised that they return the same thing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void HighWord(int addr, out zword v)
            => LowWord(addr, out v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CodeWord(out zword v)
        {
            LowWord((int)Pcp, out v);
            Pcp += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetByte(int addr, byte v)
        {
            ZMData[addr] = v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CodeByte(out zbyte v) => v = ZMData[Pcp++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LowByte(int addr, out zbyte v) => v = ZMData[addr];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GetPc(out long v) => v = Pcp - Zmp;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetPc(long v) => Pcp = Zmp + v;

        #endregion

        /*
         * Data for the undo mechanism.
         * This undo mechanism is based on the scheme used in Evin Robertson's
         * Nitfol interpreter.
         * Undo blocks are stored as differences between states.
         */

        //typedef struct undo_struct undo_t;

        internal struct UndoStruct
        {
            public long Pc;
            public long DiffSize;
            public zword FrameCount;
            public zword StackSize;
            public zword FrameOffset;
            public int Sp;
            public zword[] Stack;
            public byte[] UndoData;
 
       
            public UndoStruct(long pc, long diffSize, zword frameCount, zword stackSize, zword frameOffset, int sp,
            zword[] stack, byte[] undoData)
            {
                Pc = pc;
                DiffSize = diffSize;
                FrameCount = frameCount;
                StackSize = stackSize;
                FrameOffset = frameOffset;
                Sp = sp;
                Stack = stack;
                UndoData = undoData;
            }

        }

        // static undo_struct first_undo = null, last_undo = null, curr_undo = null;
        //static zbyte *undo_mem = NULL, *prev_zmp, *undo_diff;

        private static zbyte[] PrevZmp = new zbyte[0];
        private static zbyte[] UndoDiff = new zbyte[0];
        private static readonly List<UndoStruct> UndoMem = new();
        private static int UndoCount => UndoMem.Count;

        /*
         * get_header_extension
         *
         * Read a value from the header extension (former mouse table).
         *
         */

        internal static zword GetHeaderExtension(int entry)
        {
            return 0;

        }/* get_header_extension */

        /*
         * set_header_extension
         *
         * Set an entry in the header extension (former mouse table).
         *
         */

        internal static void SetHeaderExtension(int entry, zword val)
        {

        }/* set_header_extension */

        /*
         * restart_header
         *
         * Set all header fields which hold information about the interpreter.
         *
         */
        internal static void RestartHeader()
        {
 
        }/* restart_header */

        /*
         * init_memory
         *
         * Allocate memory and load the story file.
         *
         */

        internal static void InitMemory()
        {
  
        }/* init_memory */

        /// <summary>
        ///  Allocate memory for multiple undo. It is important not to occupy
        ///  all the memory available, since the IO interface may need memory
        ///  during the game, e.g. for loading sounds or pictures.
        /// </summary>
        internal static void InitUndo()
        {
        }

        /// <summary>
        /// Free count undo blocks from the beginning of the undo list.
        /// </summary>
        /// <param name="count"></param>
        internal static void FreeUndo(int count)
        {
            for (int i = 0; i < count; i++)
            {
                UndoMem.RemoveAt(0);
            }

        }

        /// <summary>
        /// Close the story file and deallocate memory.
        /// </summary>
        internal static void ResetMemory()
        {
            StoryFp?.Dispose();
            UndoMem.Clear();
        }

        /*
         * storeb
         *
         * Write a byte value to the dynamic Z-machine memory.
         *
         */

        internal static void StoreB(zword addr, zbyte value)
        {
 

        }/* storeb */

        /*
         * storew
         *
         * Write a word value to the dynamic Z-machine memory.
         *
         */

        internal static void StoreW(zword addr, zword value)
        {
            StoreB((zword)(addr + 0), Hi(value));
            StoreB((zword)(addr + 1), Lo(value));
        }/* storew */

        /*
         * z_restart, re-load dynamic area, clear the stack and set the PC.
         *
         * 	no zargs used
         *
         */
        internal static void ZRestart()
        {
  

        }/* z_restart */

        /*
         * get_default_name
         *
         * Read a default file name from the memory of the Z-machine and
         * copy it to a string.
         *
         */

        internal static string? GetDefaultName(zword addr)
        {
            if (addr != 0)
            {

                var vsb = new StringBuilder();

                int i;

                LowByte(addr, out zbyte len);
                addr++;

                for (i = 0; i < len; i++)
                {
                    LowByte(addr, out zbyte c);
                    addr++;

                    if (c is >= (zbyte)'A' and <= (zbyte)'Z')
                        c += 'a' - 'A';

                    // default_name[i] = c;
                    vsb.Append((char)c);

                }

                // default_name[i] = 0;
                if (vsb.ToString().IndexOf('.') == -1)
                {
                    vsb.Append(".AUX");
                    return vsb.ToString();
                }
                else
                {
                    return AuxilaryName;
                }
            }
            return null;

        }/* get_default_name */

        /*
         * z_restore, restore [a part of] a Z-machine state from disk
         *
         *	zargs[0] = address of area to restore (optional)
         *	zargs[1] = number of bytes to restore
         *	zargs[2] = address of suggested file name
         *	zargs[3] = whether to ask for confirmation of the file name
         *
         */

        internal static void ZRestore()
        {
     
        }/* z_restore */

        /// <summary>
        ///   Set diff to a Quetzal-like difference between a and b,
        ///   copying a to b as we go.  It is assumed that diff points to a
        ///   buffer which is large enough to hold the diff.
        ///   mem_size is the number of bytes to compare.
        ///   Returns the number of bytes copied to diff.
        /// </summary>
        private static int MemDiff(ReadOnlySpan<zbyte> a, Span<zbyte> b, zword mem_size, Span<zbyte> diff)
        {
            zword size = mem_size;
            int dPtr = 0;
            uint j;
            zbyte c = 0;

            int aPtr = 0;
            int bPtr = 0;

            for (; ; )
            {
                for (j = 0; size > 0 && (c = (zbyte)(a[aPtr++] ^ b[bPtr++])) == 0; j++)
                    size--;
                if (size == 0) break;

                size--;

                if (j > 0x8000)
                {
                    diff[dPtr++] = 0;
                    diff[dPtr++] = 0xff;
                    diff[dPtr++] = 0xff;
                    j -= 0x8000;
                }

                if (j > 0)
                {
                    diff[dPtr++] = 0;
                    j--;

                    if (j <= 0x7f)
                    {
                        diff[dPtr++] = (byte)j;
                    }
                    else
                    {
                        diff[dPtr++] = (byte)((j & 0x7f) | 0x80);
                        diff[dPtr++] = (byte)((j & 0x7f80) >> 7);
                    }
                }
                diff[dPtr++] = c;
                b[bPtr - 1] ^= c;
            }
            return dPtr;

        }

        /// <summary>
        /// Applies a quetzal-like diff to dest
        /// </summary>
        private static void MemUndiff(ReadOnlySpan<zbyte> diff, long diffLength, Span<zbyte> dest)
        {
            zbyte c;
            int diffPtr = 0;
            int destPtr = 0;

            while (diffLength > 0)
            {
                c = diff[diffPtr++];
                diffLength--;
                if (c == 0)
                {
                    uint runlen;

                    if (diffLength == 0) // TODO I'm not sure about this logic
                        return;  /* Incomplete run */
                    runlen = diff[diffPtr++];
                    diffLength--;
                    if ((runlen & 0x80) > 0)
                    {
                        if (diffLength == 0)
                            return; /* Incomplete extended run */
                        c = diff[diffPtr++];
                        diffLength--;
                        runlen = (runlen & 0x7f) | (((uint)c) << 7);
                    }

                    destPtr += (int)runlen + 1;
                }
                else
                {
                    dest[destPtr++] ^= c;
                }
            }

        }

        /*
         * restore_undo
         *
         * This function does the dirty work for z_restore_undo.
         *
         */

        internal static int RestoreUndo()
        {
            return 0;
        }/* restore_undo */

        /*
         * z_restore_undo, restore a Z-machine state from memory.
         *
         *	no zargs used
         *
         */

        internal static void ZRestoreUndo() => Process.Store((zword)RestoreUndo());/* z_restore_undo */

        /*
         * z_save, save [a part of] the Z-machine state to disk.
         *
         *	zargs[0] = address of memory area to save (optional)
         *	zargs[1] = number of bytes to save
         *	zargs[2] = address of suggested file name
         *	zargs[3] = whether to ask for confirmation of the file name
         *
         */

        internal static void ZSave()
        {
           
        }/* z_save */

        /*
         * save_undo
         *
         * This function does the dirty work for z_save_undo.
         *
         */

        internal static int SaveUndo()
        {
            return 1;
        }

        /*
         * z_save_undo, save the current Z-machine state for a future undo.
         *
         *	no zargs used
         *
         */

        internal static void ZSaveUndo() => Process.Store((zword)SaveUndo());/* z_save_undo */

        /*
         * z_verify, check the story file integrity.
         *
         *	no zargs used
         *
         */

        internal static void ZVerify()
        {

        }
    }
}