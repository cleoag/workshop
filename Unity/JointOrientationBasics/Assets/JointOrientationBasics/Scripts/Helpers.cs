using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class Helpers
{
    private static readonly UnityEngine.Object floorLock = new UnityEngine.Object();
    private static UnityEngine.Vector4 floorClipPlane = UnityEngine.Vector4.zero;
    public static UnityEngine.Vector4 FloorClipPlane
    {
        get
        {
            lock (floorLock)
            {
                return floorClipPlane;
            }
        }

        set
        {
            lock (floorLock)
            {
                if (!value.Equals(floorClipPlane))
                {
                    floorClipPlane = value;
                }
            }
        }
    }

    public static UnityEngine.Quaternion CalculateFloorRotationCorrection(UnityEngine.Vector4 floorNormal)
    { 
        UnityEngine.Vector3 up = floorNormal;
        UnityEngine.Vector3 forward = UnityEngine.Vector3.forward;
        UnityEngine.Vector3 right = UnityEngine.Vector3.Cross(up, forward);

        // correct forward direction
        forward = UnityEngine.Vector3.Cross(right, up);

        return UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(forward.x, -forward.y, forward.z), new UnityEngine.Vector3(up.x, up.y, -up.z));
    }

    public static UnityEngine.Quaternion QuaternionZero = new UnityEngine.Quaternion(0, 0, 0, 0);

    public static UnityEngine.Quaternion ConvertRightToLeftQuaternion(UnityEngine.Quaternion rot, int convert = 6)
    {
        switch (convert)
        {
            case 0:
                rot = new UnityEngine.Quaternion(rot.x, rot.y, rot.z, rot.w);
                break;
            case 1:
                rot = new UnityEngine.Quaternion(rot.x, rot.y, rot.z, -rot.w);
                break;
            case 2:
                rot = new UnityEngine.Quaternion(rot.x, rot.y, -rot.z, rot.w);
                break;
            case 3:
                rot = new UnityEngine.Quaternion(rot.x, rot.y, -rot.z, -rot.w);
                break;
            case 4:
                rot = new UnityEngine.Quaternion(rot.x, -rot.y, rot.z, rot.w);
                break;
            case 5:
                rot = new UnityEngine.Quaternion(rot.x, -rot.y, rot.z, -rot.w);
                break;
            case 6:
                rot = new UnityEngine.Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                break;
            case 7:
                rot = new UnityEngine.Quaternion(rot.x, -rot.y, -rot.z, -rot.w);
                break;
            case 8:
                rot = new UnityEngine.Quaternion(-rot.x, rot.y, rot.z, rot.w);
                break;
            case 9:
                rot = new UnityEngine.Quaternion(-rot.x, rot.y, rot.z, -rot.w);
                break;
            case 10:
                rot = new UnityEngine.Quaternion(-rot.x, rot.y, -rot.z, rot.w);
                break;
            case 11:
                rot = new UnityEngine.Quaternion(-rot.x, rot.y, -rot.z, -rot.w);
                break;
            case 12:
                rot = new UnityEngine.Quaternion(-rot.x, -rot.y, rot.z, rot.w);
                break;
            case 13:
                rot = new UnityEngine.Quaternion(-rot.x, -rot.y, rot.z, -rot.w);
                break;
            case 14:
                rot = new UnityEngine.Quaternion(-rot.x, -rot.y, -rot.z, rot.w);
                break;
            case 15:
                rot = new UnityEngine.Quaternion(-rot.x, -rot.y, -rot.z, -rot.w);
                break;
        }

        return rot;
    }

    public static UnityEngine.Quaternion AlignRotation(UnityEngine.Vector3 fromUp, UnityEngine.Vector3 toUp, UnityEngine.Vector3 fromForward, UnityEngine.Vector3 ToForward)
    {
        // calculate rotation from one orientation to another
        // Y-up in both coordinate spaces
        UnityEngine.Quaternion rotFromTo = UnityEngine.Quaternion.FromToRotation(fromUp, toUp);

        //// using the rotation, from will become to in the new coordinate space
        //UnityEngine.Vector3 confirm = rotFromTo * fromUp;

        // caclulate the new forward direction
        UnityEngine.Vector3 newForward = rotFromTo * fromForward;

        // align to the new direction
        UnityEngine.Vector3 alignFromForward = newForward - UnityEngine.Vector3.Project(newForward, fromUp);
        UnityEngine.Vector3 alignToForward = ToForward - UnityEngine.Vector3.Project(ToForward, toUp);

        UnityEngine.Quaternion rotAlignFromTo = UnityEngine.Quaternion.FromToRotation(alignFromForward, alignToForward);

        // return the combined rotation of the directions
        return rotAlignFromTo * rotFromTo;
    }

    public enum ColorRange { BW = 0, RGB, RGBTint, CMYK, CMYKTint };
    public static UnityEngine.Color[] Colors = { 
                                                   UnityEngine.Color.black, UnityEngine.Color.grey, UnityEngine.Color.white,
                                                   UnityEngine.Color.red, UnityEngine.Color.green, UnityEngine.Color.blue, 
                                                   new UnityEngine.Color(.5f, 0, 0), new UnityEngine.Color(0, .5f, 0), new UnityEngine.Color(0, 0, .5f), 
                                                   UnityEngine.Color.magenta, UnityEngine.Color.yellow, UnityEngine.Color.cyan, 
                                                   new UnityEngine.Color(.5f, 0, .5f), new UnityEngine.Color(.5f,.5f,0), new UnityEngine.Color(0,.5f,.5f), 
                                               };


    public static void DrawDebugQuaternion(UnityEngine.Vector3 startPosition, UnityEngine.Quaternion rotation, ColorRange colorRange = ColorRange.RGB, float scale = .05f)
    {
        UnityEngine.Vector3 right = rotation * UnityEngine.Vector3.right;
        UnityEngine.Vector3 up = rotation * UnityEngine.Vector3.up;
        UnityEngine.Vector3 forward = rotation * UnityEngine.Vector3.forward;

        Helpers.DrawDebugLine(startPosition, right * scale, 3 * (short)colorRange + 0);
        Helpers.DrawDebugLine(startPosition, up * scale, 3 * (short)colorRange + 1);
        Helpers.DrawDebugLine(startPosition, forward * scale, 3 * (short)colorRange + 2);
    }

    public static void DrawDebugBoneWithNormal(UnityEngine.Vector3 startPosition, float length, UnityEngine.Quaternion rotation, ColorRange colorRange = ColorRange.RGB)
    {
        UnityEngine.Vector3 right = rotation * UnityEngine.Vector3.right * length * .25f;
        UnityEngine.Vector3 up = rotation * UnityEngine.Vector3.up * length;
        UnityEngine.Vector3 forward = rotation * UnityEngine.Vector3.forward * length * .25f;

        Helpers.DrawDebugLine(startPosition, right, 3 * (short)colorRange + 0);
        Helpers.DrawDebugLine(startPosition, up, 3 * (short)colorRange + 1);
        Helpers.DrawDebugLine(startPosition, forward, 3 * (short)colorRange + 2);
    }

    public static void DrawDebugBone(UnityEngine.Vector3 position, UnityEngine.Vector3 right, UnityEngine.Vector3 up, UnityEngine.Vector3 forward, ColorRange colorRange = ColorRange.RGB)
    {
        right *= .2f;
        up *= .2f;
        forward *= .2f;

        Helpers.DrawDebugLine(position, right, 3 * (short)colorRange + 0);
        Helpers.DrawDebugLine(position, up, 3 * (short)colorRange + 1);
        Helpers.DrawDebugLine(position, forward, 3 * (short)colorRange + 2);

        Helpers.DrawDebugLine(position + (forward * .2f), forward, 2);
    }

    public static void DrawDebugLine(UnityEngine.Vector3 start, UnityEngine.Vector3 end, int colorIndex = 0)
    {
        UnityEngine.Debug.DrawRay(start, end, Helpers.Colors[colorIndex], 0.0f, false);
        UnityEngine.Debug.DrawRay(start + end, -(end * .05f), UnityEngine.Color.white, 0.0f, false);
    }

    public static void SetVisible(UnityEngine.GameObject gameObject, bool isVisible)
    {
        var renderers = gameObject.GetComponentsInChildren<UnityEngine.Renderer>();
        for (int i = 0; i < renderers.Length; ++i)
        {
            renderers[i].enabled = isVisible;
        }
    }
}

