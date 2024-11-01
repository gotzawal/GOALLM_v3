using System;

[System.Serializable]
public class NPCStatus
{
    public string Location;
    public string Inventory;
    public string Pose;
    public string Holding;
    public string Health;
    public string Mental;

    public NPCStatus(string location, string inventory, string pose, string holding, string health, string mental)
    {
        Location = location;
        Inventory = inventory;
        Pose = pose;
        Holding = holding;
        Health = health;
        Mental = mental;
    }
}
