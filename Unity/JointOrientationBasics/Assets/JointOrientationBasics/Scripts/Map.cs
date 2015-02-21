using UnityEngine;

/// <summary>
/// Holds mapping between Kinect joint and the mesh mode bone
/// </summary>
[System.Serializable]
public class Map
{
    [SerializeField]
    public Windows.Kinect.JointType Type;

    [SerializeField]
    public UnityEngine.Transform Bone;

    // adjustment needed to transform joint into mesh space
    [SerializeField]
    public Quaternion AdjustmentToMesh; 

    public Map(Windows.Kinect.JointType type, UnityEngine.Transform bone)
    {
        this.Type = type;
        this.Bone = bone;
        this.AdjustmentToMesh = Quaternion.identity;
    }
}
