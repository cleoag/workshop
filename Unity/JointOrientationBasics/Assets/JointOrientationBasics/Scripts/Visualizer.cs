using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Kinect = Windows.Kinect;
using System;
using System.Text;

/// <summary>
/// helper method to draw the mesh models for the JointMapping
/// </summary>
public class Visualizer : MonoBehaviour
{
    public enum MeshType { Kinect, Mesh };

    public class Bone
    {
        public GameObject BoneMesh;
        public GameObject JointMesh;

        public Bone(GameObject bone, GameObject joint)
        {
            this.BoneMesh = bone;
            this.JointMesh = joint;
        }
    }

    /// <summary>
    /// Kinect BodySourceManager to get body frames from
    /// </summary>
    public BodySourceManager BodySourceManager;

    /// <summary>
    /// instance of the joint mappings
    /// </summary>
    public JointMapping JointMapper;

    /// <summary>
    /// joint model to draw to represent the orientation
    /// </summary>
    public GameObject JointModel;
    public float JointScale = 1.0f;

    /// <summary>
    /// bone model to illustrate direction of the bone
    /// </summary>
    public GameObject BoneModel;
    public Vector3 BoneScale = Vector3.one;
    public float BoneLength = 1.0f;

    public bool DrawJoint = false;
    public bool DrawBoneModel = true;
    public bool DebugLines = true;
    public bool ApplyRotataion = false;
    public bool ApplyIdentity = false;

    public Material material;

    /// <summary>
    /// reference of the list to allow for edits while in Play mode
    /// </summary>
    public List<Map> JointList;

    private GameObject kinectVisualizerParent;
    private GameObject KinectVisualizerParent
    {
        get
        {
            if(this.kinectVisualizerParent == null)
            {
                CreateKinectSkeletonModel();
            }

            return this.kinectVisualizerParent;
        }
    }

    private Dictionary<string, Bone> kinectBodyModel;
    private Dictionary<string, Bone> KinectBodyModel
    {
        get
        {
            if(this.kinectBodyModel == null)
            {
                this.kinectBodyModel = new Dictionary<string, Bone>();
            }

            return this.kinectBodyModel;
        }
    }

    private GameObject meshVisualizerParent;
    private GameObject MeshVisualizerParent
    {
        get
        {
            if (this.meshVisualizerParent == null)
            {
                CreateMeshSkeletonModel();
            }

            return this.meshVisualizerParent;
        }
    }

    private Dictionary<string, Bone> meshBodyModel;
    private Dictionary<string, Bone> MeshBodyModel
    {
        get
        {
            if (this.meshBodyModel == null)
            {
                this.meshBodyModel = new Dictionary<string, Bone>();
            }

            return this.meshBodyModel;
        }
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // kinect specific updates
        if (null == this.BodySourceManager || null == this.JointMapper)
        {
            return;
        }

        // expose the list so we can edit the adjustment on each joint
        this.JointList = this.JointMapper.List;

        // update the visual 
        this.JointMapper.ApplyToMesh = this.ApplyRotataion;
        this.JointMapper.ApplyIdentity = this.ApplyIdentity;

        // Get the closest body
        Kinect.Body body = this.BodySourceManager.FindClosestBody();
        if (null == body)
        {
            return;
        }

        // update the skeleton with the new body joint/orientation information
        this.JointMapper.UpdateSkeletons(body);

        // update the visual models
        VisualizeJoints(this.JointMapper.GetKinectRootNode(), MeshType.Kinect);

        VisualizeJoints(this.JointMapper.GetMeshRootBone(), MeshType.Mesh);
    }

    /// <summary>
    /// recursive function to iterate the tree of joints
    /// </summary>
    /// <param name="joint">root joint to start from</param>
    private void VisualizeJoints(JointNode joint, MeshType type)
    {
        if (joint == null)
        {
            return;
        }

        switch(type)
        {
            case MeshType.Kinect:
                UpdateKinectJointVisual(joint);
                break;
            case MeshType.Mesh:
                UpdateMeshJointVisual(joint);
                break;
        }

        if (joint.Children != null)
        {
            foreach (var bone in joint.Children)
            {
                VisualizeJoints(bone, type);
            }
        }
    }    

