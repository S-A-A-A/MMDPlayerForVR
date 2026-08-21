using System.Collections.Generic;
using UnityEngine;

namespace MMDPlayerForVR.PmxPhysics
{
    public enum PmxRigidBodyShape
    {
        Sphere = 0,
        Box = 1,
        Capsule = 2
    }

    public enum PmxRigidBodyPhysicsMode
    {
        BoneFollow = 0,
        Physics = 1,
        PhysicsWithBoneAlign = 2
    }

    public sealed class PmxRigidBody
    {
        public string Name { get; }
        public string NameEnglish { get; }
        public int RelatedBoneIndex { get; }
        public byte Group { get; }
        public ushort NonCollisionGroupMask { get; }
        public PmxRigidBodyShape Shape { get; }
        public Vector3 Size { get; }
        public Vector3 Position { get; }
        public Vector3 RotationEuler { get; }
        public float Mass { get; }
        public float LinearDamping { get; }
        public float AngularDamping { get; }
        public float Restitution { get; }
        public float Friction { get; }
        public PmxRigidBodyPhysicsMode PhysicsMode { get; }

        public PmxRigidBody(
            string name,
            string nameEnglish,
            int relatedBoneIndex,
            byte group,
            ushort nonCollisionGroupMask,
            PmxRigidBodyShape shape,
            Vector3 size,
            Vector3 position,
            Vector3 rotationEuler,
            float mass,
            float linearDamping,
            float angularDamping,
            float restitution,
            float friction,
            PmxRigidBodyPhysicsMode physicsMode)
        {
            Name = name;
            NameEnglish = nameEnglish;
            RelatedBoneIndex = relatedBoneIndex;
            Group = group;
            NonCollisionGroupMask = nonCollisionGroupMask;
            Shape = shape;
            Size = size;
            Position = position;
            RotationEuler = rotationEuler;
            Mass = mass;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
            Restitution = restitution;
            Friction = friction;
            PhysicsMode = physicsMode;
        }
    }

    public sealed class PmxJoint
    {
        public string Name { get; }
        public string NameEnglish { get; }
        public int RigidBodyIndexA { get; }
        public int RigidBodyIndexB { get; }
        public Vector3 Position { get; }
        public Vector3 RotationEuler { get; }
        public Vector3 PositionLimitMin { get; }
        public Vector3 PositionLimitMax { get; }
        public Vector3 RotationLimitMin { get; }
        public Vector3 RotationLimitMax { get; }
        public Vector3 SpringPosition { get; }
        public Vector3 SpringRotation { get; }

        public PmxJoint(
            string name,
            string nameEnglish,
            int rigidBodyIndexA,
            int rigidBodyIndexB,
            Vector3 position,
            Vector3 rotationEuler,
            Vector3 positionLimitMin,
            Vector3 positionLimitMax,
            Vector3 rotationLimitMin,
            Vector3 rotationLimitMax,
            Vector3 springPosition,
            Vector3 springRotation)
        {
            Name = name;
            NameEnglish = nameEnglish;
            RigidBodyIndexA = rigidBodyIndexA;
            RigidBodyIndexB = rigidBodyIndexB;
            Position = position;
            RotationEuler = rotationEuler;
            PositionLimitMin = positionLimitMin;
            PositionLimitMax = positionLimitMax;
            RotationLimitMin = rotationLimitMin;
            RotationLimitMax = rotationLimitMax;
            SpringPosition = springPosition;
            SpringRotation = springRotation;
        }
    }

    public sealed class PmxPhysicsData
    {
        public IReadOnlyList<string> BoneNames { get; }
        public IReadOnlyList<string> BoneNamesEnglish { get; }
        public IReadOnlyList<PmxRigidBody> RigidBodies { get; }
        public IReadOnlyList<PmxJoint> Joints { get; }

        public PmxPhysicsData(
            IReadOnlyList<string> boneNames,
            IReadOnlyList<string> boneNamesEnglish,
            IReadOnlyList<PmxRigidBody> rigidBodies,
            IReadOnlyList<PmxJoint> joints)
        {
            BoneNames = boneNames;
            BoneNamesEnglish = boneNamesEnglish;
            RigidBodies = rigidBodies;
            Joints = joints;
        }
    }
}
