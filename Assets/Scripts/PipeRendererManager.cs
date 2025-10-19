using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PipeRendererManager : MonoBehaviour
{
    // --- BASIC REFERENCES ---
    [Header("Scene References")]
    [SerializeField] private Transform solarPanel;
    [SerializeField] private Transform pump;
    [SerializeField] private Transform storageTank;

    // --- UPDATED VISUALS ---
    // We now assign two separate materials instead of one material and two colors.
    [Header("Pipe Visuals")]
    [SerializeField] private Material hotPipeMaterial;
    [SerializeField] private Material coldPipeMaterial;
    [SerializeField] private float pipeThickness = 0.08f;
    [Tooltip("Check this to draw pipes with 90-degree angles.")]
    [SerializeField] private bool useElbows = true;

    // References to the lines we will create
    private LineRenderer hotPipe_PanelToPump;
    private LineRenderer hotPipe_PumpToTank;
    private LineRenderer coldReturnPipe;

    // We keep this method for potential future use, but it's not currently driving colors.
    public void SetLiveState(float panelC, float tankC, bool pumpOn) { }

    void Start()
    {
        // --- VALIDATION (Updated for new materials) ---
        if (solarPanel == null || pump == null || storageTank == null || hotPipeMaterial == null || coldPipeMaterial == null)
        {
            Debug.LogError("CRITICAL ERROR on PipeRendererManager: One or more references (Solar Panel, Pump, Tank, Hot Pipe Material, or Cold Pipe Material) are not assigned in the Inspector!", this);
            this.enabled = false;
            return;
        }

        // --- CREATE THE PIPE OBJECTS ---
        // We now assign the correct material when creating each pipe.
        hotPipe_PanelToPump = CreatePipe("HotPipe_PanelToPump", hotPipeMaterial);
        hotPipe_PumpToTank = CreatePipe("HotPipe_PumpToTank", hotPipeMaterial);
        coldReturnPipe = CreatePipe("ColdReturnPipe", coldPipeMaterial);
    }

    // --- CreatePipe now takes a material as an argument ---
    private LineRenderer CreatePipe(string name, Material materialToAssign)
    {
        var pipeObject = new GameObject(name);
        pipeObject.transform.SetParent(this.transform, false);

        var lineRenderer = pipeObject.AddComponent<LineRenderer>();

        lineRenderer.material = materialToAssign; // Assign the specific material
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = pipeThickness;
        lineRenderer.endWidth = pipeThickness;

        // We can now remove the start/end color properties, as the material controls the color.

        lineRenderer.numCapVertices = 8;
        lineRenderer.numCornerVertices = 8;

        return lineRenderer;
    }

    void Update()
    {
        // We only need to update the geometry each frame.
        UpdatePipeGeometry();
    }

    private void UpdatePipeGeometry()
    {
        Vector3 panelPos = solarPanel.position;
        Vector3 pumpPos = pump.position;
        Vector3 tankPos = storageTank.position;

        SetSegmentPositions(hotPipe_PanelToPump, panelPos, pumpPos);
        SetSegmentPositions(hotPipe_PumpToTank, pumpPos, tankPos);
        SetSegmentPositions(coldReturnPipe, tankPos, panelPos);
    }

    private void SetSegmentPositions(LineRenderer line, Vector3 startPos, Vector3 endPos)
    {
        if (useElbows)
        {
            line.positionCount = 4;
            Vector3 midPoint1 = new Vector3(startPos.x, startPos.y, endPos.z);
            Vector3 midPoint2 = new Vector3(startPos.x, endPos.y, endPos.z);
            line.SetPositions(new Vector3[] { startPos, midPoint1, midPoint2, endPos });
        }
        else
        {
            line.positionCount = 2;
            line.SetPositions(new Vector3[] { startPos, endPos });
        }
    }
}

