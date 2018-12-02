using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Media3D;

namespace IronsightRipper
{
    class MshFile
    {
        /// <summary>
        /// Parse the .msh file
        /// </summary>
        public static void Decode(string fileName)
        {
            // Get the right names
            string OutputName = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            string ModelName = Path.GetFileNameWithoutExtension(fileName).Replace("-", "_").Replace("#", "");

            using (BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
            {
                // Check if the msh file is setup correctly
                string Magic = reader.ReadFixedString(4);
                if (Magic != "MESH")
                {
                    Console.WriteLine(String.Format("File Magic \"{0}\" does not match IS's \"MESH\"", Magic));
                    return;
                }

                // Skip empty models
                if (reader.BaseStream.Length < 55)
                {
                    Console.Write("Empty Model : Skipping");
                    return;
                }

                // Skip to the joint data
                reader.Seek(36, SeekOrigin.Begin);

                // Get joint count
                int JointCount = reader.ReadInt32();

                // Make a list with all joints
                List<ModelJoint> ModelJoints = new List<ModelJoint>();

                // Go through all joints
                for (int j = 0; j < JointCount; j++)
                {
                    //Get the position for the next joint
                    long StreamPos = reader.BaseStream.Position + 56;

                    // Create a new model Joint
                    ModelJoint Joint = new ModelJoint();

                    // Get basic joint data
                    int JointHash = reader.ReadInt32();
                    int Unk = reader.ReadInt32();
                    Joint.Parent = reader.ReadInt32();

                    // Get the joint coordinates
                    Joint.LocalPosition = new Vector3D(
                        System.BitConverter.ToSingle(reader.ReadBytes(4), 0),
                        System.BitConverter.ToSingle(reader.ReadBytes(4), 0),
                        System.BitConverter.ToSingle(reader.ReadBytes(4), 0) * -1);

                    // Switch X and Y Axis
                    Joint.LocalPosition = new Vector3D(Joint.LocalPosition.X, Joint.LocalPosition.Z, Joint.LocalPosition.Y);

                    // Get the joint direction
                    Quaternion quat = new Quaternion(
                        System.BitConverter.ToSingle(reader.ReadBytes(4), 0),
                        System.BitConverter.ToSingle(reader.ReadBytes(4), 0),
                        System.BitConverter.ToSingle(reader.ReadBytes(4), 0),
                        System.BitConverter.ToSingle(reader.ReadBytes(4), 0));

                    // Change it from Quaternion to Vector3D + switch axis
                    Joint.LocalDirection = QuaternionIS.setFromQuaternion(new Quaternion(
                        quat.X,
                        quat.Z,
                        quat.Y,
                        quat.W
                        ));

                    // Add the joint to the list
                    ModelJoints.Add(Joint);

                    // Go to the next joint
                    reader.Seek(StreamPos, SeekOrigin.Begin);
                }

                // Add a base joint for static models
                if (JointCount == 0)
                {
                    // Make a basic joint
                    ModelJoint Joint = new ModelJoint();
                    Joint.Parent = -1;
                    Joint.LocalPosition = new Vector3D(0, 0, 0);
                    Joint.LocalDirection = new Vector3D(0, 0, 0);

                    // Add the joint to the list
                    ModelJoints.Add(Joint);
                }

                // Weird extra thing that i have no idea about what it does
                if (reader.ReadInt32() == 2)
                {
                    reader.ReadBytes(12);
                }
                reader.ReadBytes(8);

                // Get mesh and material count
                int MeshCount = reader.ReadInt32();
                int MaterialCount = reader.ReadInt32();

                // Make a list with all meshes
                List<ModelMesh> ModelMeshes = new List<ModelMesh>();

                // Go through all meshes
                for (int i = 0; i < MeshCount; i++)
                {
                    // Make a new mesh
                    ModelMesh Mesh = new ModelMesh();

                    // Skip first 8 bytes
                    reader.ReadBytes(8);

                    // Get mesh data
                    int WShift = reader.ReadInt32();
                    int VSecSize = reader.ReadInt32();

                    // Get size of the arrays for vertexes, UV's and weights
                    float[,] vertexCoorValues = new float[VSecSize / 32, 3];
                    float[,] UVValues = new float[VSecSize / 32, 2];
                    float[,] WeightValues = new float[VSecSize / 32, 8];

                    // Check verts for models without joints
                    if (WShift == 2)
                    {
                        // Create new arrays with new values
                        float[,] vertexCoorValues2 = new float[VSecSize / 28, 3];
                        float[,] UVValues2 = new float[VSecSize / 28, 2];
                        float[,] WeightValues2 = new float[VSecSize / 28, 8];

                        // Go through all vertexes etc
                        for (int h = 0; h < (VSecSize / 28); h++)
                        {
                            // Get next vertex pos
                            long StreamPos = reader.BaseStream.Position + 28;
                            // Read values
                            vertexCoorValues2[h, 0] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            vertexCoorValues2[h, 2] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            vertexCoorValues2[h, 1] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0) * -1;
                            UVValues2[h, 0] = reader.ReadInt16() / 1024.0f;
                            UVValues2[h, 1] = 1 - reader.ReadInt16() / 1024.0f;
                            // Set basic weights
                            WeightValues2[h, 0] = 0;
                            WeightValues2[h, 4] = 1;
                            // Go to next vertex
                            reader.Seek(StreamPos, SeekOrigin.Begin);
                        }
                        // Change the arrays
                        vertexCoorValues = vertexCoorValues2;
                        UVValues = UVValues2;
                        WeightValues = WeightValues2;
                    }
                    else if (WShift == 3)
                    {
                        // Go through all vertexes etc
                        for (int h = 0; h < VSecSize / 32; h++)
                        {
                            // Get next vertex pos
                            long StreamPos = reader.BaseStream.Position + 32;
                            // Read values
                            vertexCoorValues[h, 0] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            vertexCoorValues[h, 2] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0);
                            vertexCoorValues[h, 1] = System.BitConverter.ToSingle(reader.ReadBytes(4), 0) * -1;
                            UVValues[h, 0] = reader.ReadInt16() / 1024.0f;
                            UVValues[h, 1] = 1 - reader.ReadInt16() / 1024.0f;

                            // Skip 8 bytes
                            reader.ReadBytes(8);

                            // Get weight percentages
                            float weight3 = (int)reader.ReadByte();
                            float weight2 = (int)reader.ReadByte();
                            float weight1 = (int)reader.ReadByte();
                            float weight4 = (int)reader.ReadByte();

                            // Get weight joints
                            int Joint1 = (int)reader.ReadByte() / WShift;
                            int Joint2 = (int)reader.ReadByte() / WShift;
                            int Joint3 = (int)reader.ReadByte() / WShift;
                            int Joint4 = (int)reader.ReadByte() / WShift;

                            // Get the maxweight
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
                                    WeightValues[h, 0] = Joint1;
                                    WeightValues[h, 4] = (weight1 / 255);
                                }
                                if (weight2 != 0)
                                {
                                    WeightValues[h, 1] = Joint2;
                                    WeightValues[h, 5] = (weight2 / 255);
                                }
                                if (weight3 != 0)
                                {
                                    WeightValues[h, 2] = Joint3;
                                    WeightValues[h, 6] = (weight3 / 255);
                                }
                                if (weight4 != 0)
                                {
                                    WeightValues[h, 3] = Joint4;
                                    WeightValues[h, 7] = (weight4 / 255);
                                }
                            }

                            // Go to next vertex
                            reader.Seek(StreamPos, SeekOrigin.Begin);
                        }
                    }

