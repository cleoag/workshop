using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class BodySourceManager : MonoBehaviour 
{
    private KinectSensor _sensor = null;
    private BodyFrameReader _reader = null;

    private Body[] _bodies = null;
    public Body[] Bodies
    {
        get
        {
            return _bodies;
        }
    }

    public BodyFrameSource GetFrameSource()
    {
        return _sensor.BodyFrameSource;
    }

    void Start () 
    {
        _sensor = KinectSensor.GetDefault();

        if (_sensor != null)
        {
            _reader = _sensor.BodyFrameSource.OpenReader();
            
            if (!_sensor.IsOpen)
            {
                _sensor.Open();
            }
        }   
    }
    
    void Update () 
    {
        if (_reader != null)
        {
            bool hasBodyData = false;

            using(var frame = _reader.AcquireLatestFrame())
            {
                if (frame != null)
                {
                    hasBodyData = true;

                    if (_bodies == null)
                    {
                        _bodies = new Body[_sensor.BodyFrameSource.BodyCount];
                    }

                    frame.GetAndRefreshBodyData(_bodies);
                    UnityEngine.Vector4 floorPlane = new UnityEngine.Vector4(frame.FloorClipPlane.X,
                        frame.FloorClipPlane.Y,
                        frame.FloorClipPlane.Z,
                        frame.FloorClipPlane.W);

                    // update plane
                    Helpers.FloorClipPlane = floorPlane;
                }
            }

            // correct for floorPlane
            if (hasBodyData)
            {
                // get a local copy
                UnityEngine.Vector4 floorClipPlane = Helpers.FloorClipPlane;

                // y - up
                Vector3 up = floorClipPlane;

                // z - forward
                Vector3 forward = new Vector3(0.0f, 0.0f, 1.0f);

                // x - right
                Vector3 right = Vector3.Cross(up, forward);
                right.Normalize();

                // update matrix
                Matrix4x4 correctionMatrix = Matrix4x4.identity;
                correctionMatrix.SetColumn(0, right);
                correctionMatrix.m00 = right.x;
                correctionMatrix.m01 = right.y;
                correctionMatrix.m02 = right.z;

                correctionMatrix.SetColumn(1, up);
                correctionMatrix.m10 = up.x;
                correctionMatrix.m11 = up.y;
                correctionMatrix.m12 = up.z;

                correctionMatrix.SetColumn(2, forward);
                correctionMatrix.m20 = forward.x;
                correctionMatrix.m21 = forward.y;
                correctionMatrix.m23 = forward.z;

                // may need to be transposed
                correctionMatrix.m13 = floorClipPlane.w;
                //correctionMatrix.m33 = floorClipPlane.w;
            }
        }    
    }
    
    void OnApplicationQuit()
    {
        if (_reader != null)
        {
            _reader.Dispose();
            _reader = null;
        }
        
        if (_sensor != null)
        {
            if (_sensor.IsOpen)
            {
                _sensor.Close();
            }
            
            _sensor = null;
        }
    }

    public Body FindClosestBody()
    {
        Body result = null;
        double closestBodyDistance = double.MaxValue;

        if (this.Bodies != null)
        {
            foreach (var body in this.Bodies)
            {
                if (body.IsTracked)
                {
                    var currentLocation = body.Joints[JointType.SpineBase].Position;

                    var currentDistance = VectorLength(currentLocation);

                    if (result == null || currentDistance < closestBodyDistance)
                    {
                        result = body;
                        closestBodyDistance = currentDistance;
                    }
                }
            }
        }
        return result;
    }

    private static float VectorLength(CameraSpacePoint currentLocation)
    {
        return new Vector3(currentLocation.X, currentLocation.Y, currentLocation.Z).magnitude;
    }

}
