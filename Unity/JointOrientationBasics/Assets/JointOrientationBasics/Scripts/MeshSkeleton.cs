using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MeshSkeleton
{
    private SkinnedMeshRenderer mesh;

    private Dictionary<string, JointNode> jointNodes;
    private Dictionary<string, JointNode> JointNodes
    { 
        get
        {
            if (this.jointNodes == null)
            {
                this.jointNodes = new Dictionary<string, JointNode>();
            }

            return this.jointNodes;
        }
    }

    private List<string> _boneNames;
    internal List<string> BoneNames
    {
        get
        {
            if (this._boneNames == null)
            {
                this._boneNames = new List<string>();
            }

            return this._boneNames;
        }
    }

    internal void Init(SkinnedMeshRenderer mesh)
    {
        this.mesh = mesh;

        this.JointNodes.Clear();

        // generate the bone names
        this.BoneNames.Clear();
        foreach (var bone in this.mesh.bones)
        {
            this.BoneNames.Add(bone.name);
        }

        GenerateBasePoses();
    }

    internal void ApplyIdentityRoatations()
    {
        if(this.mesh == null)
        {
            return;
        }

        foreach (var bone in this.mesh.bones)
        {
            bone.rotation = Quaternion.identity;
        }
    }

    internal void ApplyDefaultRotation()
    {
        if (this.mesh == null)
        {
            return;
        }

        if (this.JointNodes.Count == 0)
        {
            Init(this.mesh);
        }

        foreach (var bone in this.mesh.bones)
        {
            JointNode node = this.JointNodes[bone.name];
            if (node != null)
            {
                bone.position = node.Position;
                bone.rotation = node.Rotation;
            }
        }
    }

    internal JointNode GetRootBone()
    {
        return GetBoneNode(this.mesh.rootBone);
    }

    internal JointNode GetBoneNode(Transform bone)
    {
        if (this.mesh == null || bone == null || !this.JointNodes.ContainsKey(bone.name))
        {
            return null;
        }

        return this.JointNodes[bone.name];
    }

    private void GenerateBasePoses()
    {
        if (this.mesh == null)
        {
            return;
        }

        this.JointNodes.Clear();

        CreateBoneNode(this.mesh.rootBone, null);

        // calculate the relative joint and rotation
        this.JointNodes[this.mesh.rootBone.name].CalculateOffsets(null, Vector3.zero, Quaternion.identity);
    }

    private void CreateBoneNode(Transform bone, JointNode parent)
    {
        JointNode node = new JointNode();
        node.Init(bone.name);
        node.SetRawtData(bone.position, bone.rotation);
        this.JointNodes.Add(bone.name, node);

        // add this node as a child of the parent
        if(parent != null)
        {
            parent.AddChildNode(node);
        }

        foreach (Transform child in bone)
        {
            CreateBoneNode(child, node);
        }
    }
}
