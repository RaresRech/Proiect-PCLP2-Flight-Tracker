using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

#region Data Classes

/// <summary>
/// Aici sunt clasele care contin datele despre zboruri si aeroporturi
/// Clasele sunt folosite pentru a deserializa JSON-ul primit de la API-ul de zboruri si apoi pentru a le trimite catre alte scripturi pentru a fi folosite in vizualizare
/// </summary>

[Serializable]
public class FlightData 
{ 
    public FlightInfo[] data; 
}

[Serializable]
public class FlightAirport 
{
    public string continent;
    public string airport;
    public string iata; 
    public string scheduled;
}

[Serializable]
public class FlightInfo 
{
    public FlightAirport departure;
    public FlightAirport arrival;
    public FlightAirline airline;
    public Flight flight;
    public string flight_date;
    public string flight_status;
}

[Serializable]
public class FlightAirline 
{
    public string name;
}

[Serializable]
public class Flight 
{
    public string number;
}

[Serializable]
public class Airport 
{
    public string Name;
    public string iata;
    public float Latitude;
    public float Longitude;
}

[Serializable]
public class AirportList 
{
    public List<Airport> Airports;
}
#endregion

///Aici se afla scriptul care se ocupa de obtinerea datelor despre zboruri
///Scriptul se ocupa de a face request-uri catre API-ul de zboruri http://api.aviationstack.com/v1/flights si a filtra datele, organizandu-le in obiecte a claselor mai sus declarate

public class RandomFlightInfo : MonoBehaviour
{

    ///Directivele region si endregion sunt folosite pentru a grupa anumite parti ale codului, pentru a face codul mai usor de citit si de inteles
    ///Directivele Header si SerializeField sunt folosite pentru a face variabilele private vizibile in inspectorul Unity. Acestea nu afecteaza functionalitatea codului


    ///Variabilele de mai jos sunt folosite pentru UI-ul aplicatiei, folosindu-se de libraria TextMeshPro pentru a afisa textul in mod mai estetic
    ///Variabilele sunt folosite pentru a afisa informatii despre zboruri, cum ar fi destinatia, ora de plecare, numarul de zbor si compania aeriana

    #region Inspector Fields & UI Elements
    [Header("Assign UI Elements")]  
    [SerializeField] private TextMeshProUGUI destinationText;
    [SerializeField] private TextMeshProUGUI departureTimeText;
    [SerializeField] private TextMeshProUGUI flightNumberText;
    [SerializeField] private TextMeshProUGUI airlineText;
    [SerializeField] private UnityEngine.UI.Button randomFlightButton;

    /// Variabila flightCount este folosita pentru a specifica cate zboruri sa fie obtinute de la API
    /// Variabila airportsJson este folosita pentru a specifica fisierul JSON care contine aeroporturile permise
    /// Variabila allowedAirports este folosita pentru a stoca aeroporturile permise, citite din fisierul JSON
    /// Variabilele ApiKey si ApiUrl sunt folosite pentru a face request-uri catre API-ul de zboruri. Acestea trebuie modificate din cod daca se doreste schimbarea cheii de API sau a URL-ului
    /// Variabila flightData este folosita pentru a stoca datele despre zboruri obtinute de la API
    /// Variabila currentFlightIndex este folosita pentru a tine evidenta zborului curent
    /// Variabilele currentRouteDeparture si currentRouteArrival sunt folosite pentru a stoca aeroporturile de plecare si destinatie ale zborului curent

    [Header("Flight Settings")]
    [Tooltip("The obtained flights will be the most recently scheduled ones.")]
    [SerializeField] private string flightCount = "2";

    [Header("Airports JSON File")]
    [Tooltip("JSON file containing allowed airports.")]
    [SerializeField] private TextAsset airportsJson;
    #endregion

    #region Private Fields
    private List<string> allowedAirports = new List<string>();

    ///Cheia de API si URL-ul pentru API-ul de zboruri sunt pentru o versiune trial a API-ului, care permite doar cateva request-uri pe zi si un numar maxim de 100 de zboruri pe request
    ///Pentru a folosi aplicatia fara restrictii, se poate obtine o cheie de API de la site-ul https://aviationstack.com/ si se poate folosi aceasta cheie in locul celei de mai jos
    ///Intru-o versiune de productie, e necesara o cheie de API premium pentru a face request-uri catre API-ul de zboruri cu un numar mai mare de zboruri pe request
    ///De asemenea, o cheie premium va permite feature-uri suplimentare, cum ar fi obtinerea pozitiei reale a avioanelor in timp , a numarului de pasageri si a altor informatii despre zboruri

    private const string ApiKey = "32ca66390eea47dbc213bf03cc7c6e09";
    private const string ApiUrl = "http://api.aviationstack.com/v1/flights";
    private FlightData flightData;
    private int currentFlightIndex = 0;
    public string currentRouteDeparture;
    public string currentRouteArrival;
    #endregion

    #region Unity Lifecycle Methods

    ///In Unity, cele 3 metode de baza sunt Start(), Update() si Awake(), care sunt folosite pentru initializare, update si initializare a variabilelor, respectiv pentru a face initializari la inceputul rularii aplicatiei

    private void Awake()
    {
        ///Incarca aeroporturile permise din fisierul JSON
        ///Daca fisierul JSON nu este asignat, se va afisa un mesaj de avertizare in consola Unity
        if (airportsJson != null)
        {
            AirportList airportList = JsonUtility.FromJson<AirportList>(airportsJson.text);
            foreach (var airport in airportList.Airports)
            {
                allowedAirports.Add(airport.iata);
            }
        }
        else
        {
            Debug.LogWarning("Airports JSON file not assigned.");
        }
    }

