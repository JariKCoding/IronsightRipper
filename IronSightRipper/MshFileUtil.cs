using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace IronSightRipper
{
    public class MshFile
    {
        public void Decode(string fileName)
        {
            string outputname = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            string modelname = Path.GetFileNameWithoutExtension(fileName).Replace("-", "_").Replace("#", "");

            using (BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
            {
                string Magic = reader.ReadFixedString(4);
                if (Magic != "MESH")
                {
                    Console.WriteLine(String.Format("File Magic \"{0}\" does not match IS's \"MESH\"", Magic));
                    return;
                }

                if(reader.BaseStream.Length < 55)
                {
                    Console.Write("Empty Model : Skipping");
                    return;
                }

                reader.Seek(36, SeekOrigin.Begin);
                int BoneCount = reader.ReadInt32();

                float[,] boneCoors = new float[BoneCount, 3];
                int[] boneparents = new int[BoneCount];

                for (int j = 0; j < BoneCount; j++)
                {
                    long pos = reader.BaseStream.Position + 56;
                    int BoneHash = reader.ReadInt32();
                    int Unk = reader.ReadInt32();
                    int BoneParent = reader.ReadInt32();
                    float boneCoorX = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                    float boneCoorZ = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                    float boneCoorY = System.BitConverter.ToSingle(reader.ReadBytes(4), 0) * -1;
                    boneparents[j] = BoneParent;
                    boneCoors[j, 0] = boneCoorX;
                    boneCoors[j, 1] = boneCoorY;
                    boneCoors[j, 2] = boneCoorZ;
                    reader.Seek(pos, SeekOrigin.Begin);
                }

                int Weirdextrathing = reader.ReadInt32();
                if(Weirdextrathing == 2)
                {
                    reader.ReadBytes(12);
                }
                reader.ReadBytes(8);

                int MeshCount = reader.ReadInt32();
                int MatCount = reader.ReadInt32();

                List<float[,]> VertexList = new List<float[,]>();
                List<float[,]> UVList = new List<float[,]>();
                List<float[,]> FacesList = new List<float[,]>();
                List<float[,]> WeightList = new List<float[,]>();


                for (int j = 0; j < MeshCount; j++)
                {
                    reader.ReadBytes(8);
                    int WShift = reader.ReadInt32();
                    int VSecSize = reader.ReadInt32();

                    float[,] vertexCoorValues = new float[VSecSize / 32, 3];
                    float[,] UVValues = new float[VSecSize / 32, 2];
                    float[,] WeightValues = new float[VSecSize / 32, 8];

                    if (WShift == 2)
                    {
                        float[,] vertexCoorValues2 = new float[VSecSize / 28, 3];
                        float[,] UVValues2 = new float[VSecSize / 28, 2];

                        for (int h = 0; h < (VSecSize / 28); h++)
                        {
                            long pos = reader.BaseStream.Position + 28;
                            vertexCoorValues2[h, 0] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            vertexCoorValues2[h, 2] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            vertexCoorValues2[h, 1] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0) * -1;
                            UVValues2[h,0] = reader.ReadInt16() / 1024.0f;
                            UVValues2[h,1] = reader.ReadInt16() / 1024.0f;

                            reader.Seek(pos, SeekOrigin.Begin);
                        }
                        vertexCoorValues = vertexCoorValues2;
                        UVValues = UVValues2;
                    }
                    if (WShift == 3)
                    {
                        for (int h = 0; h < VSecSize / 32; h++)
                        {
                            long pos = reader.BaseStream.Position + 32;
                            vertexCoorValues[h, 0] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            vertexCoorValues[h, 2] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            vertexCoorValues[h, 1] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0) * -1;
                            UVValues[h, 0] = reader.ReadInt16() / 1024.0f;
                            UVValues[h, 1] = 1 - reader.ReadInt16() / 1024.0f;
                            reader.ReadBytes(8);
                            float weight3 = (int)reader.ReadByte();
                            float weight2 = (int)reader.ReadByte();
                            float weight1 = (int)reader.ReadByte();
                            float weight4 = (int)reader.ReadByte();
                            
                            int bone1 = (int)reader.ReadByte() / WShift;
                            int bone2 = (int)reader.ReadByte() / WShift;
                            int bone3 = (int)reader.ReadByte() / WShift;
                            int bone4 = (int)reader.ReadByte() / WShift;
                            
                            float maxweight = 0;
                            if (weight1 != 0)
                                maxweight += weight1;
                            if (weight2 != 0)
                                maxweight += weight2;
                            if (weight3 != 0)
                                maxweight += weight3;
                            if (weight4 != 0)
                                maxweight += weight4;

                            if (maxweight != 0)
                            {
                                if (weight1 != 0)
                                {
                                    WeightValues[h, 0] = bone1;
                                    WeightValues[h, 4] = (weight1 / 255);
                                }
                                if (weight2 != 0)
                                {
                                    WeightValues[h, 1] = bone2;
                                    WeightValues[h, 5] = (weight2 / 255);
                                }
                                if (weight3 != 0)
                                {
                                    WeightValues[h, 2] = bone3;
                                    WeightValues[h, 6] = (weight3 / 255);
                                }
                                if (weight4 != 0)
                                {
                                    WeightValues[h, 3] = bone4;
                                    WeightValues[h, 7] = (weight4 / 255);
                                }
                            }
                            reader.Seek(pos, SeekOrigin.Begin);
                        }
                        WeightList.Add(WeightValues);
                    }

                    int FSecSize = reader.ReadInt32();

                    float[,] FacesValues = new float[FSecSize / 6, 3];

                    for (int h = 0; h < (FSecSize / 6); h++)
                    {
                        FacesValues[h, 0] = reader.ReadInt16() + 1;
                        FacesValues[h, 1] = reader.ReadInt16() + 1;
                        FacesValues[h, 2] = reader.ReadInt16() + 1;
                    }

                    VertexList.Add(vertexCoorValues);
                    UVList.Add(UVValues);
                    FacesList.Add(FacesValues);
                }

                long[] MtrlOffsets = reader.FindBytes(new byte[] { 0x4D, 0x54, 0x52, 0x4C }, false);

                List<string[]> MaterialList = new List<string[]>();

                for (int j = 0; j < MtrlOffsets.Length; j++)
                {
                    reader.Seek(MtrlOffsets[j], SeekOrigin.Begin);
                    int mtrlcheck = reader.ReadInt32();
                    string[] MaterialInfo = new string[2];
                    if (mtrlcheck == 9)
                    {
                        if (MtrlOffsets[j] + 80 < 0)
                        {
                            Console.WriteLine("ERROR: cannot create material");
                            continue;
                        }
                        reader.Seek(MtrlOffsets[j] + 80, SeekOrigin.Begin);
                    }
                    else
                    {
                        if (MtrlOffsets[j] + 128 < 0)
                        {
                            Console.WriteLine("ERROR: cannot create material");
                            continue;
                        }
                        reader.Seek(MtrlOffsets[j] + 128, SeekOrigin.Begin);
                    }
                    int ColormapTextureNameLenth = reader.ReadInt32();
                    string ColorTextureName = "";
                    if (ColormapTextureNameLenth < 5)
                    {
                        ColorTextureName = "unknown_image.dds";
                    }
                    else
                    {
                        ColorTextureName = Path.GetFileName(reader.ReadFixedString(ColormapTextureNameLenth));
                    }
                    string MaterialName = "";
                    if (MtrlOffsets.Length == 1)
                    {
                        MaterialName = "mtl_" + modelname ;
                    }
                    else
                    {
                        MaterialName = "mtl_" + modelname + "_" + j;
                    }
                    MaterialInfo[0] = MaterialName;
                    MaterialInfo[1] = ColorTextureName;
                    MaterialList.Add(MaterialInfo);

                    ObjUtil.CreateMtlFile(Path.GetDirectoryName(fileName) + "\\", MaterialName, ColorTextureName);
                }
                ObjUtil.ExportObjFile(outputname, VertexList, UVList, FacesList);

                MayaUtil.ExportMaFile(outputname, VertexList, UVList, FacesList, boneCoors, boneparents, MaterialList, WeightList);
            }
            Console.WriteLine("Converted model : " + modelname);
        }
    }

}
