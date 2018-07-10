using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace IronSightRipper
{
    class QuatUtil
    {
        public static Vector3D setFromQuaternion(Quaternion q, Vector3D euler)
        {
            Matrix3D matrix = new Matrix3D();
            matrix = makeRotationFromQuaternion(q, matrix);
            euler = setFromRotationMatrix(matrix, euler);
            return euler;
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

        public static double Clamp(double value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
    }
}
