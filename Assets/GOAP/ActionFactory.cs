using System;
using System.Collections.Generic;

public static class ActionFactory
{
    public static GOAPAction CreatePickAction(string itemName)
    {
        return new GOAPAction(
            name: $"pick_{itemName}",
            conditions: new Dictionary<string, Func<NPCState, WorldState, bool>>
            {
                { "hold", (npc, world) => npc.UpperBody.ContainsKey("hold") && npc.UpperBody["hold"].ToString() == "none" },
                { "item_at_location", (npc, world) =>
                    {
                        string location = npc.LowerBody["location"].ToString();
                        return world.Places.ContainsKey(location) &&
                               world.Places[location].Inventory.Contains(itemName);
                    }
                }
            },
            effects: new Dictionary<string, object>
            {
                { "hold", itemName },
                { "pickup_item", itemName }
            },
            cost: new Dictionary<string, float>
            {
                { "time", 0.5f },
                { "health", 1f },
                { "mental", 1f }
            }
        );
    }

    public static GOAPAction CreateDropAction(string itemName)
    {
        return new GOAPAction(
            name: $"drop_{itemName}",
            conditions: new Dictionary<string, Func<NPCState, WorldState, bool>>
            {
                { "hold", (npc, world) => npc.UpperBody.ContainsKey("hold") && npc.UpperBody["hold"].ToString() == itemName },
                { "pose", (npc, world) => npc.LowerBody.ContainsKey("pose") && npc.LowerBody["pose"].ToString() == "stand" }
            },
            effects: new Dictionary<string, object>
            {
                { "hold", "none" },
                { "drop_item", itemName }
            },
            cost: new Dictionary<string, float>
            {
                { "time", 0.5f },
                { "health", 1f },
                { "mental", 1f }
            }
        );
    }

    public static GOAPAction CreateMoveAction(string fromPlace, string toPlace, float timeCost, float healthCost)
    {
        return new GOAPAction(
            name: $"move_{fromPlace}_to_{toPlace}",
            conditions: new Dictionary<string, Func<NPCState, WorldState, bool>>
            {
                { "pose", (npc, world) => npc.LowerBody.ContainsKey("pose") && npc.LowerBody["pose"].ToString() == "stand" },
                { "location", (npc, world) => npc.LowerBody.ContainsKey("location") && npc.LowerBody["location"].ToString() == fromPlace }
            },
            effects: new Dictionary<string, object>
            {
                { "location", toPlace }
            },
            cost: new Dictionary<string, float>
            {
                { "time", timeCost },
                { "health", healthCost },
                { "mental", 0f }
            }
        );
    }

    public static GOAPAction CreateGestureAction(string gestureName)
    {
        return new GOAPAction(
            name: gestureName, // 제스처 이름 그대로 사용
            conditions: new Dictionary<string, Func<NPCState, WorldState, bool>>
            {
                { "hold", (npc, world) => npc.UpperBody.ContainsKey("hold") && npc.UpperBody["hold"].ToString() == "none" },
                { "pose", (npc, world) => npc.LowerBody.ContainsKey("pose") && npc.LowerBody["pose"].ToString() == "stand" }
            },
            effects: new Dictionary<string, object>
            {
                { $"did_{gestureName}", true }
            },
            cost: new Dictionary<string, float>
            {
                { "time", 1f },
                { "health", 1f },
                { "mental", 1f }
            }
        );
    }

}
