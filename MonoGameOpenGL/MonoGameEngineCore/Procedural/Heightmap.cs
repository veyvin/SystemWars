﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibNoise;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGameEngineCore.Rendering;


namespace MonoGameEngineCore.Procedural
{



    public class Sphere
    {
        private int slices;
        private int stacks;

        private List<VertexPositionColorTextureNormal> verts = new List<VertexPositionColorTextureNormal>();
        private List<short> indices = new List<short>();
        public int vertexCount;

        public Sphere(int slices, int stacks)
        {
            this.slices = slices;
            this.stacks = stacks;
        }

        public VertexPositionColorTextureNormal[] GenerateVertexArray()
        {

            for (int slice = 0; slice <= slices; ++slice)
            {
                for (int stack = 0; stack <= stacks; ++stack)
                {
                    VertexPositionColorTextureNormal v = new VertexPositionColorTextureNormal();
                    v.Position.Y = (float)System.Math.Sin(((double)stack / stacks - 0.5) * System.Math.PI);
                    if (v.Position.Y < -1) v.Position.Y = -1;
                    else if (v.Position.Y > 1) v.Position.Y = 1;
                    float l = (float)System.Math.Sqrt(1 - v.Position.Y * v.Position.Y);
                    v.Position.X = l * (float)System.Math.Sin((double)slice / slices * System.Math.PI * 2);
                    v.Position.Z = l * (float)System.Math.Cos((double)slice / slices * System.Math.PI * 2);
                    v.Normal = v.Position;
                    v.Texture = Vector3.Zero;
                    verts.Add(v);
                }
            }
            vertexCount = verts.Count;
            return verts.ToArray();
        }

        public short[] GenerateIndices()
        {
            for (int i = 0; i < slices * stacks * 6; i++) indices.Add(0);
            int six = 0;
            for (int slice = 0; slice < slices; ++slice)
            {
                for (short stack = 0; stack < stacks; ++stack)
                {
                    short v = (short)(slice * (stacks + 1) + stack);
                    indices[six++] = v;
                    indices[six++] = (short)(v + 1);
                    indices[six++] = (short)(v + (stacks + 1));
                    indices[six++] = (short)(v + (stacks + 1));
                    indices[six++] = (short)(v + 1);
                    indices[six++] = (short)(v + (stacks + 1) + 1);
                }
            }
            return indices.ToArray();
        }

        public VertexPositionColorTextureNormal[] GenerateVertexArray(FastRidgedMultifractal module)
        {
            for (int slice = 0; slice <= slices; ++slice)
            {
                for (int stack = 0; stack <= stacks; ++stack)
                {
                    VertexPositionColorTextureNormal v = new VertexPositionColorTextureNormal();
                    v.Position.Y = (float)System.Math.Sin(((double)stack / stacks - 0.5) * System.Math.PI);
                    if (v.Position.Y < -1) v.Position.Y = -1;
                    else if (v.Position.Y > 1) v.Position.Y = 1;
                    float l = (float)System.Math.Sqrt(1 - v.Position.Y * v.Position.Y);
                    v.Position.X = l * (float)System.Math.Sin((double)slice / slices * System.Math.PI * 2);
                    v.Position.Z = l * (float)System.Math.Cos((double)slice / slices * System.Math.PI * 2);
                    v.Normal = v.Position;
                    v.Texture = Vector3.Zero;



                    double heightValue = module.GetValue(v.Position.X, v.Position.Y, v.Position.Z);

                    v.Position *= (float)(heightValue + 1);


                    verts.Add(v);
                }
            }
            vertexCount = verts.Count;
            return verts.ToArray();
        }
    }


    public class Heightmap
    {

        private float scale;
        public float[,] heights;
        public int size;

        public Heightmap(int size, float scale)
        {
            this.size = size;

            this.scale = scale;
            Initialize();

        }

        private void Initialize()
        {
            heights = new float[size, size];

            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    heights[i, j] = 0;

        }

        public void RandomiseHeights()
        {
            Random r = new Random();
            for (int i = 0; i < heights.GetLength(0); i++)
                for (int j = 0; j < heights.GetLength(0); j++)
                    heights[i, j] = r.Next(0, 100) / 100f;

        }


        internal void SetData(IModule module, double sampleStep, float verticalScale, double xOffset, double yOffset)
        {
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    heights[i, j] = (float)(module.GetValue((double)i + xOffset * sampleStep, (double)j + yOffset * sampleStep, 10) + 1) / 2;
                    heights[i, j] *= verticalScale;
                }
        }

        public VertexPositionColorTextureNormal[] GenerateVertexArray()
        {

            int heightMapSize = heights.GetLength(0);
            var vertArray = new VertexPositionColorTextureNormal[heightMapSize * heightMapSize];

            int vertIndex = 0;
            for (int i = 0; i < heightMapSize; i++)
            {
                for (int j = 0; j < heightMapSize; j++)
                {
                    var vert = new VertexPositionColorTextureNormal();
                    vert.Position = new Vector3(i * scale, heights[i, j] * scale * 0.2f, j * scale);
                    vert.Color = Color.DarkOrange;
                    vert.Texture = Vector3.Zero;
                    vert.Normal = Vector3.Up;
                    vertArray[vertIndex] = vert;
                    vertIndex++;
                }
            }


            GenerateNormals(ref vertArray);



            return vertArray;

        }

        private void GenerateNormals(ref VertexPositionColorTextureNormal[] vertArray)
        {
            short[] indices = GenerateIndices();

            for (int i = 0; i < vertArray.Length; i++)
                vertArray[i].Normal = new Vector3(0, 0, 0);

            for (int i = 0; i < indices.Length / 3; i++)
            {
                Vector3 firstvec = vertArray[indices[i * 3 + 1]].Position - vertArray[indices[i * 3]].Position;
                Vector3 secondvec = vertArray[indices[i * 3]].Position - vertArray[indices[i * 3 + 2]].Position;
                Vector3 normal = Vector3.Cross(firstvec, secondvec);
                normal.Normalize();
                vertArray[indices[i * 3]].Normal += normal;
                vertArray[indices[i * 3 + 1]].Normal += normal;
                vertArray[indices[i * 3 + 2]].Normal += normal;
            }

            for (int i = 0; i < vertArray.Length; i++)
            {
                vertArray[i].Normal.Normalize();
            }
        }

        public short[] GenerateIndices()
        {
            int heightMapSize = heights.GetLength(0);

            // Construct the index array.
            var indices = new short[(heightMapSize - 1) * (heightMapSize - 1) * 6];    // 2 triangles per grid square x 3 vertices per triangle

            int indicesIndex = 0;
            for (int y = 0; y < heightMapSize - 1; ++y)
            {
                for (int x = 0; x < heightMapSize - 1; ++x)
                {
                    int start = y * heightMapSize + x;
                    indices[indicesIndex++] = (short)start;
                    indices[indicesIndex++] = (short)(start + 1);
                    indices[indicesIndex++] = (short)(start + heightMapSize);
                    indices[indicesIndex++] = (short)(start + 1);
                    indices[indicesIndex++] = (short)(start + 1 + heightMapSize);
                    indices[indicesIndex++] = (short)(start + heightMapSize);
                }
            }
            return indices.Reverse().ToArray();
        }


    }



}
