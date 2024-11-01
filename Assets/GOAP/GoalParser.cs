using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq; // LINQ 사용을 위해 추가
using UnityEngine; // Debug.Log 사용을 위해 추가

public static class GoalParser
{
    /// <summary>
    /// 주어진 문장을 분석하여 GOAP 목표(Goal)로 변환합니다.
    /// </summary>
    /// <param name="sentence">분석할 문장</param>
    /// <param name="actions">사용 가능한 GOAP 액션 목록</param>
    /// <param name="worldState">현재 월드 상태</param>
    /// <param name="weight">목표의 중요도</param>
    /// <returns>생성된 Goal 객체 또는 null</returns>
    public static Goal ParseSentenceToGoal(string sentence, List<GOAPAction> actions, WorldState worldState, float weight = 1f)
    {
        if (string.IsNullOrWhiteSpace(sentence) || sentence.Trim().ToLower() == "none")
            return null;

        // 문장 전처리: 트림 및 마침표 제거
        sentence = sentence.Trim().TrimEnd('.');

        // 1. 관사("the", "a", "an") 제거
        string[] articles = { "the", "a", "an" };
        foreach (var article in articles)
        {
            // 단어 경계(\b)를 사용하여 정확한 단어만 제거
            sentence = Regex.Replace(sentence, $@"\b{article}\b\s*", "", RegexOptions.IgnoreCase);
        }

        // 2. "Use <item> at <location>" 패턴 검사
        var matchUseAt = Regex.Match(sentence, @"Use\s+(.+?)\s+(in|on|at)\s+(.+)", RegexOptions.IgnoreCase);
        if (matchUseAt.Success)
        {
            string itemName = matchUseAt.Groups[1].Value.Trim().ToLower();
            string preposition = matchUseAt.Groups[2].Value.Trim().ToLower();
            string location = matchUseAt.Groups[3].Value.Trim().ToLower();
            string goalName = $"Use_{itemName}_at_{location}";

            // 아이템이 'use' 기능을 가지고 있는지 확인
            if (!worldState.Items.ContainsKey(itemName) || !worldState.Items[itemName].UseEffects.Any())
            {
                Debug.LogError($"아이템 '{itemName}'은(는) 사용 기능이 없습니다.");
                return null;
            }

            Func<NPCState, WorldState, bool> condition = (npcState, ws) =>
            {
                // 'used_snack=true' 상태 확인
                return npcState.StateData.TryGetValue($"used_{itemName}", out var usedValue) && Convert.ToBoolean(usedValue);
            };

            // 사용 효과를 Goal의 Effect에 포함
            var effects = new Dictionary<string, object>(worldState.Items[itemName].UseEffects, StringComparer.OrdinalIgnoreCase);

            return new Goal(goalName, condition, weight, effects);

        }

        // 3. "Use <item>" 패턴 검사
        var matchUse = Regex.Match(sentence, @"Use\s+(.+)", RegexOptions.IgnoreCase);
        if (matchUse.Success)
        {
            string itemName = matchUse.Groups[1].Value.Trim().ToLower();
            string goalName = $"Use_{itemName}";

            // 아이템이 'use' 기능을 가지고 있는지 확인
            if (!worldState.Items.ContainsKey(itemName) || !worldState.Items[itemName].UseEffects.Any())
            {
                Debug.LogError($"아이템 '{itemName}'은(는) 사용 기능이 없습니다.");
                return null;
            }

            Func<NPCState, WorldState, bool> condition = (npcState, ws) =>
            {
                // 'used_snack=true' 상태 확인
                return npcState.StateData.TryGetValue($"used_{itemName}", out var usedValue) && Convert.ToBoolean(usedValue);
            };

            // 사용 효과를 Goal의 Effect에 포함
            var effects = new Dictionary<string, object>(worldState.Items[itemName].UseEffects, StringComparer.OrdinalIgnoreCase);

            return new Goal(goalName, condition, weight, effects);

        }

        // 4. "Do <action> in/on <location>" 패턴 검사
        var matchActionAtLocation = Regex.Match(sentence, @"Do\s+(.+?)\s+(in|on)\s+(.+)", RegexOptions.IgnoreCase);
        if (matchActionAtLocation.Success)
        {
            string actionName = matchActionAtLocation.Groups[1].Value.Trim().ToLower();
            string location = matchActionAtLocation.Groups[3].Value.Trim().ToLower();
            string goalName = $"Do_{actionName}_at_{location}";

            var action = actions.Find(a => a.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase));
            if (action != null)
            {
                Func<NPCState, WorldState, bool> condition = (npcState, ws) =>
                {
                    if (!npcState.LowerBody.ContainsKey("location") || !npcState.LowerBody["location"].ToString().Equals(location, StringComparison.OrdinalIgnoreCase))
                        return false;

                    foreach (var effect in action.Effects)
                    {
                        if (!CheckEffectApplied(effect.Key, effect.Value, npcState, ws))
                            return false;
                    }
                    return true;
                };

                return new Goal(goalName, condition, weight, new Dictionary<string, object>(action.Effects, StringComparer.OrdinalIgnoreCase));
            }
            else
            {
                Debug.LogError($"Action '{actionName}' not found.");
                return null;
            }
        }

