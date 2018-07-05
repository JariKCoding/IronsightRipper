using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

/*
    Copyright (c) 2018 Philip/Scobalula - Utility Lib

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

namespace IronSightRipper
{
    class ScobUtil
    {
        public static bool CanAccessFile(string file)
        {
            if (String.IsNullOrEmpty(file))
                return false;

            if (!File.Exists(file))
                return false;

            FileStream stream = null;

            try
            {
                stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch
            {
                return false;
            }
            finally
            {
                stream?.Close();
            }

            return true;
        }

        /// <summary>
        /// Creates directories for a given path
        /// </summary>
        /// <param name="filePath">File Path</param>
        public static void CreateFilePath(string filePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            catch
            {
                return;
            }
        }
    }

    /// <summary>
    /// Common Hash Functions
    /// </summary>
    public static class HashUtil
    {
        public static MemoryStream Decode(byte[] data, string FileNameString)
        {
            MemoryStream output = new MemoryStream();
            MemoryStream input = new MemoryStream(data);

            using (DeflateStream deflateStream = new DeflateStream(input, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(output);
            }

            output.Flush();
            output.Position = 0;

            return output;
        }

        public static MemoryStream Encode(byte[] data)
        {

            MemoryStream output = new MemoryStream();

            using (DeflateStream deflateStream = new DeflateStream(output, CompressionMode.Compress))
            {
                deflateStream.Write(data, 0, data.Length);
            }

            return output;
        }
    }

    /// <summary>
    /// Class containing extensions to BinaryReader/Writer
    /// </summary>
    public static class BinaryIOExtensions
    {
        public static string ReadFixedString(this BinaryReader br, int numBytes)
        {
            return Encoding.ASCII.GetString(br.ReadBytes(numBytes)).TrimEnd('\0');
        }

        /// <summary>
        /// Sets the position of the Base Stream
        /// </summary>
        /// <param name="br"></param>
        /// <param name="offset">Offset to seek to.</param>
        /// <param name="seekOrigin">Seek Origin</param>
        public static void Seek(this BinaryReader br, long offset, SeekOrigin seekOrigin)
        {
            br.BaseStream.Seek(offset, seekOrigin);
        }

        /// <summary>
        /// Searches for bytes in file and returns offsets.
        /// </summary>
        /// <param name="br"></param>
        /// <param name="needle">Bytes to search for.</param>
        /// <returns></returns>
        public static long[] FindBytes(this BinaryReader br, byte[] needle, bool firstOccurence = false, long? from = null, bool byteStart = false)
        {
            /*
               TODO: Needs heavy improvement.

                Switched to buffer than byte by byte,
                MUCH faster.
            */
            if (from != null)
                br.Seek((long)from, SeekOrigin.Begin);
            // List of offsets in file.
            List<long> offsets = new List<long>();
            // Buffer
            byte[] buffer = new byte[1048576];
            // Bytes Read
            int bytesRead = 0;
            // Starting Offset
            long readBegin = br.BaseStream.Position;
            // Needle Index
            int needleIndex = 0;
            // Byte Array Index
            int bufferIndex = 0;
            // Read chunk of file
            while ((bytesRead = br.BaseStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                // Loop through byte array
                for (bufferIndex = 0; bufferIndex < bytesRead; bufferIndex++)
                {
                    // Check if current bytes match
                    if (needle[needleIndex] == buffer[bufferIndex])
                    {
                        // Indc
                        needleIndex++;
                        // Check if we have a match
                        if (needleIndex == needle.Length)
                        {
                            // Add Offset
                            offsets.Add(readBegin + bufferIndex + 1 - (byteStart ? needle.Length : 0));
                            // Reset Index
                            needleIndex = 0;
                            // Check before continuing
                            if (needle[needleIndex] == buffer[bufferIndex])
                                needleIndex++;
                            // If only first occurence, end search
                            if (firstOccurence)
                                goto complete;
                        }
                    }
                    else
                    {
                        // Reset Index
                        needleIndex = 0;
                        // TODO: Better way of checking if was match then 
                        // then didn't match, for now this
                        if (needle[needleIndex] == buffer[bufferIndex])
                            needleIndex++;
                    }
                }
                // Set next offset
                readBegin += bytesRead;
            }
            complete:;
            // Return offsets as an array
            return offsets.ToArray();
        }
    }
}
