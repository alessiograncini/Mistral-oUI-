import os
from flask import Flask, request, jsonify
from flask_cors import CORS
import torch
from transformers import AutoModelForCausalLM, AutoTokenizer
from torch.cuda.amp import autocast  # Import for mixed precision

os.environ['PYTORCH_CUDA_ALLOC_CONF'] = 'max_split_size_mb:50'

app = Flask(__name__)
CORS(app)

def load_model():
    if torch.cuda.is_available():
        device = torch.device("cuda")
        torch.backends.cuda.matmul.allow_tf32 = True  # Enable TensorFloat-32 for matmul
        torch.backends.cudnn.allow_tf32 = True  # Enable TensorFloat-32 for convolutions
    else:
        device = torch.device("cpu")

    # try maybe 13b or 34b 
    model_name = "codellama/CodeLlama-7b-Python-hf" 
    model = AutoModelForCausalLM.from_pretrained(model_name, torch_dtype="auto").to(device)
    tokenizer = AutoTokenizer.from_pretrained(model_name)
    return model, tokenizer

model, tokenizer = load_model()

# Set a padding token if not already defined
if tokenizer.pad_token is None:
    tokenizer.pad_token = tokenizer.eos_token

@app.route("/api/code", methods=["POST"])
def code():
    user_message = request.json["message"]
    # Reduce max_length for truncation to limit input length
    max_length = 256  # Adjust based on your context needs
    inputs = tokenizer(user_message, return_tensors="pt", padding=True, truncation=True, max_length=max_length).to(model.device)
    with autocast():  # Use mixed precision for inference
        # Reduce max_new_tokens for faster response generation
        # Increase max_new_tokens for better answer 
        outputs = model.generate(inputs.input_ids, max_new_tokens=30, attention_mask=inputs.attention_mask)
    response_text = tokenizer.decode(outputs[0], skip_special_tokens=True)
    return jsonify({"response": response_text})

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)