        // 5. "Change <state> of <object> to <value>" 패턴 검사
        var matchChangeState = Regex.Match(sentence, @"Change\s+(.+?)\s+of\s+(.+?)\s+to\s+(.+)", RegexOptions.IgnoreCase);
        if (matchChangeState.Success)
        {
            string stateKey = matchChangeState.Groups[1].Value.Trim().ToLower();
            string objName = matchChangeState.Groups[2].Value.Trim().ToLower();
            string desiredValue = matchChangeState.Groups[3].Value.Trim().ToLower();
            string goalName = $"Change_{stateKey}_of_{objName}_to_{desiredValue}";

            Func<NPCState, WorldState, bool> condition = (npcState, ws) =>
            {
                return CheckState(objName, stateKey, desiredValue, npcState, ws);
            };

            return new Goal(goalName, condition, weight);
        }

        // 6. "Go to <location>" 패턴 검사
        var matchGoToLocation = Regex.Match(sentence, @"Go\s+to\s+(.+)", RegexOptions.IgnoreCase);
        if (matchGoToLocation.Success)
        {
            string location = matchGoToLocation.Groups[1].Value.Trim().ToLower();
            string goalName = $"Go_to_{location}";

            Func<NPCState, WorldState, bool> condition = (npcState, ws) =>
            {
                return npcState.LowerBody.ContainsKey("location") && npcState.LowerBody["location"].ToString().Equals(location, StringComparison.OrdinalIgnoreCase);
            };

            return new Goal(goalName, condition, weight);
        }