    /// <summary>
    /// based on a joint location, will draw the bone visual for that joint
    /// </summary>
    /// <param name="joint">joint to show</param>
    private void UpdateKinectJointVisual(JointNode joint)
    {
        if(joint == null)
        {
            return;
        }

        // check to ensure the visualizer was created
        if(this.KinectVisualizerParent == null)
        {
            CreateKinectSkeletonModel();
        }

        if(!this.KinectBodyModel.ContainsKey(joint.Name))
        {
            return;
        }

        // get the joint visual from the collection
        Bone model = this.KinectBodyModel[joint.Name];
        if(model.BoneMesh == null || model.JointMesh == null)
        {
            return;
        }

        GameObject bone = model.BoneMesh;
        Helpers.SetVisible(bone, this.DrawBoneModel);

        GameObject jm = model.JointMesh;
        Helpers.SetVisible(jm, this.DrawJoint);

        // Kinect Joint Orientation is from the parent bone
        if (joint.Parent != null)
        {
            Vector3 child = joint.Position;
            Vector3 parent = joint.Parent.Position;

            // direction vector
            Vector3 direction = child - parent;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
            Vector3 normal = Vector3.Cross(perpendicular, direction);

            // calc world rotation needed to draw bone
            Quaternion worldRotation = Quaternion.identity;
            if (Helpers.QuaternionZero.Equals(joint.Rotation))
            {
                // calculate a rotation for extreme joints
                worldRotation.SetLookRotation(normal, direction);
            }
            else
            {
                // get the Kinect oritentaiton 
                // Y - is the direction of the bone
                // Z - normal of the bone
                // X - bi-normal
                worldRotation = joint.Rotation;
            }

            // draw debug lines
            if(DebugLines)
            {
                // visualize the rotation in world space
                Helpers.DrawDebugBoneWithNormal(parent, direction.magnitude, worldRotation);

                // visualize the local rotation quaternion
                Helpers.DrawDebugQuaternion(parent, joint.LocalRotation, Helpers.ColorRange.CMYKTint);
            }

            // update model
            UpdateBoneMesh(bone, parent, worldRotation, direction.magnitude, this.BoneLength, this.BoneScale);

            // visualize the mesh model
            UpdateJointMesh(jm, parent, worldRotation, this.JointScale);
        }
        else 
        {
            Helpers.SetVisible(bone, false);
            Helpers.SetVisible(jm, false);
        }
    }

    /// <summary>
    /// helper method to draw the Kinect skeleton structure
    /// </summary>
    private void CreateKinectSkeletonModel()
    {
        if (this.kinectVisualizerParent != null)
        {
            DestroyObject(this.KinectVisualizerParent);
            this.kinectVisualizerParent = null;
        }

        // Create debug skeleton mapping
        this.KinectBodyModel.Clear();

        // create parent gameObject for debug controls
        this.kinectVisualizerParent = new GameObject();
        this.kinectVisualizerParent.name = "KinectSkeletonModel";

        // create visualizer
        foreach (Kinect.JointType jt in Enum.GetValues(typeof(Kinect.JointType)))
        {
            var joint = (GameObject)Instantiate(this.JointModel, transform.position, transform.rotation);
            joint.name = string.Format("{0}", jt);
            joint.transform.localScale = Vector3.one * this.JointScale;
            joint.gameObject.transform.parent = this.KinectVisualizerParent.gameObject.transform;

            // build bone visual
            var bone = (GameObject)Instantiate(this.BoneModel, transform.position, transform.rotation);
            bone.name = string.Format("{0}", jt);
            bone.transform.localScale = this.BoneScale;

            // clean-up hierachy to display under one parent
            bone.gameObject.transform.parent = this.KinectVisualizerParent.gameObject.transform;

            // add to collection for the model
            this.KinectBodyModel.Add(jt.ToString(), new Bone(bone, joint));
        }
    }

    /// <summary>
    /// update the mesh model single joint
    /// </summary>
    /// <param name="joint">joint information from the mesh model</param>
    private void UpdateMeshJointVisual(JointNode joint)
    {
        if(joint == null)
        {
            return;
        }

        // check to ensure the visualizer was created
        if(this.MeshVisualizerParent == null)
        {
            CreateMeshSkeletonModel();
        }

        if(!this.MeshBodyModel.ContainsKey(joint.Name))
        {
            return;
        }

        // get the joint visual from the collection
        Bone model = this.MeshBodyModel[joint.Name];
        if(model.BoneMesh == null || model.JointMesh == null)
        {
            return;
        }

        GameObject bone = model.BoneMesh;
        Helpers.SetVisible(bone, this.DrawBoneModel);

        GameObject jm = model.JointMesh;
        Helpers.SetVisible(jm, this.DrawJoint);

        // set joint position based on direction of bone
        if (joint.Parent != null)
        {
            Vector3 direction = joint.Position - joint.Parent.Position;
            float length = direction.magnitude;

            Quaternion forwardRotation = Quaternion.LookRotation(direction);
            Vector3 position = forwardRotation * (Vector3.forward * length);

            // draw the orientaion around the joint
            if(this.DebugLines)
            {
                Helpers.DrawDebugBoneWithNormal(position, length, forwardRotation, Helpers.ColorRange.BW);
            }

            // update model
            UpdateBoneMesh(bone, joint.Position, joint.Rotation, length, this.BoneLength, this.BoneScale);
        }

        // visualize the mesh model
        UpdateJointMesh(jm, joint.Position, joint.Rotation, this.JointScale);
    }

