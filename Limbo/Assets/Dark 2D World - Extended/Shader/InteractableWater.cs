using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;



[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(EdgeCollider2D))]
[RequireComponent(typeof(WaterTriggerHandler))]
public class InteractableWater : MonoBehaviour
{
     [Header("Mesh Generation")]
     [Range(2, 500f)] public int NumOfVertices = 70;
     public float Width = 10f;
     public float Height = 4f;
     public Material WaterMaterial;
     private const int NumOf_Y_Vertices = 2;

     [Header("Gizmo Settings")]
     public Color GizmoColor = Color.white;

     private Mesh _mesh;
     private MeshRenderer _meshRenderer;
     private MeshFilter _meshFilter;
     private int[] _topVerticesIndex;
     private Vector3[] _vertices;
     private EdgeCollider2D _edgeCollider;

     private void Start()
     {
          GenerateMesh();
     }

     private void Reset()
     {
          _edgeCollider = GetComponent<EdgeCollider2D>();
          _edgeCollider.isTrigger = true;
     }

    public void ResetEdgeCollider()
    {
        _edgeCollider = GetComponent<EdgeCollider2D>();
        Vector2[] newPoints = new Vector2[2];
        Vector2 firstPoint = new Vector2(_vertices[_topVerticesIndex[0]].x, _vertices[_topVerticesIndex[0]].y);
        newPoints[0] = firstPoint;
        Vector2 secondPoint = new Vector2(_vertices[_topVerticesIndex[NumOfVertices - 1]].x, _vertices[_topVerticesIndex[NumOfVertices - 1]].y);
        newPoints[1] = secondPoint;

        _edgeCollider.offset = Vector2.zero;
        _edgeCollider.points = newPoints;
    }
     public void GenerateMesh()
    {
        _mesh = new Mesh();

        //add vertices
        _vertices = new Vector3[NumOfVertices * NumOf_Y_Vertices];
        _topVerticesIndex = new int[NumOfVertices];
        for(int y = 0; y < NumOf_Y_Vertices; y++)
        {
            for(int x = 0; x < NumOfVertices; x++)
            {
                float xPos = (x / (float)(NumOfVertices - 1)) * Width - Width / 2f;
                float yPos = (y / (float)(NumOf_Y_Vertices - 1)) * Height - Height / 2f;
                _vertices[y * NumOfVertices + x] = new Vector3(xPos, yPos, 0f);

                if(y == NumOf_Y_Vertices - 1)
                {
                    _topVerticesIndex[x] = y * NumOfVertices + x;
                }
                
            }
        }
        //consruct trinangles
        int[] triangles = new int[(NumOfVertices - 1) * (NumOf_Y_Vertices - 1) * 6];
        int index = 0;

        for(int y = 0; y < NumOf_Y_Vertices - 1; y++)
        {
            for(int x = 0; x < NumOfVertices - 1; x++)
            {
                int vertexIndex = y * NumOfVertices + x;

                int bottomLeft = y * NumOfVertices + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + NumOfVertices;
                int topRight = topLeft + 1;

                // First triangle
                triangles[index++] = bottomLeft;
                triangles[index++] = topLeft;
                triangles[index++] = bottomRight;

                // Second triangle
                triangles[index++] = topLeft;
                triangles[index++] = topRight;
                triangles[index++] = bottomRight;
            }
        }
        //Uvs
        Vector2[] uvs = new Vector2[_vertices.Length];
        for(int i = 0; i < _vertices.Length; i++)
        {
            uvs[i] = new Vector2((_vertices[i].x + Width / 2 ) / Width, (_vertices[i].y + Height / 2 ) / Height);
        }
       if (_meshRenderer == null)
           _meshRenderer = GetComponent<MeshRenderer>();
        
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

            _meshRenderer.material = WaterMaterial;

        _mesh.vertices = _vertices;
        _mesh.triangles = triangles;    
        _mesh.uv = uvs;

        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _meshFilter.mesh = _mesh;
    }    
}


[CustomEditor(typeof(InteractableWater))]    
public class InteractableWaterEditor : Editor
{
  private InteractableWater _Water;

  private void OnEnable()
  {
      _Water = (InteractableWater)target;
  }

  public override VisualElement CreateInspectorGUI()
  {
    VisualElement root = new VisualElement();

    InspectorElement.FillDefaultInspector(root, serializedObject, this);

    root.Add(new VisualElement
    {
        style =
        {
            height = 10
        }
    });

    Button generateButton = new Button(() => _Water.GenerateMesh())
    {
        text = "Generate Mesh"
    };
    root.Add(generateButton);

    Button placeEdgeColliderButton = new Button(() => _Water.ResetEdgeCollider())
    {
        text = "Place Edge Collider"
    };
    root.Add(placeEdgeColliderButton);
     return root;
  }

  private void ChangeDimesions(ref float width, ref float height, float calclatedWidthMax, float calculatedHeightMax)
  {
    width = Mathf.Clamp(width, 0.1f, calclatedWidthMax);
    height = Mathf.Clamp(height, 0.1f, calculatedHeightMax);
  }

}   