                    // Get faces count
                    int FSecSize = reader.ReadInt32();

                    // Make faces array
                    float[,] FacesValues = new float[FSecSize / 6, 3];

                    // Read all faces
                    for (int h = 0; h < (FSecSize / 6); h++)
                    {
                        FacesValues[h, 0] = reader.ReadInt16() + 1;
                        FacesValues[h, 1] = reader.ReadInt16() + 1;
                        FacesValues[h, 2] = reader.ReadInt16() + 1;
                    }

                    // Add the data to the mesh
                    Mesh.WeightValues = WeightValues;
                    Mesh.VertexCoordinates = vertexCoorValues;
                    Mesh.UVValues = UVValues;
                    Mesh.FacesValues = FacesValues;

                    // Add the mesh to the list
                    ModelMeshes.Add(Mesh);
                }

                // Get the amount of mtrl's in the .msh file
                int MaterialsCount = reader.ReadInt32();

                // Make a list for the materials
                List<Material> MaterialList = new List<Material>();

                // Loop through mtrl's
                for (int j = 0; j < MaterialsCount; j++)
                {
                    // Check if theres a material at this position
                    if(reader.ReadFixedString(4) != "MTRL")
                    {
                        Console.WriteLine("Weird position isnt a material");
                        continue;
                    }

                    // Check if its a correct material
                    int mtrlCheck = reader.ReadInt32();
                    Material Mtrl = new Material();

                    // Set the right position
                    if (mtrlCheck == 9)
                    {
                        reader.Seek(76, SeekOrigin.Current);
                    }
                    else
                    {
                        reader.Seek(124, SeekOrigin.Current);
                    }

                    string[] MaterialTextures = new string[5];

                    // Loop through materials
                    for (int i = 0; i < 5; i++)
                    {
                        // Get the length of the texture
                        int TextureNameLenth = reader.ReadInt32();

                        string TextureName = "";
                        if (TextureNameLenth < 5)
                        {
                            TextureName = "unknown_image.dds";
                        }
                        else
                        {
                            TextureName = Path.GetFileName(reader.ReadFixedString(TextureNameLenth));
                        }
                        MaterialTextures[i] = TextureName;
                    }

                    // Skip to the next material
                    reader.Seek(40, SeekOrigin.Current);
                    if (mtrlCheck != 9)
                        reader.Seek(4, SeekOrigin.Current);

                    // Get the material name
                    string MaterialName = "";
                    if (MaterialsCount == 1)
                    {
                        MaterialName = "mtl_" + ModelName;
                    }
                    else
                    {
                        MaterialName = "mtl_" + ModelName + "_" + j;
                    }

                    // Set the material data and add it to the list
                    Mtrl.Name = MaterialName;
                    Mtrl.ColorMap = MaterialTextures[0];
                    Mtrl.NormalMap = MaterialTextures[1];
                    MaterialList.Add(Mtrl);
                }

