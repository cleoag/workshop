using System;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

[System.Serializable]
public class JointMapping : MonoBehaviour
{
    [SerializeField]
    public SkinnedMeshRenderer mesh;
    public SkinnedMeshRenderer Mesh
    {
        get { return this.mesh; }
        set
        {
            if (value == this.mesh)
            {
                return;
            }

            this.mesh = value;

            Init();
        }
    }

    [SerializeField]
    public List<Map> List;

    private KinectSkeleton kinectSkeleton;
    private KinectSkeleton KinectSkeleton
    {
        get
        {
            if(this.kinectSkeleton == null)
            {
                this.kinectSkeleton = ScriptableObject.CreateInstance<KinectSkeleton>();
                this.kinectSkeleton.Init();
            }

            return this.kinectSkeleton;
        }
    }

    private MeshSkeleton meshSkeleton;
    private MeshSkeleton MeshSkeleton
    {
        get
        {
            if(this.meshSkeleton == null)
            {
                this.meshSkeleton = ScriptableObject.CreateInstance<MeshSkeleton>();
                this.meshSkeleton.Init(this.Mesh);
            }
            return this.meshSkeleton;
        }
    }

    public string[] JointTypeNames
    {
        get
        {
            return KinectSkeleton.JointNames;
        }
    }

    public List<string> BoneNames
    {
        get
        {
            if(this.MeshSkeleton != null)
            {
                return this.MeshSkeleton.BoneNames;
            }

            return null;
        }
    }

    public bool ApplyIdentity = false;
    
    public bool ApplyToMesh = false;

    public void OnEnable()
    {
        hideFlags = HideFlags.HideAndDontSave;
    }

    public void Init()
    {
        this.name = GenerateName(mesh);

        // reset anything associated with the old model
        if (this.List == null)
        {
            this.List = new List<Map>();
        }
        this.List.Clear();

        this.meshSkeleton = null;

        this.kinectSkeleton = null;
    }

    internal Map GetMapFromJointType(JointType type)
    {
        Map jointMap = null;

        foreach (Map jm in this.List)
        {
            if(jm.Type == type)
            {
                jointMap = jm;
                break;
            }
        }

        return jointMap;
    }

    internal Map GetMapFromTypeName(string typeName)
    {
        Map map = null;

        foreach (JointType jt in Enum.GetValues(typeof(JointType)))
        {
            if (jt.ToString() == typeName)
            {
                map = GetMapFromJointType(jt);
                break;
            }
        }

        return map;
    }

    internal Map GetMapFromBone(Transform bone)
    {
        if (bone == null)
        {
            return null;
        }

        Map jointMap = null;

        foreach (Map jm in this.List)
        {
            if (jm.Bone == bone)
            {
                jointMap = jm;
                break;
            }
        }

        return jointMap;
    }

    internal Map GetMapFromBoneName(string boneName)
    {
        Transform foundBone = GetBoneFromName(boneName);

        return GetMapFromBone(foundBone);
    }

    internal JointNode GetKinectNodeFromJointType(JointType type)
    {
        return this.KinectSkeleton.GetJoint(type);
    }

    internal JointNode GetKinectNodeFromJointTypeName(string jointName)
    {
        Map map = GetMapFromTypeName(jointName);
        if (map != null)
        {
            return GetKinectNodeFromJointType(map.Type);
        }

        return null;
    }

    internal JointNode GetMeshNodeFromBone(Transform bone)
    {
        return this.MeshSkeleton.GetJoint(bone);
    }

    internal JointNode GetMeshNodeFromBoneName(string boneName)
    {
        Map map = GetMapFromBoneName(boneName);
        if(map != null)
        {
            return this.MeshSkeleton.GetJoint(map.Bone);
        }

        return null;
    }

    public void AddMapping(string typeName, string boneName)
    {
        JointType? type = GetJointTypeFromName(typeName);
        Transform bone = GetBoneFromName(boneName);
        
        if(type != null && bone != null)
        {
            AddMapping(type.Value, bone);
        }
    }

    private void AddMapping(JointType type, Transform bone)
    {
        if(bone == null)
        {
            return;
        }

        // check for existing mapping for type and bone
        Map foundMapping = GetMapFromJointType(type);
        Map foundBoneMapping = GetMapFromBone(bone);

        if (foundBoneMapping != null)
        {
            UpdateMapping(foundBoneMapping, null); // reset the bone to null
        }

        if (foundMapping == null && bone != null)
        {
            // wasn't found, so create one
            foundMapping = new Map(type, bone);
            //foundMapping = ScriptableObject.CreateInstance<Map>(type,bone);
            
            // add the entry to the mapping
            this.List.Add(foundMapping);
        }
        else
        {
            UpdateMapping(foundMapping, bone);
        }
    }
    public void UpdateMapping(Map map, int boneSelectedIndex)
    {
        Transform bone = GetBoneFromName(this.MeshSkeleton.BoneNames[boneSelectedIndex]);
        if (bone == null)
        {
            return;
        }

        UpdateMapping(map, bone);
    }

