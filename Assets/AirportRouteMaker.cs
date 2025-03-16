using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aceasta clasa este responsabila pentru a crea o ruta intre doua aeroporturi selectate
/// Aceasta clasa este atasata unui obiect gol in scena si are un LineRenderer atasat pentru a desena linia dintre aeroporturi 
/// Aceasta clasa are nevoie de un obiect Flight Tracker pentru a obtine aeroporturile selectate si a desena ruta dintre ele
/// </summary>

public class AirportRouteMaker : MonoBehaviour
{

    //Referinta catre Flight Tracker (obligatorie)
    #region Inspector Fields
    [Header("Link to the Flight Tracker (Required)")]
    [SerializeField] private GameObject flightTracker;

    //Numarul de puncte de pe linie (rezolutia)
    [Header("Line Settings")]
    [SerializeField] private int numPoints = 50;
    #endregion


    //Variabile private 
    //LineRenderer-ul folosit pentru a desena linia
    //Lista de aeroporturi selectate
    //Lista de stringuri pentru aeroporturile selectate
    #region Private Fields
    private LineRenderer lineRenderer;
    private List<Transform> currentAirports = new List<Transform>();
    private List<string> currentRouteString;
    private Transform previouslySelectedDAirport;
    private Transform previouslySelectedAAirport;
    private Dictionary<string, Material> airportMaterials = new Dictionary<string, Material>();
    #endregion

    #region Unity Methods
    private void Start()
    {
        // Daca Flight Tracker nu este setat in inspector, il cautam in scena
        if (flightTracker == null)
        {
            flightTracker = GameObject.Find("Flight Tracker");
        }

        // StoreInitialMaterials() stocheaza materialele initiale ale fiecarui copil (aeroport) pentru restaurare ulterioara
        StoreInitialMaterials();

        // Initializam LineRenderer-ul
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = numPoints;
        }
    }

    private void Update()
    {
        // currentRouteString este o lista de stringuri care contine aeroporturile selectate
        // Daca lista nu este goala, schimbam materialele aeroporturilor selectate
        currentRouteString = flightTracker.GetComponent<RandomFlightInfo>().GetCurrentRoute();

        if (!string.IsNullOrEmpty(currentRouteString[0]))
        {
            ChangeAirportMaterials(currentRouteString[1], currentRouteString[0]);
        }
    }
    #endregion

    #region Line Drawing Methods
    // Metoda DrawLine deseneaza o linie intre doua puncte
    // Metoda este apelata in Update pentru a desena linia intre aeroporturi
    private void DrawLine(Transform startPoint, Transform endPoint)
    {
        if (lineRenderer == null) return;

        for (int i = 0; i < numPoints; i++)
        {
            float t = i / (float)(numPoints - 1);
            Vector3 point = Vector3.Slerp(startPoint.position, endPoint.position, t);
            lineRenderer.SetPosition(i, point);
        }
    }

    // Metoda ClearLine ascunde LineRenderer-ul
    public void ClearLine()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }
    #endregion

    #region Airport Materials Methods
    // Metoda StoreInitialMaterials stocheaza materialele initiale ale fiecarui copil (aeroport) pentru restaurare ulterioara
    // Metoda ChangeAirportMaterials schimba materialele aeroporturilor selectate si deseneaza linia dintre ele
    private void StoreInitialMaterials()
    {
        airportMaterials.Clear();
        foreach (Transform child in transform)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                airportMaterials[child.name] = renderer.material;
            }
        }
    }

    // Metoda ChangeAirportMaterials schimba materialele aeroporturilor selectate si deseneaza linia dintre ele
    // Metoda RevertAirportMaterial revine la materialele initiale ale aeroporturilor selectate
    private void ChangeAirportMaterials(string departure, string arrival)
    {
        // Re-activam materialele aeroporturilor selectate anterior
        RevertAirportMaterial(previouslySelectedDAirport);
        RevertAirportMaterial(previouslySelectedAAirport);

        // Iteram prin copiii obiectului curent (aeroporturi)
        // Daca numele copilului este egal cu aeroportul de plecare sau aeroportul de sosire, schimbam materialul aeroportului
        foreach (Transform child in transform)
        {
            if (child.name == departure || child.name == arrival)
            {
                if (!currentAirports.Contains(child))
                {
                    Debug.Log("Drawing trail");
                    currentAirports.Add(child);
                }

                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Stocam aeroporturile selectate pentru a le folosi ulterior
                    if (child.name == departure)
                    {
                        previouslySelectedDAirport = child;
                    }
                    else if (child.name == arrival)
                    {
                        previouslySelectedAAirport = child;
                    }

                    // Creeaza un material nou pentru aeroportul selectat si il coloreaza in galben cu o stralucire galbena
                    Material airportMaterial = new Material(renderer.material);
                    airportMaterial.color = Color.yellow;
                    airportMaterial.EnableKeyword("_EMISSION");
                    airportMaterial.SetColor("_EmissionColor", Color.yellow * 2.0f); 
                    renderer.material = airportMaterial;
                }
            }
        }

        // Daca exista doua aeroporturi selectate, desenam linia dintre ele folosind metoda DrawLine
        if (currentAirports != null && currentAirports.Count == 2)
        {
            lineRenderer.enabled = true;
            DrawLine(currentAirports[0], currentAirports[1]);
        }
        else
        {
            ClearLine();
        }
    }

    // Metoda RevertAirportMaterial revine la materialele initiale ale aeroporturilor selectate
    private void RevertAirportMaterial(Transform airport)
    {
        if (airport == null) return;

        // Curata lista de aeroporturi selectate
        if (currentAirports != null)
        {
            currentAirports.Clear();
        }

        Renderer renderer = airport.GetComponent<Renderer>();
        if (renderer != null && airportMaterials.TryGetValue(airport.name, out Material initialMaterial))
        {
            renderer.material = initialMaterial;
        }
    }
    #endregion
}
