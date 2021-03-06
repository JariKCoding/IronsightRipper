﻿using System.Collections.Generic;
using System.IO;

namespace IronsightRipper
{
    class MayaUtil
    {
        public static void ExportMaFile(string FileName, List<ModelMesh> ModelMeshes, List<Material> MaterialList, List<ModelJoint> JointsList)
        {
            // Create a file
            using (StreamWriter output = NewMayaFile(FileName))
            {
                // Get the model name
                string ModelName = Path.GetFileNameWithoutExtension(FileName).Replace("-", "_").Replace("#", "");

                // Creating a group for the meshes
                NewGroup(output, ModelName);

                // Go through all meshes
                for (int i = 0; i < ModelMeshes.Count; i++)
                {
                    // Check if model only has 1 mesh
                    if (ModelMeshes.Count == 1)
                    {
                        // Make a single mesh
                        NewMesh(output, "Mesh_" + ModelName, ModelName, i);
                    }
                    else
                    {
                        // Make a new mesh
                        NewMesh(output, "Mesh_" + ModelName + "_" + i, ModelName, i);
                    }

                    // Add the UV values
                    AddUvValues(output, ModelMeshes[i]);

                    // Add color set
                    AddColorSet(output);

                    // Add the vertexes
                    AddVertexValues(output, ModelMeshes[i]);

                    // Add the faces
                    AddFacesValues(output, ModelMeshes[i]);
                }

                // Add a new line just for good looks
                output.WriteLine();

                // Go through all materials
                for (int i = 0; i < MaterialList.Count; i++)
                {
                    AddMaterialBegin(output, MaterialList[i]);
                }
                for (int i = 0; i < MaterialList.Count; i++)
                {
                    AddMaterial(output, MaterialList[i], i);
                }
                output.WriteLine();
                for (int i = 0; i < MaterialList.Count; i++)
                {
                    AddMaterialAssign(output, MaterialList[i], i);
                }

                // New group for the joints
                NewGroup(output, "Joints");

                // Go through all joints
                for (int i = 0; i < JointsList.Count; i++)
                {
                    // Make the joint strings
                    string jointName = "tag_is_model_" + i;
                    string parentName = "";
                    // Check if its a root joint
                    if (JointsList[i].Parent == -1)
                    {
                        // Set the joints group as parent
                        parentName = "Joints";
                    }
                    else
                    {
                        // Get the right parentName
                        parentName = "tag_is_model_" + (JointsList[i].Parent);
                    }
                    NewJoint(output, JointsList[i], parentName, jointName);
                }

                // Create .mel bind file
                CreateMayaBindFile(FileName, ModelName, ModelMeshes, JointsList);
            }
        }

