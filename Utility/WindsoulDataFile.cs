// * ************************************************************
// * * START:                                            wdf.cs *
// * ************************************************************

// * ************************************************************
// *                      INFORMATIONS
// * ************************************************************
// * Windsoul Data File class for the library.
// * wdf.cs
// * 
// * --
// * 
// *  "               ..;;;;,                     ;;;,    
// *  '           ..;;;"` ;;:           ,;;;;;: ,;;;:.,;..          _/
// *  `     ,;..,;;"`    :;;'            :;;"`,;;;;;;"":;;         _/ 
// *        ;;;"  `:;;. ;;'           ..;;:  .;;,.;;:',;;"    _/_/_/_/_/
// *       .;;`   ,;;" .;"          ,;;;;;;" :;`',;",;;"         _/
// *      ,;;,:.,;;;  ,;:          :" ,;:` , `:.;;;;;'`         _/   
// *      ;;"'':;;:. .;; .          ,;;;,;:;;,;;;, ,;             _/
// *     :;;..;;;;;; :;' :.        :;;;"` `:;;;;;,;,.;.          _/
// *   .;;":;;`  '"";;:  ';;       '""   .;;`.;";:;;;;` ;,  _/_/_/_/_/
// *  ;;;" `'       "::. ,;;:          .;"`  ::. '   .,;;;     _/ 
// *  ""             ';;;;;;;"        ""     ';;;;;;;;;;`     _/
// *  
// *                         Windsoul++
// * 
// *                 by ÔÆ·ç (Cloud Wu)  1999-2001
// *  
// * 		http://member.netease.com/~cloudwu 
// * 		mailto:cloudwu@263.net
// *  
// * 		ÇëÔÄ¶Á readme.txt ÖÐµÄ°æÈ¨ÐÅÏ¢
// * 		See readme.txt for copyright information.
// * 
// * 		Description:		·ç»ê++ Êý¾ÝÎÄ¼þ¹ÜÀí
// *  		Original Author:	ÔÆ·ç
// * 		Authors:
// * 		Create Time:		2000/10/16
// * 		Modify Time:		2001/12/26
// * 
// * .:*W*:._.:*I*:._.:*N*:._.:*D*:._.:*S*:._.:*O*:._.:*U*:._.:*L*:._.:*/
// * 
// *
// * Feel free to use this class in your projects, but don't
// * remove the header to keep the paternity of the class.
// * 
// * ************************************************************
// *                      CREDITS
// * ************************************************************
// * Originally created by Cloud Wu (October 16th, 2000)
// * Copyright (C) 2000-2001 Cloud Wu
// *  
// * Implemented by CptSky (January 12th, 2012)
// * Copyright (C) 2012 CptSky
// *
// * Modified by Santa
// *
// * ************************************************************
// *                      SPECIAL THANKS
// * ************************************************************
// * Cloud Wu
// * Sparkie (unknownone @ e*pvp)
// * 
// * ************************************************************

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Utility
{
    public class WindsoulDataFile :IDisposable, IDataPack
    {
        public const uint WDF_ID = 0x57444650;
        internal readonly struct FileEntry
        {
            public uint Id { get; init; }
            public uint Offset { get; init; }
            public uint Size { get; init; }
            public uint Space { get; init; }
        }

        internal readonly struct WDFHeader
        {
            public uint Id { get; init; }
            public int NumberEntries { get; init; }
            public uint Offset { get; init; }
        }

        private WDFHeader Header;
        private Stream WDFStream;
        private Dictionary<uint, FileEntry> PackedFiles = new();
        private BinaryReader BinaryReader;

        public WindsoulDataFile(string FilePath)
        {
            WDFStream = File.OpenRead(FilePath);
            BinaryReader = new BinaryReader(WDFStream);

            Header = new()
            {
                Id = BinaryReader.ReadUInt32(),
                NumberEntries = BinaryReader.ReadInt32(),
                Offset = BinaryReader.ReadUInt32()
            };
            if (Header.Id != WDF_ID) throw new Exception($"Invalid File Type: {FilePath}");

            BinaryReader.BaseStream.Seek(Header.Offset, SeekOrigin.Begin);
            for (int idx = 0; idx < Header.NumberEntries; idx++)
            {
                FileEntry entry = new()
                {
                    Id = BinaryReader.ReadUInt32(),
                    Offset = BinaryReader.ReadUInt32(),
                    Size = BinaryReader.ReadUInt32(),
                    Space = BinaryReader.ReadUInt32()
                };
                PackedFiles.Add(entry.Id, entry);
            }
        }
        public byte[] GetFile(string File)
        {
            uint Id = String2Id(File);

            if(PackedFiles.TryGetValue(Id, out FileEntry fileEntry))
            {
                byte[] entryData = new byte[fileEntry.Size];

                WDFStream.Seek(fileEntry.Offset, SeekOrigin.Begin);
                WDFStream.Read(entryData);
                return entryData;

            }
            return null;
        }
        public MemoryStream GetFileStream(string File)
        {
            byte[]entryData = GetFile(File);
            if (entryData == null) return null;
            return new MemoryStream(entryData);
        }
        private static uint String2Id(string inputText)
        {

            inputText = inputText.ToLowerInvariant();
            inputText = inputText.Replace('\\', '/');

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
            BinaryReader?.Dispose();
            WDFStream?.Dispose();
        }
    }
}
