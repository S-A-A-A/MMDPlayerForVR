using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MMDPlayerForVR.PmxImporter.Core;

namespace MMDPlayerForVR.PmxImporter.Builders
{
    public class PmxBoneBuilder : IPmxBoneBuilder
    {
        public async Task<(Transform root, Transform[] bones)> BuildAsync(PmxDocument doc)
        {
            Transform[] boneTransforms = new Transform[doc.Bones.Length];
            GameObject rootObj = new GameObject("Armature");
            Transform rootTransform = rootObj.transform;

            // 1. Create all bone GameObjects and map them
            for (int i = 0; i < doc.Bones.Length; i++)
            {
                var pmxBone = doc.Bones[i];
                GameObject boneObj = new GameObject(pmxBone.Name);
                Transform boneT = boneObj.transform;

                // Invert Z for Unity coordinate system
                boneT.position = new Vector3(pmxBone.Position.x, pmxBone.Position.y, -pmxBone.Position.z);
                
                boneTransforms[i] = boneT;
            }

            // 2. Resolve hierarchy
            for (int i = 0; i < doc.Bones.Length; i++)
            {
                var pmxBone = doc.Bones[i];
                Transform current = boneTransforms[i];

                if (pmxBone.ParentBoneIndex >= 0 && pmxBone.ParentBoneIndex < doc.Bones.Length)
                {
                    current.SetParent(boneTransforms[pmxBone.ParentBoneIndex], true);
                }
                else
                {
                    current.SetParent(rootTransform, true);
                }
            }

            return await Task.FromResult((rootTransform, boneTransforms));
        }
    }
}