        public static void CreateMayaBindFile(string FileName, string ModelName, List<ModelMesh> ModelMeshes, List<ModelJoint> JointsList)
        {
            // Create a file for the .mel script
            var outputStream = new StreamWriter(FileName + "_BIND.mel", false);

            // Add the credit header
            outputStream.Write("/*" + System.Environment.NewLine + "* Generated by Ironsight extraction tool" + System.Environment.NewLine);
            outputStream.Write("* Please credit JerriGaming & Scobalula for using it!" + System.Environment.NewLine + "*/" + System.Environment.NewLine);

            // Go through all meshes
            for (int i = 0; i < ModelMeshes.Count; i++)
            {
                // Initialize the mesh name
                string meshName = "";

                // Get the right mesh name
                if (ModelMeshes.Count == 1)
                {
                    meshName = "Mesh_" + ModelName;
                }
                else
                {
                    meshName = "Mesh_" + ModelName + "_" + i;
                }

                // Write lines to begin function
                outputStream.Write(System.Environment.NewLine + "global proc " + meshName + "_BindFunc()");
                outputStream.Write(System.Environment.NewLine + "{");
                outputStream.Write(System.Environment.NewLine + "\tselect -r " + meshName + ";");

                // Go through all joints
                for (int j = 0; j < JointsList.Count; j++)
                {
                    // Add lines to select all joints
                    string jointName = "tag_is_model_" + j;
                    outputStream.Write(System.Environment.NewLine + "\tselect -add " + jointName + ";");
                }

                // Add lines to do skin cluster
                outputStream.Write(System.Environment.NewLine + "\tnewSkinCluster \"-toSelectedBones -mi 30 -omi true -dr 5.0 -rui false\";" + System.Environment.NewLine);
                outputStream.Write(System.Environment.NewLine + "\tstring $clu = findRelatedSkinCluster(\"" + meshName + "\");" + System.Environment.NewLine);

                // Go through all joints
                for (int j = 0; j < JointsList.Count; j++)
                {
                    // Get the joint name
                    string jointName = "tag_is_model_" + j;

                    // Make a array for the lower vertex
                    int[] LowerVertex = { -1, -1, -1, -1 };
                    for (int h = 0; h < ModelMeshes[i].WeightValues.GetLength(0); h++)
                    {
                        // Loop 4 times
                        for (int g = 0; g < 4; g++)
                        {
                            // CHeck if the selected vertex is for this joint
                            if (ModelMeshes[i].WeightValues[h, g] == j)
                            {
                                // Check if we hit the last weight value
                                if (h + 1 == ModelMeshes[i].WeightValues.GetLength(0))
                                {
                                    // Check if we want to bind 1 vertex
                                    if (LowerVertex[g] == -1 && ModelMeshes[i].WeightValues[h, 4 + g] != 0)
                                    {
                                        // Write the line for 1 vertex
                                        outputStream.Write(System.Environment.NewLine + "\tskinPercent -tv tag_is_model_" + ModelMeshes[i].WeightValues[h, g] + " " + ModelMeshes[i].WeightValues[h, 4 + g] + " $clu " + meshName + ".vtx[" + h + "];");
                                    }
                                    else if (ModelMeshes[i].WeightValues[h, 4 + g] != 0)
                                    {
                                        // Write line for multiple vertexes
                                        outputStream.Write(System.Environment.NewLine + "\tskinPercent -tv tag_is_model_" + ModelMeshes[i].WeightValues[h, g] + " " + ModelMeshes[i].WeightValues[h, 4 + g] + " $clu " + meshName + ".vtx[" + LowerVertex[g] + ":" + h + "];");
                                    }
                                }
                                else
                                {
                                    // Is the lower vertex value the same?
                                    if (LowerVertex[g] == -1)
                                    {
                                        // Check if the next vertex is the binded to the same joint and same value
                                        if (ModelMeshes[i].WeightValues[h + 1, g] == ModelMeshes[i].WeightValues[h, 0 + g] && ModelMeshes[i].WeightValues[h + 1, 4 + g] == ModelMeshes[i].WeightValues[h, 4 + g] && ModelMeshes[i].WeightValues[h, 4 + g] != 0)
                                        {
                                            // Change the lower vertex to this vertex
                                            LowerVertex[g] = h;
                                        }
                                        else if (ModelMeshes[i].WeightValues[h, 4 + g] != 0)
                                        {
                                            // Write line for 1 vertex
                                            outputStream.Write(System.Environment.NewLine + "\tskinPercent -tv tag_is_model_" + ModelMeshes[i].WeightValues[h, g] + " " + ModelMeshes[i].WeightValues[h, 4 + g] + " $clu " + meshName + ".vtx[" + h + "];");
                                        }
                                    }
                                    else
                                    {
                                        if (ModelMeshes[i].WeightValues[h + 1, g] == ModelMeshes[i].WeightValues[h, g] && ModelMeshes[i].WeightValues[h + 1, 4 + g] == ModelMeshes[i].WeightValues[h, 4 + g])
                                        {

                                        }
                                        else if (ModelMeshes[i].WeightValues[h, 4 + g] != 0)
                                        {
                                            // Write line for multiple vertexes
                                            outputStream.Write(System.Environment.NewLine + "\tskinPercent -tv tag_is_model_" + ModelMeshes[i].WeightValues[h, g] + " " + ModelMeshes[i].WeightValues[h, 4 + g] + " $clu " + meshName + ".vtx[" + LowerVertex[g] + ":" + h + "];");
                                            LowerVertex[g] = -1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // Close mesh function
                outputStream.Write(System.Environment.NewLine + "}");
            }
            // Main function
            outputStream.Write(System.Environment.NewLine);
            outputStream.Write(System.Environment.NewLine + "global proc RunAdvancedScript()");
            outputStream.Write(System.Environment.NewLine + "{");
            // Go through all meshes
            for (int i = 0; i < ModelMeshes.Count; i++)
            {
                // Put together mesh name
                string meshName = "";
                if (ModelMeshes.Count == 1)
                {
                    meshName = "Mesh_" + ModelName;
                }
                else
                {
                    meshName = "Mesh_" + ModelName + "_" + i;
                }
                outputStream.Write(System.Environment.NewLine + "\tcatch(" + meshName + "_BindFunc());");
            }
            // Close the function 
            outputStream.Write(System.Environment.NewLine + "}" + System.Environment.NewLine);

            // Code for name space urge
            outputStream.Write(System.Environment.NewLine + "global proc NamespacePurge()");
            outputStream.Write(System.Environment.NewLine + "{");
            outputStream.Write(System.Environment.NewLine + "\tstring $allNodes[] = `ls`;");
            outputStream.Write(System.Environment.NewLine + "\tfor($node in $allNodes) {");
            outputStream.Write(System.Environment.NewLine + "\t\tstring $buffer[];");
            outputStream.Write(System.Environment.NewLine + "\t\ttokenize $node \":\" $buffer;");
            outputStream.Write(System.Environment.NewLine + "\t\tstring $newName = $buffer[size($buffer)-1];");
            outputStream.Write(System.Environment.NewLine + "\t\t catchQuiet(`rename $node $newName`);");
            outputStream.Write(System.Environment.NewLine + "\t}");
            outputStream.Write(System.Environment.NewLine + "}" + System.Environment.NewLine);

            // Line for init code
            outputStream.Write(System.Environment.NewLine + "print(\"Currently binding the current model, please wait...\");");
            outputStream.Write(System.Environment.NewLine + "NamespacePurge();");
            outputStream.Write(System.Environment.NewLine + "RunAdvancedScript();");
            outputStream.Write(System.Environment.NewLine + "print(\"The model has been binded.\");");

            // Close the output stream
            outputStream.Close();
        }

        public static void NewGroup(StreamWriter outputstream, string groupname)
        {
            // Makes a new group
            outputstream.Write(System.Environment.NewLine + "createNode transform -n \"" + groupname + "\";");
            outputstream.Write(System.Environment.NewLine + "setAttr \".ove\" yes;");
        }

        public static void NewMesh(StreamWriter outputstream, string meshname, string groupname, int meshindex)
        {
            // Adds the lines for a new group
            outputstream.Write(System.Environment.NewLine + "createNode transform -n \"" + meshname + "\" -p \"" + groupname + "\";");
            outputstream.Write(System.Environment.NewLine + "setAttr \".rp\" -type \"double3\" 0.000000 0.000000 0.000000 ;");
            outputstream.Write(System.Environment.NewLine + "setAttr \".sp\" -type \"double3\" 0.000000 0.000000 0.000000 ;");
            outputstream.Write(System.Environment.NewLine + "createNode mesh -n \"MeshShape_" + meshindex + "\" -p \"" + meshname + "\";");
            outputstream.Write(System.Environment.NewLine + "setAttr -k off \".v\";");
            outputstream.Write(System.Environment.NewLine + "setAttr \".vir\" yes;");
            outputstream.Write(System.Environment.NewLine + "setAttr \".vif\" yes;");
        }

        public static void AddUvValues(StreamWriter output, ModelMesh Mesh)
        {
            output.Write(System.Environment.NewLine + "setAttr \".iog[0].og[0].gcl\" -type \"componentList\" 1 \"f[0:" + Mesh.UVValues.GetLength(0) + "]\";");
            output.Write(System.Environment.NewLine + "setAttr \".uvst[0].uvsn\" -type \"string\" \"map1\";");
            output.Write(System.Environment.NewLine + "setAttr -s " + Mesh.UVValues.GetLength(0) + " \".uvst[0].uvsp\";");
            output.Write(System.Environment.NewLine + "setAttr \".uvst[0].uvsp[0:" + (Mesh.UVValues.GetLength(0) - 1) + "]\" -type \"float2\"");
            for (int i = 0; i < Mesh.UVValues.GetLength(0); i++)
            {
                output.Write(" " + Mesh.UVValues[i, 0] + " " + Mesh.UVValues[i, 1]);
            }
            output.Write(";" + System.Environment.NewLine + "setAttr \".cuvs\" -type \"string\" \"map1\";");
            output.Write(System.Environment.NewLine + "setAttr \".dcol\" yes;");
        }

        public static void AddVertexValues(StreamWriter output, ModelMesh Mesh)
        {
            output.Write(System.Environment.NewLine + "setAttr \".covm[0]\"  0 1 1;");
            output.Write(System.Environment.NewLine + "setAttr \".cdvm[0]\"  0 1 1;");
            output.Write(System.Environment.NewLine + "setAttr -s " + Mesh.VertexCoordinates.GetLength(0) + " \".vt\";");
            output.Write(System.Environment.NewLine + "setAttr \".vt[0:" + (Mesh.VertexCoordinates.GetLength(0) - 1) + "]\" ");
            for (int i = 0; i < Mesh.VertexCoordinates.GetLength(0); i++)
            {
                output.Write(" " + Mesh.VertexCoordinates[i, 0] + " " + Mesh.VertexCoordinates[i, 1] + " " + Mesh.VertexCoordinates[i, 2]);
            }
            output.Write(";");
        }

        public static void AddFacesValues(StreamWriter output, ModelMesh Mesh)
        {
            output.Write(System.Environment.NewLine + "setAttr -s " + (Mesh.FacesValues.GetLength(0) * 3) + " \".ed\";");
            output.Write(System.Environment.NewLine + "setAttr \".ed[0:" + ((Mesh.FacesValues.GetLength(0) * 3) - 1) + "]\"");
            for (int i = 0; i < Mesh.FacesValues.GetLength(0); i++)
            {
                output.Write(" " + (Mesh.FacesValues[i, 0] - 1) + " " + (Mesh.FacesValues[i, 1] - 1) + " 0");
                output.Write(" " + (Mesh.FacesValues[i, 1] - 1) + " " + (Mesh.FacesValues[i, 2] - 1) + " 0");
                output.Write(" " + (Mesh.FacesValues[i, 2] - 1) + " " + (Mesh.FacesValues[i, 0] - 1) + " 0");
            }
            output.Write(";");
            output.Write(System.Environment.NewLine + "setAttr -s " + (Mesh.FacesValues.GetLength(0) * 3) + " \".n\";");
            /*output.Write(System.Environment.NewLine + "setAttr \".n[0:" + ((Mesh.FacesValues.GetLength(0) * 3) - 1) + "]\" -type \"float3\"");
            for (int i = 0; i < Mesh.VertexDirections.Count; i++)
            {
                output.Write(" " + Mesh.VertexDirections[i].X + " " + Mesh.VertexDirections[i].Y + " " + Mesh.VertexDirections[i].Z);
            }
            output.Write(";");*/
            output.Write(System.Environment.NewLine + "setAttr -s " + Mesh.FacesValues.GetLength(0) + " \".fc[0:" + (Mesh.FacesValues.GetLength(0) - 1) + "]\" -type \"polyFaces\"");
            for (int i = 0; i < Mesh.FacesValues.GetLength(0); i++)
            {
                output.Write(" f 3 " + (i * 3) + " " + ((i * 3) + 1) + " " + ((i * 3) + 2));
                output.Write(" mu 0 3 " + (Mesh.FacesValues[i, 0] - 1) + " " + (Mesh.FacesValues[i, 1] - 1) + " " + (Mesh.FacesValues[i, 2] - 1));
                output.Write(" mc 0 3 " + (i * 3) + " " + ((i * 3) + 1) + " " + ((i * 3) + 2));
            }
            output.Write(";");
            output.Write(System.Environment.NewLine + "setAttr \".cd\" -type \"dataPolyComponent\" Index_Data Edge 0 ;");
            output.Write(System.Environment.NewLine + "setAttr \".cvd\" -type \"dataPolyComponent\" Index_Data Vertex 0 ;");
            output.Write(System.Environment.NewLine + "setAttr \".hfd\" -type \"dataPolyComponent\" Index_Data Face 0 ;");
        }

        public static void AddMaterialBegin(StreamWriter output, Material Mtrl)
        {
            output.Write(System.Environment.NewLine + "createNode shadingEngine -n \"" + Mtrl.Name + "SG\";");
            output.Write(System.Environment.NewLine + "\tsetAttr \".ihi\" 0;");
            output.Write(System.Environment.NewLine + "\tsetAttr \".ro\" yes;");
            output.Write(System.Environment.NewLine + "createNode materialInfo -n \"" + Mtrl.Name + "MI\";");
            output.Write(System.Environment.NewLine + System.Environment.NewLine + "createNode phong -n \"" + Mtrl.Name + "\";");
            output.Write(System.Environment.NewLine + "\tsetAttr \".ambc\" -type \"float3\" 1 1 1 ;");
            output.Write(System.Environment.NewLine + "createNode file -n \"" + Mtrl.Name + "FILE\";");
            output.Write(System.Environment.NewLine + "\tsetAttr \".ftn\" -type \"string\" \"" + Mtrl.ColorMap + "\";");
            output.Write(System.Environment.NewLine + "createNode place2dTexture -n \"" + Mtrl.Name + "P2DT\";" + System.Environment.NewLine);
        }

        public static void AddMaterial(StreamWriter output, Material Mtrl, int MatNumber)
        {
            output.Write(System.Environment.NewLine + "connectAttr \":defaultLightSet.msg\" \"lightLinker1.lnk[" + (MatNumber + 2) + "].llnk\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "SG.msg\" \"lightLinker1.lnk[" + (MatNumber + 2) + "].olnk\";");
            output.Write(System.Environment.NewLine + "connectAttr \":defaultLightSet.msg\" \"lightLinker1.slnk[" + (MatNumber + 2) + "].sllk\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "SG.msg\" \"lightLinker1.slnk[" + (MatNumber + 2) + "].solk\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + ".oc\" \"" + Mtrl.Name + "SG.ss\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "SG.msg\" \"" + Mtrl.Name + "MI.sg\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + ".msg\" \"" + Mtrl.Name + "MI.m\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "FILE.msg\" \"" + Mtrl.Name + "MI.t\" -na;");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "FILE.oc\" \"" + Mtrl.Name + ".c\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.c\" \"" + Mtrl.Name + "FILE.c\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.tf\" \"" + Mtrl.Name + "FILE.tf\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.rf\" \"" + Mtrl.Name + "FILE.rf\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.mu\" \"" + Mtrl.Name + "FILE.mu\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.mv\" \"" + Mtrl.Name + "FILE.mv\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.s\" \"" + Mtrl.Name + "FILE.s\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.wu\" \"" + Mtrl.Name + "FILE.wu\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.wv\" \"" + Mtrl.Name + "FILE.wv\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.re\" \"" + Mtrl.Name + "FILE.re\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.of\" \"" + Mtrl.Name + "FILE.of\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.r\" \"" + Mtrl.Name + "FILE.ro\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.n\" \"" + Mtrl.Name + "FILE.n\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.vt1\" \"" + Mtrl.Name + "FILE.vt1\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.vt2\" \"" + Mtrl.Name + "FILE.vt2\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.vt3\" \"" + Mtrl.Name + "FILE.vt3\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.vc1\" \"" + Mtrl.Name + "FILE.vc1\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.o\" \"" + Mtrl.Name + "FILE.uv\";");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.ofs\" \"" + Mtrl.Name + "FILE.fs\";");
            output.Write(System.Environment.NewLine + System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "SG.pa\" \":renderPartition.st\" -na;");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + ".msg\" \":defaultShaderList1.s\" -na;");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "P2DT.msg\" \":defaultRenderUtilityList1.u\" -na;");
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "FILE.msg\" \":defaultTextureList1.tx\" -na;");
        }

        public static void AddMaterialAssign(StreamWriter output, Material Mtrl, int MatNumber)
        {
            output.Write(System.Environment.NewLine + "connectAttr \"" + Mtrl.Name + "SG.mwc\" \"MeshShape_" + MatNumber + ".iog.og[0].gco\";");
            output.Write(System.Environment.NewLine + "connectAttr \"MeshShape_" + MatNumber + ".iog\" \"" + Mtrl.Name + "SG.dsm\" -na;");
        }

        public static void AddColorSet(StreamWriter output)
        {
            output.Write(System.Environment.NewLine + "setAttr \".dcc\" -type \"string\" \"Ambient + Diffuse\";");
            output.Write(System.Environment.NewLine + "setAttr \".ccls\" -type \"string\" \"colorSet1\";");
            output.Write(System.Environment.NewLine + "setAttr \".clst[0].clsn\" -type \"string\" \"colorSet1\";");
        }

        public static void NewJoint(StreamWriter output, ModelJoint Joint, string parentName, string jointName)
        {
            output.Write(System.Environment.NewLine + System.Environment.NewLine + "createNode joint -n \"" + jointName + "\" -p \"Joints\";");
            output.Write(System.Environment.NewLine + "addAttr -ci true -sn \"liw\" -ln \"lockInfluenceWeights\" -bt \"lock\" -min 0 -max 1 -at \"bool\";");
            output.Write(System.Environment.NewLine + "setAttr \".uoc\" yes;");
            output.Write(System.Environment.NewLine + "setAttr \".ove\" yes;");
            output.Write(System.Environment.NewLine + "setAttr \".t\" -type \"double3\"  " + Joint.LocalPosition.X + " " + Joint.LocalPosition.Y + " " + Joint.LocalPosition.Z + " ;");
            output.Write(System.Environment.NewLine + "setAttr \".mnrl\" -type \"double3\" -360 -360 -360 ;");
            output.Write(System.Environment.NewLine + "setAttr \".mxrl\" -type \"double3\" 360 360 360 ;");
            output.Write(System.Environment.NewLine + "setAttr \".radi\"   0.50;");
            output.Write(System.Environment.NewLine + "setAttr \".jo\" -type \"double3\" " + Joint.LocalDirection.X + " " + Joint.LocalDirection.Y + " " + Joint.LocalDirection.Z + ";");
            //output.Write(System.Environment.NewLine + "setAttr \".jo\" -type \"double3\" 0 0 0;");
            output.Write(System.Environment.NewLine + "setAttr \".scale\" -type \"double3\" 1.000000 1.000000 1.000000;");
            output.Write(System.Environment.NewLine + "setAttr \".scale\" -type \"double3\" 1.000000 1.000000 1.000000;");
            output.Write(System.Environment.NewLine + "parent " + jointName + " " + parentName + " ;");
        }

        public static StreamWriter NewMayaFile(string FileName)
        {
            // Create the file
            var outputStream = new StreamWriter(FileName + ".ma", false);

            // Add the credits header
            outputStream.Write("// Generated by Ironsight extraction tool" + System.Environment.NewLine);
            outputStream.Write("// Please credit JerriGaming, Scobalula & DTZxPorter for using it!" + System.Environment.NewLine + System.Environment.NewLine);

            // Create the maya header and add it to the file
            string mayaheader = "requires maya \"8.5\";" + System.Environment.NewLine + "currentUnit -l centimeter -a degree -t film;" + System.Environment.NewLine + "fileInfo \"application\" \"maya\";" + System.Environment.NewLine + "fileInfo \"product\" \"Maya Unlimited 8.5\";" + System.Environment.NewLine + "fileInfo \"version\" \"8.5\";" + System.Environment.NewLine + "fileInfo \"cutIdentifier\" \"200612162224-692032\";" + System.Environment.NewLine + "createNode transform -s -n \"persp\";" + System.Environment.NewLine + "\tsetAttr \".v\" no;" + System.Environment.NewLine + "\tsetAttr \".t\" -type \"double3\" 48.186233840145825 37.816674066853686 41.0540421364379 ;" + System.Environment.NewLine + "\tsetAttr \".r\" -type \"double3\" -29.738352729603015 49.400000000000432 0 ;" + System.Environment.NewLine + "createNode camera -s -n \"perspShape\" -p \"persp\";" + System.Environment.NewLine + "\tsetAttr -k off \".v\" no;" + System.Environment.NewLine + "\tsetAttr \".fl\" 34.999999999999993;" + System.Environment.NewLine + "\tsetAttr \".fcp\" 10000;" + System.Environment.NewLine + "\tsetAttr \".coi\" 73.724849603665149;" + System.Environment.NewLine + "\tsetAttr \".imn\" -type \"string\" \"persp\";" + System.Environment.NewLine + "\tsetAttr \".den\" -type \"string\" \"persp_depth\";" + System.Environment.NewLine + "\tsetAttr \".man\" -type \"string\" \"persp_mask\";" + System.Environment.NewLine + "\tsetAttr \".hc\" -type \"string\" \"viewSet -p %camera\";" + System.Environment.NewLine + "createNode transform -s -n \"top\";";
            mayaheader += System.Environment.NewLine + "\tsetAttr \".v\" no;" + System.Environment.NewLine + "\tsetAttr \".t\" -type \"double3\" 0 100.1 0 ;" + System.Environment.NewLine + "\tsetAttr \".r\" -type \"double3\" -89.999999999999986 0 0 ;" + System.Environment.NewLine + "createNode camera -s -n \"topShape\" -p \"top\";" + System.Environment.NewLine + "\tsetAttr -k off \".v\" no;" + System.Environment.NewLine + "\tsetAttr \".rnd\" no;" + System.Environment.NewLine + "\tsetAttr \".coi\" 100.1;" + System.Environment.NewLine + "\tsetAttr \".ow\" 30;" + System.Environment.NewLine + "\tsetAttr \".imn\" -type \"string\" \"top\";" + System.Environment.NewLine + "\tsetAttr \".den\" -type \"string\" \"top_depth\";" + System.Environment.NewLine + "\tsetAttr \".man\" -type \"string\" \"top_mask\";" + System.Environment.NewLine + "\tsetAttr \".hc\" -type \"string\" \"viewSet -t %camera\";" + System.Environment.NewLine + "\tsetAttr \".o\" yes;" + System.Environment.NewLine + "createNode transform -s -n \"front\";" + System.Environment.NewLine + "\tsetAttr \".v\" no;" + System.Environment.NewLine + "\tsetAttr \".t\" -type \"double3\" 0 0 100.1 ;" + System.Environment.NewLine + "createNode camera -s -n \"frontShape\" -p \"front\";" + System.Environment.NewLine + "\tsetAttr -k off \".v\" no;" + System.Environment.NewLine + "\tsetAttr \".rnd\" no;" + System.Environment.NewLine + "\tsetAttr \".coi\" 100.1;" + System.Environment.NewLine + "\tsetAttr \".ow\" 30;" + System.Environment.NewLine + "\tsetAttr \".imn\" -type \"string\" \"front\";";
            mayaheader += System.Environment.NewLine + "\tsetAttr \".den\" -type \"string\" \"front_depth\";" + System.Environment.NewLine + "\tsetAttr \".man\" -type \"string\" \"front_mask\";" + System.Environment.NewLine + "\tsetAttr \".hc\" -type \"string\" \"viewSet -f %camera\";" + System.Environment.NewLine + "\tsetAttr \".o\" yes;" + System.Environment.NewLine + "createNode transform -s -n \"side\";" + System.Environment.NewLine + "\tsetAttr \".v\" no;" + System.Environment.NewLine + "\tsetAttr \".t\" -type \"double3\" 100.1 0 0 ;" + System.Environment.NewLine + "\tsetAttr \".r\" -type \"double3\" 0 89.999999999999986 0 ;" + System.Environment.NewLine + "createNode camera -s -n \"sideShape\" -p \"side\";" + System.Environment.NewLine + "\tsetAttr -k off \".v\" no;" + System.Environment.NewLine + "\tsetAttr \".rnd\" no;" + System.Environment.NewLine + "\tsetAttr \".coi\" 100.1;" + System.Environment.NewLine + "\tsetAttr \".ow\" 30;" + System.Environment.NewLine + "\tsetAttr \".imn\" -type \"string\" \"side\";" + System.Environment.NewLine + "\tsetAttr \".den\" -type \"string\" \"side_depth\";" + System.Environment.NewLine + "\tsetAttr \".man\" -type \"string\" \"side_mask\";" + System.Environment.NewLine + "\tsetAttr \".hc\" -type \"string\" \"viewSet -s %camera\";" + System.Environment.NewLine + "\tsetAttr \".o\" yes;" + System.Environment.NewLine + "createNode lightLinker -n \"lightLinker1\";" + System.Environment.NewLine + "\tsetAttr -s 9 \".lnk\";" + System.Environment.NewLine + "\tsetAttr -s 9 \".slnk\";";
            mayaheader += System.Environment.NewLine + "createNode displayLayerManager -n \"layerManager\";" + System.Environment.NewLine + "createNode displayLayer -n \"defaultLayer\";" + System.Environment.NewLine + "createNode renderLayerManager -n \"renderLayerManager\";" + System.Environment.NewLine + "createNode renderLayer -n \"defaultRenderLayer\";" + System.Environment.NewLine + "\tsetAttr \".g\" yes;" + System.Environment.NewLine + "createNode script -n \"sceneConfigurationScriptNode\";" + System.Environment.NewLine + "\tsetAttr \".b\" -type \"string\" \"playbackOptions -min 1 -max 24 -ast 1 -aet 48 \";" + System.Environment.NewLine + "\tsetAttr \".st\" 6;" + System.Environment.NewLine + "select -ne :time1;" + System.Environment.NewLine + "\tsetAttr \".o\" 1;" + System.Environment.NewLine + "select -ne :renderPartition;" + System.Environment.NewLine + "\tsetAttr -s 2 \".st\";" + System.Environment.NewLine + "select -ne :renderGlobalsList1;" + System.Environment.NewLine + "select -ne :defaultShaderList1;" + System.Environment.NewLine + "\tsetAttr -s 2 \".s\";" + System.Environment.NewLine + "select -ne :postProcessList1;" + System.Environment.NewLine + "\tsetAttr -s 2 \".p\";" + System.Environment.NewLine + "select -ne :lightList1;" + System.Environment.NewLine + "select -ne :initialShadingGroup;" + System.Environment.NewLine + "\tsetAttr \".ro\" yes;" + System.Environment.NewLine + "select -ne :initialParticleSE;" + System.Environment.NewLine + "\tsetAttr \".ro\" yes;";
            mayaheader += System.Environment.NewLine + "select -ne :hardwareRenderGlobals;" + System.Environment.NewLine + "\tsetAttr \".ctrs\" 256;" + System.Environment.NewLine + "\tsetAttr \".btrs\" 512;" + System.Environment.NewLine + "select -ne :defaultHardwareRenderGlobals;" + System.Environment.NewLine + "\tsetAttr \".fn\" -type \"string\" \"im\";" + System.Environment.NewLine + "\tsetAttr \".res\" -type \"string\" \"ntsc_4d 646 485 1.333\";" + System.Environment.NewLine + "select -ne :ikSystem;" + System.Environment.NewLine + "\tsetAttr -s 4 \".sol\";" + System.Environment.NewLine + "connectAttr \":defaultLightSet.msg\" \"lightLinker1.lnk[0].llnk\";" + System.Environment.NewLine + "connectAttr \":initialShadingGroup.msg\" \"lightLinker1.lnk[0].olnk\";" + System.Environment.NewLine + "connectAttr \":defaultLightSet.msg\" \"lightLinker1.lnk[1].llnk\";" + System.Environment.NewLine + "connectAttr \":initialParticleSE.msg\" \"lightLinker1.lnk[1].olnk\";" + System.Environment.NewLine + "connectAttr \":defaultLightSet.msg\" \"lightLinker1.slnk[0].sllk\";" + System.Environment.NewLine + "connectAttr \":initialShadingGroup.msg\" \"lightLinker1.slnk[0].solk\";" + System.Environment.NewLine + "connectAttr \":defaultLightSet.msg\" \"lightLinker1.slnk[1].sllk\";" + System.Environment.NewLine + "connectAttr \":initialParticleSE.msg\" \"lightLinker1.slnk[1].solk\";" + System.Environment.NewLine + "connectAttr \"layerManager.dli[0]\" \"defaultLayer.id\";" + System.Environment.NewLine + "connectAttr \"renderLayerManager.rlmi[0]\" \"defaultRenderLayer.rlid\";" + System.Environment.NewLine + "connectAttr \"lightLinker1.msg\" \":lightList1.ln\" -na;";
            outputStream.Write(mayaheader);

            // return the file
            return outputStream;
        }
    }
}
