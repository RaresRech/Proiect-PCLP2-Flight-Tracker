using UnityEngine;


// Clasa AirportMouseHandler este responsabila pentru a schimba culoarea aeroportului atunci cand mouse-ul intra sau iese din collider-ul aeroportului
// Clasa este atasata la fiecare aeroport si este folosita pentru a schimba culoarea aeroportului atunci cand mouse-ul intra sau iese din collider-ul aeroportului
// Logica colliderelor este gestionata de unity si este abstractizata in framework
public class AirportMouseHandler : MonoBehaviour
{
    private AirportPlacement airportPlacement;
    private string airportName;

    public void Initialize(AirportPlacement placement, string name)
    {
        airportPlacement = placement;
        airportName = name;
    }

    private void OnMouseEnter()
    {
        airportPlacement.OnAirportMouseEnter(airportName);
    }

    private void OnMouseExit()
    {
        airportPlacement.OnAirportMouseExit();
    }
}
