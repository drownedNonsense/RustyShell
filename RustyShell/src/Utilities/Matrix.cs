using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace RustyShell.Utilities;  
public static class Matrix {
    public static float[] AlignmentMatrix(Vec3f target)
    {
        Vec3f BASE = new (1, 0, 0);

        // Normalize v_target
        float targetMagnitude     = target.Length();
        Vec3f v_target_normalized = target.NormalizedCopy();

        // Calculate rotation axis (cross product)
        Vec3f rotationAxis = BASE.Cross(v_target_normalized);
        float rotationAxisLength = rotationAxis.Length();

        // Calculate rotation angle (dot product)
        float dotProduct = BASE.Dot(v_target_normalized);
        float rotationAngle = (float)Math.Acos(Math.Clamp(dotProduct, -1.0f, 1.0f));

        // Handle special case where the rotation axis length is near zero (i.e., vectors are collinear)
        if (rotationAxisLength < 0.0001f)
        {
            // If the vectors are collinear, check if they are in the opposite direction
            if (Math.Abs(dotProduct + 1.0f) < 0.0001f)
            {
                // Vectors are exactly opposite, return a rotation matrix for 180 degrees around an axis orthogonal to v_base
                rotationAxis  = new Vec3f(0f, 1f, 0f).NormalizedCopy();
                rotationAngle = (float)Math.PI;
            }
            else
            {
                // Vectors are already aligned
                return Mat4f.Identity_Scaled(Mat3f.Create(), targetMagnitude);
            }
        }
        else
        {
            rotationAxis = rotationAxis.NormalizedCopy();
        }

        // Compute rotation matrix (using Rodrigues' rotation formula for 3x3 submatrix)
        float cosTheta = (float)Math.Cos(rotationAngle);
        float sinTheta = (float)Math.Sin(rotationAngle);
        float x = rotationAxis[0];
        float y = rotationAxis[1];
        float z = rotationAxis[2];

        // 4x4 matrix in column-major order
        float[] rotationMatrix = new float[16];

        // First row
        rotationMatrix[0] = cosTheta + (1 - cosTheta) * x * x;
        rotationMatrix[1] = (1 - cosTheta) * x * y + sinTheta * z;
        rotationMatrix[2] = (1 - cosTheta) * x * z - sinTheta * y;
        rotationMatrix[3] = 0.0f; // No translation in x

        // Second row
        rotationMatrix[4] = (1 - cosTheta) * y * x - sinTheta * z;
        rotationMatrix[5] = cosTheta + (1 - cosTheta) * y * y;
        rotationMatrix[6] = (1 - cosTheta) * y * z + sinTheta * x;
        rotationMatrix[7] = 0.0f; // No translation in y

        // Third row
        rotationMatrix[8] = (1 - cosTheta) * z * x + sinTheta * y;
        rotationMatrix[9] = (1 - cosTheta) * z * y - sinTheta * x;
        rotationMatrix[10] = cosTheta + (1 - cosTheta) * z * z;
        rotationMatrix[11] = 0.0f; // No translation in z

        // Fourth row
        rotationMatrix[12] = 0.0f;
        rotationMatrix[13] = 0.0f;
        rotationMatrix[14] = 0.0f;
        rotationMatrix[15] = 1.0f; // Homogeneous coordinate

        // Scale the diagonal elements of the matrix by the magnitude of v_target
        return ScaleMatrix(rotationMatrix, targetMagnitude);
    }

    private static float[] IdentityMatrix4()
    {
        return new float[]
        {
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        };
    }

    private static float[] ScaleMatrix(float[] matrix, float scale)
    {
        float[] scaledMatrix = new float[16];
        for (int i = 0; i < 16; i++)
        {
            scaledMatrix[i] = matrix[i] * scale;
        }
        return scaledMatrix;
    }
} // class ..