    /// <summary>
    /// Method to create the visual of the Mesh model joints
    /// </summary>
    private void CreateMeshSkeletonModel()
    {
        if (this.JointMapper == null || this.JointMapper.List == null)
        {
            return;
        }

        if (this.meshVisualizerParent != null)
        {
            DestroyObject(this.meshVisualizerParent);
            this.meshVisualizerParent = null;
        }

        // Create debug skeleton for mesh
        this.MeshBodyModel.Clear();

        // create parent gameObject for debug controls
        this.meshVisualizerParent = new GameObject();
        this.meshVisualizerParent.name = "MeshSkeletonModel";

        // create visualizer for mapped mesh model
        foreach (var jm in this.JointMapper.List)
        {
            Transform meshBone = jm.Bone;

            // create a mesh for the joint
            var joint = (GameObject)Instantiate(this.JointModel, transform.position, transform.rotation);
            joint.name = string.Format("mesh_Joint_{0}", meshBone.name);
            joint.transform.position = jm.Bone.position;
            joint.transform.rotation = jm.Bone.rotation;
            joint.transform.localScale = Vector3.one * this.JointScale;

            // parent to the visualizer
            joint.gameObject.transform.parent = this.MeshVisualizerParent.gameObject.transform;

            // create a mesh for the bone
            var bone = (GameObject)Instantiate(this.BoneModel, transform.position, transform.rotation);
            bone.name = string.Format("mesh_Bone_{0}", meshBone.name);
            bone.transform.position = jm.Bone.position;
            bone.transform.rotation = jm.Bone.rotation;
            bone.transform.localScale = this.BoneScale;

            // clean-up hierachy to display under one parent
            bone.gameObject.transform.parent = this.MeshVisualizerParent.gameObject.transform;

            // add both models to the KinectBoneVisualizer
            this.MeshBodyModel.Add(meshBone.name, new Bone(bone, joint));
        }
    }

    /// <summary>
    /// Helper method to extend the bone in the direction to child
    /// </summary>
    /// <param name="bone">model used to visualize bone</param>
    /// <param name="position">start position for the model</param>
    /// <param name="rotation">the rotation to apply to the model</param>
    /// <param name="length">the distance to the child</param>
    /// <param name="boneLength">scale length to apply to the bone to give some buffer</param>
    /// <param name="boneScale">scale to apply to the model</param>
    private static void UpdateBoneMesh(GameObject bone, Vector3 position, Quaternion rotation, float length, float boneLength, Vector3 boneScale)
    {
        // get mesh verticies;
        MeshFilter meshFilter = (MeshFilter)bone.GetComponent("MeshFilter");
        var verticies = meshFilter.mesh.vertices;

        // calculate the forward vector based on Kinect's Y-forward direction
        Vector3 forward = rotation * Vector3.up * length;

        // determine the length in the up direction
        Vector3 lengthUp = Quaternion.Inverse(rotation) * forward * boneLength;

        // update verticies of the tip
        verticies[4] = lengthUp;
        verticies[7] = verticies[4];
        verticies[10] = verticies[4];
        verticies[13] = verticies[4];
        meshFilter.mesh.vertices = verticies;
        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();

        // move bone into position
        bone.transform.position = position;
        bone.transform.rotation = rotation;
        bone.transform.localScale = boneScale;
    }

    /// <summary>
    /// apply's transformations to the model
    /// </summary>
    /// <param name="joint">the game object to apply transformation to</param>
    /// <param name="position">the postion of the joint in world space</param>
    /// <param name="rotation">rotation to apply</param>
    /// <param name="jointScale">joint scale to adjust the size</param>
    private static void UpdateJointMesh(GameObject joint, Vector3 position, Quaternion rotation, float jointScale)
    {
        joint.transform.position = position;
        joint.transform.rotation = rotation;
        joint.transform.localScale = Vector3.one * jointScale;
    }
}
