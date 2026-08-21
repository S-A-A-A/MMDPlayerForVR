using System;
using System.Threading.Tasks;
using UnityEngine;
using MMDPlayerForVR.PmxImporter.Core;
using MMDPlayerForVR.PmxImporter.Parsers;

namespace MMDPlayerForVR.PmxImporter
{
    public interface IPmxMeshBuilder { Task<Mesh> BuildAsync(PmxDocument doc); }
    public interface IPmxBoneBuilder { Task<(Transform root, Transform[] bones)> BuildAsync(PmxDocument doc); }
    public interface IPmxMaterialBuilder { Task<Material[]> BuildAsync(PmxDocument doc, string basePath); }
    public interface IPmxPhysicsBuilder { void Build(PmxDocument doc, Transform armatureRoot); }

    public class PmxImporterPipeline
    {
        private readonly PmxParser _parser;
        private readonly IPmxMeshBuilder _meshBuilder;
        private readonly IPmxBoneBuilder _boneBuilder;
        private readonly IPmxMaterialBuilder _materialBuilder;
        private readonly IPmxPhysicsBuilder _physicsBuilder;

        public PmxImporterPipeline(
            PmxParser parser, 
            IPmxMeshBuilder meshBuilder, 
            IPmxBoneBuilder boneBuilder, 
            IPmxMaterialBuilder materialBuilder, 
            IPmxPhysicsBuilder physicsBuilder)
        {
            _parser = parser;
            _meshBuilder = meshBuilder;
            _boneBuilder = boneBuilder;
            _materialBuilder = materialBuilder;
            _physicsBuilder = physicsBuilder;
        }

        public async Task<GameObject> ImportAsync(string filePath)
        {
            try
            {
                // Stage 1: Parse Binary (Background Thread)
                PmxDocument doc = await _parser.ParseAsync(filePath);

                // Stage 3: Build Bones (Main Thread/Coroutine)
                var boneResult = await _boneBuilder.BuildAsync(doc);
                Transform rootBone = boneResult.root;
                Transform[] boneTransforms = boneResult.bones;

                // Stage 2: Build Mesh (Main Thread/Coroutine)
                Mesh mesh = await _meshBuilder.BuildAsync(doc);

                // Stage 4: Build Materials (Main Thread/Coroutine)
                string basePath = System.IO.Path.GetDirectoryName(filePath);
                Material[] materials = await _materialBuilder.BuildAsync(doc, basePath);

                // Assemble GameObject
                GameObject rootObj = new GameObject(doc.Name);
                rootBone.SetParent(rootObj.transform, false);

                SkinnedMeshRenderer smr = rootObj.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh;
                smr.sharedMaterials = materials;
                smr.rootBone = rootBone;
                smr.bones = boneTransforms;

                // Stage 5: Build Physics (Main Thread)
                _physicsBuilder.Build(doc, rootBone);

                return rootObj;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PmxImporter] Failed to import {filePath}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }
}
