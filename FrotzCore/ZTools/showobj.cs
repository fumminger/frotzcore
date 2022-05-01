/*
 * showobj - part of infodump
 *
 * Object display routines.
 */
using System;

namespace ZTools;

internal static class ShowObj
{
    /*
     * configure_object_tables
     *
     * Determine the object table start and end addresses, together with the
     * property data start and end addresses, and the number of objects.
     *
     * Format:
     *
     * The object information consists of two parts. Firstly a fixed table of
     * objects and secondly, an area of variable property data.
     *
     * The format of the object varies between game types. For pre-V4 games
     * the format is:
     *
     * struct zobject {
     *    ushort attributes[2];
     *    unsigned char parent;
     *    unsigned char next;
     *    unsigned char child;
     *    ushort property_offset;
     * }
     *
     * Post-V3 the format is:
     *
     * struct zobject {
     *    ushort attributes[3];
     *    ushort parent;
     *    ushort next;
     *    ushort child;
     *    ushort property_offset;
     * }
     *
     * Attributes are an array of bits that can be tested, set and cleared. The
     * parent, next and child fields are object numbers. These fields are used to
     * construct an object tree that represents concepts such as object contains
     * or room contains. The property offset is the address in the data file of the
     * start of the property data for the object. Objects are numbered from 1.
     * Object 0 is used as the NULL object to terminate object lists.
     *
     * Note: The start of the object table contains a list of default property
     * values that are used when a property is not present for an object. The size
     * of this table in words is the maximum number of properties minus 1.
     *
     * The format of the object properties is complex. It is:
     *
     * [Common prefix][property n]...[property n][property 0]
     *
     * Properties occur in descending order from highest property number to zero.
     * Property zero always terminates the list, but is not referenced by the Z-code.
     * Instead, property zero is used to terminate the scan down the property list, if
     * a property is not defined. This behaviour is required when loading a default
     * property list item, or to catch setting undefined property values.
     *
     * The size information is ignored for property 0, which is actually just specified
     * as a byte containing 0x00.
     *
     * Key:
     *
     * (n) = size of block in bytes
     * max = maximum number of recurring blocks
     * min = minimum number of recurring blocks
     *
     * Common prefix:
     *
     *  (1)       (2)          (2)
     * +-------+ +------+     +------+
     * | count | | text | ... | text | max=255, min=0
     * +-------+ +------+     +------+
     *
     * count = number of following text blocks
     * text = object description, encoded
     *
     * Property n (V3 format):
     *
     *  (1)             (1)          (1)
     * +--------+----+ +------+     +------+
     * | size-1 | id | | data | ... | data | max=8, min=1
     * +--------+----+ +------+     +------+
     *  7      5 4  0
     *
     * size-1 = size of property - 1
     * id = property identifier
     * data = property data
     *
     * Maximum property number = 31
     *
     * Property n (V4 format):
     *
     *  (1) Property header byte       (1) Property size byte         (1)          (1)
     * +-----------+-----------+----+ +-----------+---------+------+ +------+     +------+
     * | size byte | word data | id | | size byte | ignored | size | | data | ... | data | max=63, min=0
     * +-----------+-----------+----+ +-----------+---------+------+ +------+     +------+
     *            7           6 5  0             7         6 5    0
     *
     * size byte = if set then next data block is a the property size byte
     *             if clear then the 'word data' flag is checked and the property has no size byte
     * word data = if set then 2 data blocks follow
     *             if clear 1 data block follows
     * ignored = this flag is not used by the property manipulation opcodes, it can be set to an arbitary value
     *           (note: this bit could be used to increase the property size from 63 to 127 bytes)
     * id = property identifier
     * size = size of property
     * data = property data
     *
     * Maximum property number = 63
     */