                // Export the model to .obj
                ObjUtil.ExportObjFile(OutputName, ModelMeshes, MaterialList);

                // Export the model to .ma
                MayaUtil.ExportMaFile(OutputName, ModelMeshes, MaterialList, ModelJoints);
            }
            // Add a line in the console
            Console.WriteLine("Converted model : " + ModelName);
        }
    }

    /// <summary>
    /// Model Joint
    /// </summary>
    class ModelJoint
    {
        /// <summary>
        /// Parent
        /// </summary>
        public int Parent { get; set; }

        /// <summary>
        /// World Position
        /// </summary>
        public Vector3D LocalPosition { get; set; }

        /// <summary>
        /// World Direction
        /// </summary>
        public Vector3D LocalDirection { get; set; }
    }

    /// <summary>
    /// Model Joint
    /// </summary> 
    class ModelMesh
    {
        /// <summary>
        /// Weights to joints
        /// </summary>
        public float[,] WeightValues { get; set; }

        /// <summary>
        /// Vertex Coordinates
        /// </summary>
        public float[,] VertexCoordinates { get; set; }

        /// <summary>
        /// UV Values
        /// </summary>
        public float[,] UVValues { get; set; }

        /// <summary>
        /// Faces Values
        /// </summary>
        public float[,] FacesValues { get; set; }

        /// <summary>
        /// Vertex Normals
        /// </summary>
        public List<Vector3D> VertexNormals { get; set; }
    }

    /// <summary>
    /// Material
    /// </summary> 
    class Material
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ColorMap
        /// </summary>
        public string ColorMap { get; set; }

        /// <summary>
        /// ColorMap
        /// </summary>
        public string NormalMap { get; set; }
    }
}
