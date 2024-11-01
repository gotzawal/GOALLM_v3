# Chat GPT 4o

from openai import OpenAI
from key import api_key

class GPT4o:
    def __init__(self):
        self.model = "chatgpt-4o-latest"  # OpenAI model name
        #self.model = "gpt-4o-mini-2024-07-18" # 못알아먹음
        #self.model = "gpt-4-turbo-2024-04-09" # 너무느림
        self.api_key = api_key
        self.client = OpenAI(api_key=self.api_key)

    def get_response(self, arg):
        try:
            response = self.client.chat.completions.create(
                model=self.model,
                messages=arg["messages"],
                max_tokens=arg.get("max_tokens", 1024),
                temperature=arg.get("temperature", 1.0),
                top_p=arg.get("top_p", 1.0)
            )
            return {
                "success": True,
                "content": response.choices[0].message.content
            }
        except Exception as e:
            return {
                "success": False,
                "content": str(e)
            }