    internal static void ConfigureObjectTables(out int obj_count,
                                  out ulong obj_table_base,
                                  out ulong obj_table_end,
                                  out ulong obj_data_base,
                                  out ulong obj_data_end)
    {
        ulong object_address, address;
        uint data_count, data;

        obj_table_base = 0;
        obj_table_end = 0;
        obj_data_base = 0;
        obj_data_end = 0;
        obj_count = 0;

        /* The object table address comes from the header */
        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        obj_table_base = txio.header.objects;

        /* Calculate the number of objects and property addresses range */

        do
        {

            /* Count this object and get its address */

            //(*obj_count)++;
            //object_address = (ulong)get_object_address(*obj_count);

            obj_count++;
            object_address = (ulong)GetObjectAddress(obj_count);

            /* Check if we have got to the end of the object list */

            if (obj_data_base == 0 || object_address < obj_data_base)
            {

                /* Calculate the range of property data */

                if ((uint)txio.header.version < TxH.V4)
                    object_address += TxH.O3_PROPERTY_OFFSET;
                else
                    object_address += TxH.O4_PROPERTY_OFFSET;
                address = txio.ReadDataWord(ref object_address);
                if (obj_data_base == 0 || address < obj_data_base)
                    obj_data_base = address;
                if (obj_data_end == 0 || address > obj_data_end)
                    obj_data_end = address;
            }
        } while (object_address < obj_data_base);

        obj_table_end = object_address - 1;

        /* Skip any description for the last property */

        if (txio.ReadDataByte(ref obj_data_end) > 0)
        {
            while (((uint)txio.ReadDataWord(ref obj_data_end) & 0x8000) == 0) ;
        }

        /* Skip any properties to calculate the end address of the last property */

        while ((data = txio.ReadDataByte(ref obj_data_end)) != 0)
        {
            if ((uint)txio.header.version < TxH.V4)
                data_count = (uint)(((data & txio.property_size_mask) >> 5) + 1);
            else if ((data & 0x80) > 0)
                data_count = (uint)(txio.ReadDataByte(ref obj_data_end) & txio.property_size_mask);
            else if ((data & 0x40) > 0)
                data_count = 2;
            else
                data_count = 1;
            obj_data_end += data_count;
        }


        obj_data_end--;
        // (*obj_data_end)--; // TODO I'm not sure about this

    }/* configure_object_tables */

    /*
     * show_objects
     *
     * List all objects and property data.
     */

    internal static void show_objects(int symbolic)
    {
        ulong object_address, address;
        uint data, pobj, nobj, cobj;
        int i, j, k, list;
        ulong class_numbers_base;
        ulong property_names_base;
        ulong attr_names_base;


        /* Get objects configuration */

        ConfigureObjectTables(out int obj_count, out ulong obj_table_base, out ulong obj_table_end,
                              out ulong obj_data_base, out ulong obj_data_end);

        if (symbolic != 0)
        {
            InfInfo.ConfigureInformTables(obj_data_end, out ushort inform_version, out class_numbers_base, out ulong class_numbers_end,
                                          out property_names_base, out ulong property_names_end, out attr_names_base, out ulong attr_names_end);
        }
        else
        {
            attr_names_base = property_names_base = class_numbers_base = 0;
        }

        txio.TxPrint("\n    **** Objects ****\n\n");
        txio.TxPrintf("  Object count = {0}\n", obj_count);

        /* Iterate through each object */

        for (i = 1; (uint)i <= obj_count; i++)
        {
            txio.TxPrint('\n');

            /* Get address of object */

            object_address = (ulong)GetObjectAddress(i);

            /* Display attributes */
            if (txio.header is null)
                throw new InvalidOperationException("txio header was not initialized");

            txio.TxPrintf("{0:d3}. Attributes: ", i);
            list = 0;
            for (j = 0; j < (((uint)txio.header.version < TxH.V4) ? 4 : 6); j++)
            {
                data = txio.ReadDataByte(ref object_address);
                for (k = 7; k >= 0; k--)
                {
                    if (((data >> k) & 1) > 0)
                    {
                        txio.TxPrint((list++) > 0 ? ", " : "");
                        if (Symbols.PrintAttributeName(attr_names_base, (j * 8) + (7 - k)) > 0)
                            txio.TxPrintf("({0:d})", (j * 8) + (7 - k));
                        else
                            txio.TxPrintf("{0:d}", (j * 8) + (7 - k));
                    }
                }
            }
            if (list == 0)
                txio.TxPrint("None");
            txio.TxPrint('\n');

            /* Get object linkage information */

            if ((uint)txio.header.version < TxH.V4)
            {
                pobj = txio.ReadDataByte(ref object_address);
                nobj = txio.ReadDataByte(ref object_address);
                cobj = txio.ReadDataByte(ref object_address);
            }
            else
            {
                pobj = txio.ReadDataWord(ref object_address);
                nobj = txio.ReadDataWord(ref object_address);
                cobj = txio.ReadDataWord(ref object_address);
            }
            address = txio.ReadDataWord(ref object_address);
            txio.TxPrintf("     Parent object: {0:d3}  ", (int)pobj);
            txio.TxPrintf("Sibling object: {0:d3}  ", (int)nobj);
            txio.TxPrintf("Child object: {0:d3}\n", (int)cobj);
            txio.TxPrintf("     Property address: {0:X4}\n", address);
            txio.TxPrint("         Description: \"");

            /* If object has a description then display it */

            if ((uint)txio.ReadDataByte(ref address) > 0)
                txio.DecodeText(ref address);
            txio.TxPrint("\"\n");

            /* Print property list */

            txio.TxPrint("          Properties:\n");
            PrintPropertyList(ref address, property_names_base);
        }

    }/* show_objects */

    /*
     * get_object_address
     *
     * Given an object number calculate the data file address of the object data.
     */

