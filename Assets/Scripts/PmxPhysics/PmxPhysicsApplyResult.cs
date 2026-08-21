namespace MMDPlayerForVR.PmxPhysics
{
    public sealed class PmxPhysicsApplyResult
    {
        public int RigidBodyCount { get; }
        public int CreatedRigidBodyCount { get; }
        public int SkippedRigidBodyCount { get; }
        public int JointCount { get; }
        public int CreatedJointCount { get; }
        public int SkippedJointCount { get; }

        public PmxPhysicsApplyResult(int rigidBodyCount, int createdRigidBodyCount, int skippedRigidBodyCount, int jointCount, int createdJointCount, int skippedJointCount)
        {
            RigidBodyCount = rigidBodyCount;
            CreatedRigidBodyCount = createdRigidBodyCount;
            SkippedRigidBodyCount = skippedRigidBodyCount;
            JointCount = jointCount;
            CreatedJointCount = createdJointCount;
            SkippedJointCount = skippedJointCount;
        }
    }
}
