using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcMesh;
using static UnityEngine.ParticleSystem;
using UnityEditor;

public class AntlerGenerator : ProcMeshGenBase
{
    [Socks.Field(category="Growth")]
    public float initialChanceOfGrowth = 1.5f;

    [Socks.Field(category="Growth")]
    public float growthChanceReduction = 0.1f;
    
    [Socks.Field(category="Growth")]
    public int maximumGrowths = 5;

    [Socks.Field(category="Growth")]
    public int minimumInitialGrowths = 2;
    
    [Socks.Field(category="Size")]
    public MinMaxCurve radius;
    
    [Socks.Field(category="Size")]
    public float sizeReduction = 0.1f;
    
    [Socks.Field(category="Distance")]
    public MinMaxCurve distanceFromParent;
    
    [Socks.Field(category="Distance")]
    public float distanceReduction = 0.03f;

    [Socks.Field(category="Height")]
    public MinMaxCurve heightFromParent;

    [Socks.Field(category="Height")]
    public float heightReduction = 0.1f;

    [Socks.Field(category="Quality")]
    public int lengthSegments = 10;
    
    [Socks.Field(category="Quality")]
    public int radialSegments = 10;

    [Socks.Field(category="Positioning")]
    public bool showPosition = false;

    [Socks.Field(category="Positioning")]
    public Vector3 position;

    [Socks.Field(category="Debug")]
    public bool visualizeTree;

    [Socks.Field(category="Mesh")]
    public GameObject leftMesh;
    
    [Socks.Field(category="Mesh")]
    public GameObject rightMesh;

    void Reset()
    {
        BeginMeshGen();
    }

    public override void BeginMeshGen()
    {
        // todo: error handling?
        base.BeginMeshGen();

        //Debug.LogFormat("Creating a new procmesh. No. of tree nodes: {0}", tree.nodes.Count);
        leftMesh.GetComponent<MeshFilter>().sharedMesh = builder.CreateMesh();
        leftMesh.GetComponent<MeshRenderer>().sharedMaterials = builder.materials.ToArray();
        
        rightMesh.GetComponent<MeshFilter>().sharedMesh = builder.CreateMesh();
        rightMesh.GetComponent<MeshRenderer>().sharedMaterials = builder.materials.ToArray();
    }

    public override void GenerateNode(MeshBuilder builder, string rootNodeID, Vector3 position, Quaternion rotation, int depth, int submeshDepth)
    {
        /** Grab the number of generations */
        float numberOf = Random.Range(Mathf.Clamp(minimumInitialGrowths - (depth), 0, 1000), maximumGrowths);

        for (int i = 0; i < numberOf; ++i)
        {
            float t = (float)i/(float)numberOf;
            if ((initialChanceOfGrowth - (depth*growthChanceReduction)) > Random.Range(0f, 1f))
            {
                /** Attach new node to tree*/
                string newNodeID = tree.AttachNewNode(rootNodeID);

                float distMagnitude = distanceFromParent.Evaluate(t, Random.value) - (depth * distanceReduction);
                Vector3 dist = new Vector3(distMagnitude, 0f, 0f);

                Vector3 heightDir = Vector3.up;
                float heightMagnitude = Mathf.Clamp((heightFromParent.Evaluate(t, Random.value) - (depth * heightReduction)), 0f, 1000f);
                Vector3 height = heightDir * heightMagnitude;

                tree.Get(newNodeID).position += dist; 
                tree.Get(newNodeID).position += height;

                Vector3 distBetweenNewNodeAndParent = tree.Get(newNodeID).position - tree.Get(rootNodeID).position;
                BezierPoint p0 = new BezierPoint(tree.Get(rootNodeID).position, new Vector3(dist.x, 0f, dist.z)*0.75f);
                BezierPoint p1 = new BezierPoint(tree.Get(newNodeID).position+Vector3.up, new Vector3(0f, dist.y, 0f)*0.75f);
                BezierSpline spline = new BezierSpline();
                spline.Clear();
                spline.SetPoints(new List<BezierPoint>{p0, p1});

                /** Set up cylinder data! */
                ProcMesh.CurvedCylinderData cylinder = new ProcMesh.CurvedCylinderData();
                cylinder.curve = spline;
                cylinder.lengthSegments = lengthSegments;
                cylinder.radialSegments = radialSegments;
                cylinder.radius = radius;

                vertex[] vertices = null;
                /** Build tube */
                vertices = MeshUtils.BuildCylinderAlongBezier(builder, builder.GetSubmesh(material), cylinder);

                foreach (ProcMesh.Submesh submesh in subMeshes.FindAll(x => x.startPoint == SubmeshStartPoints.AllOverVertices))
                {
                    for (int j = 0; j < vertices.Length; ++j)
                    {
                        GenerateSubMesh(submesh, builder, newNodeID, vertices[j].index, Quaternion.FromToRotation(Vector3.up, vertices[j].normal), submeshDepth+1);
                    }
                }

                /** Generate next node */
                GenerateNode(builder, newNodeID, position, rotation, depth+1, submeshDepth);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (showPosition)
        {
            Gizmos.DrawSphere(transform.position + position, 0.1f);
            Gizmos.DrawSphere(transform.position + new Vector3(-position.x, position.y, position.z), 0.1f);
        }
    }

}
