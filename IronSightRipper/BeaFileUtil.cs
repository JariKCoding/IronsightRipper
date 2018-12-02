using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SELib;
using System.Windows.Media.Media3D;

namespace IronsightRipper
{
    class BeaFile
    {
        public static void Decode(string fileName)
        {
            // Get the right export folder and names
            string OutputName = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            string ModelName = Path.GetFileNameWithoutExtension(fileName).Replace("-", "_").Replace("#", "");

            // Make a new stream reader
            using (BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
            {
                // Make sure we are parsing a animation file
                string Magic = reader.ReadFixedString(4);
                if (Magic != "ANIM")
                {
                    Console.WriteLine(String.Format("File Magic \"{0}\" does not match IS's \"ANIM\"", Magic));
                    return;
                }

                // Get anim type
                int AnimType = reader.ReadInt32();
                if(AnimType == 10 || AnimType == 8)
                {
                    // Skip unknown bytes
                    reader.ReadBytes(4);

                    // Get base info of the animation
                    int BoneCount = reader.ReadInt32();
                    int FrameCount = reader.ReadInt32();

                    // Make a new SEAnim to export
                    SEAnim AnimFile = new SEAnim();

                    // Put the base info in the animation
                    AnimFile.BoneCount = BoneCount;
                    AnimFile.AnimType = AnimationType.Additive;
                    AnimFile.FrameCount = FrameCount;

                    // Skip unknown bytes
                    reader.ReadBytes(4);

                    // Go through all the frames
                    for (int i = 0; i < FrameCount; i++)
                    {
                        // Skip 4 bytes for now
                        // Animtype 8  -> frame count
                        // Animtype 10 -> frame byte length
                        reader.ReadBytes(4);

                        // Go through all bone data
                        for (int j = 0; j < BoneCount; j++)
                        {
                            // Get offset to the next joint data
                            long StreamPos = reader.BaseStream.Position + 20;

                            // Read the joint rotation data
                            float BoneDirX = reader.ReadInt16() / 32768.0f;
                            float BoneDirZ = reader.ReadInt16() / 32768.0f;
                            float BoneDirY = reader.ReadInt16() / 32768.0f;
                            float BoneDirW = reader.ReadInt16() / 32768.0f;

                            // Put it in a quaternion object
                            Quaternion BoneDirection = new Quaternion(BoneDirX, BoneDirY, BoneDirZ, BoneDirW);

                            // Add the rotation of the joint to the anim
                            AnimFile.AddRotationKey("tag_is_model_" + j, i, BoneDirection.X, BoneDirection.Y, BoneDirection.Z, BoneDirection.W);

                            // Read joint position data
                            float bonePosX = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            float bonePosZ = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            float bonePosY = System.BitConverter.ToSingle(reader.ReadBytes(4), 0) * -1;

                            // Add the position of the joint to the anim
                            AnimFile.AddTranslationKey("tag_is_model_" + j, i, bonePosX, bonePosY, bonePosZ);

                            // Go to the next joint data
                            reader.Seek(StreamPos, SeekOrigin.Begin);
                        }
                    }

                    // Write the SEAnim file
                    AnimFile.Write(OutputName + ".seanim");

                    // Get the length of the Es file name
                    int EsFileNameLength = reader.ReadInt32();
                    // Only read it if it got an actual name
                    if (EsFileNameLength != 0)
                    {
                        // Read the name
                        string EsFileName = Path.GetFileName(reader.ReadFixedString(EsFileNameLength));

                        // Make a stream reader
                        using (BinaryReader ESreader = new BinaryReader(new FileStream(EsFileName, FileMode.Open)))
                        {
                            // Make sure were reading an ES file
                            if (ESreader.ReadFixedString(4) != "ESES")
                            {
                                Console.WriteLine("File Magic does not match IS's \"ESES\"");
                                return;
                            }

                            // Get the ES type
                            int ESType = ESreader.ReadInt32();

                            int skipInfo = ESreader.ReadInt32();
                        }
                    }
                }
            }
        }
    }
}
