using System.Collections.Generic;
using UnityEngine;

namespace MMDPlayerForVR.PmxPhysics
{
    public sealed class PmxPhysicsApplier
    {
        private const float MinColliderSize = 0.0001f;
        private const float RadiansToDegrees = 57.2957795f;

        public PmxPhysicsApplyResult Apply(PmxPhysicsData data, Transform armatureRoot)
        {
            Dictionary<string, Transform> boneMap = BuildBoneMap(armatureRoot);
            Dictionary<int, Rigidbody> bodyMap = new Dictionary<int, Rigidbody>();
            int createdBodies = 0;
            int skippedBodies = 0;

            for (int i = 0; i < data.RigidBodies.Count; i++)
            {
                PmxRigidBody body = data.RigidBodies[i];
                if (!TryFindBone(data, body.RelatedBoneIndex, boneMap, out Transform bone))
                {
                    skippedBodies++;
                    Debug.LogWarning($"PMX rigid body skipped. Bone not found: {body.Name} (bone index: {body.RelatedBoneIndex})");
                    continue;
                }

                Rigidbody rigidbody = ConfigureRigidBody(bone.gameObject, body);
                ConfigureCollider(bone.gameObject, body);
                bodyMap[i] = rigidbody;
                createdBodies++;
            }

            int createdJoints = 0;
            int skippedJoints = 0;
            for (int i = 0; i < data.Joints.Count; i++)
            {
                PmxJoint joint = data.Joints[i];
                if (!bodyMap.TryGetValue(joint.RigidBodyIndexB, out Rigidbody targetBody) || !bodyMap.TryGetValue(joint.RigidBodyIndexA, out Rigidbody connectedBody))
                {
                    skippedJoints++;
                    Debug.LogWarning($"PMX joint skipped. RigidBody not found: {joint.Name}");
                    continue;
                }

                ConfigureJoint(targetBody.gameObject, connectedBody, joint);
                createdJoints++;
            }

            return new PmxPhysicsApplyResult(data.RigidBodies.Count, createdBodies, skippedBodies, data.Joints.Count, createdJoints, skippedJoints);
        }

        private static Dictionary<string, Transform> BuildBoneMap(Transform root)
        {
            Dictionary<string, Transform> map = new Dictionary<string, Transform>();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!map.ContainsKey(child.name))
                {
                    map.Add(child.name, child);
                }
            }

