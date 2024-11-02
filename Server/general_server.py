import traceback
import logging
import uuid

from flask import Flask, request, jsonify
from flask_cors import CORS
from pyngrok import ngrok

from prompting import Prompting
from Character_Card import char_name, description, world_status, npc_status, goap_status, first_char_mes, first_user_mes

# Initialize logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger(__name__)

# Flask App Setup
app = Flask(__name__)
CORS(app)

# Client Manager to store state for each client
client_sessions = {}

def get_or_create_client(client_id):
    if client_id not in client_sessions:
        logger.debug(f"Creating new session for client {client_id}")
        ai_manager = Prompting(char_name, first_char_mes, first_user_mes, description)
        ai_manager.debug = False
        ai_manager.set_user_name("pluto")
        ai_manager.initialize_chat()
        client_sessions[client_id] = ai_manager
    return client_sessions[client_id]

@app.route('/api/game', methods=['POST'])
def handle_game_state():
    try:
        data = request.json
        logger.debug(f"Received data: {data}")

        client_id = data.get('client_id')
        if not client_id:
            client_id = str(uuid.uuid4())  # Generate new client ID if not provided
            logger.debug(f"Generated new client_id: {client_id}")

        ai_manager = get_or_create_client(client_id)

        user_message = data.get('userInput', '')
        logger.debug(f"User Message: {user_message}")

        npc_status = data.get('npc_status', {})
        logger.debug(f"NPC Status: {npc_status}")

        ai_manager.send_message_to_api(user_message, npc_status, world_status, goap_status)

        logger.debug(f"NPC Gesture: {ai_manager.gesture}")
        logger.debug(f"NPC Think: {ai_manager.think}")
        logger.debug(f"NPC Talk Goal: {ai_manager.talk_goal}")
        logger.debug(f"NPC Move Goal: {ai_manager.move_goal}")
        logger.debug(f"NPC Item Goal: {ai_manager.item_goal}")
        logger.debug(f"NPC Action Goal: {ai_manager.action_goal}")

        response = {
            'client_id': client_id,  # Include client_id in the response for future requests
            'Gesture': ai_manager.gesture,
            'Think': ai_manager.think,
            'TalkGoal': ai_manager.talk_goal,
            'MoveGoal': ai_manager.move_goal,
            'ItemGoal': ai_manager.item_goal,
            'ActionGoal' : ai_manager.action_goal
        }

        return jsonify(response)
    
    except Exception as e:
        logger.error(f"An error occurred: {str(e)}")
        logger.error(traceback.format_exc())
        return jsonify({
            'Gesture': "Error",
            'Think': f"[System: An error occurred: {str(e)}]",
            'Talk Goal': f"[System: An error occurred: {str(e)}]",
            'Move Goal': "Error",
            'Item Goal': "Error",
            'Action Goal': "Error"
        }), 500

@app.route('/api/game', methods=['GET'])
def get_game_state():
    return jsonify({
        'Gesture': "",
        'Think': "",
        'Talk Goal': "",
        'Move Goal': "",
        'Item Goal': "",
        'Action Goal': ""
    })

# Ngrok Setup and Server Run
public_url = ngrok.connect(5000)
print(f' * ngrok tunnel "{public_url}" -> "http://127.0.0.1:5000"')

if __name__ == '__main__':
    app.run(port=5000)
