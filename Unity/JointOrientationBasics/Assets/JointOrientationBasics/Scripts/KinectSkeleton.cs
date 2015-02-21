using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Windows.Kinect;

[Serializable]
public class KinectSkeleton : ScriptableObject
{
    public enum SegmentType { Body, Head, LeftArm, LeftHand, RightArm, RightHand, LeftLeg, RightLeg };

    [SerializeField]
    public Dictionary<JointType, JointNode> Joints;

    private DoubleExponentialFilter jointSmoother;

    public void Init()
    {
        if(this.Joints == null)
        {
            BuildHeirarchy();
        }

        if (this.jointSmoother == null)
        {
            this.jointSmoother = new DoubleExponentialFilter();
        }
    }

    public void UpdateJointsFromKinectBody(Body body, Vector3 offsetPosition, Quaternion offsetRotation)
    {
        if(body == null)
        {
            return;
        }

        if(this.Joints == null || this.jointSmoother == null)
        {
            Init();
        }

        // update joint data based on the body
        UpdateJoints(body, offsetPosition, offsetRotation);
    }

    internal JointNode GetJoint(JointType type)
    {
        // ensure a collection exists
        if (this.Joints == null || !this.Joints.ContainsKey(type))
        {
            return null;
        }

        // return it
        return this.Joints[type];
    }

    internal JointNode GetRootJoint()
    {
        return GetJoint(JointType.SpineBase);
    }

    private void BuildHeirarchy()
    {
        // ensure a collection exists
        if (this.Joints == null)
        {
            CreateJoints();
        }

        // left leg
        this.Joints[JointType.SpineBase].AddChildNode(this.Joints[JointType.HipLeft]);
        this.Joints[JointType.HipLeft].AddChildNode(this.Joints[JointType.KneeLeft]);
        this.Joints[JointType.KneeLeft].AddChildNode(this.Joints[JointType.AnkleLeft]);
        this.Joints[JointType.AnkleLeft].AddChildNode(this.Joints[JointType.FootLeft]);

        // right leg
        this.Joints[JointType.SpineBase].AddChildNode(this.Joints[JointType.HipRight]);
        this.Joints[JointType.HipRight].AddChildNode(this.Joints[JointType.KneeRight]);
        this.Joints[JointType.KneeRight].AddChildNode(this.Joints[JointType.AnkleRight]);
        this.Joints[JointType.AnkleRight].AddChildNode(this.Joints[JointType.FootRight]);

        // spine to head
        this.Joints[JointType.SpineBase].AddChildNode(this.Joints[JointType.SpineMid]);
        this.Joints[JointType.SpineMid].AddChildNode(this.Joints[JointType.SpineShoulder]);
        this.Joints[JointType.SpineShoulder].AddChildNode(this.Joints[JointType.Neck]);
        this.Joints[JointType.Neck].AddChildNode(this.Joints[JointType.Head]);

        // left arm
        this.Joints[JointType.SpineShoulder].AddChildNode(this.Joints[JointType.ShoulderLeft]);
        this.Joints[JointType.ShoulderLeft].AddChildNode(this.Joints[JointType.ElbowLeft]);
        this.Joints[JointType.ElbowLeft].AddChildNode(this.Joints[JointType.WristLeft]);
        this.Joints[JointType.WristLeft].AddChildNode(this.Joints[JointType.HandLeft]);
        this.Joints[JointType.HandLeft].AddChildNode(this.Joints[JointType.HandTipLeft]);
        this.Joints[JointType.WristLeft].AddChildNode(this.Joints[JointType.ThumbLeft]);

        // right arm
        this.Joints[JointType.SpineShoulder].AddChildNode(this.Joints[JointType.ShoulderRight]);
        this.Joints[JointType.ShoulderRight].AddChildNode(this.Joints[JointType.ElbowRight]);
        this.Joints[JointType.ElbowRight].AddChildNode(this.Joints[JointType.WristRight]);
        this.Joints[JointType.WristRight].AddChildNode(this.Joints[JointType.HandRight]);
        this.Joints[JointType.HandRight].AddChildNode(this.Joints[JointType.HandTipRight]);

        this.Joints[JointType.WristRight].AddChildNode(this.Joints[JointType.ThumbRight]);
    }

    private void CreateJoints()
    {
        if (this.Joints == null)
        {
            this.Joints = new Dictionary<JointType, JointNode>();
        }

        this.Joints.Clear();

        foreach (JointType type in Enum.GetValues(typeof(JointType)))
        {
            JointNode joint =  GetJoint(type);
            if(joint == null)
            {
                joint = ScriptableObject.CreateInstance<JointNode>();

                joint.Init(type.ToString());
            }

            this.Joints.Add(type, joint);
        }
    }

