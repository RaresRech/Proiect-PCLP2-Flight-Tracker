using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

//Clasa care se ocupa de plasarea aeroporturilor pe sfera
//Aceasta clasa este atasata unui obiect gol in scena si are un TextMeshProUGUI atasat pentru a afisa numele aeroporturilor
//Aceasta clasa are nevoie de un obiect TextAsset pentru a incarca datele aeroporturilor dintr-un fisier JSON

public class AirportPlacement : MonoBehaviour
{
    #region Serializable Data Classes

    // Clasele AirportData si AirportDataList sunt folosite pentru a incarca datele aeroporturilor din fisierul JSON
    // Clasa AirportData contine numele, codul IATA, latitudinea si longitudinea aeroportului in grade
    // Clasa AirportDataList contine o lista de obiecte AirportData

    [Serializable]
    public class AirportData
    {
        public string Name;
        public string iata;
        public float Latitude;  
        public float Longitude; 
    }

    [Serializable]
    public class AirportDataList
    {
        public List<AirportData> Airports;
    }
    #endregion

    #region Inspector References and Settings
    [Header("References")]
    [SerializeField] private GameObject redDotPrefab;        
    [SerializeField] private TextAsset airportDataJSON;         
    [SerializeField] private TextMeshProUGUI airportNameText;    

    [Header("Settings")]

    // Cat de mari sa fie aeroporturile pe sfera
    [SerializeField] private float scaleFactor = 200f;
    #endregion

    #region Private Fields
    private List<AirportData> airports;   
    private GameObject hoveredAirport;
    #endregion

    #region Unity Lifecycle Methods
    private void Start()
    {
        //Incarcam datele aeroporturilor din fisierul JSON
        LoadAirportDataFromJSON();

        if (airports == null || airports.Count == 0)
        {
            Debug.LogError("No airport data loaded from JSON.");
            return;
        }

        // Plasam aeroporturile pe sfera
        foreach (var airport in airports)
        {
            Vector3 position = CalculatePositionFromLatLong(airport.Latitude, airport.Longitude);
            // Instantiem un punct rosu pentru fiecare aeroport
            GameObject redDot = Instantiate(redDotPrefab, position, Quaternion.identity, transform);
            redDot.name = airport.iata;

            // Atasam un script care sa gestioneze evenimentele mouse-ului pentru fiecare aeroport (hover)
            AirportMouseHandler handler = redDot.AddComponent<AirportMouseHandler>();
            handler.Initialize(this, airport.Name);
        }
    }
    #endregion

    #region Data Loading and Calculation

    private void LoadAirportDataFromJSON()
    {
        if (airportDataJSON != null)
        {
            AirportDataList dataList = JsonUtility.FromJson<AirportDataList>(airportDataJSON.text);
            airports = dataList.Airports;
        }
        else
        {
            Debug.LogError("No JSON file attached in the Inspector.");
        }
    }

    // Calculeaza pozitia unui aeroport pe sfera folosind latitudinea si longitudinea, folosind ecuatiile proiectilor sferice
    private Vector3 CalculatePositionFromLatLong(float latitude, float longitude)
    {
        float phi = 90f - latitude;
        float theta = longitude + 180f;

        float x = Mathf.Sin(phi * Mathf.Deg2Rad) * Mathf.Cos(theta * Mathf.Deg2Rad);
        float y = Mathf.Cos(phi * Mathf.Deg2Rad);
        float z = Mathf.Sin(phi * Mathf.Deg2Rad) * Mathf.Sin(theta * Mathf.Deg2Rad);

        return new Vector3(x, y, z) * scaleFactor;
    }
    #endregion

    #region Mouse Event Handlers
    // Metoda care se apeleaza cand mouse-ul intra in zona unui aeroport
    // Aceasta metoda seteaza textul din TextMeshProUGUI cu numele aeroportului
    public void OnAirportMouseEnter(string airportName)
    {
        if (airportNameText != null)
        {
            airportNameText.text = airportName;
        }
    }

    // Metoda care se apeleaza cand mouse-ul iese din zona unui aeroport
    // Aceasta metoda sterge textul din TextMeshProUGUI
    public void OnAirportMouseExit()
    {
        if (airportNameText != null)
        {
            airportNameText.text = "";
        }
    }
    #endregion
}
