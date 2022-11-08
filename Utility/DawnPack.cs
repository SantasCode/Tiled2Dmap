// * ************************************************************
// * * START:                                            dnp.cs *
// * ************************************************************

// * ************************************************************
// *                      INFORMATIONS
// * ************************************************************
// * TQ Digital DawnPack class for the library.
// * wdf.cs
// * 
// * Feel free to use this class in your projects, but don't
// * remove the header to keep the paternity of the class.
// * 
// * ************************************************************
// *                      CREDITS
// * ************************************************************
// * Originally created by CptSky (June 16th, 2012)
// * Copyright (C) 2012 CptSky
// *
// * Modified by Santa
// *
// * ************************************************************
// *                      SPECIAL THANKS
// * ************************************************************
// * Sparkie (unknownone @ e*pvp)
// * 
// * ************************************************************


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiled2Dmap.CLI.Extensions;

namespace Tiled2Dmap.CLI.Utility
{
    internal class DawnPack : IDataPack
    {
        public const string DNP_IDENTIFIER = "DawnPack.TqDigital";
        public const int MAX_IDENTIFIERSIZE = 0x20;

        public const int MIN_VERSION = 1000;
        public const int MAX_VERSION = 1001;

        internal struct Header
        {
            public string Identifier;
            public int Version;
            public int Number;
        };

        internal struct Entry
        {
            public uint UID;
            public uint Size;
            public uint Offset; //Address of the first byte of data
        };

        private Header pHeader;
        private Stream DNPStream;
        private BinaryReader binaryReader;
        private Dictionary<uint, Entry> Entries;

        private String Filename = null;

        public String GetFilename() { return Filename; }
        public int GetAmount() { return pHeader.Number; }

        public DawnPack(string filePath)
        {
            DNPStream = File.OpenRead(filePath);
            binaryReader = new BinaryReader(DNPStream);
            
            pHeader = new()          
            {
                Identifier = binaryReader.ReadASCIIString(MAX_IDENTIFIERSIZE),
                Version = binaryReader.ReadInt32(),
                Number = binaryReader.ReadInt32()
            };

            if (pHeader.Identifier != DNP_IDENTIFIER)
                throw new Exception("Invalid DNP Header in file " + Filename);

            if (pHeader.Version < MIN_VERSION || pHeader.Version > MAX_VERSION)
                throw new Exception("Unsupported DNP version for file: " + Filename);

            Entries = new(pHeader.Number);

            for (Int32 i = 0; i < pHeader.Number; i++)
            {
                Entry entry;
                if (pHeader.Version == 1001)
                {
                    entry = new Entry()
                    {
                        UID = binaryReader.ReadUInt32() ^ 0x95279527,
                        Size = binaryReader.ReadUInt32() ^ 0x96120059,
                        Offset = binaryReader.ReadUInt32() ^ 0x99589958
                    };
                }
                else
                {
                    entry = new Entry()
                    {
                        UID = binaryReader.ReadUInt32(),
                        Size = binaryReader.ReadUInt32(),
                        Offset = binaryReader.ReadUInt32()
                    };
                }


                if (Entries.ContainsKey(entry.UID))
                    throw new Exception("Doublon of " + entry.UID + " in the file: " + Filename);

                Entries.Add(entry.UID, entry);
            }
        }

        public bool ContainsEntry(uint Id)
        {
            lock(Entries)
            {
                return Entries.ContainsKey(Id);
            }
        }
        public byte[] GetFile(string File)
        {
            uint Id = String2Id(File);

            if(Entries.TryGetValue(Id, out Entry entry))
            {
                byte[] data = new byte[entry.Size];

                DNPStream.Seek(entry.Offset, SeekOrigin.Begin);
                DNPStream.Read(data);
                return data;
            }
            return null;
        }
        public MemoryStream GetFileStream(string File)
        {
            byte[] entryData = GetFile(File);
            if (entryData == null) return null;
            return new MemoryStream(entryData);
        }

        private static uint String2Id(string inputText)
        {

            inputText = inputText.ToLowerInvariant();
            inputText = inputText.Replace('/', '\\');

            //x86 - 32 bits - Registers
            uint eax, ebx, ecx, edx, edi, esi;
            ulong num = 0;

            uint v;
            int i;
            Span<uint> m = stackalloc uint[0x46];
            Span<byte> buffer = stackalloc byte[0x100];

            var input = inputText.ToLowerInvariant();

            for (i = 0; i < input.Length; i++)
                buffer[i] = (byte)input[i];

            int Length = (input.Length % 4 == 0 ? input.Length / 4 : input.Length / 4 + 1);
            for (i = 0; i < Length; i++)
                m[i] = BitConverter.ToUInt32(buffer.Slice(i * 4));

            m[i++] = 0x9BE74448; //
            m[i++] = 0x66F42C48; //

            v = 0xF4FA8928; //

            edi = 0x7758B42B;
            esi = 0x37A8470E; //

            for (ecx = 0; ecx < i; ecx++)
            {
                ebx = 0x267B0B11; //
                v = (v << 1) | (v >> 0x1F);
                ebx ^= v;
                eax = m[(int)ecx];
                esi ^= eax;
                edi ^= eax;
                edx = ebx;
                edx += edi;
                edx |= 0x02040801; // 
                edx &= 0xBFEF7FDF;//
                num = edx;
                num *= esi;
                eax = (uint)num;
                edx = (uint)(num >> 0x20);
                if (edx != 0)
                    eax++;
                num = eax;
                num += edx;
                eax = (uint)num;
                if (((uint)(num >> 0x20)) != 0)
                    eax++;
                edx = ebx;
                edx += esi;
                edx |= 0x00804021; //
                edx &= 0x7DFEFBFF; //
                esi = eax;
                num = edi;
                num *= edx;
                eax = (uint)num;
                edx = (uint)(num >> 0x20);
                num = edx;
                num += edx;
                edx = (uint)num;
                if (((uint)(num >> 0x20)) != 0)
                    eax++;
                num = eax;
                num += edx;
                eax = (uint)num;
                if (((uint)(num >> 0x20)) != 0)
                    eax += 2;
                edi = eax;
            }
            esi ^= edi;
            v = esi;

            return v;
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