    private static int GetObjectAddress(int obj)
    {
        int offset;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        /* Address calculation is object table base + size of default properties area +
           object number-1 * object size */

        offset = txio.header.objects;
        if ((uint)txio.header.version <= TxH.V3)
            offset += ((TxH.P3_MAX_PROPERTIES - 1) * 2) + ((obj - 1) * TxH.O3_SIZE);
        else
            offset += ((TxH.P4_MAX_PROPERTIES - 1) * 2) + ((obj - 1) * TxH.O4_SIZE);

        return offset;

    }/* get_object_address */

    /*
     * print_property_list
     *
     * Display the data associated with each object property.
     */

    internal static void PrintPropertyList(ref ulong address, ulong property_names_base)
    {
        int data, count;

        /* Scan down the property address displaying each property */

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        for (data = txio.ReadDataByte(ref address); data > 0; data = txio.ReadDataByte(ref address))
        {
            txio.TxPrint("            ");
            if (Symbols.PrintPropertyName(property_names_base, data & txio.property_mask) > 0)
                txio.TxPrint("\n              ");
            else
                txio.TxPrint("  ");
            txio.TxPrintf("[{0,2}] ", data & txio.property_mask);
            if ((uint)txio.header.version <= TxH.V3)
                count = ((data & txio.property_size_mask) >> 5) + 1;
            else if ((data & 0x80) > 0)
                count = (int)((uint)txio.ReadDataByte(ref address) & txio.property_size_mask);
            else if ((data & 0x40) > 0)
                count = 2;
            else
                count = 1;
            while (count-- > 0)
                txio.TxPrintf("{0:X2} ", (uint)txio.ReadDataByte(ref address));
            txio.TxPrint('\n');
        }

    }/* print_property_list */

    /*
     * show_tree
     *
     * Use the object linkage information to display a hierarchical list of
     * objects.
     */

    internal static void ShowTree()
    {
        ulong object_address;
        int i, parent;


        /* Get objects configuration */

        ConfigureObjectTables(out int obj_count, out ulong obj_table_base, out ulong obj_table_end,
                              out ulong obj_data_base, out ulong obj_data_end);

        txio.TxPrint("\n    **** Object tree ****\n\n");

        /* Iterate through each object */

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        for (i = 1; i <= obj_count; i++)
        {
            /* Get object address */

            object_address = (ulong)GetObjectAddress(i);

            /* Get parent for this object */

            if ((uint)txio.header.version <= TxH.V3)
            {
                object_address += TxH.O3_PARENT;
                parent = txio.ReadDataByte(ref object_address);
            }
            else
            {
                object_address += TxH.O4_PARENT;
                parent = txio.ReadDataWord(ref object_address);
            }

            /*
             * If object has no parent then it is a root object so display the tree
             * from the object.
             */

            if (parent == 0)
                PrintObject(i, 0);
        }

    }/* show_tree */

    /*
     * print_object
     *
     * Print an object description and its children for a point in the object tree.
     */

    private static void PrintObject(int obj, int depth)
    {
        ulong object_address, address;
        int child, i;

        /* Continue until the next object number is NULL */
        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        while (obj > 0)
        {

            /* Display object depth and description */

            for (i = 0; i < depth; i++)
                txio.TxPrint(" . ");
            txio.TxPrintf("[{0:d3}] ", obj);
            PrintObjectDesc(obj);
            txio.TxPrint("\n");

            /* Get object address */

            object_address = (ulong)GetObjectAddress(obj);

            /* Get any child object and the next object at this level */

            if ((uint)txio.header.version <= TxH.V3)
            {
                address = object_address + TxH.O3_CHILD;
                child = txio.ReadDataByte(ref address);
                address = object_address + TxH.O3_NEXT;
                obj = txio.ReadDataByte(ref address);
            }
            else
            {
                address = object_address + TxH.O4_CHILD;
                child = txio.ReadDataWord(ref address);
                address = object_address + TxH.O4_NEXT;
                obj = txio.ReadDataWord(ref address);
            }

            /* If this object has a child then print its tree */

            if (child > 0)
                PrintObject(child, depth + 1);
        }

    }/* print_object */

    /*
     * print_object_description
     *
     * Display the description of an object.
     */

    internal static void PrintObjectDesc(int obj)
    {
        ulong object_address, address;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        txio.TxPrint('"');

        /* Check for a NULL object number */

        if (obj > 0)
        {

            /* Get object address */

            object_address = (ulong)GetObjectAddress(obj);
            address = (uint)txio.header.version <= TxH.V3
                ? object_address + TxH.O3_PROPERTY_OFFSET
                : object_address + TxH.O4_PROPERTY_OFFSET;

            /* Get the property address */

            address = txio.ReadDataWord(ref address);

            /* Display the description if the object has one */

            if ((uint)txio.ReadDataByte(ref address) > 0)
                txio.DecodeText(ref address);
        }
        txio.TxPrint('"');

    }/* print_object_desc */
}
