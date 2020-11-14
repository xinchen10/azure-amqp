// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Amqp.Encoding
{
    using System.Text;
    using Microsoft.Azure.Amqp.Framing;

    sealed class SymbolEncoding : EncodingBase
    {
        public SymbolEncoding()
            : base(FormatCode.Symbol32)
        {
        }

        public static int GetValueSize(AmqpSymbol value)
        {
            return value.Value == null ? FixedWidth.Null : Encoding.ASCII.GetByteCount(value.Value);
        }

        public static int GetEncodeSize(AmqpSymbol value)
        {
            if (value.Value == null)
            {
                return FixedWidth.NullEncoded;
            }

            int valueSize = GetValueSize(value);
            return FixedWidth.FormatCode + AmqpEncoding.GetEncodeWidthBySize(valueSize) + valueSize;
        }

        public static void Encode(AmqpSymbol value, ByteBuffer buffer)
        {
            if (value.Value == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                int width = AmqpEncoding.GetEncodeWidthBySize(value.Value.Length);
                Encode(value, width == FixedWidth.UByte ? FormatCode.Symbol8 : FormatCode.Symbol32, buffer);
            }
        }

        public static AmqpSymbol Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return new AmqpSymbol();
            }

            int count;
            AmqpEncoding.ReadCount(buffer, formatCode, FormatCode.Symbol8, FormatCode.Symbol32, out count);
            string value = Encoding.ASCII.GetString(buffer.Buffer, buffer.Offset, count);
            buffer.Complete(count);

            return new AmqpSymbol(value);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.UInt + Encoding.ASCII.GetByteCount(((AmqpSymbol)value).Value);
            }
            else
            {
                return SymbolEncoding.GetEncodeSize((AmqpSymbol)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                SymbolEncoding.Encode((AmqpSymbol)value, FixedWidth.UInt, buffer);
            }
            else
            {
                SymbolEncoding.Encode((AmqpSymbol)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return SymbolEncoding.Decode(buffer, formatCode);
        }

        internal static void Encode(AmqpSymbol value, FormatCode formatCode, ByteBuffer buffer)
        {
            AmqpBitConverter.WriteUByte(buffer, formatCode);
            EncodeValue(value, formatCode, buffer);
        }

        internal static void EncodeValue(AmqpSymbol value, FormatCode formatCode, ByteBuffer buffer)
        {
            int len = value.Value.Length;
            if (formatCode == FormatCode.Symbol8)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)len);
            }
            else
            {
                AmqpBitConverter.WriteUInt(buffer, (uint)len);
            }

            buffer.Validate(write: true, len);
            int bytes = Encoding.ASCII.GetBytes(value.Value, 0, len, buffer.Buffer, buffer.WritePos);
            if (bytes != len)
            {
                throw new AmqpException(new Error()
                {
                    Condition = AmqpErrorCode.InternalError,
                    Description = "Symbol encoded byte count not equal to its length."
                });
            }

            buffer.Append(len);
        }
    }
}