            return map;
        }

        private static bool TryFindBone(PmxPhysicsData data, int boneIndex, Dictionary<string, Transform> boneMap, out Transform bone)
        {
            bone = null;
            if (boneIndex < 0 || boneIndex >= data.BoneNames.Count)
            {
                return false;
            }

            if (boneMap.TryGetValue(data.BoneNames[boneIndex], out bone))
            {
                return true;
            }

            if (boneIndex < data.BoneNamesEnglish.Count)
            {
                return boneMap.TryGetValue(data.BoneNamesEnglish[boneIndex], out bone);
            }

            return false;
        }

        private static Rigidbody ConfigureRigidBody(GameObject gameObject, PmxRigidBody body)
        {
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            rigidbody.mass = Mathf.Max(MinColliderSize, body.Mass);
            rigidbody.linearDamping = Mathf.Max(0f, body.LinearDamping);
            rigidbody.angularDamping = Mathf.Max(0f, body.AngularDamping);
            rigidbody.isKinematic = body.PhysicsMode == PmxRigidBodyPhysicsMode.BoneFollow;
            return rigidbody;
        }

        private static void ConfigureCollider(GameObject gameObject, PmxRigidBody body)
        {
            RemoveExistingPmxCollider(gameObject);
            Collider collider = CreateCollider(gameObject, body);
            collider.sharedMaterial = CreatePhysicMaterial(body);
        }

        private static Collider CreateCollider(GameObject gameObject, PmxRigidBody body)
        {
            switch (body.Shape)
            {
                case PmxRigidBodyShape.Sphere:
                    SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                    sphere.center = body.Position;
                    sphere.radius = Mathf.Max(MinColliderSize, body.Size.x);
                    return sphere;
                case PmxRigidBodyShape.Box:
                    BoxCollider box = gameObject.AddComponent<BoxCollider>();
                    box.center = body.Position;
                    box.size = new Vector3(
                        Mathf.Max(MinColliderSize, body.Size.x * 2f),
                        Mathf.Max(MinColliderSize, body.Size.y * 2f),
                        Mathf.Max(MinColliderSize, body.Size.z * 2f));
                    return box;
                case PmxRigidBodyShape.Capsule:
                    CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
                    capsule.center = body.Position;
                    capsule.radius = Mathf.Max(MinColliderSize, body.Size.x);
                    capsule.height = Mathf.Max(capsule.radius * 2f, body.Size.y * 2f);
                    capsule.direction = 1;
                    return capsule;
                default:
                    Debug.LogWarning($"Unsupported PMX rigid body shape: {body.Shape}. SphereCollider was used instead.");
                    SphereCollider fallback = gameObject.AddComponent<SphereCollider>();
                    fallback.center = body.Position;
                    fallback.radius = Mathf.Max(MinColliderSize, body.Size.x);
                    return fallback;
            }
        }

        private static void RemoveExistingPmxCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static PhysicsMaterial CreatePhysicMaterial(PmxRigidBody body)
        {
            PhysicsMaterial material = new PhysicsMaterial($"PMX_{body.Name}_Material")
            {
                bounciness = Mathf.Clamp01(body.Restitution),
                dynamicFriction = Mathf.Max(0f, body.Friction),
                staticFriction = Mathf.Max(0f, body.Friction),
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Average
            };
            return material;
        }

        private static void ConfigureJoint(GameObject gameObject, Rigidbody connectedBody, PmxJoint pmxJoint)
        {
            ConfigurableJoint joint = gameObject.GetComponent<ConfigurableJoint>();
            if (joint == null)
            {
                joint = gameObject.AddComponent<ConfigurableJoint>();
            }

            joint.connectedBody = connectedBody;
            joint.anchor = (pmxJoint.PositionLimitMin + pmxJoint.PositionLimitMax) * 0.5f;
            joint.axis = Quaternion.Euler(pmxJoint.RotationEuler * RadiansToDegrees) * Vector3.right;
            joint.secondaryAxis = Quaternion.Euler(pmxJoint.RotationEuler * RadiansToDegrees) * Vector3.up;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit linearLimit = joint.linearLimit;
            linearLimit.limit = MaxComponent(pmxJoint.PositionLimitMax - pmxJoint.PositionLimitMin) * 0.5f;
            joint.linearLimit = linearLimit;

            SoftJointLimit lowAngularXLimit = joint.lowAngularXLimit;
            lowAngularXLimit.limit = pmxJoint.RotationLimitMin.x * RadiansToDegrees;
            joint.lowAngularXLimit = lowAngularXLimit;

            SoftJointLimit highAngularXLimit = joint.highAngularXLimit;
            highAngularXLimit.limit = pmxJoint.RotationLimitMax.x * RadiansToDegrees;
            joint.highAngularXLimit = highAngularXLimit;

            SoftJointLimit angularYLimit = joint.angularYLimit;
            angularYLimit.limit = Mathf.Max(Mathf.Abs(pmxJoint.RotationLimitMin.y), Mathf.Abs(pmxJoint.RotationLimitMax.y)) * RadiansToDegrees;
            joint.angularYLimit = angularYLimit;

            SoftJointLimit angularZLimit = joint.angularZLimit;
            angularZLimit.limit = Mathf.Max(Mathf.Abs(pmxJoint.RotationLimitMin.z), Mathf.Abs(pmxJoint.RotationLimitMax.z)) * RadiansToDegrees;
            joint.angularZLimit = angularZLimit;

            JointDrive positionDrive = CreateDrive(pmxJoint.SpringPosition);
            joint.xDrive = positionDrive;
            joint.yDrive = positionDrive;
            joint.zDrive = positionDrive;

            JointDrive angularDrive = CreateDrive(pmxJoint.SpringRotation);
            joint.angularXDrive = angularDrive;
            joint.angularYZDrive = angularDrive;
        }

        private static JointDrive CreateDrive(Vector3 spring)
        {
            return new JointDrive
            {
                positionSpring = MaxComponent(spring),
                positionDamper = 0f,
                maximumForce = Mathf.Infinity
            };
        }

        private static float MaxComponent(Vector3 value)
        {
            return Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}