    private static string[] jointNames;
    public static string[] JointNames
    {
        get 
        {
            if (KinectSkeleton.jointNames == null || KinectSkeleton.jointNames.Length == 0)
            {
                KinectSkeleton.jointNames = Enum.GetNames(typeof(JointType)); 
            }

            return KinectSkeleton.jointNames;
        }
    }

    private void UpdateJoints(Body body, Vector3 offsetPosition, Quaternion offsetRotation)
    {
        if(body == null)
        {
            return;
        }

        DoubleExponentialFilter.TRANSFORM_SMOOTH_PARAMETERS smoothingParams = jointSmoother.SmoothingParameters;

        foreach (JointType jt in Enum.GetValues(typeof(JointType)))
        {
            // If inferred, we smooth a bit more by using a bigger jitter radius
            Windows.Kinect.Joint joint = body.Joints[jt];
            if (joint.TrackingState == TrackingState.Inferred)
            {
                smoothingParams.fJitterRadius *= 2.0f;
                smoothingParams.fMaxDeviationRadius *= 2.0f;
            }

            // set initial joint value from Kinect
            DoubleExponentialFilter.Joint fj = new DoubleExponentialFilter.Joint(
                    ConvertJointPositionToUnityVector3(body, jt),
                    ConvertJointQuaternionToUnityQuaterion(body, jt));

            fj = jointSmoother.UpdateJoint(jt, fj, smoothingParams);

            // correct for floor plane
            UnityEngine.Vector4 floorClipPlane = Helpers.FloorClipPlane;

            // get rotation of floor/camera
            Quaternion cameraRotation = Helpers.CalculateFloorRotationCorrection(floorClipPlane);

            // generate a vertical offset from floor plane
            Vector3 floorOffset = cameraRotation * Vector3.up * floorClipPlane.w;

            fj.Position = cameraRotation * fj.Position + floorOffset; // correct for height of camera
            fj.Rotation = cameraRotation * fj.Rotation;

            // set the offset position to the spine location
            if (jt == JointType.SpineBase)
            {
                offsetPosition = fj.Position;
                offsetRotation = fj.Rotation;
            }

            // set the raw joint value for the node
            this.Joints[jt].SetRawtData(fj.Position, fj.Rotation);
        }

        offsetPosition = Vector3.zero;
        offsetRotation = Quaternion.identity;

        // calculate the relative joint and rotation
        this.Joints[JointType.SpineBase].CalculateOffsets(null, offsetPosition, offsetRotation);
    }

    internal static KinectSkeleton.SegmentType GetSegmentType(JointType type)
    {
        KinectSkeleton.SegmentType segment = KinectSkeleton.SegmentType.Body;

        if (type == JointType.Neck || type == JointType.Head)
        {
            segment = KinectSkeleton.SegmentType.Head;
        }
        else if (type == JointType.ShoulderLeft || type == JointType.ElbowLeft || type == JointType.WristLeft || type == JointType.HandLeft)
        {
            segment = KinectSkeleton.SegmentType.LeftArm;
        }
        else if (type == JointType.HandLeft || type == JointType.ThumbLeft || type == JointType.HandTipLeft)
        {
            segment = KinectSkeleton.SegmentType.LeftHand;
        }
        else if (type == JointType.ShoulderRight || type == JointType.ElbowRight || type == JointType.WristRight)
        {
            segment = KinectSkeleton.SegmentType.RightArm;
        }
        else if (type == JointType.HandRight || type == JointType.ThumbRight || type == JointType.HandTipRight)
        {
            segment = KinectSkeleton.SegmentType.RightHand;
        }
        else if (type == JointType.HipLeft || type == JointType.KneeLeft || type == JointType.AnkleLeft || type == JointType.FootLeft)
        {
            segment = KinectSkeleton.SegmentType.LeftLeg;
        }
        else if (type == JointType.HipRight || type == JointType.KneeRight || type == JointType.AnkleRight || type == JointType.FootRight)
        {
            segment = KinectSkeleton.SegmentType.RightLeg;
        }

        return segment;
    }

    private static Vector3 ConvertJointPositionToUnityVector3(Body body, JointType type, bool mirror = true)
    {
        Vector3 position = new Vector3(body.Joints[type].Position.X,
            body.Joints[type].Position.Y,
            body.Joints[type].Position.Z);

        // translate -x
        if (mirror)
        {
            position.x *= -1;
        }

        return position;
    }

    private static Quaternion ConvertJointQuaternionToUnityQuaterion(Body body, JointType jt, bool mirror = true)
    {
        Quaternion rotation = new Quaternion(body.JointOrientations[jt].Orientation.X,
            body.JointOrientations[jt].Orientation.Y,
            body.JointOrientations[jt].Orientation.Z,
            body.JointOrientations[jt].Orientation.W);

        // flip rotation
        if (mirror)
        {
            rotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
        }

        return rotation;
    }

}
