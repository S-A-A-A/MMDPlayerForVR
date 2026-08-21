using System.Collections.Generic;
using UnityEngine;
using MMDPlayerForVR.PmxImporter.Core;

namespace MMDPlayerForVR.PmxImporter.Builders
{
    public class PmxPhysicsBuilder : IPmxPhysicsBuilder
    {
        private const float MinColliderSize = 0.0001f;
        private const float RadiansToDegrees = 57.2957795f;

        public void Build(PmxDocument doc, Transform armatureRoot, Transform[] boneTransforms)
        {
            Dictionary<int, Rigidbody> bodyMap = new Dictionary<int, Rigidbody>();

            // 1. Rigidbodies
            for (int i = 0; i < doc.RigidBodies.Length; i++)
            {
                var rbData = doc.RigidBodies[i];
                if (rbData.RelatedBoneIndex < 0 || rbData.RelatedBoneIndex >= boneTransforms.Length) continue;

                Transform boneTransform = boneTransforms[rbData.RelatedBoneIndex];
                if (boneTransform == null)
                {
                    Debug.LogWarning($"[PmxPhysicsBuilder] Bone not found for RigidBody: {rbData.Name}");
                    continue;
                }

                GameObject go = boneTransform.gameObject;
                Rigidbody rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
                
                rb.mass = Mathf.Max(MinColliderSize, rbData.Mass);
                rb.linearDamping = Mathf.Max(0f, rbData.LinearDamping);
                rb.angularDamping = Mathf.Max(0f, rbData.AngularDamping);
                rb.isKinematic = rbData.PhysicsMode == 0; // 0: FollowBone

                ConfigureCollider(go, rbData);
                bodyMap[i] = rb;
            }

            // 2. Joints
            for (int i = 0; i < doc.Joints.Length; i++)
            {
                var jData = doc.Joints[i];
                if (!bodyMap.TryGetValue(jData.RigidBodyIndexA, out Rigidbody connectedBody) ||
                    !bodyMap.TryGetValue(jData.RigidBodyIndexB, out Rigidbody targetBody))
                {
                    continue;
                }

                GameObject go = targetBody.gameObject;
                ConfigurableJoint joint = go.AddComponent<ConfigurableJoint>();
                joint.connectedBody = connectedBody;
                
                // MMD and Unity are both Left-Handed. No Z inversion needed.
                Vector3 anchor = jData.Position;
                joint.anchor = targetBody.transform.InverseTransformPoint(anchor);

                // TODO: Properly convert Spring limits and rotation for Unity Left-Handed
                joint.xMotion = ConfigurableJointMotion.Limited;
                joint.yMotion = ConfigurableJointMotion.Limited;
                joint.zMotion = ConfigurableJointMotion.Limited;
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Limited;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
            }
        }

        private void ConfigureCollider(GameObject go, PmxRigidBody rbData)
        {
            Collider collider = null;
            switch(rbData.Shape)
            {
                case 0: // Sphere
                    var sphere = go.AddComponent<SphereCollider>();
                    sphere.radius = Mathf.Max(MinColliderSize, rbData.Size.x);
                    collider = sphere;
                    break;
                case 1: // Box
                    var box = go.AddComponent<BoxCollider>();
                    box.size = new Vector3(Mathf.Max(MinColliderSize, rbData.Size.x * 2f),
                                           Mathf.Max(MinColliderSize, rbData.Size.y * 2f),
                                           Mathf.Max(MinColliderSize, rbData.Size.z * 2f));
                    collider = box;
                    break;
                case 2: // Capsule
                    var capsule = go.AddComponent<CapsuleCollider>();
                    capsule.radius = Mathf.Max(MinColliderSize, rbData.Size.x);
                    capsule.height = Mathf.Max(capsule.radius * 2f, rbData.Size.y * 2f);
                    collider = capsule;
                    break;
            }

            if (collider != null)
            {
                // MMD coordinate conversion
                collider.bounds.Encapsulate(rbData.Position);
            }
        }
    }
}
