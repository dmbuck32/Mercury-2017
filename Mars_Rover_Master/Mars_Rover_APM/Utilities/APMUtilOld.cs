using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Mars_Rover_APM.Utilities
{
    public static class APMUtilOld
    {

        public static void ByteArrayToStructure(byte[] bytearray, ref object obj, int startoffset)
        {
            if (bytearray[0] == 'U')
            {
                ByteArrayToStructureEndian(bytearray, ref obj, startoffset);
            }
            else
            {

                int len = Marshal.SizeOf(obj);

                IntPtr i = Marshal.AllocHGlobal(len);

                // create structure from ptr
                obj = Marshal.PtrToStructure(i, obj.GetType());

                try
                {
                    // copy byte array to ptr
                    Marshal.Copy(bytearray, startoffset, i, len);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ByteArrayToStructure FAIL " + ex.Message);
                }

                obj = Marshal.PtrToStructure(i, obj.GetType());

                Marshal.FreeHGlobal(i);

            }
        }

        public static void ByteArrayToStructureEndian(byte[] bytearray, ref object obj, int startoffset)
        {

            int len = Marshal.SizeOf(obj);
            IntPtr i = Marshal.AllocHGlobal(len);
            byte[] temparray = (byte[])bytearray.Clone();

            // create structure from ptr
            obj = Marshal.PtrToStructure(i, obj.GetType());

            // do endian swap
            object thisBoxed = obj;
            Type test = thisBoxed.GetType();

            int reversestartoffset = startoffset;

            // Enumerate each structure field using reflection.
            foreach (var field in test.GetFields())
            {
                // field.Name has the field's name.
                object fieldValue = field.GetValue(thisBoxed); // Get value

                // Get the TypeCode enumeration. Multiple types get mapped to a common typecode.
                TypeCode typeCode = Type.GetTypeCode(fieldValue.GetType());

                if (typeCode != TypeCode.Object)
                {
                    Array.Reverse(temparray, reversestartoffset, Marshal.SizeOf(fieldValue));
                    reversestartoffset += Marshal.SizeOf(fieldValue);
                }
                else
                {
                    reversestartoffset += ((byte[])fieldValue).Length;
                }

            }

            try
            {
                // copy byte array to ptr
                Marshal.Copy(temparray, startoffset, i, len);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ByteArrayToStructure FAIL" + ex.ToString());
            }

            obj = Marshal.PtrToStructure(i, obj.GetType());

            Marshal.FreeHGlobal(i);

        }

        /// <summary>
        /// Convert a struct to an array of bytes, struct fields being reperesented in 
        /// little endian (LSB first)
        /// </summary>
        /// <remarks>Note - assumes little endian host order</remarks>
        public static byte[] StructureToByteArray(object obj)
        {
            int len = Marshal.SizeOf(obj);
            byte[] arr = new byte[len];
            
            IntPtr ptr = Marshal.AllocHGlobal(len);
            
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);
            
            return arr;
        }

        /// <summary>
        /// Create a new mavlink packet object from a byte array as recieved over mavlink
        /// Endianess will be detetected using packet inspection
        /// </summary>
        /// <typeparam name="TMavlinkPacket">The type of mavlink packet to create</typeparam>
        /// <param name="bytearray">The bytes of the mavlink packet</param>
        /// <param name="startoffset">The position in the byte array where the packet starts</param>
        /// <returns>The newly created mavlink packet</returns>
        public static TAPMPacket ByteArrayToStructure<TAPMPacket>(this byte[] bytearray, int startoffset = 6) where TAPMPacket : struct
        {
            object newPacket = new TAPMPacket();
            ByteArrayToStructure(bytearray, ref newPacket, startoffset);
            return (TAPMPacket)newPacket;
        }
    }
}
