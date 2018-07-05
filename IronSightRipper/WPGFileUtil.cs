using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace IronSightRipper
{
    public class WPGFile
    {
        public class WpgFileHeader
        {
            public string Magic { get; set; }

            public string Name { get; set; }

            public static WpgFileHeader Load(BinaryReader inputStream)
            {
                WpgFileHeader header = new WpgFileHeader();

                header.Magic = inputStream.ReadFixedString(4);
                inputStream.Seek(4, SeekOrigin.Current);
                header.Name = inputStream.ReadFixedString(16);

                return header;
            }
        }

        public WpgFileHeader Header { get; set; }

        public void Decode(string fileName)
        {
            using (BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
            {
                Header = WpgFileHeader.Load(reader);

                if (Header.Magic != "RPKG")
                {
                    Console.WriteLine(String.Format("File Magic \"{0}\" does not match IS's \"RPKG\"", Header.Magic));
                    return;
                }

                reader.Seek(136, SeekOrigin.Begin);

                int AssetCount = reader.ReadInt32();

                for (int i = 1; i < AssetCount + 1; i++)
                {
                    reader.Seek(i * 140, SeekOrigin.Begin);
                    string FileNameString = reader.ReadFixedString(128);
                    int location = reader.ReadInt32();
                    int length = reader.ReadInt32();
                    string filetype = Path.GetExtension(FileNameString.Replace("-", "_"));
                    if (filetype == ".msh" || filetype == ".dds" || filetype == ".fsb")
                    {
                        reader.Seek(location, SeekOrigin.Begin);
                        Console.WriteLine("Exporting file  : " + FileNameString);
                        int blockSize = reader.ReadInt32();
                        int EncryptionNumber = reader.ReadInt16();
                        if (EncryptionNumber == -25480)
                        {
                            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                            string output = path + "\\exported_files\\" + FileNameString;
                            ScobUtil.CreateFilePath(path + "\\exported_files\\" + FileNameString);
                            MemoryStream DecodedCodeStream = HashUtil.Decode(reader.ReadBytes(blockSize - 2), FileNameString);
                            using (var outputStream = new FileStream(output, FileMode.Create))
                            {
                                DecodedCodeStream.CopyTo(outputStream);
                                if(filetype == ".msh")
                                {
                                    outputStream.Close();
                                    MshFile mshFile = new MshFile();
                                    mshFile.Decode(output);
                                    File.Delete(output);
                                }
                            }
                        }
                    }
                } 
            }
        }
    }
}