namespace Numbers
{
    public class Quaternion
    {
        public Quaternion(float w_, float xi, float yj, float zk)
        {
            w = w_;
            x = xi;
            y = yj;
            z = zk;
        }
        private float w;
        public float W
        {
            set
            { w = value; }
            get
            { return w; }
        }
        private float x;
        public float X
        {
            set { x = value; }
            get { return x; }
        }
        private float y;
        public float Y
        {
            set { y = value; }
            get { return y; }
        }
        private float z;
        public float Z
        {
            set { z = value; }
            get { return z; }
        }
        /// <summary>
        /// Euclidean norm
        /// </summary>
        public float Norm
        {
            get { return (float)Math.Sqrt(w * w + x * x + y * y + z * z); }
        }
        /// <summary>
        /// Conjugate
        /// </summary>
        public Quaternion Conj
        {
            get { return new Quaternion(w, -x, -y, -z); }
        }
        public static Quaternion operator +(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.W + q2.W, q1.X + q2.X, q1.Y + q2.Y, q1.Z + q2.Z);
        }
        public static Quaternion operator -(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.W - q2.W, q1.X - q2.X, q1.Y - q2.Y, q1.Z - q2.Z);
        }
        /// <summary>
        /// product of two quaterions
        /// </summary>
        /// <param name="q1">Quaternion1
        /// <param name="q2">Quaternion2
        /// <returns>Quaternion1*Quaternion2</returns>
        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z
                , q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y
                , q1.w * q2.y + q1.y * q2.w + q1.z * q2.x - q1.x * q2.z
                , q1.w * q2.z + q1.z * q2.w + q1.x * q2.y - q1.y * q2.x);
        }
        public static Quaternion operator *(float f, Quaternion q)
        {
            return new Quaternion(f * q.w, f * q.x, f * q.y, f * q.z);
        }
        public static Quaternion operator *(Quaternion q, float f)
        {
            return new Quaternion(f * q.w, f * q.x, f * q.y, f * q.z);
        }
        public static Quaternion operator /(Quaternion q, float f)
        {
            if (f == 0.0f) { throw new DivideByZeroException(); }
            return new Quaternion(1 / f * q.w, 1 / f * q.x, 1 / f * q.y, 1 / f * q.z);
        }
        public static Quaternion operator /(float f, Quaternion q)
        {
            if (q.Norm == 0.0f) { throw new DivideByZeroException(); }
            return f / (q.Norm * q.Norm) * q.Conj;
        }
        public static Quaternion operator /(Quaternion q1, Quaternion q2)
        {
            return q1 * q2.Conj / (q2.Norm * q2.Norm);
        }
        public static bool operator ==(Quaternion q1, Quaternion q2)
        {
            if (Math.Abs(q1.w - q2.w) < 0.00001f
                && Math.Abs(q1.x - q2.x) < 0.00001f
                && Math.Abs(q1.y - q2.y) < 0.00001f
                && Math.Abs(q1.z - q2.z) < 0.00001f)
            {
                return true;
            }
            return false;
        }
        public static bool operator !=(Quaternion q1, Quaternion q2)
        {
            if (q1.w == q2.w && q1.x == q2.x && q1.y == q2.y && q1.z == q2.z)
            {
                return false;
            }
            return true;
        }
        public override bool Equals(object obj)
        {
            Quaternion q = obj as Quaternion;
            return q == this;
        }
        public override int GetHashCode()
        {
            return this.w.GetHashCode() ^ (this.x.GetHashCode() * this.y.GetHashCode() * this.z.GetHashCode());
        }
        public float[] Rotate(float x1, float y1, float z1)
        {
            Quaternion q = new Quaternion(0.0f, x1, y1, z1);
            Quaternion r = this * q * this.Conj;
            return new float[3] { r.X, r.Y, r.Z };
        }
    }
}