        // 7. "Pick up <item> at <place>" 패턴 검사
        var matchPickUpAt = Regex.Match(sentence, @"Pick\s+up\s+(.+?)\s+at\s+(.+)", RegexOptions.IgnoreCase);
        if (matchPickUpAt.Success)
        {
            string itemName = matchPickUpAt.Groups[1].Value.Trim().ToLower();
            string placeName = matchPickUpAt.Groups[2].Value.Trim().ToLower();
            string goalName = $"Pick_up_{itemName}_at_{placeName}";

            Func<NPCState, WorldState, bool> condition = (npcState, ws) =>
            {
                // Goal is achieved when NPC has the item (대소문자 무시)
                return npcState.Inventory.Any(i => i.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            };

            return new Goal(goalName, condition, weight);
        }

        // 8. "Pick up <item>" 패턴 검사
        var matchPickUp = Regex.Match(sentence, @"Pick\s+up\s+(.+)", RegexOptions.IgnoreCase);
        if (matchPickUp.Success)
        {
            string itemName = matchPickUp.Groups[1].Value.Trim().ToLower();
            string goalName = $"Pick_up_{itemName}";

            Func<NPCState, WorldState, bool> condition = (npcState, ws) =>
            {
                // Goal is achieved when NPC has the item (대소문자 무시)
                return npcState.Inventory.Any(i => i.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            };

            return new Goal(goalName, condition, weight);
        }

        // 9. "Drop <item> at <location>" 패턴 검사
        var matchDropItem = Regex.Match(sentence, @"Drop\s+(.+?)\s+at\s+(.+)", RegexOptions.IgnoreCase);
        if (matchDropItem.Success)
        {
            string itemName = matchDropItem.Groups[1].Value.Trim().ToLower();
            string location = matchDropItem.Groups[2].Value.Trim().ToLower();
            string goalName = $"Drop_{itemName}_at_{location}";

            Func<NPCState, WorldState, bool> condition = (npcState, ws) =>
            {
                if (!npcState.LowerBody.ContainsKey("location") || !npcState.LowerBody["location"].ToString().Equals(location, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!npcState.Inventory.Any(i => i.Equals(itemName, StringComparison.OrdinalIgnoreCase)))
                    return false;

                if (!ws.Places.ContainsKey(location))
                    return false;

                return ws.Places[location].Inventory.Any(i => i.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            };

            return new Goal(goalName, condition, weight);
        }

        // 10. "Do <action>" 패턴 검사
        var matchAction = Regex.Match(sentence, @"Do\s+(.+)", RegexOptions.IgnoreCase);
        if (matchAction.Success)
        {
            string actionName = matchAction.Groups[1].Value.Trim().ToLower();
            string goalName = $"Do_{actionName}";

            var action = actions.Find(a => a.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase));
            if (action != null)
            {
                Func<NPCState, WorldState, bool> condition = (npcState, ws) =>
                {
                    if (action.Effects.Count == 0)
                        return npcState.StateData.ContainsKey($"did_{actionName}") && Convert.ToBoolean(npcState.StateData[$"did_{actionName}"]);

                    foreach (var effect in action.Effects)
                    {
                        if (!CheckEffectApplied(effect.Key, effect.Value, npcState, ws))
                            return false;
                    }
                    return true;
                };

                return new Goal(goalName, condition, weight, new Dictionary<string, object>(action.Effects, StringComparer.OrdinalIgnoreCase));
            }
            else
            {
                Debug.LogError($"Action '{actionName}' not found.");
                return null;
            }
        }

        Debug.Log("Sentence parsing failed.");
        return null;
    }

    /// <summary>
    /// 주어진 효과가 NPC 상태에 적용되었는지 확인합니다.
    /// </summary>
    private static bool CheckEffectApplied(string effectKey, object effectValue, NPCState npcState, WorldState worldState)
    {
        bool result = false;
        if (npcState.UpperBody.TryGetValue(effectKey, out var upperValue))
        {
            result = upperValue.ToString().Equals(effectValue.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else if (npcState.LowerBody.TryGetValue(effectKey, out var lowerValue))
        {
            result = lowerValue.ToString().Equals(effectValue.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else if (npcState.Resources.TryGetValue(effectKey, out var resourceValue))
        {
            if (float.TryParse(effectValue.ToString(), out float floatValue))
            {
                result = Math.Abs(resourceValue - floatValue) < 0.001f;
            }
        }
        else if (effectKey.StartsWith("place_state:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = effectKey.Split(':');
            if (parts.Length == 3)
            {
                string placeName = parts[1].ToLower();
                string stateKey = parts[2].ToLower();
                if (worldState.Places.TryGetValue(placeName, out var place))
                {
                    result = place.State.TryGetValue(stateKey, out var placeStateValue) &&
                             placeStateValue.ToString().Equals(effectValue.ToString(), StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        else if (effectKey.StartsWith("item_state:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = effectKey.Split(':');
            if (parts.Length == 3)
            {
                string itemName = parts[1].ToLower();
                string stateKey = parts[2].ToLower();
                if (worldState.Items.TryGetValue(itemName, out var item))
                {
                    result = item.State.TryGetValue(stateKey, out var itemStateValue) &&
                             itemStateValue.ToString().Equals(effectValue.ToString(), StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        else if (effectKey.Equals("pose", StringComparison.OrdinalIgnoreCase))
        {
            result = npcState.LowerBody.TryGetValue("pose", out var poseValue) &&
                     poseValue.ToString().Equals(effectValue.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else if (effectKey.Equals("use_item", StringComparison.OrdinalIgnoreCase))
        {
            string usedItemKey = $"used_{effectValue.ToString().ToLower()}";
            result = npcState.StateData.TryGetValue(usedItemKey, out var usedValue) &&
                     Convert.ToBoolean(usedValue);
        }
        else if (effectKey.Equals("pickup_item", StringComparison.OrdinalIgnoreCase))
        {
            string itemName = effectValue.ToString().ToLower();
            result = npcState.Inventory.Any(i => i.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        }
        else if (effectKey.Equals("drop_item", StringComparison.OrdinalIgnoreCase))
        {
            string itemName = effectValue.ToString().ToLower();
            string currentLocation = npcState.LowerBody.TryGetValue("location", out var loc) ? loc.ToString().ToLower() : "";
            if (worldState.Places.TryGetValue(currentLocation, out var place))
            {
                result = place.Inventory.Any(i => i.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            }
        }
        else
        {
            result = npcState.StateData.TryGetValue(effectKey, out var stateValue) &&
                     stateValue.ToString().Equals(effectValue.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        Debug.Log($"Effect '{effectKey}': '{effectValue}' check result: {result}");
        return result;
    }

    /// <summary>
    /// 주어진 객체의 상태가 원하는 값으로 변경되었는지 확인합니다.
    /// </summary>
    private static bool CheckState(string objName, string stateKey, string desiredValue, NPCState npcState, WorldState worldState)
    {
        if (objName.Equals("NPC", StringComparison.OrdinalIgnoreCase))
        {
            if (npcState.UpperBody.TryGetValue(stateKey, out var upperValue))
            {
                return upperValue.ToString().Equals(desiredValue, StringComparison.OrdinalIgnoreCase);
            }
            else if (npcState.LowerBody.TryGetValue(stateKey, out var lowerValue))
            {
                return lowerValue.ToString().Equals(desiredValue, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return npcState.StateData.TryGetValue(stateKey, out var stateDataValue) &&
                       stateDataValue.ToString().Equals(desiredValue, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (worldState.Places.TryGetValue(objName, out var place))
        {
            return place.State.TryGetValue(stateKey, out var placeStateValue) &&
                   placeStateValue.ToString().Equals(desiredValue, StringComparison.OrdinalIgnoreCase);
        }
        else if (worldState.Items.TryGetValue(objName, out var item))
        {
            return item.State.TryGetValue(stateKey, out var itemStateValue) &&
                   itemStateValue.ToString().Equals(desiredValue, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            return false;
        }
    }
}

