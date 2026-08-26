using System;
using System.Threading.Tasks;
using UnityEngine;
using MMDPlayerForVR.PmxImporter.Core;
using MMDPlayerForVR.PmxImporter.Parsers;

namespace MMDPlayerForVR.PmxImporter
{
    public interface IPmxMeshBuilder { Task<Mesh> BuildAsync(PmxDocument doc); }
    public interface IPmxBoneBuilder { Task<(Transform root, Transform[] bones, Matrix4x4[] bindposes)> BuildAsync(PmxDocument doc); }
    public interface IPmxMaterialBuilder { Task<Material[]> BuildAsync(PmxDocument doc, string basePath); }
    public interface IPmxPhysicsBuilder { void Build(PmxDocument doc, Transform armatureRoot, Transform[] boneTransforms); }

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
                Matrix4x4[] bindposes = boneResult.bindposes;

                // Stage 2: Build Mesh (Main Thread/Coroutine)
                Mesh mesh = await _meshBuilder.BuildAsync(doc);

                // bindposes must be set BEFORE assigning the mesh to SkinnedMeshRenderer.
                // Without this, Unity cannot correctly compute skinning transforms,
                // which causes the mesh Bounds to be calculated incorrectly at origin,
                // leading to erroneous frustum culling (model disappears unexpectedly).
                mesh.bindposes = bindposes;

                // Stage 4: Build Materials (Main Thread/Coroutine)
                string basePath = System.IO.Path.GetDirectoryName(filePath);
                Material[] materials = await _materialBuilder.BuildAsync(doc, basePath);

                // Assemble GameObject
                GameObject rootObj = new GameObject(doc.Name);
                // Convert MMD scale to Unity scale (1/10th)
                rootObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                rootBone.SetParent(rootObj.transform, false);

                SkinnedMeshRenderer smr = rootObj.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh;
                smr.sharedMaterials = materials;
                smr.rootBone = rootBone;
                smr.bones = boneTransforms;
                // Disable off-screen culling until bindposes produce accurate Bounds.
                // This ensures the model is always rendered regardless of camera frustum.
                smr.updateWhenOffscreen = true;

                // Stage 5: Build Physics (Main Thread)
                _physicsBuilder.Build(doc, rootBone, boneTransforms);

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
