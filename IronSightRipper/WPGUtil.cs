using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace IronsightRipper
{
    class WPGFile
    {
        public static void Decode(string fileName)
        {
            using (BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
            {
                // Check if the file is in fact a WPG file
                if (reader.ReadFixedString(4) != "RPKG")
                {
                    Console.WriteLine("File Magic does not match IS's \"RPKG\"");
                }

                // Go to the start of the assets
                reader.Seek(136, SeekOrigin.Begin);

                // Read how many assets there are
                int AssetCount = reader.ReadInt32();

                // Start task to make is fast as fuck boy
                Task task1 = Task.Factory.StartNew(() => SearchWPGAsset(1, AssetCount, reader));
                Task.WaitAny(task1);
            }
        }

        static private void SearchWPGAsset(int startIndex, int AssetCount, BinaryReader streamReader)
        {
            // Go through all assets
            for (int i = startIndex; i < AssetCount + 1; i += 1)
            {
                // Go to the first byte of the assetinfo
                streamReader.Seek(i * 140, SeekOrigin.Begin);

                // Get all the needed info
                string AssetName = streamReader.ReadFixedString(128);
                int Assetlocation = streamReader.ReadInt32();
                int AssetLength = streamReader.ReadInt32();
                int AssetFlag = streamReader.ReadInt32();
                string AssetFileType = Path.GetExtension(AssetName.Replace("-", "_"));

                // Only export models, textures or sound files 
                if (AssetFileType == ".msh" || AssetFileType == ".dds" || AssetFileType == ".fsb" /*|| AssetFileType == ".bea" || AssetFileType == ".gad"*/)
                {
                    // Go to the asset data
                    streamReader.Seek(Assetlocation, SeekOrigin.Begin);

                    Console.WriteLine("Exporting file  : " + AssetName);

                    int unpackedSize = streamReader.ReadInt32();
                    byte[] EncryptionNumber = streamReader.ReadBytes(2);
                    // Checking if the asset uses zlib as encryption
                    if (EncryptionNumber[0] == 0x78 && EncryptionNumber[1] == 0x9C)
                    {
                        // Get the location to export the asset
                        string ProgramPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        string OutputFolder = ProgramPath + "\\exported_files\\" + AssetName;
                        ScobUtil.CreateFilePath(ProgramPath + "\\exported_files\\" + AssetName);

                        // Skip the files that already are exported
                        if (File.Exists(ProgramPath + "\\exported_files\\" + AssetName))
                        {
                            if (new FileInfo(ProgramPath + "\\exported_files\\" + AssetName).Length == unpackedSize)
                            {
                                continue;
                            }
                        }

                        // Decode the data
                        MemoryStream DecodedCodeStream = DeflateUtil.Decode(streamReader.ReadBytes(AssetLength - 6));

                        // Export raw data for files
                        using (var outputStream = new FileStream(OutputFolder, FileMode.Create))
                        {
                            DecodedCodeStream.CopyTo(outputStream);
                            // Check if its a model that needs to be parsed
                            if (AssetFileType == ".msh")
                            {
                                outputStream.Close();
                                MshFile.Decode(OutputFolder);
                            }
                            // Animations arent parsed right yet (Joint rotations on anims arent working correctly)
                            /*// Check if its an animation that needs to be parsed
                            else if(AssetFileType == ".bea" || AssetFileType == ".gad")
                            {
                                outputStream.Close();
                                BeaFile.Decode(OutputFolder);
                            }*/
                        }
                    }
                }
            }
        }
    }
}
