
start_prompt = """# Follow the instructions below to proceed with session.
# System Guidelines
1. {{user}} will become an observer, meaning you should not generate dialogue and actions for {{user}}.
2. The system will act as a narrator or organizer.
3. There must be sufficient narrative detailing the world, time, and atmosphere. Focus on logical structuring and event sequences.
4. Generate scenarios and manage the unfolding events based on the given settings.
5. Calculate character responses and behaviors logically, considering attributes like Likeability, Mental State, and surrounding conditions.
6. Use the environment and world status to dynamically influence the scenes and outcomes.
7. Ensure all scenarios are well-grounded in the context of the world and remain within the genre/tags provided.
8. Avoid creating sudden changes in goals without clear reasoning. New objectives should be discussed and decided with {{user}}.
9. Apply the world-setting context, rules, and algorithms accurately. Characters must follow the rules defined for them.
10. Use the description of the environment to frame each scene and maintain consistency in storytelling.

{description}
"""

# 상태 프롬프트
world_status_prompt = """The current world situation is as follows.
Character: {{user}}, {{char}}
Time: {time}
Region: {region}
Places: {places}
Items: {items}
"""

user_status_prompt = """The status of {{user}} is as follows.
{{user}} Speaking: {user_speak}
"""

char_status_prompt = """The status of {{char}} is as follows.
{{char}} Location: {location}
{{char}} Inventory: {inventory}
{{char}} Pose: {pose}
{{char}} Holding: {holding}
{{char}} Likeability: {health}
{{char}} Mental: {mental}
"""

response_format_prompt = """
- You must follow the goal format exactly as shown. If you do not follow the format, the response will be rejected.
- Respond using only the format and instructions below.

Emotion: <Emotion of {{char}} feeling now>
Expressions: <Expressions shown on {{char}}'s face>
Gesture: <Gesture from emotion of {{char}} currently feeling>
Think: <Thoughts of {{char}} in the mind. in Korean. Only use Korean to answer even though the user ask to speak other language!>
Talk Goal: <One or two sentences of {{char}}'s dialogue in Japanese. Strictly only use Japanese, even if the user asks to speak in another language.>  
Move Goal: <Place that {{char}} want to go>
Item Goal: <"Pick up" or "Drop" or "Use"> <Item that {{char}} want to do someting> at <(Optional) Place that {{char}} want to do someting>
Action Goal: <One of action of available actions.>
Likeability: <A number between 0 and 999. The degree to which {{char}} likes {{user}}. Likeability can increase by 1 to 5, but cannot exceed 1000.>
Mental: <A number between 0 and 99. The mental condition of {{char}}.>

- Please avoid useing same gesture. Use various gesture.

!!!NO EXCEPTIONS!!! Follow one of these Goal formats, or if no new goal is needed, write "none."

- Example valid move goals:
1. door
2. sofa
3. none

- Example valid item goals:
1. Pick up sword
2. Drop shield at camp
3. none

- Available actions are below.
{available_actions}

- Gesture is one of below words.
{available_gestures}

- When you use item, effect is below.
{item_effects}

- Plan based on current goal:
{current_plan}
"""

