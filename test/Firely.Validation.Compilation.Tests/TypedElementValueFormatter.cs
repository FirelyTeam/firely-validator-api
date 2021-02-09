// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Globalization;
using System.Text;
using S = Hl7.Fhir.ElementModel.Types;

namespace MessagePack.Formatters
{
    public class TypedElementValueFormatter : IMessagePackFormatter<object?>
    {
        public const sbyte DATETIME_EXT_CODE = 50;
        public const sbyte TIME_EXT_CODE = 51;
        public const sbyte DATE_EXT_CODE = 52;
        public const sbyte DECIMAL_EXT_CODE = 53;

        public static readonly IMessagePackFormatter<object?> Instance = new TypedElementValueFormatter();

        private TypedElementValueFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, object? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            switch (value)
            {
                case S.DateTime dtm:
                    {
                        var serialized = Encoding.UTF8.GetBytes(dtm.ToString());
                        writer.WriteExtensionFormat(new ExtensionResult(DATETIME_EXT_CODE, serialized));
                        break;
                    }
                case S.Time tm:
                    {
                        var serialized = Encoding.UTF8.GetBytes(tm.ToString());
                        writer.WriteExtensionFormat(new ExtensionResult(TIME_EXT_CODE, serialized));
                        break;
                    }
                case S.Date dt:
                    {
                        var serialized = Encoding.UTF8.GetBytes(dt.ToString());
                        writer.WriteExtensionFormat(new ExtensionResult(DATE_EXT_CODE, serialized));
                        break;
                    }
                case decimal d:
                    {
                        var serialized = Encoding.UTF8.GetBytes(d.ToString(CultureInfo.InvariantCulture));
                        writer.WriteExtensionFormat(new ExtensionResult(DECIMAL_EXT_CODE, serialized));
                        break;
                    }
                case bool b:
                    writer.Write(b);
                    break;
                case long l:
                    writer.WriteInt64(l);
                    break;
                case string s:
                    writer.Write(s);
                    break;
                default:
                    throw new MessagePackSerializationException($"Encountered unsupported ITypedElement.Value of type {value.GetType()}");
            }
        }


        public object? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            MessagePackType type = reader.NextMessagePackType;

            switch (type)
            {
                case MessagePackType.Integer:
                    var code = reader.NextCode;
                    return code == MessagePackCode.Int64
                        ? reader.ReadInt64()
                        : throw new MessagePackSerializationException($"Invalid integer type for ITypedElement.Value. Found pack code {code}.");
                case MessagePackType.Boolean:
                    return reader.ReadBoolean();
                case MessagePackType.String:
                    return reader.ReadString();
                case MessagePackType.Extension:
                    {
                        var ext = reader.ReadExtensionFormat();

                        switch (ext.TypeCode)
                        {
                            case DATETIME_EXT_CODE:
                                {
                                    var value = Encoding.UTF8.GetString(ext.Data.ToArray());
                                    return S.DateTime.Parse(value);
                                }
                            case TIME_EXT_CODE:
                                {
                                    var value = Encoding.UTF8.GetString(ext.Data.ToArray());
                                    return S.Time.Parse(value);
                                }
                            case DATE_EXT_CODE:
                                {
                                    var value = Encoding.UTF8.GetString(ext.Data.ToArray());
                                    return S.Date.Parse(value);
                                }
                            case DECIMAL_EXT_CODE:
                                {
                                    var value = Encoding.UTF8.GetString(ext.Data.ToArray());
                                    return decimal.Parse(value, CultureInfo.InvariantCulture);
                                }
                            default:
                                throw new MessagePackSerializationException($"Invalid extension type for ITypedElement.Value. Found extension type code {ext.TypeCode}.");
                        }
                    }
                case MessagePackType.Nil:
                    reader.ReadNil();
                    return null;
                default:
                    throw new MessagePackSerializationException("Invalid primitive bytes.");
            }
        }
    }
}
