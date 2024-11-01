using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InteractionControl : MonoBehaviour
{
    [Header("References")]
    public GOAPExample goapExample; // Assign via Inspector
    public Transform handTransform; // Assign via Inspector

    [Header("Items")]
    public List<ItemObject> items; // Assign via Inspector

    // Dictionary for quick item lookup
    private Dictionary<string, ItemObject> itemDict;

    // Member Variable to Store Reference to NPCState
    private NPCState npcState;

    void Start()
    {
        // Validate references
        if (goapExample == null)
        {
            goapExample = FindObjectOfType<GOAPExample>();
            if (goapExample == null)
            {
                Debug.LogError("GOAPExample script not found in the scene. Please assign it in the InteractionControl.");
                return;
            }
            else
            {
                Debug.Log("GOAPExample reference found via FindObjectOfType.");
            }
        }
        else
        {
            Debug.Log("GOAPExample reference assigned via Inspector.");
        }

        if (handTransform == null)
        {
            Debug.LogError("Hand Transform not assigned in InteractionControl. Please assign it in the Inspector.");
            return;
        }

        // Initialize npcState from goapExample
        npcState = goapExample.NpcState;
        if (npcState == null)
        {
            Debug.LogError("NpcState in GOAPExample is null. Ensure that GOAPExample initializes NpcState correctly.");
            return;
        }
        else
        {
            Debug.Log("InteractionControl: npcState successfully obtained from GOAPExample.");
        }

        // Initialize the item dictionary for quick lookup
        InitializeItemDictionary();

        // Assign items to their initial Places
        AssignItemsToPlaces();
    }

    /// <summary>
    /// Initializes the item dictionary for efficient item lookup.
    /// </summary>
    private void InitializeItemDictionary()
    {
        itemDict = new Dictionary<string, ItemObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.itemName))
            {
                Debug.LogWarning("An item in InteractionControl has an empty itemName. Please assign a valid name.");
                continue;
            }

            if (item.itemGameObject == null)
            {
                Debug.LogWarning($"Item '{item.itemName}' does not have an assigned GameObject. Please assign it in the Inspector.");
                continue;
            }

            if (!itemDict.ContainsKey(item.itemName))
            {
                itemDict.Add(item.itemName, item);
            }
            else
            {
                Debug.LogWarning($"Duplicate itemName detected: '{item.itemName}'. Each item must have a unique name.");
            }
        }

        Debug.Log("InteractionControl: Item dictionary initialized successfully.");
    }

    /// <summary>
    /// Assigns each item to its initial Place based on their current assigned Place in the Inspector.
    /// If not assigned, attempts to find the closest Place.
    /// </summary>
    private void AssignItemsToPlaces()
    {
        foreach (var item in itemDict.Values)
        {
            if (item.currentPlace != null)
            {
                // Add the item to the Place's inventory
                if (!goapExample.Places.ContainsKey(item.currentPlace.Name))
                {
                    Debug.LogWarning($"Place '{item.currentPlace.Name}' for item '{item.itemName}' does not exist in GOAPExample.Places.");
                    continue;
                }

                Place place = goapExample.Places[item.currentPlace.Name];
                if (!place.Inventory.Contains(item.itemName))
                {
                    place.Inventory.Add(item.itemName);
                    Debug.Log($"InteractionControl: Assigned item '{item.itemName}' to Place '{place.Name}'.");
                }

                // Ensure the item's GameObject is positioned at the Place
                item.itemGameObject.transform.position = place.GameObject.transform.position;
                item.itemGameObject.transform.SetParent(null); // Ensure no parent
            }
            else
            {
                // Attempt to find the closest Place based on item's position
                Place closestPlace = FindClosestPlace(item.itemGameObject.transform.position);
                if (closestPlace != null)
                {
                    closestPlace.Inventory.Add(item.itemName);
                    item.currentPlace = closestPlace;
                    item.itemGameObject.transform.position = closestPlace.GameObject.transform.position;
                    item.itemGameObject.transform.SetParent(null); // Ensure no parent
                    Debug.Log($"InteractionControl: Automatically assigned item '{item.itemName}' to closest Place '{closestPlace.Name}'.");
                }
                else
                {
                    Debug.LogWarning($"Item '{item.itemName}' does not have an assigned initial Place and no closest Place was found.");
                }
            }
        }
    }

    /// <summary>
    /// Finds the closest Place to the given position.
    /// </summary>
    /// <param name="position">The position to find the closest Place to.</param>
    /// <returns>The closest Place object, or null if none found.</returns>
    private Place FindClosestPlace(Vector3 position)
    {
        float minDistance = Mathf.Infinity;
        Place closestPlace = null;

        foreach (var place in goapExample.Places.Values)
        {
            if (place.GameObject == null)
            {
                Debug.LogWarning($"Place '{place.Name}' does not have a GameObject assigned.");
                continue;
            }

            float distance = Vector3.Distance(position, place.GameObject.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPlace = place;
            }
        }

        return closestPlace;
    }

    /// <summary>
    /// Handles the NPC picking up an item.
    /// </summary>
    /// <param name="itemName">Name of the item to pick up.</param>
    /// <param name="npcState">Reference to the NPC's state.</param>
    /// <param name="worldState">Reference to the world state.</param>
    public void PickUpItem(string itemName, NPCState npcState, WorldState worldState)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("PickUpItem called with an empty itemName.");
            return;
        }

        if (!itemDict.TryGetValue(itemName, out ItemObject item))
        {
            Debug.LogError($"PickUpItem: Item '{itemName}' not found in InteractionControl.");
            return;
        }

        if (npcState.Inventory.Contains(itemName))
        {
            Debug.LogWarning($"PickUpItem: NPC already has the item '{itemName}' in inventory.");
            return;
        }

        if (item.currentPlace == null)
        {
            Debug.LogWarning($"PickUpItem: Item '{itemName}' is not assigned to any Place.");
            return;
        }

        // Remove the item from the Place's inventory
        Place place = item.currentPlace;
        if (place.Inventory.Contains(itemName))
        {
            place.Inventory.Remove(itemName);
            Debug.Log($"PickUpItem: Removed '{itemName}' from Place '{place.Name}'.");
        }
        else
        {
            Debug.LogWarning($"PickUpItem: Place '{place.Name}' does not contain item '{itemName}'.");
        }

        // Add the item to the NPC's inventory
        npcState.Inventory.Add(itemName);
        npcState.UpperBody["hold"] = itemName;
        Debug.Log($"PickUpItem: NPC picked up '{itemName}'. Added to inventory.");

        // Attach the item's GameObject to the NPC's hand
        item.itemGameObject.transform.SetParent(handTransform);
        item.itemGameObject.transform.localPosition = Vector3.zero;
        item.itemGameObject.transform.localRotation = Quaternion.identity;
        Debug.Log($"PickUpItem: '{itemName}' attached to NPC's hand.");
    }

    /// <summary>
    /// Handles the NPC dropping an item.
    /// </summary>
    /// <param name="itemName">Name of the item to drop.</param>
    /// <param name="npcState">Reference to the NPC's state.</param>
    /// <param name="worldState">Reference to the world state.</param>
    public void DropItem(string itemName, NPCState npcState, WorldState worldState)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("DropItem called with an empty itemName.");
            return;
        }

        if (!itemDict.TryGetValue(itemName, out ItemObject item))
        {
            Debug.LogError($"DropItem: Item '{itemName}' not found in InteractionControl.");
            return;
        }

        if (!npcState.Inventory.Contains(itemName))
        {
            Debug.LogWarning($"DropItem: NPC does not have the item '{itemName}' in inventory.");
            return;
        }

        // Remove the item from the NPC's inventory
        npcState.Inventory.Remove(itemName);
        npcState.UpperBody["hold"] = "none";
        Debug.Log($"DropItem: Removed '{itemName}' from NPC's inventory.");

        // Detach the item's GameObject from the hand
        item.itemGameObject.transform.SetParent(null);
        Debug.Log($"DropItem: '{itemName}' detached from NPC's hand.");

        // Determine the current Place based on NPC's location
        string npcLocation = npcState.LowerBody.ContainsKey("location") ? npcState.LowerBody["location"].ToString() : "unknown";
        if (!worldState.Places.TryGetValue(npcLocation, out Place currentPlace))
        {
            Debug.LogWarning($"DropItem: NPC's current location '{npcLocation}' does not exist in WorldState.Places.");
            return;
        }

        // Add the item to the current Place's inventory
        currentPlace.Inventory.Add(itemName);
        item.currentPlace = currentPlace;
        Debug.Log($"DropItem: '{itemName}' added to Place '{currentPlace.Name}' inventory.");

        // Position the item's GameObject at the Place's location
        item.itemGameObject.transform.position = currentPlace.GameObject.transform.position;
        Debug.Log($"DropItem: '{itemName}' positioned at Place '{currentPlace.Name}'.");
    }

    /// <summary>
    /// Handles the NPC using an item.
    /// </summary>
    /// <param name="itemName">Name of the item to use.</param>
    /// <param name="npcState">Reference to the NPC's state.</param>
    /// <param name="worldState">Reference to the world state.</param>
    public void UseItem(string itemName, NPCState npcState, WorldState worldState)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("UseItem called with an empty itemName.");
            return;
        }

        if (!itemDict.TryGetValue(itemName, out ItemObject item))
        {
            Debug.LogError($"UseItem: Item '{itemName}' not found in InteractionControl.");
            return;
        }

        if (!npcState.Inventory.Contains(itemName))
        {
            Debug.LogWarning($"UseItem: NPC does not have the item '{itemName}' in inventory.");
            return;
        }

        // Retrieve the IUsableItem component from the item's GameObject
        IUsableItem usableItem = item.itemGameObject.GetComponent<IUsableItem>();
        if (usableItem == null)
        {
            Debug.LogError($"UseItem: Item '{itemName}' does not have a usable script attached.");
            return;
        }

        // Execute the item's Use method
        usableItem.Use();
        Debug.Log($"UseItem: Executed Use() on '{itemName}'.");

        // Handle item effects based on item type
        // For example, updating NPC's resources
        // This assumes that item usage effects are handled elsewhere or within the item scripts

        // Optionally, if using an item consumes it, you might want to remove it from inventory
        // But according to your requirement, the item should not disappear
        // Therefore, we won't remove it from inventory here
    }
}

