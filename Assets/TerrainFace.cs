using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace
{

    ShapeGenerator shapeGenerator;
    Mesh mesh;
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp)
    {
        this.shapeGenerator = shapeGenerator;
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    public void ConstructMesh()
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;
        Vector2[] uv = (mesh.uv.Length == vertices.Length)?mesh.uv:new Vector2[vertices.Length];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                float unscaledElevation = shapeGenerator.CalculateUnscaledElevation(pointOnUnitSphere);
                vertices[i] = pointOnUnitSphere * shapeGenerator.GetScaledElevation(unscaledElevation);
                uv[i].y = unscaledElevation;

                if (x != resolution - 1 && y != resolution - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }
        mesh.Clear();
		//mesh.IndexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
		mesh.uv = uv;
        //mesh.RecalculateNormals();

		Terrace(mesh);
    }

    public void UpdateUVs(ColourGenerator colourGenerator)
    {
        Vector2[] uv = mesh.uv;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                uv[i].x = colourGenerator.BiomePercentFromPoint(pointOnUnitSphere);
            }
        }
        mesh.uv = uv;
    }


    public void Terrace(Mesh mesh) {

        Vector3[] vertices =  mesh.vertices;
        int[] triangles = mesh.triangles;

        
		List<Vector3> newVertices = new List<Vector3>();
		List<int> newTriangles = new List<int>();
        //int iv = 0;

		float terraceHeight = 1f;


        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

			//v1 = transform.TransformPoint(v1);
			//v2 = transform.TransformPoint(v2);
			//v3 = transform.TransformPoint(v3);
            if (i < 50) {
                MonoBehaviour.print(v1);
            }
            /* float h1 = v1.y;
            float h2 = v2.y;
            float h3 = v3.y; */

			float h1 = v1.magnitude; // Magnitude of the vector from the origin to v1
			float h2 = v2.magnitude;
			float h3 = v3.magnitude;

			float min = Mathf.Min(h1, h2, h3);
			float max = Mathf.Max(h1, h2, h3);

            float h_min = Mathf.Floor(min);
            float h_max = Mathf.Floor(max);
			

            for (float h = h_min; h <= h_max; h += terraceHeight)
            {
                int points_above = 0;

				if (h1 < h) {
					if (h2 < h) {
						if (h3 < h) {}
							// All points are below the plane, no triangles will be
							// added to the mesh (should not be possible)
						else {
							points_above = 1;        // v3 is above
							// no need to swap values, they're already fine
						}
					}
					else {
						if (h3 < h) {
							points_above = 1;   
							(v1, v2, v3) = (v3, v1, v2);  // make it v3 instead
						}
						else {
							points_above = 2;         // v2 and v3 are above
							(v1, v2, v3) = (v2, v3, v1);  // make them v1 and v2 instead
						}
					}
				}
				else {
					if (h2 < h) {
						if (h3 < h) {
							points_above = 1;         // v1 is above
							(v1, v2, v3) = (v2, v3, v1);  // make it v3 instead
						}
						else {
							points_above = 2;         // v1 and v3 are above
							(v1, v2, v3) = (v3, v1, v2);  // make them v1 and v2 instead
						}
					}
					else {
						if (h3 < h) points_above = 2;         // v1 and v2 are above // no need to swap values, they're already fine

						else points_above = 3;         // all vectors are above
					}

				}


				h1 = v1.magnitude;  // Since we've swapped values of the points, let's find their heights again
				h2 = v2.magnitude;
				h3 = v3.magnitude;


				// Current plane:
				Vector3 v1_c = Vector3.Normalize(v1) * h;
                Vector3 v2_c = Vector3.Normalize(v2) * h;
                Vector3 v3_c = Vector3.Normalize(v3) * h;
				/* Vector3 v1_c = new Vector3(v1.x, h1, v1.z);
				Vector3 v2_c = new Vector3(v2.x, h2, v2.z);
				Vector3 v3_c = new Vector3(v3.x, h3, v3.z);  */


				// The plane below; these vertices will be used to make vertical "walls"
				// between planes
				/* Vector3 v1_b = new Vector3(v1.x, h1 - 1, v1.z);
				Vector3 v2_b = new Vector3(v2.x, h2 - 1, v2.z);
				Vector3 v3_b = new Vector3(v3.x, h3 - 1, v3.z);  */
				Vector3 v1_b = Vector3.Normalize(v1) * (h - terraceHeight);
                Vector3 v2_b = Vector3.Normalize(v2) * (h - terraceHeight);
                Vector3 v3_b = Vector3.Normalize(v3) * (h - terraceHeight);



                if (points_above == 3)
                {
                    AddMeshVertex(v1_c, ref newVertices);
                    AddMeshVertex(v2_c, ref newVertices);
                    AddMeshVertex(v3_c, ref newVertices);

					int baseIndex = newVertices.Count - 3;
					newTriangles.Add(baseIndex);
					newTriangles.Add(baseIndex + 1);
					newTriangles.Add(baseIndex + 2);
                    
                }
				else {

					float t1 = (h1 - h) / (h1 - h3);  // Interpolation value for v1 and v3
					Vector3 v1_c_n = Vector3.Lerp(v1_c, v3_c, t1);
					Vector3 v1_b_n = Vector3.Lerp(v1_b, v3_b, t1);

					float t2 = (h2 - h) / (h2 - h3);  // Interpolation value for v2 and v3
					Vector3 v2_c_n = Vector3.Lerp(v2_c, v3_c, t2);
					Vector3 v2_b_n = Vector3.Lerp(v2_b, v3_b, t2);


					if (points_above == 2) {

						// Add "roof" part of the step
						AddMeshVertex(v1_c, ref newVertices);
						AddMeshVertex(v2_c, ref newVertices);
						AddMeshVertex(v2_c_n, ref newVertices);
						AddMeshVertex(v1_c_n, ref newVertices);

						int baseIndex = newVertices.Count - 4;
						AddMeshTriangle(ref newTriangles, baseIndex, baseIndex + 1, baseIndex + 2);  //(iv, iv + 1, iv + 2)
						AddMeshTriangle(ref newTriangles, baseIndex + 2, baseIndex + 3, baseIndex);
						

						// Add "wall" part of the step
						AddMeshVertex(v1_c_n, ref newVertices); 
						AddMeshVertex(v2_c_n, ref newVertices); 
						AddMeshVertex(v2_b_n, ref newVertices);
						AddMeshVertex(v1_b_n, ref newVertices);

						baseIndex = newVertices.Count - 4;
						AddMeshTriangle(ref newTriangles, baseIndex, baseIndex + 1, baseIndex + 2);
						AddMeshTriangle(ref newTriangles, baseIndex, baseIndex + 2, baseIndex + 3);
					}

					else if (points_above == 1) {

						// Add "roof" part of the step
						AddMeshVertex(v3_c, ref newVertices);
						AddMeshVertex(v1_c_n, ref newVertices); 
						AddMeshVertex(v2_c_n, ref newVertices); 

						int baseIndex = newVertices.Count - 3;
						AddMeshTriangle(ref newTriangles, baseIndex, baseIndex + 1, baseIndex + 2);

						// Add "wall" part of the step
						
						AddMeshVertex(v2_c_n, ref newVertices);
						AddMeshVertex(v1_c_n, ref newVertices);
						AddMeshVertex(v1_b_n, ref newVertices);
						AddMeshVertex(v2_b_n, ref newVertices);

						baseIndex = newVertices.Count - 4;
						AddMeshTriangle(ref newTriangles, baseIndex, baseIndex + 1, baseIndex + 3);
						AddMeshTriangle(ref newTriangles, baseIndex + 1, baseIndex + 2, baseIndex + 3);

					}

				}
            }
        }

        mesh.vertices = newVertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.RecalculateNormals();

	}

	void AddMeshVertex(Vector3 vertex, ref List<Vector3> vertices)
    {
        vertices.Add(vertex);
    }

    void AddMeshTriangle(ref List<int> triangles, int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

}
