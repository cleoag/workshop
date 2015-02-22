using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Windows.Kinect;

public class KinectSkeleton
{
    private enum SegmentType { Body, Head, LeftArm, LeftHand, RightArm, RightHand, LeftLeg, RightLeg };

    private Dictionary<JointType, JointNode> jointNodes;
    private Dictionary<JointType, JointNode> JointNodes
    {
        get
        {
            if (this.jointNodes == null)
            {
                this.jointNodes = new Dictionary<JointType, JointNode>();
            }

            return this.jointNodes;
        }
    }

    private DoubleExponentialFilter jointSmoother;

    internal void Init()
    {
        if (this.JointNodes.Count == 0)
        {
            BuildHeirarchy();
        }

        if (this.jointSmoother == null)
        {
            this.jointSmoother = new DoubleExponentialFilter();
        }
    }

    internal void UpdateJointsFromKinectBody(Body body, Vector3 offsetPosition, Quaternion offsetRotation)
    {
        if(body == null)
        {
            return;
        }

        if (this.JointNodes.Count == 0 || this.jointSmoother == null)
        {
            Init();
        }

        // update joint data based on the body
        UpdateJoints(body, offsetPosition, offsetRotation);
    }

    internal JointNode GetJoint(JointType type)
    {
        // ensure a collection exists
        if (this.JointNodes.Count == 0 || !this.JointNodes.ContainsKey(type))
        {
            return null;
        }

        // return it
        return this.JointNodes[type];
    }

    internal JointNode GetRootJoint()
    {
        return GetJoint(JointType.SpineBase);
    }

    private void BuildHeirarchy()
    {
        // ensure a collection exists
        if (this.JointNodes.Count == 0)
        {
            CreateJoints();
        }

        // left leg
        this.JointNodes[JointType.SpineBase].AddChildNode(this.JointNodes[JointType.HipLeft]);
        this.JointNodes[JointType.HipLeft].AddChildNode(this.JointNodes[JointType.KneeLeft]);
        this.JointNodes[JointType.KneeLeft].AddChildNode(this.JointNodes[JointType.AnkleLeft]);
        this.JointNodes[JointType.AnkleLeft].AddChildNode(this.JointNodes[JointType.FootLeft]);

        // right leg
        this.JointNodes[JointType.SpineBase].AddChildNode(this.JointNodes[JointType.HipRight]);
        this.JointNodes[JointType.HipRight].AddChildNode(this.JointNodes[JointType.KneeRight]);
        this.JointNodes[JointType.KneeRight].AddChildNode(this.JointNodes[JointType.AnkleRight]);
        this.JointNodes[JointType.AnkleRight].AddChildNode(this.JointNodes[JointType.FootRight]);

        // spine to head
        this.JointNodes[JointType.SpineBase].AddChildNode(this.JointNodes[JointType.SpineMid]);
        this.JointNodes[JointType.SpineMid].AddChildNode(this.JointNodes[JointType.SpineShoulder]);
        this.JointNodes[JointType.SpineShoulder].AddChildNode(this.JointNodes[JointType.Neck]);
        this.JointNodes[JointType.Neck].AddChildNode(this.JointNodes[JointType.Head]);

        // left arm
        this.JointNodes[JointType.SpineShoulder].AddChildNode(this.JointNodes[JointType.ShoulderLeft]);
        this.JointNodes[JointType.ShoulderLeft].AddChildNode(this.JointNodes[JointType.ElbowLeft]);
        this.JointNodes[JointType.ElbowLeft].AddChildNode(this.JointNodes[JointType.WristLeft]);
        this.JointNodes[JointType.WristLeft].AddChildNode(this.JointNodes[JointType.HandLeft]);
        this.JointNodes[JointType.HandLeft].AddChildNode(this.JointNodes[JointType.HandTipLeft]);
        this.JointNodes[JointType.WristLeft].AddChildNode(this.JointNodes[JointType.ThumbLeft]);

        // right arm
        this.JointNodes[JointType.SpineShoulder].AddChildNode(this.JointNodes[JointType.ShoulderRight]);
        this.JointNodes[JointType.ShoulderRight].AddChildNode(this.JointNodes[JointType.ElbowRight]);
        this.JointNodes[JointType.ElbowRight].AddChildNode(this.JointNodes[JointType.WristRight]);
        this.JointNodes[JointType.WristRight].AddChildNode(this.JointNodes[JointType.HandRight]);
        this.JointNodes[JointType.HandRight].AddChildNode(this.JointNodes[JointType.HandTipRight]);

        this.JointNodes[JointType.WristRight].AddChildNode(this.JointNodes[JointType.ThumbRight]);
    }

    private void CreateJoints()
    {
        this.JointNodes.Clear();

        foreach (JointType type in Enum.GetValues(typeof(JointType)))
        {
            JointNode joint =  GetJoint(type);
            if(joint == null)
            {
                joint = new JointNode();
                joint.Init(type.ToString());
            }

            this.JointNodes.Add(type, joint);
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
            this.JointNodes[jt].SetRawtData(fj.Position, fj.Rotation);
        }

        offsetPosition = Vector3.zero;
        offsetRotation = Quaternion.identity;

        // calculate the relative joint and rotation
        this.JointNodes[JointType.SpineBase].CalculateOffsets(null, offsetPosition, offsetRotation);
    }

    private static KinectSkeleton.SegmentType GetSegmentType(JointType type)
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
