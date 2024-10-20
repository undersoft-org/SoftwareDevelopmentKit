﻿namespace Undersoft.SDK.Uniques
{
    using Hashing;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Undersoft.SDK;
    using Undersoft.SDK.Extracting;

    public unsafe static class UniqueKey64Extensions
    {
        public static double ComparableDouble(this object obj, Type t = null)
        {
            if (t == null)
                t = obj.GetType();

            if (t.IsAssignableTo(typeof(IUnique)))
                return ((IUnique)obj).Id;
            if (t.IsValueType)
                return GetSequentialValueTypeUniqueKey64((ValueType)obj);
            if (t == typeof(string))
                return (((string)obj)).UniqueKey64();
            if (t.IsAssignableTo(typeof(Type)))
                return UniqueKey64((Type)obj);
            if (t.IsAssignableTo(typeof(IList)))
            {
                if (t == typeof(byte[]))
                    return Hasher64.ComputeKey((byte[])obj);

                IList o = (IList)obj;
                if (o.Count == 1)
                    return UniqueKey64(o[0]);

                return UniqueKey64(o);
            }

            return UniqueKey64(obj);
        }

        public static long ComparableInt64(this object obj, Type type = null)
        {
            if (type == null)
                type = obj.GetType();

            if (obj is string)
            {
                if (type != typeof(string))
                {
                    if (type == typeof(IUnique))
                        return new Uscn((string)obj).UniqueKey();
                    if (type == typeof(DateTime))
                        return (long)((DateTime)Convert.ChangeType(obj, type)).ToBinary();
                    if (type == typeof(Enum))
                        return (long)(Enum.Parse(type, (string)obj));
                    return Convert.ToInt64(Convert.ChangeType(obj, type));
                }
                return ((string)obj).UniqueKey64();
            }

            if (obj is IUnique)
                return ((IUnique)obj).UniqueKey();
            if (type == typeof(DateTime))
                return (long)((DateTime)obj).Ticks;
            if (type == typeof(Enum))
                return (long)((int)obj);
            if (type.IsPrimitive)
                return Convert.ToInt64(obj);
            return obj.UniqueKey64();
        }

        public static int GetHashCode(this byte[] obj, long seed = 0)
        {
            return obj.UniqueKey32(seed);
        }

        public static int GetHashCode<T>(this IEquatable<T> obj, long seed = 0)
        {
            return obj.UniqueKey32(seed);
        }

        public static int GetHashCode(this IList obj, long seed = 0)
        {
            return obj.UniqueKey32(seed);
        }

        public static int GetHashCode(this IntPtr ptr, int length, long seed = 0)
        {
            return ptr.UniqueKey32(length, seed);
        }

        public static int GetHashCode(this IOrigin obj)
        {
            return obj.UniqueBytes32().ToInt32();
        }

        public static int GetHashCode(this IIdentifiable obj)
        {
            return obj.UniqueBytes32().ToInt32();
        }

        public static int GetHashCode(this object obj, long seed = 0)
        {
            return obj.UniqueKey32(seed);
        }

        public static int GetHashCode(this string obj, long seed = 0)
        {
            return obj.UniqueKey32(seed);
        }

        public static int GetHashCode(this Type obj, long seed = 0)
        {
            return obj.UniqueKey32(seed);
        }

        public static bool NullOrEquals(this ICollection obj, object value)
        {
            if (obj != null)
            {
                if (obj.Count > 0)
                    return (obj.Equals(value));
                return true;
            }
            return (obj == null && value == null);
        }

        public static bool NullOrEquals(this object obj, object value)
        {
            if (obj != null)
            {
                if (obj is ICollection)
                    return NullOrEquals((ICollection)obj, value);
                return obj.Equals(value);
            }
            if (value != null)
            {
                if (value is ICollection)
                    return NullOrEquals((ICollection)value, obj);
                return value.Equals(obj);
            }
            return (obj == null && value == null);
        }

        public static byte[] UniqueBytes64(this byte[] bytes, long seed = 0)
        {
            return Hasher64.ComputeBytes(bytes, seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] UniqueBytes64(
            this IList obj,
            int[] sizes,
            int totalsize,
            long seed = 0
        )
        {
            byte[] bytes = new byte[totalsize];
            fixed (byte* buff = bytes)
            {
                byte* buffer = buff;
                int[] _sizes = sizes;
                int offset = 0;
                for (int i = 0; i < obj.Count; i++)
                {
                    object o = obj[i];
                    var t = o.GetType();
                    int s = _sizes[i];
                    if (t == typeof(string))
                    {
                        string str = ((string)o);
                        fixed (char* c = str)
                            Extract.CopyBlock(buffer, (byte*)c, offset, s);
                    }
                    else
                    {
                        if (t.IsAssignableTo(typeof(IUnique)))
                        {
                            s = 8;
                            *((long*)(buffer + offset)) = ((IUnique)o).Id;
                        }
                        else
                        {
                            Extract.StructureToPointer(o, buffer + offset);
                        }
                    }
                    offset += s;
                }

                return Hasher64.ComputeBytes(buffer, offset, seed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] UniqueBytes64(this IList obj, long seed = 0)
        {
            int length = 1024,
                offset = 0,
                postoffset = 0,
                count = obj.Count,
                s = 0;

            byte* buffer = stackalloc byte[length];
            bool toResize = false;

            for (int i = 0; i < count; i++)
            {
                object o = obj[i];
                var t = o.GetType();
                if (t == typeof(string))
                {
                    string str = ((string)o);
                    s = str.Length * sizeof(char);
                    postoffset = (s + offset);

                    if (postoffset > length)
                        toResize = true;
                    else
                        fixed (char* c = str)
                            Extract.CopyBlock(buffer, (byte*)c, offset, s);
                }
                else
                {
                    if (t.IsAssignableTo(typeof(IUnique)))
                    {
                        s = 8;
                        postoffset = (s + offset);

                        if (postoffset > length)
                            toResize = true;
                        else
                            *((long*)(buffer + offset)) = ((IUnique)o).Id;
                    }
                    else
                    {
                        if (t.IsAssignableTo(typeof(Type)))
                        {
                            o = ((Type)o).FullName;
                            s = ((Type)o).FullName.Length * 2;
                        }
                        else
                        {
                            s = o.GetSize();
                        }

                        postoffset = (s + offset);

                        if (postoffset > length)
                            toResize = true;
                        else
                            Extract.StructureToPointer(o, buffer + offset);
                    }
                }

                if (toResize)
                {
                    throw new Exception("Key list total size of 1024 bytes exceeded");
                }
                else
                    offset = postoffset;
            }

            return Hasher64.ComputeBytes(buffer, offset, seed);
        }

        public static byte[] UniqueBytes64(this IntPtr bytes, int length, long seed = 0)
        {
            return Hasher64.ComputeBytes((byte*)bytes.ToPointer(), length, seed);
        }

        public static byte[] UniqueBytes64(this IOrigin obj)
        {
            return obj.Id.GetBytes();
        }

        public static byte[] UniqueBytes64(this IIdentifiable obj)
        {
            return obj.Id.GetBytes();
        }

        public static byte[] UniqueBytes64(this object obj, long seed = 0)
        {
            if (obj == null)
                return new byte[0];

            var t = obj.GetType();

            if (t.IsAssignableTo(typeof(IIdentifiable)))
                return ((IIdentifiable)obj).Id.GetBytes();
            if (t.IsValueType)
                return GetValueTypeUniqueBytes64((ValueType)obj, seed);
            if (t == typeof(string))
                return (((string)obj)).UniqueBytes64(seed);
            if (t.IsAssignableTo(typeof(Type)))
                return UniqueBytes64((Type)obj, seed);
            if (t.IsAssignableTo(typeof(IList)))
            {
                if (t == typeof(byte[]))
                    return Hasher64.ComputeBytes((byte[])obj, seed);

                IList o = (IList)obj;
                if (o.Count == 1)
                    return UniqueBytes64(o[0], seed);

                return UniqueBytes64(o, seed);
            }
            return Hasher64.ComputeBytes(obj.GetBytes(true), seed);
        }

        public static byte[] UniqueBytes64(
            this object[] obj,
            int[] sizes,
            int totalsize,
            long seed = 0
        )
        {
            if (obj.Length == 1)
                return UniqueBytes64(obj[0], seed);
            return UniqueBytes64((IList)obj, sizes, totalsize, seed);
        }

        public static byte[] UniqueBytes64(this object[] obj, long seed = 0)
        {
            if (obj.Length == 1)
                return UniqueBytes64(obj[0], seed);
            return UniqueBytes64((IList)obj, seed);
        }

        public static byte[] UniqueBytes64(this string obj, long seed = 0)
        {
            fixed (char* c = obj)
                return Hasher64.ComputeBytes((byte*)c, obj.Length * sizeof(char), seed);
        }

        public static byte[] UniqueBytes64(this Type obj, long seed = 0)
        {
            fixed (char* b = obj.FullName)
            {
                return Hasher64.ComputeBytes((byte*)b, obj.FullName.Length * 2, seed);
            }
        }

        public static long UniqueKey(this byte[] bytes, long seed = 0)
        {
            return UniqueKey64(bytes, seed);
        }

        public static long UniqueKey<T>(this IEquatable<T> obj, long seed = 0)
        {
            return UniqueKey64(obj, seed);
        }

        public static long UniqueKey(this IList obj, long seed = 0)
        {
            return UniqueKey64(obj, seed);
        }

        public static long UniqueKey(this IntPtr ptr, int length, long seed = 0)
        {
            return UniqueKey64(ptr, length, seed);
        }

        public static long UniqueKey(this IOrigin obj)
        {
            return UniqueKey64(obj);
        }

        public static long UniqueKey(this IOrigin obj, long seed)
        {
            return UniqueKey64(obj, seed);
        }

        public static long UniqueKey(this IIdentifiable obj)
        {
            return obj.Id;
        }

        public static long UniqueKey(this IIdentifiable obj, long seed)
        {
            return (long)Hasher64.ComputeKey(obj.Id.GetBytes(), seed);
        }

        public static long UniqueKey(this object obj, long seed = 0)
        {
            return UniqueKey64(obj, seed);
        }

        public static long UniqueKey(this object[] obj, long seed = 0)
        {
            if (obj.Length == 1)
                return UniqueKey64(obj[0], seed);
            return UniqueKey64((IList)obj, seed);
        }

        public static long UniqueKey(this string obj, long seed = 0)
        {
            return UniqueKey64(obj, seed);
        }

        public static long UniqueKey(this Type obj, long seed = 0)
        {
            return UniqueKey64(obj, seed);
        }

        public static long UniqueKey64(this byte[] bytes, long seed = 0)
        {
            return (long)Hasher64.ComputeKey(bytes, seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long UniqueKey64(this IList obj, int[] sizes, int totalsize, long seed = 0)
        {
            byte[] bytes = new byte[totalsize];
            fixed (byte* buff = bytes)
            {
                byte* buffer = buff;
                int[] _sizes = sizes;
                int offset = 0;
                for (int i = 0; i < obj.Count; i++)
                {
                    object o = obj[i];
                    int s = _sizes[i];
                    Type t = o.GetType();

                    if (t == typeof(string))
                    {
                        string str = ((string)o);
                        fixed (char* c = str)
                            Extract.CopyBlock(buffer, (byte*)c, offset, s);
                    }
                    else
                    {
                        if (t.IsAssignableTo(typeof(IUnique)))
                        {
                            s = 8;
                            *((long*)(buffer + offset)) = ((IUnique)o).Id;
                        }
                        else
                        {
                            if (t.IsAssignableTo(typeof(Type)))
                                o = ((Type)o).FullName;

                            Extract.StructureToPointer(o, buffer + offset);
                        }
                    }
                    offset += s;
                }

                return (long)Hasher64.ComputeKey(buffer, offset, seed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long UniqueKey64(this IList obj, long seed = 0)
        {
            int length = 1024,
                offset = 0,
                postoffset = 0,
                count = obj.Count,
                s = 0;

            byte* buffer = stackalloc byte[length];
            bool toResize = false;

            for (int i = 0; i < count; i++)
            {
                object o = obj[i];
                Type t = o.GetType();

                if (t == typeof(string))
                {
                    string str = ((string)o);
                    s = str.Length * 2;
                    postoffset = (s + offset);
                    if (postoffset > length)
                        toResize = true;
                    else
                    {
                        fixed (char* c = str)
                            Extract.CopyBlock(buffer, (byte*)c, offset, s);
                    }
                }
                else
                {
                    if (t.IsAssignableTo(typeof(IUnique)))
                    {
                        s = 8;
                        postoffset = (s + offset);

                        if (postoffset > length)
                            toResize = true;
                        else
                            *((long*)(buffer + offset)) = ((IUnique)o).Id;
                    }
                    else
                    {
                        if (t.IsAssignableTo(typeof(Type)))
                        {
                            o = ((Type)o).FullName;
                            s = ((Type)o).FullName.Length * 2;
                        }
                        else
                        {
                            s = o.GetSize();
                        }

                        postoffset = (s + offset);

                        if (postoffset > length)
                            toResize = true;
                        else
                            Extract.StructureToPointer(o, buffer + offset);
                    }
                }

                if (toResize)
                {
                    throw new Exception("Key list total size of 1024 bytes exceeded");
                }
                else
                    offset = postoffset;
            }

            return (long)Hasher64.ComputeKey(buffer, offset, seed);
        }

        public static long UniqueKey64(this IntPtr ptr, int length, long seed = 0)
        {
            return (long)Hasher64.ComputeKey((byte*)ptr.ToPointer(), length, seed);
        }

        public static long UniqueKey64(this IIdentifiable obj)
        {
            return obj.Id;
        }

        public static long UniqueKey64(this IIdentifiable obj, long seed)
        {
            return (long)Hasher64.ComputeKey(obj.Id.GetBytes(), seed);
        }

        public static long UniqueKey64(this object obj, long seed = 0)
        {
            if (obj == null)
                return 0;

            var t = obj.GetType();

            if (t == typeof(long) && seed == 0)
                return (long)obj;

            if (t.IsAssignableTo(typeof(IIdentifiable)) && seed == 0)
                return (long)((IIdentifiable)obj).Id;

            if (t.IsValueType)
                return GetValueTypeUniqueKey64((ValueType)obj, seed);

            if (t == typeof(string))
                return (((string)obj)).UniqueKey64(seed);

            if (t.IsAssignableTo(typeof(Type)))
                return UniqueKey64((Type)obj, seed);

            if (t.IsAssignableTo(typeof(IList)))
            {
                if (t == typeof(byte[]))
                    return (long)Hasher64.ComputeKey((byte[])obj, seed);

                IList o = (IList)obj;
                if (o.Count == 1)
                    return UniqueKey64(o[0], seed);

                return UniqueKey64(o, seed);
            }

            return (long)Hasher64.ComputeKey(obj.GetBytes(true), seed);
        }

        public static long UniqueKey64(
            this object[] obj,
            int[] sizes,
            int totalsize,
            long seed = 0
        )
        {
            if (obj.Length == 1)
                return UniqueKey64(obj[0], seed);
            return UniqueKey64((IList)obj, sizes, totalsize, seed);
        }

        public static long UniqueKey64(this object[] obj, long seed = 0)
        {
            if (obj.Length == 1)
                return UniqueKey64(obj[0], seed);
            return UniqueKey64((IList)obj, seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long UniqueKey64(this string obj, long seed = 0)
        {
            fixed (char* c = obj)
            {
                return (long)Hasher64.ComputeKey((byte*)c, obj.Length * sizeof(char), seed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long UniqueKey64(this Type obj, long seed = 0)
        {
            var name = obj.FullName;
            if (name == null)
                name = obj.Namespace + "." + obj.Name;

            fixed (char* b = name)
            {
                return (long)Hasher64.ComputeKey((byte*)b, name.Length * sizeof(char), seed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] GetSequentialValueTypeHashBytes64(ValueType obj, long seed = 0)
        {
            int size = obj.GetSize();
            byte[] s = new byte[size];
            fixed (byte* ps = s)
            {
                Extract.StructureToPointer(obj, ps);
                return Hasher64.ComputeBytes(ps, size, seed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetSequentialValueTypeUniqueKey64(ValueType obj, long seed = 0)
        {
            int size = obj.GetSize();
            byte* ps = stackalloc byte[size];
            Extract.StructureToPointer(obj, ps);
            return (long)Hasher64.ComputeKey(ps, size, seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] GetValueTypeUniqueBytes64(ValueType obj, long seed = 0)
        {
            byte[] s = new byte[8];
            fixed (byte* ps = s)
            {
                Extract.StructureToPointer(obj, ps);
                if (seed == 0)
                    return s;
                if (*(int*)ps == 0)
                    *(long*)ps = Unique.NewId;
                return Hasher64.ComputeBytes(ps, 8, seed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetValueTypeUniqueKey64(ValueType obj, long seed = 0)
        {
            byte[] s = new byte[8];
            fixed (byte* ps = s)
            {
                Extract.StructureToPointer(obj, ps);
                ulong r = *(ulong*)ps;
                if (seed == 0)
                    return *(long*)ps;
                if (r == 0)
                    *(long*)ps = Unique.NewId;
                return (long)Hasher64.ComputeKey(ps, 8, seed);
            }
        }
    }
}
