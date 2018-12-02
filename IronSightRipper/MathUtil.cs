using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace IronsightRipper
{
    class Matrix
    {
        public static Matrix3D CreateFromQuaternion(Quaternion quaternion)
        {
            // Buffer
            Matrix3D Result = new Matrix3D();

            // Get squared calculations
            double XX = quaternion.X * quaternion.X;
            double XY = quaternion.X * quaternion.Y;
            double XZ = quaternion.X * quaternion.Z;
            double XW = quaternion.X * quaternion.W;

            double YY = quaternion.Y * quaternion.Y;
            double YZ = quaternion.Y * quaternion.Z;
            double YW = quaternion.Y * quaternion.W;

            double ZZ = quaternion.Z * quaternion.Z;
            double ZW = quaternion.Z * quaternion.W;

            // Calculate matrix
            Result.M11 = 1 - 2 * (YY + ZZ);
            Result.M21 = 2 * (XY - ZW);
            Result.M31 = 2 * (XZ + YW);

            Result.M12 = 2 * (XY + ZW);
            Result.M22 = 1 - 2 * (XX + ZZ);
            Result.M32 = 2 * (YZ - XW);

            Result.M13 = 2 * (XZ - YW);
            Result.M23 = 2 * (YZ + XW);
            Result.M33 = 1 - 2 * (XX + YY);

            Result.M14 = 0;
            Result.M24 = 0;
            Result.M34 = 0;
            Result.M44 = 1;

            // Return it
            return Result;
        }

        public static Vector3D TransformVector(Vector3D Vector, Matrix3D Value)
        {
            // Buffer
            Vector3D Result = new Vector3D();

            // Calculate
            Result.X = (Vector.X * Value.M11) + (Vector.Y * Value.M21) + (Vector.Z * Value.M31) + 0;
            Result.Y = (Vector.X * Value.M12) + (Vector.Y * Value.M22) + (Vector.Z * Value.M32) + 0;
            Result.Z = (Vector.X * Value.M13) + (Vector.Y * Value.M23) + (Vector.Z * Value.M33) + 0;

            // Return result
            return Result;
        }
    }

    class QuaternionIS
    {
        public static Vector3D setFromQuaternion(Quaternion Quat)
        {
            Matrix3D matrix = new Matrix3D();
            Vector3D euler = new Vector3D(0, 0, 0);
            matrix = makeRotationFromQuaternion(Quat, matrix);
            euler = setFromRotationMatrix(matrix, euler);
            return euler;
        }

        public static Matrix3D makeRotationFromQuaternion(Quaternion q, Matrix3D matrix)
        {
            Vector3D zero = new Vector3D(0, 0, 0);
            Vector3D one = new Vector3D(1, 1, 1);
            matrix = compose(zero, q, one, matrix);
            return matrix;
        }

        public static Matrix3D compose(Vector3D position, Quaternion quaternion, Vector3D scale, Matrix3D matrix)
        {
            double x = quaternion.X;
            double y = quaternion.Y;
            double z = quaternion.Z;
            double w = quaternion.W;
            double x2 = x + x;
            double y2 = y + y;
            double z2 = z + z;
            double xx = x * x2;
            double xy = x * y2;
            double xz = x * z2;
            double yy = y * y2;
            double yz = y * z2;
            double zz = z * z2;
            double wx = w * x2;
            double wy = w * y2;
            double wz = w * z2;

            double sx = scale.X;
            double sy = scale.Y;
            double sz = scale.Z;

            matrix.M11 = (1 - (yy + zz)) * sx;
            matrix.M12 = (xy + wz) * sx;
            matrix.M13 = (xz - wy) * sx;
            matrix.M14 = 0;

            matrix.M21 = (xy - wz) * sy;
            matrix.M22 = (1 - (xx + zz)) * sy;
            matrix.M23 = (yz + wx) * sy;
            matrix.M24 = 0;

            matrix.M31 = (xz + wy) * sz;
            matrix.M32 = (yz - wx) * sz;
            matrix.M33 = (1 - (xx + yy)) * sz;
            matrix.M34 = 0;

            /*matrix.M41 = position.X;
            matrix.M42 = position.Y;
            matrix.M43 = position.Z;*/
            matrix.M44 = 1;

            return matrix;
        }

        public static Vector3D setFromRotationMatrix(Matrix3D matrix, Vector3D euler)
        {
            euler.Y = Math.Asin(Clamp(matrix.M13, -1, 1));
            if (Math.Abs(matrix.M13) < 0.99999)
            {

                euler.X = Math.Atan2(-matrix.M23, matrix.M33);
                euler.Z = Math.Atan2(-matrix.M12, matrix.M11);

            }
            else
            {

                euler.X = Math.Atan2(matrix.M32, matrix.M22);
                euler.Z = 0;

            }
            euler.Y = RadianToDegree(euler.Y);
            euler.X = RadianToDegree(euler.X);
            euler.Z = RadianToDegree(euler.Z);
            return euler;
        }

        public static double Clamp(double value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        public static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }
    }

    class Vector
    {
        public static Vector3D RotateVector(Vector3D Position, Vector3D Direction)
        {
            // Do the Z axis rotation
            Position.X = (Position.X * Math.Cos(Direction.Z)) - (Position.Y * Math.Sin(Direction.Z));
            Position.Y = (Position.X * Math.Sin(Direction.Z)) + (Position.Y * Math.Cos(Direction.Z));

            // Do the Y axis rotation
            Position.X = (Position.X * Math.Cos(Direction.Y)) + (Position.Z * Math.Sin(Direction.Y));
            Position.Z = (-Position.X * Math.Sin(Direction.Y)) + (Position.Z * Math.Cos(Direction.Y));

            // Do the X axis rotation
            Position.Y = (Position.Y * Math.Cos(Direction.Y)) - (Position.Z * Math.Sin(Direction.Y));
            Position.Z = (Position.Y * Math.Sin(Direction.Y)) + (Position.Z * Math.Cos(Direction.Y));

            return Position;
        }
    }
}
