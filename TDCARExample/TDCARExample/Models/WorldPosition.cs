namespace TDCARExample.Models
{
    public class WorldPosition
    {
        public WorldPosition(float x, float y, float z, float rotationX = 0, float rotationY = 0, float rotationZ = 0, float rotationW = 1)
        {
            X = x;
            Y = y;
            Z = z;
            RotationX = rotationX;
            RotationY = rotationY;
            RotationZ = rotationZ;
            RotationW = rotationW;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotationX { get; }
        public float RotationY { get; }
        public float RotationZ { get; }
        public float RotationW { get; }
    }
}
