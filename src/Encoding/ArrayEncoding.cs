// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Amqp.Encoding
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    sealed class ArrayEncoding : EncodingBase
    {
        public ArrayEncoding()
            : base(FormatCode.Array32)
        {
        }

        public static int GetEncodeSize<T>(T[] value)
        {
            return value == null ?
                FixedWidth.NullEncoded :
                ArrayEncoding.GetEncodeSize(value, value.Length, typeof(T), false, out _);
        }

        public static void Encode<T>(T[] value, ByteBuffer buffer)
        {
            if (value == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                ArrayEncoding.Encode(value, value.Length, typeof(T), buffer);
            }
        }

        public static T[] Decode<T>(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            int size;
            int count;
            AmqpEncoding.ReadSizeAndCount(buffer, formatCode, FormatCode.Array8, FormatCode.Array32, out size, out count);

            formatCode = AmqpEncoding.ReadFormatCode(buffer);
            return ArrayEncoding.Decode<T>(buffer, size, count, formatCode);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            IList array = (IList)value;
            Type itemType = array.Count > 0 ? array[0].GetType() : null;
            return ArrayEncoding.GetEncodeSize(array, array.Count, itemType, arrayEncoding, out _);
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            IList array = (IList)value;
            Type itemType = array.Count > 0 ? array[0].GetType() : null;
            int width;
            int encodeSize = ArrayEncoding.GetEncodeSize(array, array.Count, itemType, arrayEncoding, out width);
            AmqpBitConverter.WriteUByte(buffer, width == FixedWidth.UByte ? FormatCode.Array8 : FormatCode.Array32);
            ArrayEncoding.Encode(array, array.Count, itemType, width, encodeSize, buffer);
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            int size = 0;
            int count = 0;
            AmqpEncoding.ReadSizeAndCount(buffer, formatCode, FormatCode.Array8, FormatCode.Array32, out size, out count);

            formatCode = AmqpEncoding.ReadFormatCode(buffer);
            Array array = null;
            switch (formatCode)
            {
                case FormatCode.Boolean:
                    array = ArrayEncoding.Decode<bool>(buffer, size, count, formatCode);
                    break;
                case FormatCode.UByte:
                    array = ArrayEncoding.Decode<byte>(buffer, size, count, formatCode);
                    break;
                case FormatCode.UShort:
                    array = ArrayEncoding.Decode<ushort>(buffer, size, count, formatCode);
                    break;
                case FormatCode.UInt:
                case FormatCode.SmallUInt:
                    array = ArrayEncoding.Decode<uint>(buffer, size, count, formatCode);
                    break;
                case FormatCode.ULong:
                case FormatCode.SmallULong:
                    array = ArrayEncoding.Decode<ulong>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Byte:
                    array = ArrayEncoding.Decode<sbyte>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Short:
                    array = ArrayEncoding.Decode<short>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Int:
                case FormatCode.SmallInt:
                    array = ArrayEncoding.Decode<int>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Long:
                case FormatCode.SmallLong:
                    array = ArrayEncoding.Decode<long>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Float:
                    array = ArrayEncoding.Decode<float>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Double:
                    array = ArrayEncoding.Decode<double>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Char:
                    array = ArrayEncoding.Decode<char>(buffer, size, count, formatCode);
                    break;
                case FormatCode.TimeStamp:
                    array = ArrayEncoding.Decode<DateTime>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Uuid:
                    array = ArrayEncoding.Decode<Guid>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Binary32:
                case FormatCode.Binary8:
                    array = ArrayEncoding.Decode<ArraySegment<byte>>(buffer, size, count, formatCode);
                    break;
                case FormatCode.String32Utf8:
                case FormatCode.String8Utf8:
                    array = ArrayEncoding.Decode<string>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Symbol32:
                case FormatCode.Symbol8:
                    array = ArrayEncoding.Decode<AmqpSymbol>(buffer, size, count, formatCode);
                    break;
                case FormatCode.List32:
                case FormatCode.List8:
                    array = ArrayEncoding.Decode<IList>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Map32:
                case FormatCode.Map8:
                    array = ArrayEncoding.Decode<AmqpMap>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Array32:
                case FormatCode.Array8:
                    array = ArrayEncoding.Decode<Array>(buffer, size, count, formatCode);
                    break;
                default:
                    throw new NotSupportedException(CommonResources.GetString(CommonResources.NotSupportFrameCode, formatCode));
            }

            return array;
        }

        internal static int GetEncodeSize(IList array, int count, Type itemType, bool arrayEncoding, out int width)
        {
            int size = FixedWidth.FormatCode + ArrayEncoding.GetValueSize(array, count, itemType);
            width = arrayEncoding ? FixedWidth.UInt : AmqpEncoding.GetEncodeWidthByCountAndSize(count, size);
            size += FixedWidth.FormatCode + width + width;
            return size;
        }

        internal static void Encode(IList value, int count, Type itemType, ByteBuffer buffer)
        {
            int width;
            int encodeSize = ArrayEncoding.GetEncodeSize(value, count, itemType, false, out width);
            AmqpBitConverter.WriteUByte(buffer, width == FixedWidth.UByte ? FormatCode.Array8 : FormatCode.Array32);
            ArrayEncoding.Encode(value, count, itemType, width, encodeSize, buffer);
        }

        internal static int GetValueSize(IList value, int count, Type itemType)
        {
            if (count == 0)
            {
                return 0;
            }

            Fx.Assert(itemType != null, "Item type must be provided.");
            var encoding = AmqpEncoding.GetEncoding(itemType);
            switch (encoding.FormatCode)
            {
                case FormatCode.UInt:
                    return count * FixedWidth.UInt;
                case FormatCode.Int:
                    return count * FixedWidth.Int;
                case FormatCode.ULong:
                    return count * FixedWidth.ULong;
                case FormatCode.Long:
                    return count * FixedWidth.Long;
                case FormatCode.Symbol32:
                    IList<AmqpSymbol> symbols = (IList<AmqpSymbol>)value;
                    int size = 0;
                    for (int i = 0; i < value.Count; i++)
                    {
                        size += FixedWidth.UInt + SymbolEncoding.GetValueSize(symbols[i]);
                    }
                    return size;
                case FormatCode.Boolean:
                    return count * FixedWidth.Boolean;
                case FormatCode.UByte:
                    return count * FixedWidth.UByte;
                case FormatCode.Byte:
                    return count * FixedWidth.Byte;
                case FormatCode.UShort:
                    return count * FixedWidth.UShort;
                case FormatCode.Short:
                    return count * FixedWidth.Short;
                case FormatCode.Float:
                    return count * FixedWidth.Float;
                case FormatCode.Double:
                    return count * FixedWidth.Double;
                case FormatCode.Decimal128:
                    return count * FixedWidth.Decimal128;
                case FormatCode.Char:
                    return count * FixedWidth.Char;
                case FormatCode.TimeStamp:
                    return count * FixedWidth.TimeStamp;
                case FormatCode.Uuid:
                    return count * FixedWidth.Uuid;
                default:
                    break;
            }

            int valueSize = 0;
            foreach (object item in value)
            {
                bool arrayEncoding = true;
                if (encoding.FormatCode == FormatCode.Described && valueSize == 0)
                {
                    arrayEncoding = false;
                }

                valueSize += encoding.GetObjectEncodeSize(item, arrayEncoding);
            }

            return valueSize;
        }

        internal static void Encode(IList value, int count, Type itemType, int width, int encodeSize, ByteBuffer buffer)
        {
            encodeSize -= FixedWidth.FormatCode + width;
            if (width == FixedWidth.UByte)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)encodeSize);
                AmqpBitConverter.WriteUByte(buffer, (byte)count);
            }
            else
            {
                AmqpBitConverter.WriteUInt(buffer, (uint)encodeSize);
                AmqpBitConverter.WriteUInt(buffer, (uint)count);
            }

            EncodeValue(value, count, itemType, buffer);
        }


        internal static void EncodeValue(IList value, int count, Type itemType, ByteBuffer buffer)
        {
            if (count == 0)
            {
                return;
            }

            EncodingBase encoding = AmqpEncoding.GetEncoding(itemType);
            switch (encoding.FormatCode)
            {
                case FormatCode.UInt:
                    EncodeArray<uint>(buffer, (IList<uint>)value, count, FormatCode.UInt, (b, v, i) => AmqpBitConverter.WriteUInt(b, v));
                    return;
                case FormatCode.Int:
                    buffer.Validate(true, FixedWidth.Int * count);
                    EncodeArray<int>(buffer, (IList<int>)value, count, FormatCode.Int, (b, v, i) =>
                    {
                        AmqpBitConverter.WriteInt(b.Buffer, b.WritePos + i * FixedWidth.Int, v);
                    });
                    buffer.Append(FixedWidth.Int * count);
                    return;
                case FormatCode.ULong:
                    EncodeArray<ulong>(buffer, (IList<ulong>)value, count, FormatCode.ULong, (b, v, i) => AmqpBitConverter.WriteULong(b, v));
                    return;
                case FormatCode.Long:
                    EncodeArray<long>(buffer, (IList<long>)value, count, FormatCode.Long, (b, v, i) => AmqpBitConverter.WriteLong(b, v));
                    return;
                case FormatCode.Symbol32:
                    EncodeArray<AmqpSymbol>(buffer, (IList<AmqpSymbol>)value, count, FormatCode.Symbol32, (b, v, i) => SymbolEncoding.EncodeValue(v, FormatCode.Symbol32, b));
                    return;
                case FormatCode.Boolean:
                    EncodeArray<bool>(buffer, (IList<bool>)value, count, FormatCode.Boolean, (b, v, i) => AmqpBitConverter.WriteUByte(b, (byte)(v ? 1 : 0)));
                    return;
                case FormatCode.UByte:
                    EncodeArray<byte>(buffer, (IList<byte>)value, count, FormatCode.UByte, (b, v, i) => AmqpBitConverter.WriteUByte(b, v));
                    return;
                case FormatCode.Byte:
                    EncodeArray<sbyte>(buffer, (IList<sbyte>)value, count, FormatCode.Byte, (b, v, i) => AmqpBitConverter.WriteByte(b, v));
                    return;
                case FormatCode.UShort:
                    EncodeArray<ushort>(buffer, (IList<ushort>)value, count, FormatCode.UShort, (b, v, i) => AmqpBitConverter.WriteUShort(b, v));
                    return;
                case FormatCode.Short:
                    EncodeArray<short>(buffer, (IList<short>)value, count, FormatCode.Short, (b, v, i) => AmqpBitConverter.WriteShort(b, v));
                    return;
                case FormatCode.Float:
                    EncodeArray<float>(buffer, (IList<float>)value, count, FormatCode.Float, (b, v, i) => AmqpBitConverter.WriteFloat(b, v));
                    return;
                case FormatCode.Double:
                    EncodeArray<double>(buffer, (IList<double>)value, count, FormatCode.Double, (b, v, i) => AmqpBitConverter.WriteDouble(b, v));
                    return;
                case FormatCode.Decimal128:
                    EncodeArray<decimal>(buffer, (IList<decimal>)value, count, FormatCode.Decimal128, (b, v, i) => DecimalEncoding.EncodeValue(v, b));
                    return;
                case FormatCode.Char:
                    EncodeArray<char>(buffer, (IList<char>)value, count, FormatCode.Char, (b, v, i) => AmqpBitConverter.WriteChar(b, v));
                    return;
                case FormatCode.TimeStamp:
                    EncodeArray<DateTime>(buffer, (IList<DateTime>)value, count, FormatCode.TimeStamp, (b, v, i) => AmqpBitConverter.WriteTimestamp(b, v));
                    return;
                case FormatCode.Uuid:
                    EncodeArray<Guid>(buffer, (IList<Guid>)value, count, FormatCode.Uuid, (b, v, i) => AmqpBitConverter.WriteUuid(b, v));
                    return;
                default:
                    break;
            }

            object firstItem = value[0];
            AmqpBitConverter.WriteUByte(buffer, encoding.FormatCode);
            if (encoding.FormatCode == FormatCode.Described)
            {
                DescribedType describedValue = (DescribedType)firstItem;
                AmqpEncoding.EncodeObject(describedValue.Descriptor, buffer);
                AmqpBitConverter.WriteUByte(buffer, AmqpEncoding.GetEncoding(describedValue.Value).FormatCode);
            }

            for (int i = 0; i < count; i++)
            {
                encoding.EncodeObject(value[i], true, buffer);
            }
        }

        static void EncodeArray<T>(ByteBuffer buffer, IList<T> array, int count, FormatCode formatCode,
            Action<ByteBuffer, T, int> encoder)
        {
            AmqpBitConverter.WriteUByte(buffer, formatCode);
            for (int i = 0; i < count; i++)
            {
                encoder(buffer, array[i], i);
            }
        }

        static IList DecodeArray<T>(ByteBuffer buffer, FormatCode formatCode, int count,
            Func<ByteBuffer, FormatCode, T> decoder) where T : struct
        {
            T[] array = new T[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = decoder(buffer, formatCode);
            }

            return array;
        }

        static T[] Decode<T>(ByteBuffer buffer, int size, int count, FormatCode formatCode)
        {
            object descriptor = null;
            if (formatCode == FormatCode.Described)
            {
                descriptor = AmqpEncoding.DecodeObject(buffer);
                formatCode = AmqpEncoding.ReadFormatCode(buffer);
            }

            // Special cases for primitive types
            switch (formatCode)
            {
                case FormatCode.UInt0:
                case FormatCode.SmallUInt:
                    return (T[])DecodeArray<uint>(buffer, formatCode, count, (b, f) => UIntEncoding.Decode(b, f).Value);
                case FormatCode.UInt:
                    return (T[])DecodeArray<uint>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadUInt(b));
                case FormatCode.SmallInt:
                    return (T[])DecodeArray<int>(buffer, formatCode, count, (b, f) => IntEncoding.Decode(b, f).Value);
                case FormatCode.Int:
                    return (T[])DecodeArray<int>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadInt(b));
                case FormatCode.ULong0:
                case FormatCode.SmallULong:
                    return (T[])DecodeArray<ulong>(buffer, formatCode, count, (b, f) => ULongEncoding.Decode(b, f).Value);
                case FormatCode.ULong:
                    return (T[])DecodeArray<ulong>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadULong(b));
                case FormatCode.SmallLong:
                    return (T[])DecodeArray<long>(buffer, formatCode, count, (b, f) => LongEncoding.Decode(b, f).Value);
                case FormatCode.Long:
                    return (T[])DecodeArray<long>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadLong(b));
                case FormatCode.Symbol8:
                case FormatCode.Symbol32:
                    return (T[])DecodeArray<AmqpSymbol>(buffer, formatCode, count, (b, f) => SymbolEncoding.Decode(b, f));
                case FormatCode.Boolean:
                    return (T[])DecodeArray<bool>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadUByte(b) > 0);
                case FormatCode.UByte:
                    return (T[])DecodeArray<byte>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadUByte(b));
                case FormatCode.Byte:
                    return (T[])DecodeArray<sbyte>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadByte(b));
                case FormatCode.UShort:
                    return (T[])DecodeArray<ushort>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadUShort(b));
                case FormatCode.Short:
                    return (T[])DecodeArray<short>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadShort(b));
                case FormatCode.Float:
                    return (T[])DecodeArray<float>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadFloat(b));
                case FormatCode.Double:
                    return (T[])DecodeArray<double>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadDouble(b));
                case FormatCode.Decimal32:
                case FormatCode.Decimal64:
                case FormatCode.Decimal128:
                    return (T[])DecodeArray<decimal>(buffer, formatCode, count, (b, f) => DecimalEncoding.DecodeValue(b, f));
                case FormatCode.Char:
                    return (T[])DecodeArray<char>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadChar(b));
                case FormatCode.TimeStamp:
                    return (T[])DecodeArray<DateTime>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadTimestamp(b));
                case FormatCode.Uuid:
                    return (T[])DecodeArray<Guid>(buffer, formatCode, count, (b, f) => AmqpBitConverter.ReadUuid(b));
                default:
                    break;
            }

            // General path
            EncodingBase encoding = AmqpEncoding.GetEncoding(formatCode);
            T[] array = new T[count];
            for (int i = 0; i < count; ++i)
            {
                object value = encoding.DecodeObject(buffer, formatCode);
                if (descriptor != null)
                {
                    value = new DescribedType(descriptor, value);
                }

                array[i] = (T)value;
            }

            return array;
        }
    }
}
