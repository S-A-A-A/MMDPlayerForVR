using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MMDPlayerForVR.PmxPhysics
{
    public sealed class PmxPhysicsReader
    {
        private const string PmxMagic = "PMX ";
        private const int HeaderConfigMinLength = 8;
        private const int Vector2Size = 8;
        private const int Vector3Size = 12;
        private const int Vector4Size = 16;

        public PmxPhysicsData Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("PMX file path is empty.", nameof(filePath));
            }

            using FileStream stream = File.OpenRead(filePath);
            using PmxBinaryReader reader = new PmxBinaryReader(stream);

            ValidateHeader(reader, out PmxHeader header);
            SkipModelInfo(reader);
            SkipVertices(reader, header.ExtraUvCount, header.BoneIndexSize);
            SkipFaces(reader, header.VertexIndexSize);
            SkipTextures(reader);
            SkipMaterials(reader, header.TextureIndexSize);
            ReadBoneNames(reader, header.BoneIndexSize, out List<string> boneNames, out List<string> boneNamesEnglish);
            SkipMorphs(reader, header.VertexIndexSize, header.BoneIndexSize, header.MaterialIndexSize, header.MorphIndexSize, header.RigidBodyIndexSize);
            SkipDisplayFrames(reader, header.BoneIndexSize, header.MorphIndexSize);

            List<PmxRigidBody> rigidBodies = ReadRigidBodies(reader, header.BoneIndexSize);
            List<PmxJoint> joints = ReadJoints(reader, header.RigidBodyIndexSize);

            return new PmxPhysicsData(boneNames, boneNamesEnglish, rigidBodies, joints);
        }

        private static void ValidateHeader(PmxBinaryReader reader, out PmxHeader header)
        {
            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != PmxMagic)
            {
                throw new InvalidDataException("The selected file is not a PMX file.");
            }

            float version = reader.ReadSingle();
            if (version < 2.0f || version >= 2.2f)
            {
                throw new InvalidDataException($"Unsupported PMX version: {version}");
            }

            int configLength = reader.ReadByte();
            if (configLength < HeaderConfigMinLength)
            {
                throw new InvalidDataException($"Invalid PMX header config length: {configLength}");
            }

            byte[] config = reader.ReadBytes(configLength);
            reader.SetTextEncoding(config[0]);

            header = new PmxHeader(
                config[1],
                config[2],
                config[3],
                config[4],
                config[5],
                config[6],
                config[7]);
        }

        private static void SkipModelInfo(PmxBinaryReader reader)
        {
            reader.ReadText();
            reader.ReadText();
            reader.ReadText();
            reader.ReadText();
        }

        private static void SkipVertices(PmxBinaryReader reader, int extraUvCount, int boneIndexSize)
        {
            int vertexCount = reader.ReadInt32();
            for (int i = 0; i < vertexCount; i++)
            {
                reader.Skip(Vector3Size + Vector3Size + Vector2Size + (Vector4Size * extraUvCount));
                byte weightType = reader.ReadByte();

                switch (weightType)
                {
                    case 0:
                        reader.Skip(boneIndexSize);
                        break;
                    case 1:
                        reader.Skip((boneIndexSize * 2) + 4);
                        break;
                    case 2:
                        reader.Skip((boneIndexSize * 4) + 16);
                        break;
                    case 3:
                        reader.Skip((boneIndexSize * 2) + 4 + (Vector3Size * 3));
                        break;
                    case 4:
                        reader.Skip((boneIndexSize * 4) + 16);
                        break;
                    default:
                        throw new InvalidDataException($"Unsupported PMX vertex weight type: {weightType}");
                }

                reader.Skip(4);
            }
        }

        private static void SkipFaces(PmxBinaryReader reader, int vertexIndexSize)
        {
            int faceIndexCount = reader.ReadInt32();
            reader.Skip((long)faceIndexCount * vertexIndexSize);
        }

        private static void SkipTextures(PmxBinaryReader reader)
        {
            int textureCount = reader.ReadInt32();
            for (int i = 0; i < textureCount; i++)
            {
                reader.ReadText();
            }
        }

        private static void SkipMaterials(PmxBinaryReader reader, int textureIndexSize)
        {
            int materialCount = reader.ReadInt32();
            for (int i = 0; i < materialCount; i++)
            {
                reader.ReadText();
                reader.ReadText();
                reader.Skip(Vector4Size + Vector3Size + 4 + Vector3Size + 1 + Vector4Size + 4);
                reader.Skip(textureIndexSize);
                reader.Skip(textureIndexSize);
                reader.Skip(1);

                byte toonFlag = reader.ReadByte();
                reader.Skip(toonFlag == 0 ? textureIndexSize : 1);
                reader.ReadText();
                reader.Skip(4);
            }
        }

        private static void ReadBoneNames(PmxBinaryReader reader, int boneIndexSize, out List<string> names, out List<string> namesEnglish)
        {
            int boneCount = reader.ReadInt32();
            names = new List<string>(boneCount);
            namesEnglish = new List<string>(boneCount);

            for (int i = 0; i < boneCount; i++)
            {
                names.Add(reader.ReadText());
                namesEnglish.Add(reader.ReadText());
                reader.Skip(Vector3Size + boneIndexSize + 4);
                ushort flags = reader.ReadUInt16();

                if ((flags & 0x0001) != 0)
                {
                    reader.Skip(boneIndexSize);
                }
                else
                {
                    reader.Skip(Vector3Size);
                }

                if ((flags & 0x0100) != 0 || (flags & 0x0200) != 0)
                {
                    reader.Skip(boneIndexSize + 4);
                }

                if ((flags & 0x0400) != 0)
                {
                    reader.Skip(Vector3Size);
                }

                if ((flags & 0x0800) != 0)
                {
                    reader.Skip(Vector3Size + Vector3Size);
                }

                if ((flags & 0x2000) != 0)
                {
                    reader.Skip(boneIndexSize);
                }

                if ((flags & 0x0020) != 0)
                {
                    int ikLoopCountSize = 4;
                    reader.Skip(boneIndexSize + ikLoopCountSize + 4);
                    int ikLinkCount = reader.ReadInt32();
                    for (int linkIndex = 0; linkIndex < ikLinkCount; linkIndex++)
                    {
                        reader.Skip(boneIndexSize);
                        byte hasLimit = reader.ReadByte();
                        if (hasLimit != 0)
                        {
                            reader.Skip(Vector3Size + Vector3Size);
                        }
                    }
                }
            }
        }

        private static void SkipMorphs(PmxBinaryReader reader, int vertexIndexSize, int boneIndexSize, int materialIndexSize, int morphIndexSize, int rigidBodyIndexSize)
        {
            int morphCount = reader.ReadInt32();
            for (int i = 0; i < morphCount; i++)
            {
                reader.ReadText();
                reader.ReadText();
                reader.Skip(1);
                byte morphType = reader.ReadByte();
                int offsetCount = reader.ReadInt32();

                for (int offsetIndex = 0; offsetIndex < offsetCount; offsetIndex++)
                {
                    switch (morphType)
                    {
                        case 0:
                            reader.Skip(morphIndexSize + 4);
                            break;
                        case 1:
                            reader.Skip(vertexIndexSize + Vector3Size);
                            break;
                        case 2:
                            reader.Skip(boneIndexSize + Vector3Size + Vector4Size);
                            break;
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                            reader.Skip(vertexIndexSize + Vector4Size);
                            break;
                        case 8:
                            reader.Skip(114); 
                            break;
                        case 9:
                            reader.Skip(morphIndexSize + 4);
                            break;
                        case 10:
                            reader.Skip(rigidBodyIndexSize + 1 + Vector3Size + Vector3Size);
                            break;
                        default:
                            throw new InvalidDataException($"Unsupported PMX morph type: {morphType}");
                    }
                }
            }
        }

        private static void SkipDisplayFrames(PmxBinaryReader reader, int boneIndexSize, int morphIndexSize)
        {
            int displayFrameCount = reader.ReadInt32();
            for (int i = 0; i < displayFrameCount; i++)
            {
                reader.ReadText();
                reader.ReadText();
                reader.Skip(1);
                int elementCount = reader.ReadInt32();
                for (int elementIndex = 0; elementIndex < elementCount; elementIndex++)
                {
                    byte elementType = reader.ReadByte();
                    reader.Skip(elementType == 0 ? boneIndexSize : morphIndexSize);
                }
            }
        }

        private static List<PmxRigidBody> ReadRigidBodies(PmxBinaryReader reader, int boneIndexSize)
        {
            int rigidBodyCount = reader.ReadInt32();
            List<PmxRigidBody> rigidBodies = new List<PmxRigidBody>(rigidBodyCount);

            for (int i = 0; i < rigidBodyCount; i++)
            {
                string name = reader.ReadText();
                string nameEnglish = reader.ReadText();
                int boneIndex = reader.ReadIndex(boneIndexSize);
                byte group = reader.ReadByte();
                ushort mask = reader.ReadUInt16();
                PmxRigidBodyShape shape = (PmxRigidBodyShape)reader.ReadByte();
                Vector3 size = reader.ReadVector3();
                Vector3 position = reader.ReadVector3();
                Vector3 rotationEuler = reader.ReadVector3();
                float mass = reader.ReadSingle();
                float linearDamping = reader.ReadSingle();
                float angularDamping = reader.ReadSingle();
                float restitution = reader.ReadSingle();
                float friction = reader.ReadSingle();
                PmxRigidBodyPhysicsMode mode = (PmxRigidBodyPhysicsMode)reader.ReadByte();

                rigidBodies.Add(new PmxRigidBody(name, nameEnglish, boneIndex, group, mask, shape, size, position, rotationEuler, mass, linearDamping, angularDamping, restitution, friction, mode));
            }

            return rigidBodies;
        }

        private static List<PmxJoint> ReadJoints(PmxBinaryReader reader, int rigidBodyIndexSize)
        {
            int jointCount = reader.ReadInt32();
            List<PmxJoint> joints = new List<PmxJoint>(jointCount);

            for (int i = 0; i < jointCount; i++)
            {
                string name = reader.ReadText();
                string nameEnglish = reader.ReadText();
                byte jointType = reader.ReadByte();
                if (jointType != 0)
                {
                    throw new InvalidDataException($"Unsupported PMX joint type: {jointType}");
                }

                joints.Add(new PmxJoint(
                    name,
                    nameEnglish,
                    reader.ReadIndex(rigidBodyIndexSize),
                    reader.ReadIndex(rigidBodyIndexSize),
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadVector3()));
            }

            return joints;
        }

        private readonly struct PmxHeader
        {
            public int ExtraUvCount { get; }
            public int VertexIndexSize { get; }
            public int TextureIndexSize { get; }
            public int MaterialIndexSize { get; }
            public int BoneIndexSize { get; }
            public int MorphIndexSize { get; }
            public int RigidBodyIndexSize { get; }

            public PmxHeader(int extraUvCount, int vertexIndexSize, int textureIndexSize, int materialIndexSize, int boneIndexSize, int morphIndexSize, int rigidBodyIndexSize)
            {
                ExtraUvCount = extraUvCount;
                VertexIndexSize = vertexIndexSize;
                TextureIndexSize = textureIndexSize;
                MaterialIndexSize = materialIndexSize;
                BoneIndexSize = boneIndexSize;
                MorphIndexSize = morphIndexSize;
                RigidBodyIndexSize = rigidBodyIndexSize;
            }
        }
    }
}