    ///In metoda Start() se adauga un listener pentru butonul de zbor aleatoriu, care va apela metoda GetRandomFlight() cand butonul este apasat
    ///De asemenea, se initializeaza variabilele currentRouteDeparture si currentRouteArrival cu valoarea ""

    private void Start()
    {
        randomFlightButton.onClick.AddListener(GetRandomFlight);
        currentRouteDeparture = "";
        currentRouteArrival = "";
    }

    ///In metoda Update() se apeleaza metoda UpdateRoute() cu indexul zborului curent 

    private void Update()
    {
        UpdateRoute(currentFlightIndex);
    }
    #endregion

    #region Flight Data Methods

    ///Metoda GetRandomFlight() este folosita pentru a face un request catre API-ul de zboruri si a obtine date despre zboruri.
    ///Request-ul este facut cu ajutorul clasei UnityWebRequest, care este folosita pentru a face request-uri HTTP in Unity
    ///StartCorountine() este folosit pentru a face request-ul in mod asincron, pentru a nu bloca thread-ul principal al aplicatiei
    public void GetRandomFlight()
    {
        string url = $"{ApiUrl}?access_key={ApiKey}&limit={flightCount}";
        StartCoroutine(FetchFlightData(url));
    }

    ///Functia FetchFlightData este functia de baza a requset-ului catre API-ul de zboruri. 
    ///Functia face un request GET catre URL-ul specificat si asteapta raspunsul de la server
    ///Daca raspunsul este de succes, se parseaza JSON-ul primit si se filtreaza zborurile in functie de aeroporturile permise
    ///Daca nu sunt gasite zboruri care sa corespunda aeroporturilor permise, se va afisa un mesaj de eroare in consola Unity
    ///Daca sunt gasite zboruri care corespund aeroporturilor permise, se va afisa numarul de zboruri gasite si se va afisa primul zbor gasit

    private IEnumerator FetchFlightData(string url)
    {
        Debug.Log($"Status: Getting {flightCount} flights...");
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {www.error}");
                yield break;
            }

            /// Parse the JSON response.
            flightData = JsonUtility.FromJson<FlightData>(www.downloadHandler.text);
            if (flightData == null || flightData.data.Length == 0)
            {
                Debug.LogError("No flight data found in the response.");
                yield break;
            }

            /// Filtram zborurile in functie de aeroporturile permise
            List<FlightInfo> filteredFlights = new List<FlightInfo>();
            foreach (var flight in flightData.data)
            {
                ///Verificam daca aeroporturile de plecare si destinatie sunt permise
                ///Daca sunt permise, adaugam zborul in lista de zboruri filtrate
                if (allowedAirports.Contains(flight.departure.iata) && allowedAirports.Contains(flight.arrival.iata))
                {
                    filteredFlights.Add(flight);
                }
            }

            Debug.Log($"Number of flights after filtering: {filteredFlights.Count}");
            if (filteredFlights.Count > 0)
            {
                flightData.data = filteredFlights.ToArray();
                Debug.Log($"Filtered to {flightData.data.Length} flights matching allowed airports.");
                DisplayFlightInfo(currentFlightIndex);
            }
            else
            {
                Debug.LogError("No flights found matching the specified airports.");
            }
        }
    }

    ///Functia UpdateRoute este folosita pentru a actualiza aeroporturile de plecare si destinatie ale zborului curent

    private void UpdateRoute(int index)
    {
        if (flightData != null && flightData.data.Length > 0)
        {
            FlightInfo flightInfo = flightData.data[index];
            currentRouteArrival = flightInfo.departure.iata;
            currentRouteDeparture = flightInfo.arrival.iata;
        }
    }

    ///Functia DisplayFlightInfo este folosita pentru a afisa informatii despre zborul cu indexul specificat
    ///Daca datele despre zbor sunt disponibile si indexul este valid, se afiseaza informatiile despre zbor in UI

    private void DisplayFlightInfo(int index)
    {
        if (flightData != null && flightData.data.Length > index)
        {
            FlightInfo flightInfo = flightData.data[index];

            /// Build the route string, using airport names if available; fallback to IATA codes.
            string route = !string.IsNullOrEmpty(flightInfo.departure.airport) && !string.IsNullOrEmpty(flightInfo.arrival.airport)
                ? $"{flightInfo.departure.airport} to {flightInfo.arrival.airport}"
                : $"{flightInfo.departure.iata} to {flightInfo.arrival.iata}";

            destinationText.text = $"Route: {route}";
            flightNumberText.text = $"Flight Number: {flightInfo.flight.number}";
            airlineText.text = $"Airline: {flightInfo.airline.name}";
            departureTimeText.text = $"Departure Time: {flightInfo.departure.scheduled}";
        }
    }
    #endregion

    #region UI Navigation Methods

    ///Acestea sunt functiile pentru navigarea intre zboruri in UI.
    ///Functia GetCurrentRoute() este folosita pentru a obtine aeroporturile de plecare si destinatie ale zborului curent

    public void NextFlight()
    {

        if (flightData == null || flightData.data.Length == 0) return;
        currentFlightIndex = (currentFlightIndex + 1) % flightData.data.Length;
        DisplayFlightInfo(currentFlightIndex);
    }

    public void PreviousFlight()
    {
        if (flightData == null || flightData.data.Length == 0) return;
        currentFlightIndex = (currentFlightIndex - 1 + flightData.data.Length) % flightData.data.Length;
        DisplayFlightInfo(currentFlightIndex);
    }

    public List<string> GetCurrentRoute()
    {
        return new List<string> { currentRouteDeparture, currentRouteArrival };
    }
    #endregion
}
