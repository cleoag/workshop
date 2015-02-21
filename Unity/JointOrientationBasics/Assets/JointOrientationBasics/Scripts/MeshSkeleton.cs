using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MeshSkeleton : ScriptableObject
{
    public class Pose
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public Pose(Vector3 position, Quaternion rotation)
        {
            this.Position = position;
            this.Rotation = rotation;
        }
    }

    private SkinnedMeshRenderer mesh;

    private Dictionary<Transform, JointNode> joints;

    private Dictionary<Transform, Pose> basePose;

    internal List<string> BoneNames { get; set; }

    public void Init(SkinnedMeshRenderer mesh)
    {
        this.mesh = mesh;

        if(this.joints == null)
        {
            this.joints = new Dictionary<Transform, JointNode>();
        }
        this.joints.Clear();

        if (this.basePose == null)
        {
            this.basePose = new Dictionary<Transform, Pose>();
        }
        this.basePose.Clear();

        if (this.BoneNames == null)
        {
            this.BoneNames = new List<string>();
        }
        this.BoneNames.Clear();

        // generate the bone names
        foreach (var bone in this.mesh.bones)
        {
            this.BoneNames.Add(bone.name);
        }

        GetBasePose();

        BuildHeirarchy();
    }

    internal void ApplyIdentityRoatations()
    {
        if(this.mesh == null)
        {
            return;
        }

        if (this.joints == null)
        {
            Init(this.mesh);
        }

        foreach (var bone in this.mesh.bones)
        {
            bone.rotation = Quaternion.identity;

            JointNode joint = this.joints[bone];
            if(joint != null)
            {
                joint.SetRawtData(bone.position, bone.rotation);
            }
        }
    }

    internal void ApplyDefaultRotation()
    {
        if (this.mesh == null)
        {
            return;
        }

        if (this.joints == null || this.basePose == null)
        {
            Init(this.mesh);
        }

        foreach (var bone in this.mesh.bones)
        {
            Pose pose = this.basePose[bone];
            if (pose != null)
            {
                bone.position = pose.Position;
                bone.rotation = pose.Rotation;
            }

            JointNode joint = this.joints[bone];
            if (joint != null)
            {
                joint.SetRawtData(bone.position, bone.rotation);
            }
        }
    }

    internal void UpdateHierachy()
    {
        if (this.mesh == null)
        {
            return;
        }

        if (this.joints == null)
        {
            Init(this.mesh);
        }

        BuildHeirarchy();
    }

    internal JointNode GetRootBone()
    {
        if(this.mesh == null)
        {
            return null;
        }

        Transform root = this.mesh.rootBone;

        JointNode joint = null;
        if (this.joints != null && this.joints.ContainsKey(root))
        {
            joint = this.joints[root];
        }

        return joint;
    }

    internal JointNode GetJoint(Transform bone)
    {
        // ensure a collection exists
        if (this.joints == null || !this.joints.ContainsKey(bone))
        {
            return null;
        }

        // return it
        return this.joints[bone];
    }

    private void BuildHeirarchy()
    {
        if (this.mesh == null || this.joints == null)
        {
            return;
        }

        this.joints.Clear();

        CreateJoints(this.mesh.rootBone, null);
    }

    private void CreateJoints(Transform bone, JointNode parent)
    {
        if(bone == null)
        {
            return;
        }

        JointNode joint = GetJoint(bone);
        if (joint == null)
        {
            joint = ScriptableObject.CreateInstance<JointNode>();
            joint.Init(bone.name);

            joint.SetRawtData(bone.position, bone.rotation);
        }

        // add to collection
        this.joints.Add(bone, joint);

        //  add this as a child
        if(parent != null)
        {
            parent.AddChildNode(joint);
        }

        foreach (Transform child in bone)
        {
            CreateJoints(child, joint);
        }
    }

    internal void GetBasePose()
    {
        if (this.mesh == null || this.basePose == null)
        {
            return;
        }

        this.basePose.Clear();
        foreach (var bone in this.mesh.bones)
        {
            this.basePose.Add(bone, new Pose(bone.position, bone.rotation));
        }
    }
}
