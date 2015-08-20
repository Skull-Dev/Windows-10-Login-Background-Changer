﻿using System;
using System.Collections.Generic;
using System.IO;

namespace SharedLibrary
{
    public static class LogonPriEditor
    {
        public static void ModifyLogonPri(string currentPri, string outputPri, string image)
        {
            //Change to gen1 if you want to test gen1
            ModifyLogonGen2(currentPri, outputPri, image);
        }

        //Do not delete gen1 as it will be used down the road! (Will throw warning as unused)
        private static void ModifyLogonGen1(string currentPri, string outputPri, string image)
        {
            var inputStream = File.OpenRead(currentPri);
            var outputStream = File.Create(outputPri);
            //var replacementStream = File.OpenRead(image);
            var replacementStream = File.ReadAllBytes(image);

            var inputReader = new BinaryReader(inputStream);
            var outputWriter = new BinaryWriter(outputStream);

            inputStream.CopyTo(outputStream);

            var replacementLengthAligned = (Math.Ceiling((double)replacementStream.Length / 8) * 8);

            //Header
            inputStream.Seek(0x14, SeekOrigin.Begin);
            var headerLength = inputReader.ReadUInt32();
            inputStream.Seek(0xB8, SeekOrigin.Begin);
            var dataitemOffset = inputReader.ReadUInt32();
            var origDataitemLength = inputReader.ReadUInt32();
            var dataitemLength = origDataitemLength + replacementLengthAligned;
            outputStream.Seek(0xBC, SeekOrigin.Begin);
            outputWriter.Write((int)dataitemLength);

            //dataitem
            outputStream.Seek(headerLength + dataitemOffset + 0x18, SeekOrigin.Begin);
            outputWriter.Write((int)dataitemLength);
            inputStream.Seek(headerLength + dataitemOffset + 0x24, SeekOrigin.Begin);
            var stringCount = inputReader.ReadUInt16();
            var blobCount = inputReader.ReadUInt16();
            var origDataLength = inputReader.ReadUInt32();
            outputStream.Seek(0xC, SeekOrigin.Current);
            outputWriter.Write((int)(origDataLength + replacementLengthAligned));
            outputStream.Seek(stringCount * 4, SeekOrigin.Current);
            for (var i = 0; i < 10; i++)
            {
                outputWriter.Write(origDataLength);
                outputWriter.Write((int)replacementStream.Length);
            }
            outputStream.Seek((blobCount - 10) * 8, SeekOrigin.Current);

            //data
            outputStream.Seek(origDataLength, SeekOrigin.Current);
            if (outputStream.Length - outputStream.Position != 0x18)
            {
                throw new Exception("Not compatible with this PRI file.");
            }
            outputWriter.Write(replacementStream);
            //replacementStream.CopyTo(outputStream);

            //footer
            outputStream.Seek((long)(replacementLengthAligned - replacementStream.Length), SeekOrigin.Current);
            outputWriter.Write(0xDEF5FADE);
            outputWriter.Write((int)dataitemLength);
            outputWriter.Write(0xDEFFFADE);
            outputWriter.Write(0x00000000);
            outputWriter.Write("mrm_pri2".ToCharArray());

            outputStream.Seek(0xC, SeekOrigin.Begin);
            outputWriter.Write((int)outputStream.Length);
            outputStream.Seek(-0xC, SeekOrigin.End);
            outputWriter.Write((int)outputStream.Length);

            inputReader.Close();
            outputWriter.Close();
            //replacementStream.Close();
        }

        private static void ModifyLogonGen2(string currentPri, string outputPri, string image)
        {
            var inputStream = File.OpenRead(currentPri);
            var outputStream = File.Create(outputPri);
            var replacementStream = File.OpenRead(image);

            var inputReader = new BinaryReader(inputStream);
            var outputWriter = new BinaryWriter(outputStream);

            inputStream.CopyTo(outputStream);

            var replacementLengthAligned = (Math.Ceiling((double) replacementStream.Length/8) * 8);

            // header
            inputStream.Seek(0x14, SeekOrigin.Begin);
            var headerLength = inputReader.ReadUInt32();
            inputStream.Seek(0xB8, SeekOrigin.Begin);
            var dataitemOffset = inputReader.ReadUInt32();
            var origDataitemLength = inputReader.ReadUInt32();
            var dataitemLength = origDataitemLength + replacementLengthAligned;
            outputStream.Seek(0xBC, SeekOrigin.Begin);
            outputWriter.Write((int)dataitemLength);

            // dataitem
            outputStream.Seek(headerLength + dataitemOffset + 0x18, SeekOrigin.Begin);
            outputWriter.Write((int) dataitemLength);
            inputStream.Seek(headerLength + dataitemOffset + 0x24, SeekOrigin.Begin);
            var stringCount = inputReader.ReadUInt16();
            var blobCount = inputReader.ReadUInt16();
            var origDataLength = inputReader.ReadUInt32();
            outputStream.Seek(0xC, SeekOrigin.Current);
            outputWriter.Write((int) (origDataLength + replacementLengthAligned));
            outputStream.Seek(stringCount * 4, SeekOrigin.Current);
            var blobTableOffset = outputStream.Position;
            var dataOffset = blobTableOffset + blobCount*8;
            var wallpaperBlobs = new List<int>();
            for (var i = 0; i < blobCount; i++)
            {
                inputStream.Seek(blobTableOffset + i * 8, SeekOrigin.Begin);
                var offset = inputReader.ReadUInt32();
                var length = inputReader.ReadUInt32();
                inputStream.Seek(dataOffset + offset, SeekOrigin.Begin);
                if (inputReader.ReadUInt16() == 0xD8FF)
                {
                    wallpaperBlobs.Add(i);
                }
            }
            if (wallpaperBlobs.Count != 10)
            {
                throw new Exception("Not compatible with this PRI file.");
            }
            foreach (var id in wallpaperBlobs)
            {
                outputStream.Seek(blobTableOffset + id * 8, SeekOrigin.Begin);
                outputWriter.Write(origDataLength);
                outputWriter.Write((int) replacementStream.Length);
            }

            //data
            outputStream.Seek(dataOffset + origDataLength, SeekOrigin.Begin);
            if (outputStream.Length - outputStream.Position != 0x18)
            {
                throw new Exception("Not compatible with this PRI file.");
            }
            replacementStream.CopyTo(outputStream);

            // footer
            outputStream.Seek((long)(replacementLengthAligned - replacementStream.Length), SeekOrigin.Current);
            outputWriter.Write(0xDEF5FADE);
            outputWriter.Write((int)dataitemLength);
            outputWriter.Write(0xDEFFFADE);
            outputWriter.Write(0x00000000);
            outputWriter.Write("mrm_pri2".ToCharArray());

            outputStream.Seek(0xC, SeekOrigin.Begin);
            outputWriter.Write((int) outputStream.Length);
            outputStream.Seek(-0xC, SeekOrigin.End);
            outputWriter.Write((int)outputStream.Length);

            inputReader.Close();
            outputWriter.Close();
            replacementStream.Close();
        }
    }
}