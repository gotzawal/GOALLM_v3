import copy
import re
from typing import List, Dict
import json

from OpenAI_API import GPT4o
from default_prompt import start_prompt, world_status_prompt, user_status_prompt, char_status_prompt, response_format_prompt


class Prompting:
    def __init__(self, char_name: str, first_char_mes: str, first_user_mes: str, description: str):
        self.user_name = ""
        self.llm = GPT4o() #클로드로 바꿀 수 있음
        #self.llm = Claude35Sonnet()

        self.conversation_history: List[Dict[str, str]] = []
        self.max_context_tokens = 10000
        self.name = char_name
        self.first_char_mes = first_char_mes
        self.first_user_mes = first_user_mes
        self.description = description

        self.gesture = ""
        self.think = ""
        self.talk_goal = ""
        self.move_goal = ""
        self.item_goal = ""
        self.action_goal = ""

        self.debug = False

    def set_user_name(self, name: str, ):
        self.user_name = name
        self.update_prompts()  # 사용자 이름이 설정된 후 프롬프트 업데이트

    def update_prompts(self):
        self.world_status_prompt = self.process_script(world_status_prompt)
        self.user_status_prompt = self.process_script(user_status_prompt)
        self.char_status_prompt = self.process_script(char_status_prompt)
        self.response_format_prompt = self.process_script(response_format_prompt)
        self.first_char_mes = self.process_script(self.first_char_mes)
        self.first_user_mes = self.process_script(self.first_user_mes)
        self.description = self.process_script(self.description)

    def initialize_chat(self):
        self.add_message("user", self.process_script(self.first_user_mes))
        self.add_message("assistant", self.process_script(self.first_char_mes))

    def add_message(self, role: str, content: str):
        self.conversation_history.append({"role": role, "content": content})
        if self.debug:
            print(f"{'You' if role == 'user' else self.name}: {content}\n")
        self.manage_context_size()

    def manage_context_size(self):
        while sum(len(msg['content']) for msg in self.conversation_history) > self.max_context_tokens and len(self.conversation_history) > 1:
            self.conversation_history.pop(0)

    def send_message_to_api(self, user_message: str, npc_status: Dict, world_status: Dict, goap_status: Dict):
        request_data = self.prepare_api_request(user_message, npc_status, world_status, goap_status)

        if self.debug:
            print("\n--- API 요청 내용 ---")
            print(json.dumps(request_data, indent=2, ensure_ascii=False))
            print("----------------------\n")

        response = self.make_api_request(request_data)

        if response["success"]:
            self.process_api_response(response["content"])
        else:
            if self.debug:
                print(f"Error: {response['content']}")

    def prepare_api_request(self, user_message: str, npc_status: Dict, world_status: Dict, goap_status: Dict) -> Dict:
        user_status = self.user_status_prompt.format(user_speak=user_message)
        self.add_message("user", user_status)

        world_status = self.format_world_status(world_status)
        char_status = self.format_char_status(npc_status)
        response_format = self.format_goap_status(goap_status)

        api_conversation_history = copy.deepcopy(self.conversation_history)
        api_conversation_history[-1]['content'] = (
            world_status + "\n" +
            user_status + "\n" +
            char_status + "\n" +
            response_format
        )

        system_prompt = self.build_system_prompt()

        return {
            "messages": [{"role": "system", "content": system_prompt}] + api_conversation_history
        }

    def make_api_request(self, request_data: Dict) -> Dict:
        return self.llm.get_response(request_data)

    def process_api_response(self, api_response: str):
        self.add_message("assistant", self.process_script(api_response))

        gesture_match = re.search(r"Gesture: (.+?)(?=\n|$)", api_response, re.DOTALL)
        if gesture_match:
            self.gesture = gesture_match.group(1).strip()
        else:
            self.gesture = api_response.strip()

        think_match = re.search(r"Think: (.+?)(?=\n|$)", api_response, re.DOTALL)
        if think_match:
            self.think = think_match.group(1).strip()
        else:
            self.think = api_response.strip()

        talk_goal_match = re.search(r"Talk Goal: (.+?)(?=\n|$)", api_response, re.DOTALL)
        if talk_goal_match:
            self.talk_goal = talk_goal_match.group(1).strip()
        else:
            self.talk_goal = api_response.strip()

        move_goal_match = re.search(r"Move Goal: (.+?)(?=\n|$)", api_response, re.DOTALL)
        if talk_goal_match:
            self.move_goal = move_goal_match.group(1).strip()
        else:
            self.move_goal = api_response.strip()

        item_goal_match = re.search(r"Item Goal: (.+?)(?=\n|$)", api_response, re.DOTALL)
        if item_goal_match:
            self.item_goal = item_goal_match.group(1).strip()
        else:
            self.item_goal = api_response.strip()

        action_goal_match = re.search(r"Action Goal: (.+?)(?=\n|$)", api_response, re.DOTALL)
        if action_goal_match:
            self.action_goal = action_goal_match.group(1).strip()
        else:
            self.action_goal = api_response.strip()

    def format_world_status(self, world_status: Dict) -> str:
        return self.world_status_prompt.format(
            time = world_status["Time"],
            region = world_status["Region"],
            places = world_status["Places"],
            items = world_status["Items"]
        )

    def format_char_status(self, npc_status: Dict) -> str:
        return self.char_status_prompt.format(
            location = npc_status["Location"],
            inventory = npc_status["Inventory"],
            pose = npc_status["Pose"],
            holding = npc_status["Holding"],
            health = npc_status["Health"],
            mental = npc_status["Mental"]
        )

    def format_goap_status(self, goap_status: Dict) -> str:
        return self.response_format_prompt.format(
            available_actions = goap_status["Available Actions"],
            available_gestures = goap_status["Available Gestures"],
            item_effects = goap_status["Item Effects"],
            current_plan = goap_status["Current Plan"]
        )

    def build_system_prompt(self):
        return self.process_script(start_prompt.replace("{{char}}", self.name)
                                   .replace("{{user}}", self.user_name).format(description=self.process_script(self.description)))

    def process_script(self, input_text: str) -> str:
        return input_text.replace("{{char}}", self.name).replace("{{user}}", self.user_name)