    public void UpdateMapping(Map jointMap, Transform bone)
    {
        // is it already selected by another map
        var foundBoneMapping = GetMapFromBone(bone);
        if (foundBoneMapping != null && foundBoneMapping != jointMap)
        {
            // swap the bone for this one
            foundBoneMapping.Bone = jointMap.Bone;
        }

        if (bone == null)
        {
            // remove it from the map
            RemoveMapping(jointMap);
        }
        else
        {
            // map the bone to this jointMap
            jointMap.Bone = bone;
        }
    }

    public void RemoveMapping(int boneSelectedIndex)
    {
        Transform bone = GetBoneFromName(this.MeshSkeleton.BoneNames[boneSelectedIndex]);
        if(bone == null)
        {
            return;
        }

        Map map = GetMapFromBone(bone);
        if (bone != null) // cannot delete in the loop
        {
            RemoveMapping(map);
        }

    }

    private void RemoveMapping(Map jointMap)
    {
        this.List.Remove(jointMap);
    }

    private JointType? GetJointTypeFromName(string jointName)
    {
        int index = Array.IndexOf<string>(KinectSkeleton.JointNames, jointName);
        if(index != -1)
        {
            return (Windows.Kinect.JointType)index;
        }

        return null;
    }

    private Transform GetBoneFromName(string boneName)
    {
        if(string.IsNullOrEmpty(boneName) || this.MeshSkeleton == null)
        {
            return null;
        }

        Transform foundBone = null;

        if (this.Mesh != null)
        {
            foreach (var bone in this.Mesh.bones)
            {
                if (bone.name == boneName)
                {
                    foundBone = bone;
                    break;
                }
            }
        }

        return foundBone;
    }

    internal void UpdateKinectSkeleton(Body body)
    {
        // update the skeleton with the new body joint/orientation information
        this.KinectSkeleton.UpdateJointsFromKinectBody(body, Vector3.zero, Quaternion.identity);

        // upadte the mesh skeleton
        if (this.ApplyIdentity)
        {
            this.MeshSkeleton.ApplyIdentityRoatations();
        }
        else if (!this.ApplyToMesh)
        {
            this.MeshSkeleton.ApplyDefaultRotation();
        }
        else
        {
            Transform bone = this.Mesh.rootBone;
            JointNode kinectNode = this.KinectSkeleton.GetRootJoint();

            UpdateBone(bone, kinectNode);

            this.MeshSkeleton.UpdateHierachy();
        }
    }

    internal void UpdateBone(Transform bone, JointNode kinectNode)
    {
        JointNode joint = this.MeshSkeleton.GetJoint(bone);
        if(joint != null)
        {
            Quaternion rotation = kinectNode.Rotation;
            if (kinectNode.Parent != null)
            {
                Map parent = GetMapFromTypeName(kinectNode.Parent.Name);
                if (parent == null)
                {
                    rotation = kinectNode.Parent.Rotation * kinectNode.LocalRotation;
                }
            }

            bone.rotation = rotation;
        }

        foreach (var child in kinectNode.Children)
        {
            Map map = GetMapFromTypeName(child.Name);
            if (map != null)
            {
                UpdateBone(map.Bone, child);
            }
        }
    }

    internal JointNode GetKinectRootNode()
    {
        return this.KinectSkeleton.GetRootJoint();
    }

    internal JointNode GetMeshRootBone()
    {
        return this.MeshSkeleton.GetRootBone();
    }

    public static JointMapping Create(GameObject baseObject)
    {
        // create joint mapper script and attach to GameObject
        JointMapping jm = new GameObject().AddComponent<JointMapping>() as JointMapping;
        DontDestroyOnLoad(jm.gameObject);

        // if a object was selected when creating, attach to that and find the bones/mesh object for that object
        SkinnedMeshRenderer smr = null;
        if (baseObject != null)
        {
            var allChildren = baseObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var child in allChildren)
            {
                if (child != null)
                {
                    smr = child as SkinnedMeshRenderer;
                    break;
                }
            }

            if(smr != null)
            {
                jm.Mesh = smr;
            }
        }

        // create name based smr
        jm.name = GenerateName(smr);

        return jm;
    }

    private static string GenerateName(SkinnedMeshRenderer smr)
    {
        String name = "JointMapping";

        if (smr != null)
        {
            name += "_" + smr.name;
        }

        return name;
    }
}

