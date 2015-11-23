// --------------------------------------------------------------------------------------------------
// <copyright file = "TraceEventInfoWrapper.cs" company="Nino Crudele">
//   Copyright (c) 2013 - 2015 Nino Crudele. All Rights Reserved.
// </copyright>
// <summary>
// The MIT License (MIT)
// 
// Copyright (c) 2013 - 2015 Nino Crudele
// Blog: http://ninocrudele.me
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </summary>
// --------------------------------------------------------------------------------------------------
namespace Core.Eventing.Interop
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    using GrabCaster.SDK.ETW;

    internal sealed class TraceEventInfoWrapper : IDisposable
    {
        /// <summary>
        ///     Base address of the native TraceEventInfo structure.
        /// </summary>
        private IntPtr _address;

        /// <summary>
        ///     Marshalled array of EventPropertyInfo objects.
        /// </summary>
        private EventPropertyInfo[] _eventPropertyInfoArray;

        //
        // True if the event has a schema with well defined properties.
        //
        private bool _hasProperties;

        /// <summary>
        ///     Managed representation of the native TraceEventInfo structure.
        /// </summary>
        private TraceEventInfo _traceEventInfo;

        internal TraceEventInfoWrapper(EventRecord eventRecord)
        {
            this.Initialize(eventRecord);
        }

        internal string EventName { private set; get; }

        public void Dispose()
        {
            this.ReleaseMemory();
            GC.SuppressFinalize(this);
        }

        ~TraceEventInfoWrapper()
        {
            this.ReleaseMemory();
        }

        internal PropertyBag GetProperties(EventRecord eventRecord)
        {
            // We only support top level properties and simple types
            var properties = new PropertyBag(this._traceEventInfo.TopLevelPropertyCount);

            if (this._hasProperties)
            {
                var offset = 0;

                for (var i = 0; i < this._traceEventInfo.TopLevelPropertyCount; i++)
                {
                    var info = this._eventPropertyInfoArray[i];

                    // Read the current property name
                    var propertyName = Marshal.PtrToStringUni(new IntPtr(this._address.ToInt64() + info.NameOffset));

                    object value;
                    string mapName;
                    int length;
                    var dataPtr = new IntPtr(eventRecord.UserData.ToInt64() + offset);

                    value = this.ReadPropertyValue(info, dataPtr, out mapName, out length);

                    // If we have a map name, return both map name and map value as a pair.
                    if (!string.IsNullOrEmpty(mapName))
                    {
                        value = new KeyValuePair<string, object>(mapName, value);
                    }

                    offset += length;
                    properties.Add(propertyName, value);
                }

                if (offset < eventRecord.UserDataLength)
                {
                    //
                    // There is some extra information not mapped.
                    //
                    var dataPtr = new IntPtr(eventRecord.UserData.ToInt64() + offset);
                    var length = eventRecord.UserDataLength - offset;
                    var array = new byte[length];

                    for (var index = 0; index < length; index++)
                    {
                        array[index] = Marshal.ReadByte(dataPtr, index);
                    }

                    properties.Add("__ExtraPayload", array);
                }
            }
            else
            {
                // NOTE: It is just a guess that this is an Unicode string
                var str = Marshal.PtrToStringUni(eventRecord.UserData);

                properties.Add("EventData", str);
            }

            return properties;
        }

        private void Initialize(EventRecord eventRecord)
        {
            var size = 0;
            const uint BufferTooSmall = 122;
            const uint ErrorlementNotFound = 1168;

            var error = NativeMethods.TdhGetEventInformation(ref eventRecord, 0, IntPtr.Zero, IntPtr.Zero, ref size);
            if (error == ErrorlementNotFound)
            {
                // Nothing else to do here.
                this._hasProperties = false;
                return;
            }
            this._hasProperties = true;

            if (error != BufferTooSmall)
            {
                throw new Win32Exception(error);
            }

            // Get the event information (schema)
            this._address = Marshal.AllocHGlobal(size);
            error = NativeMethods.TdhGetEventInformation(ref eventRecord, 0, IntPtr.Zero, this._address, ref size);
            if (error != 0)
            {
                throw new Win32Exception(error);
            }

            // Marshal the first part of the trace event information.
            this._traceEventInfo = (TraceEventInfo)Marshal.PtrToStructure(this._address, typeof(TraceEventInfo));

            // Marshal the second part of the trace event information, the array of property info.
            var actualSize = Marshal.SizeOf(this._traceEventInfo);
            if (size != actualSize)
            {
                var structSize = Marshal.SizeOf(typeof(EventPropertyInfo));
                var itemsLeft = (size - actualSize) / structSize;

                this._eventPropertyInfoArray = new EventPropertyInfo[itemsLeft];
                var baseAddress = this._address.ToInt64() + actualSize;
                for (var i = 0; i < itemsLeft; i++)
                {
                    var structPtr = new IntPtr(baseAddress + (i * structSize));
                    var info = (EventPropertyInfo)Marshal.PtrToStructure(structPtr, typeof(EventPropertyInfo));
                    this._eventPropertyInfoArray[i] = info;
                }
            }

            // Get the opcode name
            if (this._traceEventInfo.OpcodeNameOffset > 0)
            {
                this.EventName =
                    Marshal.PtrToStringUni(new IntPtr(this._address.ToInt64() + this._traceEventInfo.OpcodeNameOffset));
            }
        }

        private object ReadPropertyValue(EventPropertyInfo info, IntPtr dataPtr, out string mapName, out int length)
        {
            length = info.LengthPropertyIndex;

            if (info.NonStructTypeValue.MapNameOffset != 0)
            {
                mapName =
                    Marshal.PtrToStringUni(new IntPtr(this._address.ToInt64() + info.NonStructTypeValue.MapNameOffset));
            }
            else
            {
                mapName = string.Empty;
            }

            switch (info.NonStructTypeValue.InType)
            {
                case TdhInType.Null:
                    break;
                case TdhInType.UnicodeString:
                    {
                        var str = Marshal.PtrToStringUni(dataPtr);
                        length = (str.Length + 1) * sizeof(char);
                        return str;
                    }
                case TdhInType.AnsiString:
                    {
                        var str = Marshal.PtrToStringAnsi(dataPtr);
                        length = (str.Length + 1);
                        return str;
                    }
                case TdhInType.Int8:
                    return (sbyte)Marshal.ReadByte(dataPtr);
                case TdhInType.UInt8:
                    return Marshal.ReadByte(dataPtr);
                case TdhInType.Int16:
                    return Marshal.ReadInt16(dataPtr);
                case TdhInType.UInt16:
                    return (uint)Marshal.ReadInt16(dataPtr);
                case TdhInType.Int32:
                    return Marshal.ReadInt32(dataPtr);
                case TdhInType.UInt32:
                    return (uint)Marshal.ReadInt32(dataPtr);
                case TdhInType.Int64:
                    return Marshal.ReadInt64(dataPtr);
                case TdhInType.UInt64:
                    return (ulong)Marshal.ReadInt64(dataPtr);
                case TdhInType.Float:
                    break;
                case TdhInType.Double:
                    break;
                case TdhInType.Boolean:
                    return Marshal.ReadInt32(dataPtr) != 0;
                case TdhInType.Binary:
                    break;
                case TdhInType.Guid:
                    return new Guid(
                        Marshal.ReadInt32(dataPtr),
                        Marshal.ReadInt16(dataPtr, 4),
                        Marshal.ReadInt16(dataPtr, 6),
                        Marshal.ReadByte(dataPtr, 8),
                        Marshal.ReadByte(dataPtr, 9),
                        Marshal.ReadByte(dataPtr, 10),
                        Marshal.ReadByte(dataPtr, 11),
                        Marshal.ReadByte(dataPtr, 12),
                        Marshal.ReadByte(dataPtr, 13),
                        Marshal.ReadByte(dataPtr, 14),
                        Marshal.ReadByte(dataPtr, 15));
                case TdhInType.Pointer:
                    break;
                case TdhInType.FileTime:
                    return DateTime.FromFileTime(Marshal.ReadInt64(dataPtr));
                case TdhInType.SystemTime:
                    break;
                case TdhInType.SID:
                    break;
                case TdhInType.HexInt32:
                    break;
                case TdhInType.HexInt64:
                    break;
                case TdhInType.CountedString:
                    break;
                case TdhInType.CountedAnsiString:
                    break;
                case TdhInType.ReversedCountedString:
                    break;
                case TdhInType.ReversedCountedAnsiString:
                    break;
                case TdhInType.NonNullTerminatedString:
                    break;
                case TdhInType.NonNullTerminatedAnsiString:
                    break;
                case TdhInType.UnicodeChar:
                    break;
                case TdhInType.AnsiChar:
                    break;
                case TdhInType.SizeT:
                    break;
                case TdhInType.HexDump:
                    break;
                case TdhInType.WbemSID:
                    break;
                default:
                    Debugger.Break();
                    break;
            }

            throw new NotSupportedException();
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private void ReleaseMemory()
        {
            if (this._address != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this._address);
                this._address = IntPtr.Zero;
            }
        }
    }
}