using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JointNode : ScriptableObject
{
    public string Name;

    public JointNode Parent;

    public List<JointNode> Children;

    public Vector3 LocalPosition;

    public Quaternion LocalRotation;

    public Vector3 RawPosition;

    public Quaternion RawRotation;

    /// <summary>
    /// returns the postion of the joint from the parent
    /// </summary>
    public Vector3 Position
    {
        get
        {
            Vector3 position = this.LocalPosition;

            // traverse tree to get all positions to this joint
            if (this.Parent != null)
            {
                position += Parent.Position;
            }

            return position;
        }
    }

    /// <summary>
    /// returns the total rotation from all the parents and itself
    /// </summary>
    public Quaternion Rotation
    {
        get
        {
            Quaternion rotation = this.LocalRotation;

            // traverse tree to get all rotations to this joint
            if (this.Parent != null)
            {
                // note order of operation is important
                rotation = Parent.Rotation * this.LocalRotation;
            }

            return rotation;
        }
    }

    public Quaternion ChangeOfBasisRotation
    {
        get
        {
            Quaternion rotation = Quaternion.identity;

            // get rotation from parent
            if(this.Parent != null)
            {
                rotation = this.Parent.ChangeOfBasisRotation;
            }
            else
            {
                rotation = Quaternion.Inverse(this.Rotation) * this.LocalRotation;
            }

            return rotation;
        }
    }

    /// <summary>
    /// Initialize the node
    /// </summary>
    /// <param name="name">joint name</param>
    public void Init(string name)
    {
        this.Name = name;

        this.Parent = null;

        this.Children = null;

        this.LocalPosition = Vector3.zero;

        this.LocalRotation = Quaternion.identity;

        this.RawPosition = Vector3.zero;

        this.RawRotation = Quaternion.identity;
    }

    /// <summary>
    /// Generates the hierarchial infromation from raw data
    /// </summary>
    /// <param name="parent">parent to this node</param>
    /// <param name="offsetPosition">offset in the world to correct for</param>
    public void CalculateOffsets(
        JointNode parent,
        UnityEngine.Vector3 offsetPosition,
        UnityEngine.Quaternion offsetRotation)
    {
        // set parent for this joint
        this.Parent = parent;

        // calculate local position from parent
        if (this.Parent == null)
        {
            this.LocalPosition = this.RawPosition + offsetPosition;
        }
        else
        {
            this.LocalPosition = this.RawPosition - offsetPosition;
        }

        // to calculate local rotation from parent
        this.LocalRotation = Quaternion.Inverse(offsetRotation) * this.RawRotation;

        // update children
        if (this.Children != null)
        {
            var offsetPos = offsetPosition;
            var offsetRot = offsetRotation;
            foreach (var bone in this.Children)
            {
                if(this.Parent == null)
                {
                    offsetPosition = this.RawPosition;
                    offsetRotation = this.RawRotation;
                }
                else
                {
                    offsetPosition += this.LocalPosition;
                    offsetRotation = offsetRotation * this.LocalRotation;
                }
                bone.CalculateOffsets(this, offsetPosition, offsetRotation);

                offsetPosition = offsetPos;
                offsetRotation = offsetRot;
            }
        }
    }

    public void AddChildNode(JointNode joint)
    {
        if (this.Children == null)
        {
            this.Children = new List<JointNode>();
        }

        this.Children.Add(joint);
    }

    public void SetRawtData(Vector3 position, Quaternion rotation)
    {
        this.RawPosition = position;
        this.RawRotation = rotation;
    }

}
